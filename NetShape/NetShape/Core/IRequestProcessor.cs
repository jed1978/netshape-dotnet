namespace NetShape.Core;

public interface IRequestProcessor<TRequest, TResponse>
{
    Task<TResponse> ProcessAsync(TRequest request);
}