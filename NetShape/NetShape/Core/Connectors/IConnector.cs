using NetShape.Core.Models;

namespace NetShape.Core;

public interface IConnector<TRequest, TResponse>
{
    event Func<IRequest<TRequest>, Task> OnRequestReceived;
    Task SendResponseAsync(string connectionId, IResponse<TResponse> response);
}