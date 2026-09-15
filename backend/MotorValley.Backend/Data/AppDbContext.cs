using Microsoft.EntityFrameworkCore;
using MotorValley.Backend.Models;

namespace MotorValley.Backend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<CriticalAlert> CriticalAlerts => Set<CriticalAlert>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CriticalAlert>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.MachineId).HasMaxLength(64);
            e.Property(x => x.Message).HasMaxLength(128);
            e.HasIndex(x => x.MachineId);
            e.HasIndex(x => x.Timestamp);
        });
    }
}
