using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetShape.Core.Models;
using NetShape.Core.Queues;

namespace NetShape.Core;

public class ReceiverCoordinator<TRequest, TResponse> : IHostedService
{
    private readonly IConnector<TRequest, TResponse> _connector;
    private readonly IQueueService<GenericRequest<TRequest>> _requestQueue;
    private readonly IQueueService<GenericResponse<TResponse>> _responseQueue;
    private readonly ILogger<ReceiverCoordinator<TRequest, TResponse>> _logger;
    private readonly IRequestReceiver<TRequest> _requestReceiver;
    
    private CancellationTokenSource _cancellationTokenSource;
    private Task _responseProcessingTask;
    
    public ReceiverCoordinator(
        IConnector<TRequest, TResponse> connector,
        IQueueService<GenericRequest<TRequest>> requestQueue,
        IQueueService<GenericResponse<TResponse>> responseQueue,
        ILogger<ReceiverCoordinator<TRequest, TResponse>> logger, 
        IRequestReceiver<TRequest> requestReceiver)
    {
        _connector = connector ?? throw new ArgumentNullException(nameof(connector));
        _requestQueue = requestQueue ?? throw new ArgumentNullException(nameof(requestQueue));
        _responseQueue = responseQueue ?? throw new ArgumentNullException(nameof(responseQueue));;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        requestReceiver.OnRequestReceived += OnRequestReceivedAsync;
        _requestReceiver = requestReceiver;
    }
    private async Task OnRequestReceivedAsync(IRequest<TRequest> request)
    {
        var req = (GenericRequest<TRequest>)request;
        _logger.LogInformation($"Received request. RequestId: {req.RequestId}, ConnectionId: {req.ConnectionId}, Data: {req.Data}");
        await _requestQueue.EnqueueAsync(req);
        _logger.LogDebug($"Enqueued request. RequestId: {request.RequestId}");
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Receiver coordinator is starting.");

        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _responseProcessingTask = Task.Run(() => ProcessResponseQueueAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
        return Task.CompletedTask;
    }

    private async Task? ProcessResponseQueueAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting to process the response queue.");

        while (!cancellationToken.IsCancellationRequested)
        {
            var response = await _responseQueue.DequeueAsync();

            if (response != null)
            {
                _logger.LogInformation($"Processing response, RequestId: {response.RequestId}");

                try
                {
                    await _connector.SendResponseAsync(response.ConnectionId, response);
                    _logger.LogInformation($"Response sent, RequestId: {response.RequestId}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"An error occurred while sending the response, RequestId: {response.RequestId}");
                    // Handle the exception as needed
                }
            }
            else
            {
                await Task.Delay(100, cancellationToken); // Wait when there is no response
            }
        }

        _logger.LogInformation("Finished processing the response queue.");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping the receiver coordinator.");

        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Cancel();

            try
            {
                await _responseProcessingTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Response processing task has been canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while stopping the coordinator.");
            }
            finally
            {
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }
        }

        _logger.LogInformation("Receiver coordinator has been stopped.");
    }
}