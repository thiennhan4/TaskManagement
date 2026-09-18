using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using System.Collections.Concurrent;
using TaskHub.Application.Services.Interfaces;

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
        private readonly IRealtimeAccessService _realtimeAccess;

        public NotificationHub(IRealtimeAccessService realtimeAccess)
        {
            _realtimeAccess = realtimeAccess;
        }

        private Guid CurrentUserId => Guid.TryParse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id)
            ? id : throw new HubException("Authentication is required.");

        private static Guid ParseResourceId(string value) => Guid.TryParse(value, out var id) && id != Guid.Empty
            ? id : throw new HubException("Invalid resource ID.");

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

        public async Task JoinBoard(string boardId)
        {
            var userId = CurrentUserId;
            var id = ParseResourceId(boardId);
            await _realtimeAccess.AuthorizeBoardAsync(userId, id);
            var display = await _realtimeAccess.GetUserDisplayAsync(userId);
            boardId = id.ToString();
            var connectionId = Context.ConnectionId;

            await Groups.AddToGroupAsync(connectionId, $"board_{boardId}");

            var boardMembers = BoardPresence.GetOrAdd(boardId, _ => new ConcurrentDictionary<string, BoardMemberInfo>());
            boardMembers[connectionId] = new BoardMemberInfo
            {
                ConnectionId = connectionId,
                UserId = userId.ToString(),
                FullName = display.FullName,
                AvatarUrl = display.AvatarUrl
            };

            await NotifyBoardPresenceAsync(boardId);
        }

        public async Task LeaveBoard(string boardId)
        {
            var id = ParseResourceId(boardId);
            boardId = id.ToString();
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
            var id = ParseResourceId(taskId);
            await _realtimeAccess.AuthorizeTaskAsync(CurrentUserId, id);
            taskId = id.ToString();
            await Groups.AddToGroupAsync(Context.ConnectionId, $"task_{taskId}");
        }

        public async Task LeaveTask(string taskId)
        {
            taskId = ParseResourceId(taskId).ToString();
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"task_{taskId}");
        }

        public async Task JoinProject(string projectId)
        {
            var id = ParseResourceId(projectId);
            await _realtimeAccess.AuthorizeProjectAsync(CurrentUserId, id);
            await Groups.AddToGroupAsync(Context.ConnectionId, $"project_{id}");
        }

        public Task LeaveProject(string projectId) =>
            Groups.RemoveFromGroupAsync(Context.ConnectionId, $"project_{ParseResourceId(projectId)}");

        public async Task JoinTeam(string teamId)
        {
            var id = ParseResourceId(teamId);
            await _realtimeAccess.AuthorizeTeamAsync(CurrentUserId, id);
            await Groups.AddToGroupAsync(Context.ConnectionId, $"team_{id}");
        }

        public Task LeaveTeam(string teamId) =>
            Groups.RemoveFromGroupAsync(Context.ConnectionId, $"team_{ParseResourceId(teamId)}");

        public async Task StartTyping(string taskId, string userName)
        {
            var id = ParseResourceId(taskId);
            await _realtimeAccess.AuthorizeTaskAsync(CurrentUserId, id);
            taskId = id.ToString();
            userName = (await _realtimeAccess.GetUserDisplayAsync(CurrentUserId)).FullName;
            await Clients.OthersInGroup($"task_{taskId}").SendAsync("UserTyping", taskId, userName, true);
        }

        public async Task StopTyping(string taskId, string userName)
        {
            var id = ParseResourceId(taskId);
            await _realtimeAccess.AuthorizeTaskAsync(CurrentUserId, id);
            taskId = id.ToString();
            userName = (await _realtimeAccess.GetUserDisplayAsync(CurrentUserId)).FullName;
            await Clients.OthersInGroup($"task_{taskId}").SendAsync("UserTyping", taskId, userName, false);
        }
    }
}




