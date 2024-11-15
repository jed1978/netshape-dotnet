using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using NetShape.Core;
using NetShape.Core.Models;

namespace NetShape.Connectors.SignalR;

/// <summary>
/// SignalR connector used to handle client requests and responses.
/// </summary>
public class SignalRConnector<TRequest, TResponse> : IConnector<TRequest, TResponse>
{
    private readonly ILogger<SignalRConnector<TRequest, TResponse>> _logger;
    private readonly IRequestReceiver<TRequest> _requestReceiver;
    private readonly IHubContext<RequestHub> _hubContext;
    
    public SignalRConnector(
        ILogger<SignalRConnector<TRequest, TResponse>> logger,
        IRequestReceiver<TRequest> requestReceiver, 
        IHubContext<RequestHub> hubContext)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _requestReceiver = requestReceiver ?? throw new ArgumentNullException(nameof(requestReceiver));
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
    }

    /// <summary>
    /// Handles requests sent by the client.
    /// </summary>
    /// <param name="requestId">The unique identifier for the request.</param>
    /// <param name="data">The data of the request.</param>
    /// <param name="connectionId"></param>
    public async Task SendRequestAsync(string requestId, TRequest data, string connectionId)
    {
        if (string.IsNullOrEmpty(requestId))
        {
            _logger.LogWarning("The received request ID is empty.");
            throw new ArgumentException("The request ID cannot be empty.", nameof(requestId));
        }

        if (data == null)
        {
            _logger.LogWarning("The received request data is null.");
            throw new ArgumentNullException(nameof(data));
        }

        _logger.LogInformation($"Received client request. RequestId: {requestId}, ConnectionId: {connectionId}");

        var request = new GenericRequest<TRequest>
        {
            RequestId = requestId,
            ConnectionId = connectionId,
            Data = data
        };

        try
        {
            await _requestReceiver.ReceiveRequestAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"An error occurred while processing the request. RequestId: {requestId}");
            throw;
        }
    }

    /// <summary>
    /// Sends a response to the specified client.
    /// </summary>
    /// <param name="connectionId">The connection ID of the client.</param>
    /// <param name="response">The response data.</param>
    public async Task SendResponseAsync(string connectionId, IResponse<TResponse> response)
    {
        if (string.IsNullOrEmpty(connectionId))
        {
            _logger.LogWarning("The provided connection ID is empty.");
            throw new ArgumentException("The connection ID cannot be empty.", nameof(connectionId));
        }

        if (response == null)
        {
            _logger.LogWarning("The provided response object is null.");
            throw new ArgumentNullException(nameof(response));
        }

        try
        {
            _logger.LogInformation($"Sending response. RequestId: {response.RequestId}, ConnectionId: {connectionId}");
            await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveResponse", response);
            _logger.LogInformation($"Response sent. RequestId: {response.RequestId}, ConnectionId: {connectionId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"An error occurred while sending the response. RequestId: {response.RequestId}, ConnectionId: {connectionId}");
            throw;
        }
    }
}
