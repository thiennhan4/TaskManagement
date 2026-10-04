using Microsoft.AspNetCore.SignalR;
using TaskHub.Application.Hubs;
namespace TaskHub.Application.Services.Interfaces;
public interface IProtectedHubContext : IHubContext<NotificationHub>
{
    Task JoinAsync(string connectionId, Guid userId, string group);
    Task LeaveAsync(string connectionId, string group);
    Task DisconnectAsync(string connectionId);
    Task RevalidateAsync();
}
