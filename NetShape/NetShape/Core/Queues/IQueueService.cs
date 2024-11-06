namespace NetShape.Core.Queues;

public interface IQueueService<T>
{
    Task EnqueueAsync(T item);
    Task<T> DequeueAsync();
}