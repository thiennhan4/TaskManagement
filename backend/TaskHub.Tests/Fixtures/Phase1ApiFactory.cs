using System.Net.Http.Headers;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TaskHub.Application.Services.Interfaces;
using TaskHub.Domain.Entities;
using TaskHub.Infrastructure.Data;

namespace TaskHub.Tests.Fixtures;

public class Phase1ApiFactory : WebApplicationFactory<Program>
{
    public string AttachmentRoot { get; } = Path.Combine(Path.GetTempPath(), "TaskHub-tests", Guid.NewGuid().ToString("N"));
    private readonly string _database=Guid.NewGuid().ToString();
    private readonly string _key=Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_,config)=>config.AddInMemoryCollection(new Dictionary<string,string?>
        { ["Jwt:Key"]=_key, ["Google:ClientId"]="", ["Attachments:PrivateRoot"]=AttachmentRoot }));
        builder.ConfigureTestServices(services=>
        {
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options=>options.UseInMemoryDatabase(_database));
        });
    }
    public HttpClient Client(Guid? user=null)
    {
        var client=CreateClient(new() { BaseAddress=new Uri("https://localhost"), HandleCookies=false });
        if(user.HasValue)
        {
            using var scope=Services.CreateScope();
            var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var account=db.Users.Single(u=>u.Id==user.Value);
            var jwt=scope.ServiceProvider.GetRequiredService<ITokenService>().GenerateJwtToken(account);
            client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",jwt);
        }
        return client;
    }
    public async Task<SecuritySeed> SeedAsync()
    {
        using var scope=Services.CreateScope();
        var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var owner=new AppUser { Email="owner@example.invalid", FullName="Owner", PasswordHash="SYNTHETIC_HASH_DO_NOT_EXPOSE" };
        var outsider=new AppUser { Email="outsider@example.invalid", FullName="Outsider", PasswordHash="SYNTHETIC_HASH_DO_NOT_EXPOSE" };
        var project=new Project { Name="Private project", Slug="private", Owner=owner, OwnerId=owner.Id, ProjectType=ProjectType.Personal };
        var board=new Board { Name="Main", OwnerId=owner.Id, Project=project, ProjectId=project.Id };
        var list=new BoardList { Name="Todo", Board=board, BoardId=board.Id };
        var task=new TaskItem { Title="Persisted task", List=list, ListId=list.Id, Owner=owner, OwnerId=owner.Id };
        db.Users.AddRange(owner,outsider); db.Projects.Add(project); db.Boards.Add(board); db.Lists.Add(list); db.Tasks.Add(task);
        db.Tasks.Add(new TaskItem { Title="Deleted task", List=list, ListId=list.Id, OwnerId=owner.Id, IsDeleted=true });
        db.ProjectActivityLogs.Add(new ProjectActivityLog { Project=project,ProjectId=project.Id,User=owner,UserId=owner.Id,Action=ProjectActivityAction.Created,Description="Created" });
        await db.SaveChangesAsync();
        return new(owner.Id,outsider.Id,project.Id,board.Id,list.Id,task.Id);
    }

    public async Task<TeamSecuritySeed> SeedTeamAsync()
    {
        var seed = await SeedAsync();
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var team = new Team { Name = "Workspace", CreatedById = seed.Owner };
        var member = new AppUser { Email = "member@example.invalid", FullName = "Member", PasswordHash = "SYNTHETIC_HASH" };
        var manager = new AppUser { Email = "manager@example.invalid", FullName = "Manager", PasswordHash = "SYNTHETIC_HASH" };
        var guest = new AppUser { Email = "guest@example.invalid", FullName = "Guest", PasswordHash = "SYNTHETIC_HASH" };
        db.Users.AddRange(member, manager, guest);
        db.Teams.Add(team);
        db.TeamMembers.AddRange(new TeamMember { TeamId = team.Id, UserId = seed.Owner, Role = TeamRole.Owner },
            new TeamMember { TeamId = team.Id, UserId = member.Id, Role = TeamRole.Member },
            new TeamMember { TeamId = team.Id, UserId = manager.Id, Role = TeamRole.Manager });
        var project = await db.Projects.SingleAsync(p => p.Id == seed.Project);
        project.ProjectType = ProjectType.Team;
        project.WorkspaceId = team.Id;
        db.ProjectMembers.AddRange(new ProjectMember { ProjectId = project.Id, UserId = seed.Owner, Role = ProjectRole.Owner },
            new ProjectMember { ProjectId = project.Id, UserId = member.Id, Role = ProjectRole.Member },
            new ProjectMember { ProjectId = project.Id, UserId = manager.Id, Role = ProjectRole.Admin },
            new ProjectMember { ProjectId = project.Id, UserId = guest.Id, Role = ProjectRole.Guest });
        var task = await db.Tasks.SingleAsync(t => t.Id == seed.Task);
        task.TeamId = team.Id;
        task.AssignedToId = member.Id;
        await db.SaveChangesAsync();
        return new(seed, team.Id, member.Id, manager.Id, guest.Id);
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        if (Directory.Exists(AttachmentRoot)) Directory.Delete(AttachmentRoot, recursive: true);
    }
}
public record SecuritySeed(Guid Owner,Guid Outsider,Guid Project,Guid Board,Guid List,Guid Task);
public record TeamSecuritySeed(SecuritySeed Resources, Guid Team, Guid Member, Guid Manager, Guid Guest);
