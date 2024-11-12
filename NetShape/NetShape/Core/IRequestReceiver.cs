using NetShape.Core.Models;

namespace NetShape.Core;

public interface IRequestReceiver<TRequest>
{
    event Func<GenericRequest<TRequest>, Task> OnRequestReceived;
    Task ReceiveRequestAsync(GenericRequest<TRequest> request);
}