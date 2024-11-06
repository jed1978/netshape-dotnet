using NetShape.Core.Models;
using NetShape.Core.Processors;
using NetShape.Core.Queues;

namespace NetShape.Core;

public class Coordinator<TRequest, TResponse>
{
    private readonly IConnector<TRequest, TResponse> _connector;
    private readonly IQueueService<GenericRequest<TRequest>> _requestQueue;
    private readonly IRequestProcessor<TRequest, TResponse> _processor;
    public Coordinator(IConnector<TRequest,TResponse> connector, 
        IQueueService<GenericRequest<TRequest>> queue, 
        IRequestProcessor<TRequest, TResponse> processor)
    {
        _connector = connector;
        _processor = processor;
        _requestQueue = queue;
        _connector.OnRequestReceived += OnRequestReceivedAsync;
    }

    private async Task OnRequestReceivedAsync(IRequest<TRequest> request)
    {
        await _requestQueue.EnqueueAsync((GenericRequest<TRequest>)request);
    }

    public async Task StartAsync()
    {
        // 啟動處理隊列的任務
        _ = Task.Run(ProcessQueueAsync);
    }
    
    private async Task ProcessQueueAsync()
    {
        while (true)
        {
            var request = await _requestQueue.DequeueAsync();
            if (request != null)
            {
                var responseData = await _processor.ProcessAsync(request.Data);
                var response = new GenericResponse<TResponse>
                {
                    RequestId = request.RequestId,
                    Data = responseData
                };
                await _connector.SendResponseAsync(request.ConnectionId, response);
            }
            else
            {
                await Task.Delay(100); // 無請求時等待
            }
        }
    }
}

internal class GenericResponse<T>: IResponse<T>
{
    public string RequestId { get; set; }
    public T Data { get; set; }
}