namespace MotorValley.Backend.Models;

public record LatestStatusDto(string MachineId, double Temperature, string Message, DateTime Timestamp);
