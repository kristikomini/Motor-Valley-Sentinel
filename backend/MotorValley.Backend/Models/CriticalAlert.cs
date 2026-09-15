namespace MotorValley.Backend.Models;

public class CriticalAlert
{
    public int Id { get; set; }
    public required string MachineId { get; set; }
    public double Temperature { get; set; }
    public int ConsecutiveCount { get; set; }
    public required string Message { get; set; }
    public DateTime Timestamp { get; set; }
}
