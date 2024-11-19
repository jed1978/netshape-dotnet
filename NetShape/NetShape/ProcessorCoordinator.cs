using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NetShape.Core.Models;
using NetShape.Core.Processors;
using NetShape.Core.Queues;

namespace NetShape;

public class ProcessorCoordinator<TRequest, TResponse> : IHostedService
{
    private readonly IQueueService<GenericRequest<TRequest>> _requestQueue;
    private readonly IQueueService<GenericResponse<TResponse>> _responseQueue;
    private readonly IRequestProcessor<TRequest, TResponse> _processor;
    private readonly ILogger<ProcessorCoordinator<TRequest, TResponse>> _logger;
    private CancellationTokenSource _cancellationTokenSource;
    private Task _processingTask;
    
    public ProcessorCoordinator(
        IQueueService<GenericRequest<TRequest>> requestQueue,
        IQueueService<GenericResponse<TResponse>> responseQueue,
        IRequestProcessor<TRequest, TResponse> processor,
        ILogger<ProcessorCoordinator<TRequest, TResponse>> logger)
    {
        _requestQueue = requestQueue ?? throw new ArgumentNullException(nameof(requestQueue));
        _responseQueue = responseQueue ?? throw new ArgumentNullException(nameof(responseQueue));
        _processor = processor ?? throw new ArgumentNullException(nameof(processor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("ProcessorCoordinator is starting.");

        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _processingTask = Task.Run(() => ProcessQueueAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("ProcessorCoordinator is stopping.");

        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Cancel();

            try
            {
                await _processingTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Processor Task was cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stopping processor task failed.");
            }
            finally
            {
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }
        }

        _logger.LogInformation("ProcessorCoordinator is stopped.");
    }

    private async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting processing queue.");

        while (!cancellationToken.IsCancellationRequested)
        {
            var request = await _requestQueue.DequeueAsync();

            if (request != null)
            {
                _logger.LogInformation($"Processing request, RequestId: {request}");

                try
                {
                    var responseData = await _processor.ProcessAsync(request.Data);

                    var response = new GenericResponse<TResponse>
                    {
                        RequestId = request.RequestId,
                        ConnectionId = request.ConnectionId, // Retain connection id
                        Data = responseData
                    };
                    
                    await _responseQueue.EnqueueAsync(response);

                    _logger.LogInformation($"Request processing complete, RequestId: {request.RequestId}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"An error occurred while processing the request, RequestId: {request.RequestId}");
                }
            }
            else
            {
                await Task.Delay(100, cancellationToken);
            }
        }

        _logger.LogInformation("Finished processing the queue.");
    }
}