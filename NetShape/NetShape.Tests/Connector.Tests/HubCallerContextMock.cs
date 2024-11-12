using System.Security.Claims;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;

namespace NetShape.Tests.Connector.Tests;


/// <summary>
/// Simulate HubCallerContext to provide ConnectionId.
/// </summary>
public class HubCallerContextMock : HubCallerContext
{
    public override string ConnectionId { get; }
    public override string? UserIdentifier { get; }
    public override ClaimsPrincipal? User { get; }
    public override IDictionary<object, object?> Items { get; }
    public override IFeatureCollection Features { get; }
    public override CancellationToken ConnectionAborted { get; }
    
    public HubCallerContextMock(string connectionId)
    {
        ConnectionId = connectionId;
    }

    public override void Abort()
    {
        throw new NotImplementedException();
    }
}
