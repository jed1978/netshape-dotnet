using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NetShape.Connectors;
using NetShape.Core;
using NetShape.Core.Models;

namespace NetShape.Tests.Connector.Tests;

public class SignalRConnectorTests: IAsyncLifetime
{
    private TestServer _server;
    private HubConnection _connection;
    private string _connectionId;
    private readonly Mock<IRequestReceiver<string>> _mockRequestReceiver;
    
    public SignalRConnectorTests()
    {
        _mockRequestReceiver = new Mock<IRequestReceiver<string>>();
    }
    
    /// <summary>
    /// 設定測試伺服器並配置 SignalR Hub
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task InitializeAsync()
    {
        var builder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddSignalR();
                services.AddLogging();
                services.AddSingleton<ILogger<SignalRConnector<string, string>>, NullLogger<SignalRConnector<string, string>>>();
                services.AddSingleton(_mockRequestReceiver.Object);
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapHub<SignalRConnector<string, string>>("/hub");
                });
            });

        _server = new TestServer(builder);

        // 建立客戶端連線
        _connection = new HubConnectionBuilder()
            .WithUrl("http://localhost/hub", options =>
            {
                options.HttpMessageHandlerFactory = _ => _server.CreateHandler();
            })
            .ConfigureLogging(logging => logging.AddConsole())
            .Build();

        // 啟動連線並取得 ConnectionId
        try
        {
            await _connection.StartAsync();
            _connectionId = _connection.ConnectionId;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to start HubConnection.", ex);
        }
    }

    public async Task DisposeAsync()
    {
        if (_connection != null)
        {
            await _connection.DisposeAsync();
        }

        if (_server != null)
        {
            _server.Dispose();
        }
    }
    
    [Fact]
    public async Task Should_Invoke_OnRequestReceived_When_Client_Sends_Request()
    {
        // Arrange
        var requestId = "1";
        var requestData = "Test Data";
        
        var request = new GenericRequest<string>
        {
            RequestId = requestId,
            ConnectionId = _connectionId,
            Data = requestData
        };

        _mockRequestReceiver
            .Setup(r => r.ReceiveRequestAsync(It.Is<GenericRequest<string>>(req =>
                req.RequestId == requestId &&
                req.ConnectionId == _connectionId &&
                req.Data == requestData)))
            .Returns(Task.CompletedTask)
            .Verifiable();
        
        // Act
        await _connection.InvokeAsync("SendRequest", requestId, requestData);

        // Assert
        _mockRequestReceiver.Verify(r => r.ReceiveRequestAsync(It.Is<GenericRequest<string>>(req =>
            req.RequestId == requestId &&
            req.ConnectionId == _connectionId &&
            req.Data == requestData)), Times.Once);
    }


    

}