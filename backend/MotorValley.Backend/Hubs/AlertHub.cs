using Microsoft.AspNetCore.SignalR;

namespace MotorValley.Backend.Hubs;

public class AlertHub : Hub
{
    public async Task JoinDashboard() => await Groups.AddToGroupAsync(Context.ConnectionId, "dashboard");
}
