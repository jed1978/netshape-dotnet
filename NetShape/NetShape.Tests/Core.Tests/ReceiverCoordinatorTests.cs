using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NetShape.Core;
using NetShape.Core.Models;
using NetShape.Core.Queues;

namespace NetShape.Tests.Core.Tests;

public class ReceiverCoordinatorTests
{
    [Fact]
    public async Task Should_Enqueue_Request_When_Received()
    {
        // Arrange
        var mockConnector = new Mock<IConnector<string, object>>();
        var mockRequestQueue = new Mock<IQueueService<GenericRequest<string>>>();
        var logger = NullLogger<ReceiverCoordinator<string>>.Instance;

        var coordinator = new ReceiverCoordinator<string>(
            mockConnector.Object,
            mockRequestQueue.Object,
            logger
        );

        var request = new GenericRequest<string>
        {
            RequestId = "1",
            ConnectionId = "conn1",
            Data = "Test Data"
        };
        
        // Act
        mockConnector.Raise(c => c.OnRequestReceived += null, request);

        // Assert
        mockRequestQueue.Verify(q => q.EnqueueAsync(request), Times.Once);
    }
}