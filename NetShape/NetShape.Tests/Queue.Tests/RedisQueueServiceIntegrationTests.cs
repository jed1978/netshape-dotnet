using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using NetShape.Models;
using NetShape.Queues;
using StackExchange.Redis;

namespace NetShape.Tests.Queue.Tests;

public class RedisQueueServiceIntegrationTests : IAsyncLifetime
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly IDatabase _database;
    private readonly RedisQueueService<GenericRequest<string>> _requestQueueService;
    private readonly RedisQueueService<GenericResponse<string>> _responseQueueService;
    private const string RequestQueueKey = "test-requests";
    private const string ResponseQueueKey = "test-responses";

    public RedisQueueServiceIntegrationTests()
    {
        // Connect to your Redis service.
        _connectionMultiplexer = ConnectionMultiplexer.Connect("localhost:6379");
        _database = _connectionMultiplexer.GetDatabase();
        
        var mockRequestQueueLogger = new Mock<ILogger<RedisQueueService<GenericRequest<string>>>>();
        var mockResponseQueueLogger = new Mock<ILogger<RedisQueueService<GenericResponse<string>>>>();
        
        // Initialize RedisQueueService
        _requestQueueService = new RedisQueueService<GenericRequest<string>>(
            _connectionMultiplexer, RequestQueueKey, mockRequestQueueLogger.Object);
        _responseQueueService = new RedisQueueService<GenericResponse<string>>(
            _connectionMultiplexer, ResponseQueueKey, mockResponseQueueLogger.Object);
    }

    public async Task InitializeAsync()
    {
        // Clear the queue to ensure the test environment is clean
        await _database.KeyDeleteAsync(RequestQueueKey);
        await _database.KeyDeleteAsync(ResponseQueueKey);
    }

    public async Task DisposeAsync()
    {
        // Clear the queue to avoid data interference between tests.
        await _database.KeyDeleteAsync(RequestQueueKey);
        await _database.KeyDeleteAsync(ResponseQueueKey);
        _connectionMultiplexer.Dispose();
    }

    [Fact]
    public async Task EnqueueAsync_Should_Add_Item_To_Request_Queue()
    {
        // Arrange
        var item = new GenericRequest<string>
        {
            RequestId = "req-100",
            ConnectionId = "conn-100",
            Data = "Test Request - 100"
        };

        // Act
        var result = await _requestQueueService.EnqueueAsync(item);

        // Assert
        Assert.Equal(1, result); // ListLeftPushAsync returns the new length
    }

    [Fact]
    public async Task DequeueAsync_Should_Return_Item_From_Request_Queue()
    {
        // Arrange
        var item = new GenericRequest<string>
        {
            RequestId = "req-101",
            ConnectionId = "conn-101",
            Data = "Test Request - 101"
        };
        await _requestQueueService.EnqueueAsync(item);

        // Act
        var result = await _requestQueueService.DequeueAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(item.RequestId, result.RequestId);
        Assert.Equal(item.ConnectionId, result.ConnectionId);
        Assert.Equal(item.Data, result.Data);
    }

    [Fact]
    public async Task DequeueAsync_Should_Return_Null_When_Queue_Is_Empty()
    {
        // Arrange
        await _database.KeyDeleteAsync(RequestQueueKey);

        // Act
        var result = await _requestQueueService.DequeueAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task EnqueueAsync_Should_Add_Item_To_Response_Queue()
    {
        // Arrange
        var response = new GenericResponse<string>
        {
            RequestId = "resp-200",
            ConnectionId = "conn-200",
            Data = "Test Response - 200"
        };

        // Act
        var result = await _responseQueueService.EnqueueAsync(response);

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task DequeueAsync_Should_Return_Item_From_Response_Queue()
    {
        // Arrange
        var response = new GenericResponse<string>
        {
            RequestId = "resp-201",
            ConnectionId = "conn-201",
            Data = "Test Response - 201"
        };
        await _responseQueueService.EnqueueAsync(response);

        // Act
        var result = await _responseQueueService.DequeueAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(response.RequestId, result.RequestId);
        Assert.Equal(response.ConnectionId, result.ConnectionId);
        Assert.Equal(response.Data, result.Data);
    }

    [Fact]
    public async Task DequeueAsync_Should_Return_Null_From_Response_Queue_When_Empty()
    {
        // Arrange
        await _database.KeyDeleteAsync(ResponseQueueKey);

        // Act
        var result = await _responseQueueService.DequeueAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task EnqueueAsync_Should_Throw_Exception_When_Item_Is_Null()
    {
        // Arrange
        GenericRequest<string>? item = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _requestQueueService.EnqueueAsync(item!));
    }
}