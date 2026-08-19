using Xunit;

namespace ECommerce.Fulfillment.Tests.Integration;

public class OrderPaidInboxDeduplicationTests : IAsyncLifetime
{
    private readonly OrderPaidInboxDeduplicationSteps _steps = new();

    public ValueTask InitializeAsync() => _steps.InitializeAsync();

    public ValueTask DisposeAsync() => _steps.DisposeAsync();

    [Fact]
    public async Task OrderPaidConsumer_WhenSameMessageIdDeliveredTwice_InboxStateContainsExactlyOneRow()
    {
        await _steps.Given_HarnessWithPostgresInboxAndOutbox();

        var messageId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        const string userId = "user-1";
        await _steps.When_SameOrderStatusChangedPublishedTwice(messageId, orderId, userId);

        await _steps.Then_InboxStateHasExactlyOneRow();
    }
}
