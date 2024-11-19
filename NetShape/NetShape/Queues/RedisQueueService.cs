using System.Text.Json;
using Microsoft.Extensions.Logging;
using NetShape.Core;
using StackExchange.Redis;

namespace NetShape.Queues;

/// <summary>
/// A queue implemented using Redis.
/// </summary>
/// <typeparam name="T">The type of items in the queue.</typeparam>
public class RedisQueueService<T> : IQueueService<T>
{
    private readonly IConnectionMultiplexer _redis;
    private readonly string _queueName;
    private readonly ILogger<RedisQueueService<T>> _logger;

    public RedisQueueService(IConnectionMultiplexer redis, string queueName, ILogger<RedisQueueService<T>> logger)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _queueName = queueName ?? throw new ArgumentNullException(nameof(queueName));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }


    /// <summary>
    /// Asynchronously adds the specified item to the end of the queue.
    /// </summary>
    /// <param name="item">The item to be added to the queue.</param>
    /// <returns>The Queue length</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public async Task<long> EnqueueAsync(T item)
    {
        if (item == null)
        {
            _logger.LogWarning("Null items cannot be added to the queue.");
            throw new ArgumentNullException(nameof(item));
        }

        try
        {
            var db = _redis.GetDatabase();
            var message = JsonSerializer.Serialize(item);
            var queueLength = await db.ListRightPushAsync(_queueName, message);
            _logger.LogInformation($"The item has been added to the queue {_queueName}.");
            return queueLength;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"An error occurred during enqueue. Queue name: {_queueName}.");
            throw;
        }
    }

    /// <summary>
    /// Asynchronously removes and returns the next item from the queue.
    /// </summary>
    /// <returns>The retrieved item will return null if the queue is empty.</returns>
    public async Task<T?> DequeueAsync()
    {
        try
        {
            var db = _redis.GetDatabase();
            var message = await db.ListLeftPopAsync(_queueName, CommandFlags.None);

            if (message.IsNullOrEmpty)
            {
                _logger.LogDebug($"Queue {_queueName} is empty.");
                return default;
            }

            var item = JsonSerializer.Deserialize<T>(message);
            _logger.LogInformation($"Retrieved an item from the queue {_queueName}.");
            return item;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"An error occurred while retrieving an item from the queue. Queue name: {_queueName}.");
            throw;
        }
    }
}