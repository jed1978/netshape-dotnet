using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NetShape.Core;
using NetShape.Core.Models;
using NetShape.Core.Processors;
using NetShape.Core.Queues;

namespace NetShape.Tests.Core.Tests;

public class ReceiverCoordinatorTests
{
    [Fact]
    public async Task Should_Enqueue_Request_When_Received()
    {
        // Arrange
        var mockConnector = new Mock<IConnector<string, string>>();
        var mockRequestQueue = new Mock<IQueueService<GenericRequest<string>>>();
        var mockResponseQueue = new Mock<IQueueService<GenericResponse<string>>>();
        var logger = NullLogger<ReceiverCoordinator<string, string>>.Instance;
        var mockReceiverRequest = new Mock<IRequestReceiver<string>>();
        var coordinator = new ReceiverCoordinator<string, string>(
            mockConnector.Object,
            mockRequestQueue.Object,
            mockResponseQueue.Object,
            logger,
            mockReceiverRequest.Object
        );

        var request = new GenericRequest<string>
        {
            RequestId = "1",
            ConnectionId = "conn1",
            Data = "Test Data"
        };
        
        // Act
        await mockReceiverRequest.RaiseAsync(c => c.OnRequestReceived += null, request);

        // Assert
        mockRequestQueue.Verify(q => q.EnqueueAsync(request), Times.Once);
    }
    
    [Fact]
    public async Task Should_Send_Response_To_Client_When_Response_Is_Dequeued()
    {
        // Arrange
        var mockConnector = new Mock<IConnector<string, string>>();
        var mockRequestQueue = new Mock<IQueueService<GenericRequest<string>>>();
        var mockResponseQueue = new Mock<IQueueService<GenericResponse<string>>>();
        var logger = NullLogger<ReceiverCoordinator<string, string>>.Instance;
        var mockReceiverRequest = new Mock<IRequestReceiver<string>>();
        
        var response = new GenericResponse<string>
        {
            RequestId = "1",
            ConnectionId = "conn1",
            Data = "Test Response"
        };
        
        mockResponseQueue.SetupSequence(q => q.DequeueAsync())
            .ReturnsAsync(response)
            .ReturnsAsync((GenericResponse<string>)null);

        var coordinator = new ReceiverCoordinator<string, string>(
            mockConnector.Object,
            mockRequestQueue.Object,
            mockResponseQueue.Object,
            logger,
            mockReceiverRequest.Object
        );

        var cts = new CancellationTokenSource();
        cts.CancelAfter(500); // Cancel after 500ms to exit the loop

        // Act
        await coordinator.StartAsync(cts.Token);
        await Task.Delay(600); // Wait for the processing loop
        await coordinator.StopAsync(cts.Token);

        // Assert
        mockConnector.Verify(c => c.SendResponseAsync(response.ConnectionId, response), Times.Once);
    }
    
