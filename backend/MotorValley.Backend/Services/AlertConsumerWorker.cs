using System.Text.Json;
using Confluent.Kafka;
using MotorValley.Backend.Data;
using MotorValley.Backend.Hubs;
using MotorValley.Backend.Models;
using MotorValley.Backend.Services;
using Microsoft.AspNetCore.SignalR;

namespace MotorValley.Backend.Services;

public class AlertConsumerWorker : BackgroundService
{
    private readonly IConfiguration _config;
    private readonly IServiceProvider _services;

    public AlertConsumerWorker(IConfiguration config, IServiceProvider services)
    {
        _config = config;
        _services = services;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var bootstrap = _config["Kafka:BootstrapServers"] ?? "localhost:9093";
        var topic = _config["Kafka:AlertsTopic"] ?? "critical-alerts";
        var groupId = _config["Kafka:GroupId"] ?? "alert-consumer-group";

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = bootstrap,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
        };

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();
                consumer.Subscribe(topic);

                while (!stoppingToken.IsCancellationRequested)
                {
                    var result = consumer.Consume(stoppingToken);
                    try
                    {
                        var dto = JsonSerializer.Deserialize<CriticalAlertDto>(result.Message.Value);
                        if (dto == null) continue;

                        using var scope = _services.CreateScope();
                        var repo = scope.ServiceProvider.GetRequiredService<IAlertRepository>();
                        var hub = scope.ServiceProvider.GetRequiredService<IHubContext<AlertHub>>();

                        var alert = new CriticalAlert
                        {
                            MachineId = dto.MachineId,
                            Temperature = dto.Temperature,
                            ConsecutiveCount = dto.ConsecutiveCount,
                            Message = dto.Message,
                            Timestamp = DateTime.TryParse(dto.Timestamp, out var ts) ? ts : DateTime.UtcNow
                        };

                        await repo.AddAsync(alert, stoppingToken);
                        await hub.Clients.All.SendAsync("ReceiveAlert", alert, stoppingToken);

                        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();
                        var latest = new MotorValley.Backend.Models.LatestStatusDto(alert.MachineId, alert.Temperature, alert.Message, alert.Timestamp);
                        await cache.SetAsync("motorvalley:latest-status", latest, TimeSpan.FromMinutes(5), stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Alert processing error: {ex.Message}");
                    }
                }
            }
            catch (ConsumeException ex)
            {
                Console.WriteLine($"Kafka consume error: {ex.Error.Reason}, retrying in 5s...");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Kafka consumer error: {ex.Message}, retrying in 5s...");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private class CriticalAlertDto
    {
        public string MachineId { get; set; } = "";
        public double Temperature { get; set; }
        public int ConsecutiveCount { get; set; }
        public string Message { get; set; } = "";
        public string Timestamp { get; set; } = "";
    }
}
