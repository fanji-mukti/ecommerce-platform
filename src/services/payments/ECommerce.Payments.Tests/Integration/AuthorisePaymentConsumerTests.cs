using ECommerce.Payments.Events.V1;
using Xunit;

namespace ECommerce.Payments.Tests.Integration;

public class AuthorisePaymentConsumerTests : IAsyncLifetime
{
    private readonly AuthorisePaymentConsumerSteps _steps = new();

    public ValueTask InitializeAsync() => _steps.InitializeAsync();

    public ValueTask DisposeAsync() => _steps.DisposeAsync();

    [Fact]
    public async Task AuthorisePayment_WhenAmountEndsIn99_OutcomeIsFailedAndPaymentFailedPublished()
    {
        await _steps.Given_HarnessWithPostgresOutbox();

        var checkoutId = Guid.NewGuid();
        await _steps.When_AuthorisePaymentPublished(checkoutId, amount: 19.99m);

        await _steps.Then_ProcessedPaymentOutcomeIs(checkoutId, "Failed");
        Assert.Equal(1, await _steps.Then_PublishedCount<PaymentFailed>());
    }

    [Fact]
    public async Task AuthorisePayment_WhenAmountEndsIn99_PublishedPaymentFailedCarriesUserId()
    {
        await _steps.Given_HarnessWithPostgresOutbox();

        var checkoutId = Guid.NewGuid();
        await _steps.When_AuthorisePaymentPublished(checkoutId, amount: 19.99m, userId: "user-42");

        await _steps.Then_PublishedPaymentFailedHasUserId("user-42");
    }

    [Fact]
    public async Task AuthorisePayment_WhenSimulatePaymentFailureTrue_OutcomeIsFailedRegardlessOfAmount()
    {
        await _steps.Given_HarnessWithPostgresOutbox();

        var checkoutId = Guid.NewGuid();
        await _steps.When_AuthorisePaymentPublished(checkoutId, amount: 25.00m, simulateFail: true);

        await _steps.Then_ProcessedPaymentOutcomeIs(checkoutId, "Failed");
        Assert.Equal(1, await _steps.Then_PublishedCount<PaymentFailed>());
    }

    [Fact]
    public async Task AuthorisePayment_WhenRoundAmountAndNoSimulateFailure_OutcomeIsAuthorised()
    {
        await _steps.Given_HarnessWithPostgresOutbox();

        var checkoutId = Guid.NewGuid();
        await _steps.When_AuthorisePaymentPublished(checkoutId, amount: 25.00m, simulateFail: false);

        await _steps.Then_ProcessedPaymentOutcomeIs(checkoutId, "Authorised");
        Assert.Equal(1, await _steps.Then_PublishedCount<PaymentAuthorised>());
    }

    [Fact]
    public async Task AuthorisePayment_WhenDuplicateDeliveryWithDifferentMessageId_IsIdempotentByCheckoutId()
    {
        await _steps.Given_HarnessWithPostgresOutbox();

        var checkoutId = Guid.NewGuid();
        await _steps.When_AuthorisePaymentPublishedTwice(checkoutId, amount: 25.00m);

        // Exactly one ProcessedPayment row exists (PAY-03 business-key idempotency)...
        await _steps.Then_ExactlyOneProcessedPaymentRow(checkoutId);
        await _steps.Then_ProcessedPaymentOutcomeIs(checkoutId, "Authorised");

        // ...but the outcome event is published once per delivery — the second delivery
        // republays the STORED outcome rather than recomputing or re-inserting.
        Assert.Equal(2, await _steps.Then_PublishedCount<PaymentAuthorised>());
    }

    [Fact]
    public async Task RefundPayment_AfterAuthorisedPayment_OutcomeBecomesRefundedAndIdempotentOnReplay()
    {
        await _steps.Given_HarnessWithPostgresOutbox();

        var checkoutId = Guid.NewGuid();
        await _steps.Given_ProcessedPaymentExists(checkoutId, outcome: "Authorised", amount: 25.00m);

        await _steps.When_RefundPaymentPublished(checkoutId, amount: 25.00m, reason: "Fulfillment failed");

        await _steps.Then_ProcessedPaymentOutcomeIs(checkoutId, "Refunded");
        Assert.Equal(1, await _steps.Then_PublishedCount<PaymentRefunded>());

        // A second identical RefundPayment delivery is a no-op — still exactly one
        // PaymentRefunded published in total (T-04-10).
        await _steps.When_RefundPaymentPublished(checkoutId, amount: 25.00m, reason: "Fulfillment failed");

        await _steps.Then_ProcessedPaymentOutcomeIs(checkoutId, "Refunded");
        Assert.Equal(1, await _steps.Then_PublishedCount<PaymentRefunded>());
    }

    [Fact]
    public async Task AuthorisePayment_WhenRedeliveredAfterRefund_DoesNotRepublishAsPaymentFailed()
    {
        await _steps.Given_HarnessWithPostgresOutbox();

        var checkoutId = Guid.NewGuid();
        await _steps.Given_ProcessedPaymentExists(checkoutId, outcome: "Refunded", amount: 25.00m);

        // WR-02: a redelivered AuthorisePayment for an already-refunded payment must be a
        // no-op — never relabeled as PaymentFailed (which would smuggle a null Reason into a
        // non-nullable contract field) nor as PaymentAuthorised.
        await _steps.When_AuthorisePaymentPublished(checkoutId, amount: 25.00m);

        await _steps.Then_ProcessedPaymentOutcomeIs(checkoutId, "Refunded");
        Assert.Equal(0, await _steps.Then_PublishedCount<PaymentFailed>());
        Assert.Equal(0, await _steps.Then_PublishedCount<PaymentAuthorised>());
    }

    [Fact]
    public async Task RefundPayment_WhenPaymentWasNeverAuthorised_IsRejectedAsNoOp()
    {
        await _steps.Given_HarnessWithPostgresOutbox();

        var checkoutId = Guid.NewGuid();
        await _steps.Given_ProcessedPaymentExists(checkoutId, outcome: "Failed", amount: 25.00m);

        // WR-03: RefundPaymentConsumer must reject refunding a payment whose outcome is not
        // Authorised — defense-in-depth against refunding money that was never taken.
        await _steps.When_RefundPaymentPublished(checkoutId, amount: 25.00m, reason: "Fulfillment failed");

        await _steps.Then_ProcessedPaymentOutcomeIs(checkoutId, "Failed");
        Assert.Equal(0, await _steps.Then_PublishedCount<PaymentRefunded>());
    }
}
