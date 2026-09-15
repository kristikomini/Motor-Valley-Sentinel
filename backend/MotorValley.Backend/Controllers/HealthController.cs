using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MotorValley.Backend.Data;
using StackExchange.Redis;

namespace MotorValley.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConnectionMultiplexer _redis;

    public HealthController(AppDbContext db, IConnectionMultiplexer redis)
    {
        _db = db;
        _redis = redis;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var dbOk = false;
        var redisOk = false;

        try { dbOk = await _db.Database.CanConnectAsync(ct); } catch { }

        try { redisOk = _redis.GetDatabase().Ping().TotalMilliseconds >= 0; } catch { }

        var healthy = dbOk && redisOk;
        return Ok(new
        {
            status = healthy ? "healthy" : "degraded",
            database = dbOk ? "up" : "down",
            redis = redisOk ? "up" : "down"
        });
    }
}
