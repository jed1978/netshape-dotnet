namespace NetShape.Core;

public interface IConnector<TResponse>
{
    Task SendResponseAsync(string connectionId, IResponse<TResponse> response);
}