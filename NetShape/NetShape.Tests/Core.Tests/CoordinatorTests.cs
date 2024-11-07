using Castle.Core.Logging;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NetShape.Core;
using NetShape.Core.Models;
using NetShape.Core.Processors;
using NetShape.Core.Queues;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using NullLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger;

namespace NetShape.Tests.Core.Tests;

public class CoordinatorTests
{
    [Fact]
    public async Task Coordinator_Should_Process_Request_And_Send_Response()
    {
        // Arrange
        var mockConnector = new Mock<IConnector<string, string>>();
        var mockQueueService = new Mock<IQueueService<GenericRequest<string>>>();
        var mockProcessor = new Mock<IRequestProcessor<string, string>>();
        var mockLogger = new Mock<ILogger>();
        
        var request = new GenericRequest<string> { RequestId = "1", Data = "Test Request" };
        var response = "Processed Response";
        var cts = new CancellationTokenSource();

        // 模擬enqueue/dequeue
        mockQueueService.SetupSequence(q => q.DequeueAsync())
            .ReturnsAsync(request)
            .ReturnsAsync((GenericRequest<string>)null);
        
        // 模擬處理器返回結果
        mockProcessor.Setup(p => p.ProcessAsync(request.Data)).ReturnsAsync(response);

        // 模擬連接器的事件觸發
        var coordinator = new Coordinator<string, string>(
            mockConnector.Object,
            mockQueueService.Object,
            mockProcessor.Object, 
            mockLogger.Object);

        // Act
        await coordinator.StartAsync(cts.Token);

        // 觸發請求接收事件
        mockConnector.Raise(c => c.OnRequestReceived += null, request);

        // 等待處理完成
        await Task.Delay(100);

        // Assert
        mockQueueService.Verify(q => q.EnqueueAsync(request), Times.Once);
        mockProcessor.Verify(p => p.ProcessAsync(request.Data), Times.Once);
        mockConnector.Verify(c => c.SendResponseAsync(It.IsAny<string>(), It.IsAny<IResponse<string>>()), Times.Once);
    }
    
    [Fact]
    public void Constructor_Should_Throw_ArgumentNullException_When_Processor_Is_Null()
    {
        // Arrange
        var mockConnector = new Mock<IConnector<string, string>>();
        var mockQueueService = new Mock<IQueueService<GenericRequest<string>>>();
        IRequestProcessor<string, string> nullProcessor = null;
        var mocklogger = new Mock<ILogger>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new Coordinator<string, string>(
                    mockConnector.Object,
                    mockQueueService.Object,
                    nullProcessor, 
                    mocklogger.Object)
        );

        exception.Should().BeOfType<ArgumentNullException>()
            .Which.ParamName.Should().Be("processor");
    }
    
    [Fact]
    public void Coordinator_Constructor_Should_Throw_ArgumentNullException_When_Connector_Is_Null()
    {
        // Arrange
        IConnector<string, string> nullConnector = null;
        var mockQueueService = new Mock<IQueueService<GenericRequest<string>>>();
        var mockProcessor = new Mock<IRequestProcessor<string, string>>();
        var mocklogger = new Mock<ILogger>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new Coordinator<string, string>(
            nullConnector,
            mockQueueService.Object,
            mockProcessor.Object, 
            mocklogger.Object));
        
        exception.Should().BeOfType<ArgumentNullException>()
            .Which.ParamName.Should().Be("connector");
    }

    [Fact]
    public void Coordinator_Constructor_Should_Throw_ArgumentNullException_When_QueueService_Is_Null()
    {
        // Arrange
        var mockConnector = new Mock<IConnector<string, string>>();
        IQueueService<GenericRequest<string>> nullQueueService = null;
        var mockProcessor = new Mock<IRequestProcessor<string, string>>();
        var mockLogger = new Mock<ILogger>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new Coordinator<string, string>(
            mockConnector.Object,
            nullQueueService,
            mockProcessor.Object, 
            mockLogger.Object));
        exception.Should().BeOfType<ArgumentNullException>()
            .Which.ParamName.Should().Be("queue");
    }
    
    [Fact]
    public async Task Coordinator_Should_Stop_Gracefully_When_CancellationToken_Is_Cancelled()
    {
        // Arrange
        var mockConnector = new Mock<IConnector<string, string>>();
        var mockQueueService = new Mock<IQueueService<GenericRequest<string>>>();
        var mockProcessor = new Mock<IRequestProcessor<string, string>>();
        var mockLogger = new Mock<ILogger>();

        var request = new GenericRequest<string> { RequestId = "1", Data = "Test Request", ConnectionId = "Connection1" };
        var response = "Test Response";

        // Setup the queue to return the request on the first dequeue, and then null on the second dequeue
        mockQueueService.SetupSequence(q => q.DequeueAsync())
            .ReturnsAsync(request)
            .Returns(async () =>
            {
                // Simulate the cancellation operation after processing the first request
                await Task.Delay(100); // Simulate delay
                return null;
            });
            
        // Simulate ProcessAsync behavior
        mockProcessor.Setup(p => p.ProcessAsync(request.Data)).ReturnsAsync(response);

        // Create CancellationTokenSource
        var cts = new CancellationTokenSource();

        // Create Coordinator instance
        var coordinator = new Coordinator<string, string>(
            mockConnector.Object,
            mockQueueService.Object,
            mockProcessor.Object,
            mockLogger.Object
        );

        // Act
        await coordinator.StartAsync(cts.Token);

        // Simulate received the request
        await mockConnector.RaiseAsync(c => c.OnRequestReceived += null, request);

        // Trigger the cancellation operation
        cts.Cancel();

        // Wait for gracefully stop
        await coordinator.StopAsync();
        
        // Assert
        mockQueueService.Verify(q => q.EnqueueAsync(request), Times.Once);
        mockProcessor.Verify(p => p.ProcessAsync(request.Data), Times.Once);
        mockConnector.Verify(c => c.SendResponseAsync(request.ConnectionId, It.IsAny<IResponse<string>>()), Times.Once);
    }
}