using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetShape;
using NetShape.Connectors;
using NetShape.Core;
using NetShape.Models;
using NetShape.Queues;
using StackExchange.Redis;

namespace ProcessorWorker;

class Program
{
    public static async Task Main(string[] args)
    {
        // 建立 HostBuilder
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                // 配置 Logging
                services.AddLogging(configure => configure.AddConsole());

                // 讀取 Redis 連接字串
                var redisConnectionString = hostContext.Configuration.GetConnectionString("Redis") 
                    ?? Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING") 
                    ?? "localhost:6379";

                // 建立並註冊 Redis 連接
                var connectionMultiplexer = ConnectionMultiplexer.Connect(redisConnectionString);
                services.AddSingleton<IConnectionMultiplexer>(connectionMultiplexer);

                services.AddSingleton<IQueueService<GenericRequest<string>>, RedisQueueService<GenericRequest<string>>>(
                    provider => new RedisQueueService<GenericRequest<string>>(
                        provider.GetRequiredService<IConnectionMultiplexer>(),
                        "test-requests",
                        provider.GetRequiredService<ILogger<RedisQueueService<GenericRequest<string>>>>()
                    ));
                
                // 註冊 RedisQueueService for GenericResponse<TResponse>
                services.AddSingleton<IQueueService<GenericResponse<string>>, RedisQueueService<GenericResponse<string>>>(
                    provider => new RedisQueueService<GenericResponse<string>>(
                        provider.GetRequiredService<IConnectionMultiplexer>(),
                        "test-responses",
                        provider.GetRequiredService<ILogger<RedisQueueService<GenericResponse<string>>>>()
                    ));

                // 註冊 IRequestProcessor
                services.AddSingleton<IRequestProcessor<string, string>, MyRequestProcessor>();

                // 註冊 ProcessorCoordinator 為 Hosted Service
                services.AddSingleton<ProcessorCoordinator<string, string>>();
                services.AddHostedService(provider => provider.GetService<ProcessorCoordinator<string, string>>());
            })
            .Build();

        // 運行 Host
        await host.RunAsync();
    }
}