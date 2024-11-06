using NetShape.Core.Queues;

namespace NetShape.Tests.Queue.Tests;

public class QueueServiceTests
{
    [Fact]
    public async Task QueueService_Should_Enqueue_And_Dequeue_Items()
    {
        // Arrange
        var queueService = new InMemoryQueueService<string>();
        var item = "Test Item";

        // Act
        await queueService.EnqueueAsync(item);
        var dequeuedItem = await queueService.DequeueAsync();

        // Assert
        Assert.Equal(item, dequeuedItem);
    }
}