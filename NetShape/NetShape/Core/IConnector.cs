using NetShape.Core.Models;

namespace NetShape.Core.Connectors;

public interface IConnector<TResponse>
{
    Task SendResponseAsync(string connectionId, IResponse<TResponse> response);
}