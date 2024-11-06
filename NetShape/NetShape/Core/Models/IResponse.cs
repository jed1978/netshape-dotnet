namespace NetShape.Core;

public interface IResponse<T>
{
    string RequestId { get; }
    T Data { get; }
}