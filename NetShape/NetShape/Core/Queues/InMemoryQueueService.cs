using System.Collections.Concurrent;

namespace NetShape.Core.Queues;

/// <summary>
/// Provides an in-memory queue service for development and testing purposes only.
/// Not recommended for use in production environments due to potential performance and reliability issues.
/// </summary>
/// <typeparam name="T">The type of elements stored in the queue. It can be any reference or value type.</typeparam>
public class InMemoryQueueService<T>: IQueueService<T>
{
    private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();

    public Task EnqueueAsync(T item)
    {
        _queue.Enqueue(item);
        return Task.CompletedTask;
    }

    public Task<T> DequeueAsync()
    {
        if (_queue.TryDequeue(out T item))
        {
            return Task.FromResult(item);
        }
        return Task.FromResult(default(T));
    }
}