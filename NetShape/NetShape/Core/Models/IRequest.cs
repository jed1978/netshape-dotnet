namespace NetShape.Core.Models;

public interface IRequest<T>
{
    string RequestId { get; }
    T Data { get; }
}