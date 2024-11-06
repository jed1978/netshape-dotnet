namespace NetShape.Core.Processors;

public interface IRequestProcessor<TRequest, TResponse>
{
    Task<TResponse> ProcessAsync(TRequest request);
}