using Xunit;

namespace ECommerce.Notifications.Tests.Integration;

public class CatalogSeededInboxDeduplicationTests : IAsyncLifetime
{
    private readonly CatalogSeededInboxDeduplicationSteps _steps = new();

    public ValueTask InitializeAsync() => _steps.InitializeAsync();

    public ValueTask DisposeAsync() => _steps.DisposeAsync();

    [Fact]
    public async Task CatalogSeededConsumer_WhenSameMessageIdDeliveredTwice_InboxStateContainsExactlyOneRow()
    {
        await _steps.Given_HarnessWithPostgresInbox();

        var messageId = Guid.NewGuid();
        await _steps.When_SameMessagePublishedTwice(messageId);

        await _steps.Then_InboxStateHasExactlyOneRow();
    }
}
