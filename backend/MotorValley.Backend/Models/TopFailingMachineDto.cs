namespace MotorValley.Backend.Models;

public record TopFailingMachineDto(
    int Id,
    string MachineId,
    double Temperature,
    int ConsecutiveCount,
    string Message,
    DateTime Timestamp,
    int AlertCount
);
