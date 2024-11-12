using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using NetShape.Core;
using NetShape.Core.Connectors;
using NetShape.Core.Models;

namespace NetShape.Connectors;

/// <summary>
/// SignalR 連接器，用於處理客戶端的請求和回應。
/// </summary>
public class SignalRConnector<TRequest, TResponse> : Hub, IConnector<TResponse>
{
    private readonly ILogger<SignalRConnector<TRequest, TResponse>> _logger;
    private readonly IRequestReceiver<TRequest> _requestReceiver;
    
    public SignalRConnector(
        ILogger<SignalRConnector<TRequest, TResponse>> logger,
        IRequestReceiver<TRequest> requestReceiver)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _requestReceiver = requestReceiver ?? throw new ArgumentNullException(nameof(requestReceiver));
    }

    /// <summary>
    /// Handles requests sent by the client.
    /// </summary>
    /// <param name="requestId">The unique identifier for the request.</param>
    /// <param name="data">The data of the request.</param>
    public async Task SendRequest(string requestId, TRequest data)
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

        _logger.LogInformation($"Received client request. RequestId: {requestId}, ConnectionId: {Context.ConnectionId}");

        var request = new GenericRequest<TRequest>
        {
            RequestId = requestId,
            ConnectionId = Context.ConnectionId,
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
            await Clients.Client(connectionId).SendAsync("ReceiveResponse", response);
            _logger.LogInformation($"Response sent. RequestId: {response.RequestId}, ConnectionId: {connectionId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"An error occurred while sending the response. RequestId: {response.RequestId}, ConnectionId: {connectionId}");
            throw;
        }
    }

    /// <summary>
    /// Event handler for when a client connects.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation($"Client has connected. ConnectionId: {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Event handler for when a client disconnects.
    /// </summary>
    /// <param name="exception">The exception that caused the disconnection, if any.</param>
    public override async Task OnDisconnectedAsync(Exception exception)
    {
        if (exception != null)
        {
            _logger.LogWarning(exception, $"Client disconnected unexpectedly. ConnectionId: {Context.ConnectionId}");
        }
        else
        {
            _logger.LogInformation($"Client has disconnected. ConnectionId: {Context.ConnectionId}");
        }
        await base.OnDisconnectedAsync(exception);
    }
}
