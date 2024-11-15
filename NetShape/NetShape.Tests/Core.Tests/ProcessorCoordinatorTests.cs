using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NetShape.Core.Models;
using NetShape.Core.Processors;
using NetShape.Core.Queues;

namespace NetShape.Tests.Core.Tests;

public class ProcessorCoordinatorTests
{
    [Fact]
    public async Task Should_Process_Request_And_Enqueue_Response()
    {
        // Arrange
        var mockRequestQueue = new Mock<IQueueService<GenericRequest<string>>>();
        var mockResponseQueue = new Mock<IQueueService<GenericResponse<string>>>();
        var mockProcessor = new Mock<IRequestProcessor<string, string>>();
        var logger = NullLogger<ProcessorCoordinator<string, string>>.Instance;

        var request = new GenericRequest<string>
        {
            RequestId = "1",
            ConnectionId = "conn1",
            Data = "Test Data"
        };

        var responseData = "Processed Data";

        mockRequestQueue.SetupSequence(q => q.DequeueAsync())
            .ReturnsAsync(request)
            .ReturnsAsync((GenericRequest<string>)null);

        mockProcessor.Setup(p => p.ProcessAsync(request.Data)).ReturnsAsync(responseData);

        var coordinator = new ProcessorCoordinator<string, string>(
            mockRequestQueue.Object,
            mockResponseQueue.Object,
            mockProcessor.Object,
            logger
        );

        var cts = new CancellationTokenSource();
        cts.CancelAfter(500); // Cancel after 500ms to exit the loop

        // Act
        await coordinator.StartAsync(cts.Token);
        await Task.Delay(600); // Wait for the processing loop
        await coordinator.StopAsync(cts.Token);

        // Assert
        mockProcessor.Verify(p => p.ProcessAsync(request.Data), Times.Once);
        mockResponseQueue.Verify(q => q.EnqueueAsync(It.Is<GenericResponse<string>>(r =>
            r.RequestId == request.RequestId &&
            r.ConnectionId == request.ConnectionId &&
            r.Data == responseData
        )), Times.Once);
    }
    
    [Fact]
    public async Task Should_Log_Error_When_Exception_Occurs_During_Processing()
    {
        // Arrange
        var mockRequestQueue = new Mock<IQueueService<GenericRequest<string>>>();
        var mockResponseQueue = new Mock<IQueueService<GenericResponse<string>>>();
        var mockProcessor = new Mock<IRequestProcessor<string, string>>();
        var mockLogger = new Mock<ILogger<ProcessorCoordinator<string, string>>>();

        var request = new GenericRequest<string>
        {
            RequestId = "1",
            ConnectionId = "conn1",
            Data = "Test Data"
        };
        
        mockRequestQueue.SetupSequence(q => q.DequeueAsync())
            .ReturnsAsync(request)
            .ReturnsAsync((GenericRequest<string>)null);
        
        var exception = new System.Exception("Processing error");
        mockProcessor.Setup(p => p.ProcessAsync(request.Data)).ThrowsAsync(exception);
        
        var coordinator = new ProcessorCoordinator<string, string>(
            mockRequestQueue.Object,
            mockResponseQueue.Object,
            mockProcessor.Object,
            mockLogger.Object
        );

        var cts = new CancellationTokenSource();
        cts.CancelAfter(500);

        // Act
        await coordinator.StartAsync(cts.Token);
        await Task.Delay(600);
        await coordinator.StopAsync(cts.Token);

        // Assert
        mockProcessor.Verify(p => p.ProcessAsync(request.Data), Times.Once);
        mockLogger.Verify(l => l.Log(LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, _) => 
                v.ToString().Contains($"An error occurred while processing the request, RequestId: {request.RequestId}")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()
        ));
        mockResponseQueue.Verify(q => q.EnqueueAsync(It.IsAny<GenericResponse<string>>()), Times.Never);
    }
}