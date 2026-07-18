using Xunit;

namespace ECommerce.Notifications.Tests.Integration;

public class CatalogSeededConsumerTests
{
    [Fact]
    public async Task CatalogSeededConsumer_WhenSameMessageIdDeliveredTwice_ProcessesExactlyOnce()
    {
        await using var steps = new CatalogSeededConsumerSteps();

        await steps.Given_HarnessWithInMemoryTransport();

        var messageId = Guid.NewGuid();
        await steps.When_SameMessagePublishedTwice(messageId);

        steps.Then_ConsumerBodyInvokedExactlyOnce();
    }
}
