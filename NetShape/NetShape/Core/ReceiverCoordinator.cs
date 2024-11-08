using Microsoft.Extensions.Logging;
using NetShape.Core.Models;
using NetShape.Core.Queues;

namespace NetShape.Core;

public class ReceiverCoordinator<TRequest>
{
    private readonly IConnector<TRequest, object> _connector;
    private readonly IQueueService<GenericRequest<TRequest>> _requestQueue;
    private readonly ILogger<ReceiverCoordinator<TRequest>> _logger;

    public ReceiverCoordinator(
        IConnector<TRequest, object> connector,
        IQueueService<GenericRequest<TRequest>> requestQueue,
        ILogger<ReceiverCoordinator<TRequest>> logger)
    {
        _connector = connector ?? throw new ArgumentNullException(nameof(connector));
        _requestQueue = requestQueue ?? throw new ArgumentNullException(nameof(requestQueue));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _connector.OnRequestReceived += OnRequestReceivedAsync;
    }
    private async Task OnRequestReceivedAsync(IRequest<TRequest> request)
    {
        var req = (GenericRequest<TRequest>)request;
        _logger.LogInformation($"Received request. RequestId: {req.RequestId}, ConnectionId: {req.ConnectionId}, Data: {req.Data}");
        await _requestQueue.EnqueueAsync(req);
        _logger.LogDebug($"Enqueued request. RequestId: {request.RequestId}");
    }
}