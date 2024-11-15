using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using NetShape.Core;

namespace NetShape.Connectors.SignalR;

public class RequestHub: Hub
{
    private readonly ILogger<RequestHub> _logger;
    private readonly IConnector<string, string> _connector;
    
    public RequestHub(ILogger<RequestHub> logger, IConnector<string, string> connector)
    {
        _logger = logger;
        _connector = connector;
    }
    
    /// <summary>
    /// Receives client requests and passes them to the SignalRConnector for processing.
    /// </summary>
    /// <param name="requestId">The unique identifier for the request.</param>
    /// <param name="data">The data of the request.</param>
    public async Task SendRequest(string requestId, string data)
    {
        if (string.IsNullOrEmpty(requestId))
        {
            _logger.LogWarning("The received request ID is empty.");
            throw new HubException("The request ID cannot be empty.");
        }

        if (data == null)
        {
            _logger.LogWarning("The received request data is null.");
            throw new HubException("The request data cannot be null.");
        }

        string connectionId = Context.ConnectionId;

        _logger.LogInformation($"Received client request. RequestId: {requestId}, ConnectionId: {connectionId}");

        try
        {
            await _connector.SendRequestAsync(requestId, data, connectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"An error occurred while processing the request. RequestId: {requestId}");
            throw new HubException("An error occurred while processing your request.");
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