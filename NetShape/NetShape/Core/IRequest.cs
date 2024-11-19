namespace NetShape.Core;

public interface IRequest<T>
{
    string RequestId { get; }
    T Data { get; }
}