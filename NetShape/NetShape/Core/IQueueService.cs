namespace NetShape.Core;

public interface IQueueService<T>
{
    Task<long> EnqueueAsync(T item);
    Task<T?> DequeueAsync();
}