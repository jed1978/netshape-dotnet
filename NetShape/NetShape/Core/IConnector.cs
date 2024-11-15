namespace NetShape.Core;

public interface IConnector<TRequest, TResponse>
{
    Task SendRequestAsync(string requestId, TRequest data, string connectionId);
    Task SendResponseAsync(string connectionId, IResponse<TResponse> response);
}