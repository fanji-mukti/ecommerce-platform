using Xunit;

namespace ECommerce.Orders.Tests.Integration;

public class OrderReadModelInboxDeduplicationTests : IAsyncLifetime
{
    private readonly OrderReadModelInboxDeduplicationSteps _steps = new();

    public ValueTask InitializeAsync() => _steps.InitializeAsync();

    public ValueTask DisposeAsync() => _steps.DisposeAsync();

    [Fact]
    public async Task OrderReadModelProjector_WhenSameMessageIdDeliveredTwice_InboxAndReadModelEachContainExactlyOneRow()
    {
        await _steps.Given_HarnessWithPostgresInbox();

        var messageId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        await _steps.When_SameOrderCreatedMessagePublishedTwice(messageId, orderId);

        await _steps.Then_InboxStateHasExactlyOneRow();
        await _steps.Then_OrderReadModelHasExactlyOneRowFor(orderId);
    }
}
