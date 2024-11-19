using NetShape.Core;

namespace NetShape.Tests.Connector.Tests;

public class MockConnector<TRequest, TResponse> : IConnector<TRequest, TResponse>
{
    public event Func<IRequest<TRequest>, Task> OnRequestReceived;

    public Task SendRequestAsync(string requestId, TRequest data, string connectionId)
    {
        return Task.CompletedTask;
    }

    public Task SendResponseAsync(string connectionId, IResponse<TResponse> response)
    {
        return Task.CompletedTask;
    }

    public async Task SimulateReceiveRequest(IRequest<TRequest> request)
    {
        if (OnRequestReceived != null)
        {
            await OnRequestReceived(request);
        }
    }
}