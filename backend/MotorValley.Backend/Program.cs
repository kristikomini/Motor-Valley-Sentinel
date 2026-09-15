using Microsoft.EntityFrameworkCore;
using MotorValley.Backend.Data;
using MotorValley.Backend.Hubs;
using MotorValley.Backend.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddControllers();

// SignalR runs in-process locally; in Azure it scales out through the
// Azure SignalR Service backplane the moment a connection string is present,
// so the WebSocket transport survives multiple backend replicas.
var signalR = builder.Services.AddSignalR();
var azureSignalR = builder.Configuration.GetConnectionString("AzureSignalR");
if (!string.IsNullOrWhiteSpace(azureSignalR))
    signalR.AddAzureSignalR(azureSignalR);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Relational store — provider is configurable. PostgreSQL is the default that
// ships and runs in Docker; Oracle is wired and selectable via Database:Provider,
// since the JD-side estate is Oracle. EF Core keeps the repository code identical
// across both — only the provider and connection string change.
var dbProvider = builder.Configuration["Database:Provider"] ?? "Postgres";
builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (string.Equals(dbProvider, "Oracle", StringComparison.OrdinalIgnoreCase))
        options.UseOracle(builder.Configuration.GetConnectionString("Oracle"));
    else
        options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSQL"));
});

// Redis
var redisConn = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var config = ConfigurationOptions.Parse(redisConn);
    return ConnectionMultiplexer.Connect(config);
});
builder.Services.AddScoped<ICacheService, RedisCacheService>();

// Kafka consumer + DbContext
builder.Services.Configure<HostOptions>(opts => opts.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore);
builder.Services.AddHostedService<AlertConsumerWorker>();
builder.Services.AddScoped<IAlertRepository, AlertRepository>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(policy =>
{
    policy.AllowAnyOrigin()
          .AllowAnyMethod()
          .AllowAnyHeader();
});

app.MapControllers();
app.MapHub<AlertHub>("/hubs/alerts");

// Apply EF Core migrations on startup (versioned schema, not EnsureCreated()).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.Run();
