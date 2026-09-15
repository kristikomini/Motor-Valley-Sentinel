using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace MotorValley.Backend.Data;

/// <summary>
/// Design-time factory used by the EF Core tools (`dotnet ef migrations add ...`).
/// Reads the same configuration as the running app, so the provider that migrations
/// are generated for is selected with <c>Database:Provider</c> — PostgreSQL by default,
/// Oracle when set. Runtime uses the DI-registered context in Program.cs instead.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var provider = config["Database:Provider"] ?? "Postgres";
        var options = new DbContextOptionsBuilder<AppDbContext>();

        if (string.Equals(provider, "Oracle", StringComparison.OrdinalIgnoreCase))
        {
            options.UseOracle(
                config.GetConnectionString("Oracle")
                    ?? "User Id=motorvalley;Password=motorvalley;Data Source=localhost:1521/XEPDB1");
        }
        else
        {
            options.UseNpgsql(
                config.GetConnectionString("PostgreSQL")
                    ?? "Host=localhost;Port=5432;Database=motorvalley;Username=postgres;Password=postgres");
        }

        return new AppDbContext(options.Options);
    }
}
