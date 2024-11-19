using NetShape.Core;

namespace NetShape.Models;

public class GenericRequest<T> : IRequest<T>
{
    public string RequestId { get; set; }
     public string ConnectionId { get; set; }   
     public T Data { get; set; }
}