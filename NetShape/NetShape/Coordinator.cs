using NetShape.Core.Models;
using NetShape.Core.Processors;
using NetShape.Core.Queues;
using Microsoft.Extensions.Logging;
using NetShape.Core.Connectors;

namespace NetShape.Core;

public class Coordinator<TRequest, TResponse>
{
    private readonly IConnector<TRequest, TResponse> _connector;
    private readonly IQueueService<GenericRequest<TRequest>> _requestQueue;
    private readonly IRequestProcessor<TRequest, TResponse> _processor;
    private readonly ILogger _logger;
    private CancellationTokenSource _cancellationTokenSource;
    private Task _processingTask;
    
    public Coordinator(IConnector<TRequest,TResponse> connector, 
        IQueueService<GenericRequest<TRequest>> queue, 
        IRequestProcessor<TRequest, TResponse> processor, 
        ILogger logger)
    {
        _connector = connector ?? throw new ArgumentNullException(nameof(connector));
        _requestQueue = queue ?? throw new ArgumentNullException(nameof(queue));
        _processor = processor ?? throw new ArgumentNullException(nameof(processor));
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

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        // 啟動處理隊列的任務
        _logger.LogInformation("Coordinator is starting.");
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _processingTask = Task.Run(() => ProcessQueueAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
        await Task.CompletedTask;
    }
    
    public async Task StopAsync()
    {
        _logger.LogInformation("Coordinator is stopping.");

        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Cancel();
            try
            {
                await _processingTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Processing task was cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during stopping Coordinator.");
            }
            finally
            {
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }
        }
        _logger.LogInformation("Coordinator has stopped.");
    }
    
    private async Task ProcessQueueAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var request = await _requestQueue.DequeueAsync();
            if (request != null)
            {
                _logger.LogDebug($"Dequeued request for processing. RequestId: {request.RequestId}");
                try
                {
                    _logger.LogInformation($"Started processing request. RequestId: {request.RequestId}");
                    var responseData = await _processor.ProcessAsync(request.Data);
                    _logger.LogInformation($"Finished processing request. RequestId: {request.RequestId}");
                    
                    var response = new GenericResponse<TResponse>
                    {
                        RequestId = request.RequestId,
                        Data = responseData
                    };
                    await _connector.SendResponseAsync(request.ConnectionId, response);
                    _logger.LogDebug($"Sent response for request. RequestId: {response.RequestId}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing request: {@Request}", request);
                }
            }
            else
            {
                await Task.Delay(100, token); // 無請求時等待
            }
        }
        _logger.LogInformation("Processing queue stopped.");
    }
}