namespace NetShape.Core.Queues;

public interface IQueueService<T>
{
    Task<long> EnqueueAsync(T item);
    Task<T?> DequeueAsync();
}