using NetShape.Core;
using NetShape.Core.Models;

namespace NetShape.Tests.Connector.Tests;

public class MockConnector<TRequest, TResponse> : IConnector<TResponse>
{
    public event Func<IRequest<TRequest>, Task> OnRequestReceived;

    public Task SendResponseAsync(string connectionId, IResponse<TResponse> response)
    {
        // 模擬發送回應
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