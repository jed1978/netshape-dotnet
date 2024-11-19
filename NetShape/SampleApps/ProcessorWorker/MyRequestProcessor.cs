using Microsoft.Extensions.Logging;
using NetShape.Core;

namespace ProcessorWorker;

public class MyRequestProcessor: IRequestProcessor<string, string>
{
    private readonly ILogger<MyRequestProcessor> _logger;

    public MyRequestProcessor(ILogger<MyRequestProcessor> logger)
    {
        _logger = logger;
    }
    public Task<string> ProcessAsync(string request)
    {
        _logger.LogInformation($"Processing data: {request}");
        var responseData = $"Processed: {request}";
        return Task.FromResult(responseData);
    }
}