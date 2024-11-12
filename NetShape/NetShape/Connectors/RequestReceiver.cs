using Microsoft.Extensions.Logging;
using NetShape.Core;
using NetShape.Core.Models;

namespace NetShape.Connectors;

public class RequestReceiver<TRequest>: IRequestReceiver<TRequest>
{
    public event Func<GenericRequest<TRequest>, Task>? OnRequestReceived;

    private readonly ILogger<RequestReceiver<TRequest>> _logger;

    public RequestReceiver(ILogger<RequestReceiver<TRequest>> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task ReceiveRequestAsync(GenericRequest<TRequest> request)
    {
        if (OnRequestReceived != null)
        {
            try
            {
                await OnRequestReceived.Invoke(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error invoking OnRequestReceived for RequestId: {request.RequestId}");
                throw;
            }
        }
        else
        {
            _logger.LogError("The OnRequestReceived event has no subscribers.");
            throw new InvalidOperationException("There are no subscribers to handle the request.");
        }
    }
}