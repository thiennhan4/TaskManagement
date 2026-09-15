using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using System.Collections.Concurrent;

namespace TaskHub.Application.Hubs
{
    public class BoardMemberInfo
    {
        public string ConnectionId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
    }

    [Authorize]
    public class NotificationHub : Hub
    {
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, BoardMemberInfo>> BoardPresence = new();

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                // Group connections by userId
                await Groups.AddToGroupAsync(Context.ConnectionId, userId);
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var connectionId = Context.ConnectionId;

            // Remove from board presence
            var boardsToNotify = new List<string>();
            foreach (var boardId in BoardPresence.Keys)
            {
                if (BoardPresence.TryGetValue(boardId, out var members))
                {
                    if (members.TryRemove(connectionId, out _))
                    {
                        boardsToNotify.Add(boardId);
                    }
                }
            }

            foreach (var boardId in boardsToNotify)
            {
                await NotifyBoardPresenceAsync(boardId);
            }

            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinBoard(string boardId, string fullName, string avatarUrl)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
            var connectionId = Context.ConnectionId;

            await Groups.AddToGroupAsync(connectionId, $"board_{boardId}");

            var boardMembers = BoardPresence.GetOrAdd(boardId, _ => new ConcurrentDictionary<string, BoardMemberInfo>());
            boardMembers[connectionId] = new BoardMemberInfo
            {
                ConnectionId = connectionId,
                UserId = userId,
                FullName = fullName,
                AvatarUrl = avatarUrl
            };

            await NotifyBoardPresenceAsync(boardId);
        }

        public async Task LeaveBoard(string boardId)
        {
            var connectionId = Context.ConnectionId;
            await Groups.RemoveFromGroupAsync(connectionId, $"board_{boardId}");

            if (BoardPresence.TryGetValue(boardId, out var members))
            {
                members.TryRemove(connectionId, out _);
            }

            await NotifyBoardPresenceAsync(boardId);
        }

        private async Task NotifyBoardPresenceAsync(string boardId)
        {
            if (BoardPresence.TryGetValue(boardId, out var members))
            {
                var uniqueMembers = members.Values
                    .GroupBy(m => m.UserId)
                    .Select(g => g.First())
                    .Select(m => new { m.UserId, m.FullName, m.AvatarUrl })
                    .ToList();

                await Clients.Group($"board_{boardId}").SendAsync("UpdateBoardPresence", uniqueMembers);
            }
        }

        public async Task JoinTask(string taskId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"task_{taskId}");
        }

        public async Task LeaveTask(string taskId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"task_{taskId}");
        }

        public async Task StartTyping(string taskId, string userName)
        {
            await Clients.OthersInGroup($"task_{taskId}").SendAsync("UserTyping", taskId, userName, true);
        }

        public async Task StopTyping(string taskId, string userName)
        {
            await Clients.OthersInGroup($"task_{taskId}").SendAsync("UserTyping", taskId, userName, false);
        }
    }
}




