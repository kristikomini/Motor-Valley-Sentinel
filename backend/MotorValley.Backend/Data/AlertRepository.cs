using Microsoft.EntityFrameworkCore;
using MotorValley.Backend.Models;

namespace MotorValley.Backend.Data;

public class AlertRepository : IAlertRepository
{
    private readonly AppDbContext _db;

    public AlertRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(CriticalAlert alert, CancellationToken ct = default)
    {
        _db.CriticalAlerts.Add(alert);
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Top 5 machines by alert count (optimized SQL).
    /// </summary>
    public async Task<IEnumerable<CriticalAlert>> GetTopFailingMachinesAsync(int limit = 5, CancellationToken ct = default)
    {
        var topIds = await _db.CriticalAlerts
            .GroupBy(a => a.MachineId)
            .Select(g => new { MachineId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(limit)
            .Select(x => x.MachineId)
            .ToListAsync(ct);

        var result = new List<CriticalAlert>();
        foreach (var mid in topIds)
        {
            var latest = await _db.CriticalAlerts
                .Where(a => a.MachineId == mid)
                .OrderByDescending(a => a.Timestamp)
                .FirstOrDefaultAsync(ct);
            if (latest != null) result.Add(latest);
        }
        return result;
    }

    public async Task<LatestStatusDto?> GetLatestStatusAsync(CancellationToken ct = default)
    {
        var row = await _db.CriticalAlerts
            .OrderByDescending(a => a.Timestamp)
            .Select(a => new LatestStatusDto(a.MachineId, a.Temperature, a.Message, a.Timestamp))
            .FirstOrDefaultAsync(ct);
        return row;
    }

    public async Task<IEnumerable<TopFailingMachineDto>> GetTopFailingWithCountsAsync(int limit = 5, CancellationToken ct = default)
    {
        var counts = await _db.CriticalAlerts
            .GroupBy(a => a.MachineId)
            .Select(g => new { MachineId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(limit)
            .ToListAsync(ct);

        var result = new List<TopFailingMachineDto>();
        foreach (var c in counts)
        {
            var latest = await _db.CriticalAlerts
                .Where(a => a.MachineId == c.MachineId)
                .OrderByDescending(a => a.Timestamp)
                .FirstOrDefaultAsync(ct);
            if (latest != null)
                result.Add(new TopFailingMachineDto(latest.Id, latest.MachineId, latest.Temperature, latest.ConsecutiveCount, latest.Message, latest.Timestamp, c.Count));
        }
        return result;
    }
}
