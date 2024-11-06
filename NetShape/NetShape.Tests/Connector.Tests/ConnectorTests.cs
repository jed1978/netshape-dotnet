using NetShape.Core.Models;

namespace NetShape.Tests.Connector.Tests;

public class ConnectorTests
{
    [Fact]
    public async Task Connector_Should_Raise_OnRequestReceived_Event()
    {
        // Arrange
        var connector = new MockConnector<string, string>();
        var request = new GenericRequest<string> { RequestId = "1", Data = "Test Data" };
        bool eventRaised = false;

        connector.OnRequestReceived += (req) =>
        {
            eventRaised = true;
            Assert.Equal(request.RequestId, req.RequestId);
            Assert.Equal(request.Data, req.Data);
            return Task.CompletedTask;
        };

        // Act
        await connector.SimulateReceiveRequest(request);

        // Assert
        Assert.True(eventRaised);
    }
}