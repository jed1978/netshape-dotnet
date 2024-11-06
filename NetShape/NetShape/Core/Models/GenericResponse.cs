namespace NetShape.Core.Models;

internal class GenericResponse<T>: IResponse<T>
{
    public string RequestId { get; set; }
    public T Data { get; set; }
}