using NetShape.Connectors;
using NetShape.Core;
using NetShape.Core.Models;
using NetShape.Core.Queues;
using NetShape.Queues;
using StackExchange.Redis;

namespace RequestReceiverApp;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
        var connectionMultiplexer = ConnectionMultiplexer.Connect(redisConnectionString);
        builder.Services.AddSingleton<IConnectionMultiplexer>(connectionMultiplexer);
        
        builder.Services.AddSingleton<IQueueService<GenericRequest<string>>, RedisQueueService<GenericRequest<string>>>(
            provider =>
            {
                var connection = provider.GetRequiredService<IConnectionMultiplexer>();
                var logger = provider.GetRequiredService<ILogger<RedisQueueService<GenericRequest<string>>>>();
                return new RedisQueueService<GenericRequest<string>>(
                    connection,
                    "test-requests",
                    logger
                );
            });
        
        builder.Services.AddSingleton<IQueueService<GenericResponse<string>>, RedisQueueService<GenericResponse<string>>>(
            provider => new RedisQueueService<GenericResponse<string>>(
                provider.GetRequiredService<IConnectionMultiplexer>(),
                "test-response",
                provider.GetRequiredService<ILogger<RedisQueueService<GenericResponse<string>>>>()
            ));
        
        // 註冊 IRequestReceiver
        builder.Services.AddSingleton<IRequestReceiver<string>, RequestReceiver<string>>();
        
        // Register SignalR Connector
        builder.Services.AddSingleton<IConnector<string>, SignalRConnector<string, string>>();

        // Register ReceiverCoordinator
        builder.Services.AddSingleton<ReceiverCoordinator<string, string>>();

        // Register ReceiverCoordinator as Hosted Service
        builder.Services.AddHostedService<ReceiverCoordinator<string, string>>();

        // Add SignalR service
        builder.Services.AddSignalR();

        // Add services to the container.
        builder.Services.AddRazorPages();

        var app = builder.Build();
        
        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }
        
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthorization();

        app.MapRazorPages();
        
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHub<SignalRConnector<string, string>>("/hub"); // SignalR Hub
        });

        app.Run();
    }
}