    [Fact]
    public async Task Should_Handle_Client_Disconnection_When_Sending_Response()
    {
        // Arrange
        var mockConnector = new Mock<IConnector<string, string>>();
        var mockRequestQueue = new Mock<IQueueService<GenericRequest<string>>>();
        var mockResponseQueue = new Mock<IQueueService<GenericResponse<string>>>();
        var mockLogger = new Mock<ILogger<ReceiverCoordinator<string, string>>>();
        var mockReceiverRequest = new Mock<IRequestReceiver<string>>();
        
        var response = new GenericResponse<string>
        {
            RequestId = "1",
            ConnectionId = "conn1",
            Data = "Test Response"
        };

        mockResponseQueue.SetupSequence(q => q.DequeueAsync())
            .ReturnsAsync(response)
            .ReturnsAsync((GenericResponse<string>)null);

        mockConnector.Setup(c => c.SendResponseAsync(response.ConnectionId, response)).ThrowsAsync(new System.Exception("Client disconnected"));

        var coordinator = new ReceiverCoordinator<string, string>(
            mockConnector.Object,
            mockRequestQueue.Object,
            mockResponseQueue.Object,
            mockLogger.Object,
            mockReceiverRequest.Object
        );

        var cts = new CancellationTokenSource();
        cts.CancelAfter(500);

        // Act
        await coordinator.StartAsync(cts.Token);
        await Task.Delay(600);
        await coordinator.StopAsync(cts.Token);

        // Assert
        mockConnector.Verify(c => c.SendResponseAsync(response.ConnectionId, response), Times.Once);
        mockLogger.Verify(l => l.Log(LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, _) => 
                v.ToString().Contains($"An error occurred while sending the response, RequestId: {response.RequestId}")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()
        ));
    }
    
    [Fact]
    public void Constructor_Should_Throw_ArgumentNullException_When_Connector_Is_Null()
    {
        // Arrange
        IConnector<string, string> nullConnector = null;
        var mockRequestQueue = new Mock<IQueueService<GenericRequest<string>>>();
        var mockResponseQueue = new Mock<IQueueService<GenericResponse<string>>>();
        var mockProcessor = new Mock<IRequestProcessor<string, string>>();
        var logger = NullLogger<ReceiverCoordinator<string, string>>.Instance;
        var requestReceiver = new Mock<IRequestReceiver<string>>();
        
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new ReceiverCoordinator<string, string>(
            nullConnector,
            mockRequestQueue.Object,
            mockResponseQueue.Object,
            logger,
            requestReceiver.Object
        ));
        Assert.Equal("connector", exception.ParamName);
    }

    [Fact]
    public void Constructor_Should_Throw_ArgumentNullException_When_RequestQueue_Is_Null()
    {
        // Arrange
        var mockConnector = new Mock<IConnector<string, string>>();
        IQueueService<GenericRequest<string>> nullRequestQueue = null;
        var mockResponseQueue = new Mock<IQueueService<GenericResponse<string>>>();
        var mockProcessor = new Mock<IRequestProcessor<string, string>>();
        var logger = NullLogger<ReceiverCoordinator<string, string>>.Instance;
        var mockRequestReceiver = new Mock<IRequestReceiver<string>>();
        
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new ReceiverCoordinator<string, string>(
            mockConnector.Object,
            nullRequestQueue,
            mockResponseQueue.Object,
            logger,
            mockRequestReceiver.Object
        ));
        Assert.Equal("requestQueue", exception.ParamName);
    }

    [Fact]
    public void Constructor_Should_Throw_ArgumentNullException_When_ResponseQueue_Is_Null()
    {
        // Arrange
        var mockConnector = new Mock<IConnector<string, string>>();
        var mockRequestQueue = new Mock<IQueueService<GenericRequest<string>>>();
        IQueueService<GenericResponse<string>> nullResponseQueue = null;
        var mockProcessor = new Mock<IRequestProcessor<string, string>>();
        var logger = NullLogger<ReceiverCoordinator<string, string>>.Instance;
        var mockRequestReceiver = new Mock<IRequestReceiver<string>>();
        

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new ReceiverCoordinator<string, string>(
            mockConnector.Object,
            mockRequestQueue.Object,
            nullResponseQueue,
            logger,
            mockRequestReceiver.Object
        ));
        Assert.Equal("responseQueue", exception.ParamName);
    }

    [Fact]
    public void Constructor_Should_Throw_ArgumentNullException_When_RequestReceiver_Is_Null()
    {
        // Arrange
        var mockConnector = new Mock<IConnector<string, string>>();
        var mockRequestQueue = new Mock<IQueueService<GenericRequest<string>>>();
        var mockResponseQueue = new Mock<IQueueService<GenericResponse<string>>>();
        var logger = NullLogger<ReceiverCoordinator<string, string>>.Instance;
        IRequestReceiver<string> nullRequestReceiver = null;
        
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new ReceiverCoordinator<string, string>(
            mockConnector.Object,
            mockRequestQueue.Object,
            mockResponseQueue.Object,
            logger,
            nullRequestReceiver
        ));
        Assert.Equal("requestReceiver", exception.ParamName);
    }

    [Fact]
    public void Constructor_Should_Throw_ArgumentNullException_When_Logger_Is_Null()
    {
        // Arrange
        var mockConnector = new Mock<IConnector<string, string>>();
        var mockRequestQueue = new Mock<IQueueService<GenericRequest<string>>>();
        var mockResponseQueue = new Mock<IQueueService<GenericResponse<string>>>();
        var mockProcessor = new Mock<IRequestProcessor<string, string>>();
        ILogger<ReceiverCoordinator<string, string>> nullLogger = null;
        var mockRequestReceiver = new Mock<IRequestReceiver<string>>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new ReceiverCoordinator<string, string>(
            mockConnector.Object,
            mockRequestQueue.Object,
            mockResponseQueue.Object,
            nullLogger,
            mockRequestReceiver.Object
        ));
        Assert.Equal("logger", exception.ParamName);
    }
}