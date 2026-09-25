using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using TaskHub.Application.Hubs;
using TaskHub.Application.Services.Interfaces;
using TaskHub.Domain.Exceptions;
namespace TaskHub.Application.Services;

// Single-host registry. The gate spans permission checks and sends; revocation waits
// for prior sends and removes denied subscriptions before its HTTP call completes.
public sealed class ProtectedHubContext : IProtectedHubContext
{
    private readonly IHubContext<NotificationHub> _raw;
    private readonly IServiceScopeFactory _scopes;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Dictionary<string, Dictionary<string, Guid>> _groups = new();
    public IHubClients Clients { get; }
    public IGroupManager Groups => _raw.Groups;
    public ProtectedHubContext(IHubContext<NotificationHub> raw, IServiceScopeFactory scopes)
    {
        _raw=raw; _scopes=scopes; Clients=new ProtectedClients(this, raw.Clients);
    }
    private async Task<bool> Allowed(Guid user, string group)
    {
        var parts=group.Split('_',2);
        if(parts.Length!=2 || !Guid.TryParse(parts[1],out var id)) return false;
        await using var scope=_scopes.CreateAsyncScope();
        var access=scope.ServiceProvider.GetRequiredService<IRealtimeAccessService>();
        try
        {
            switch(parts[0])
            {
                case "board": await access.AuthorizeBoardAsync(user,id); break;
                case "task": await access.AuthorizeTaskAsync(user,id); break;
                case "project": await access.AuthorizeProjectAsync(user,id); break;
                case "team": await access.AuthorizeTeamAsync(user,id); break;
                default: return false;
            }
            return true;
        }
        catch(AppException ex) when(ex.StatusCode is 403 or 404 or 401) { return false; }
    }
    public async Task JoinAsync(string connectionId, Guid userId, string group)
    {
        await _gate.WaitAsync();
        try
        {
            if(!await Allowed(userId,group)) throw new ForbiddenException("Access denied.");
            if(!_groups.TryGetValue(group,out var members)) _groups[group]=members=new();
            members[connectionId]=userId;
            await Groups.AddToGroupAsync(connectionId,group);
        }
        finally { _gate.Release(); }
    }
    public async Task LeaveAsync(string connectionId,string group)
    {
        await _gate.WaitAsync();
        try { if(_groups.TryGetValue(group,out var members)) { members.Remove(connectionId); if(members.Count==0) _groups.Remove(group); } await Groups.RemoveFromGroupAsync(connectionId,group); }
        finally { _gate.Release(); }
    }
    public async Task DisconnectAsync(string connectionId)
    {
        await _gate.WaitAsync();
        try { foreach(var group in _groups.Keys.ToArray()) { _groups[group].Remove(connectionId); if(_groups[group].Count==0) _groups.Remove(group); } }
        finally { _gate.Release(); }
    }
    public async Task RevalidateAsync()
    {
        await _gate.WaitAsync();
        try
        {
            foreach(var group in _groups.Keys.ToArray())
            {
                foreach(var member in _groups[group].ToArray())
                    if(!await Allowed(member.Value,group)) { _groups[group].Remove(member.Key); await Groups.RemoveFromGroupAsync(member.Key,group); }
                if(_groups[group].Count==0) _groups.Remove(group);
            }
        }
        finally { _gate.Release(); }
    }
    private async Task Send(string group, string method, object?[] args, IReadOnlyList<string> excluded, CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            if(!_groups.TryGetValue(group,out var members)) return;
            var recipients=new List<string>();
            foreach(var member in members.ToArray())
            {
                if(await Allowed(member.Value,group)) { if(!excluded.Contains(member.Key)) recipients.Add(member.Key); }
                else { members.Remove(member.Key); await Groups.RemoveFromGroupAsync(member.Key,group,ct); }
            }
            if(recipients.Count>0) await _raw.Clients.Clients(recipients).SendCoreAsync(method,args,ct);
        }
        finally { _gate.Release(); }
    }
    private sealed class Proxy(ProtectedHubContext owner,string group,IReadOnlyList<string> excluded) : IClientProxy
    {
        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken=default) => owner.Send(group,method,args,excluded,cancellationToken);
    }
    private sealed class MultiProxy(IEnumerable<IClientProxy> proxies) : IClientProxy
    {
        public Task SendCoreAsync(string method,object?[] args,CancellationToken cancellationToken=default) => Task.WhenAll(proxies.Select(p=>p.SendCoreAsync(method,args,cancellationToken)));
    }
    private sealed class ProtectedClients(ProtectedHubContext owner,IHubClients raw) : IHubClients
    {
        public IClientProxy All => throw new InvalidOperationException("Protected events require a resource group.");
        public IClientProxy AllExcept(IReadOnlyList<string> ids) => throw new InvalidOperationException("Protected events require a resource group.");
        public IClientProxy Client(string id) => raw.Client(id);
        public IClientProxy Clients(IReadOnlyList<string> ids) => raw.Clients(ids);
        public IClientProxy Group(string name) => new Proxy(owner,name,Array.Empty<string>());
        public IClientProxy GroupExcept(string name,IReadOnlyList<string> ids) => new Proxy(owner,name,ids);
        public IClientProxy Groups(IReadOnlyList<string> names) => new MultiProxy(names.Select(Group));
        public IClientProxy User(string id) => raw.User(id);
        public IClientProxy Users(IReadOnlyList<string> ids) => raw.Users(ids);
    }
}
