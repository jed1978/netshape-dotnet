using NetShape.Core;

namespace NetShape.Models;

public class GenericResponse<T>: IResponse<T>
{
    public string RequestId { get; set; }
    public T Data { get; set; }
    public string ConnectionId { get; set; }
}