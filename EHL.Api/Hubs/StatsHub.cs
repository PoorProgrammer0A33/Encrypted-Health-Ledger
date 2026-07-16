using Microsoft.AspNetCore.SignalR;

namespace EHL.Api.Hubs;

public class StatsHub : Hub
{
    public override Task OnConnectedAsync()
    {
        Console.WriteLine($"[Hub] Client connected: {Context.ConnectionId}");
        return base.OnConnectedAsync();
    }
}