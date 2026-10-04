$ErrorActionPreference = "Stop"
Set-Location "d:\web\TaskHub\backend"

Write-Host "Moving Repository Interfaces to Application layer..."
if (!(Test-Path "TaskHub.Application\Repositories")) { New-Item -ItemType Directory -Path "TaskHub.Application\Repositories" }
Move-Item "TaskHub.Infrastructure\Repositories\Interfaces" "TaskHub.Application\Repositories\Interfaces" -Force

Write-Host "Moving Hubs to Application layer temporarily to fix circular dependencies..."
if (!(Test-Path "TaskHub.Application\Hubs")) { New-Item -ItemType Directory -Path "TaskHub.Application\Hubs" }
Move-Item "TaskHub.API\Hubs\*" "TaskHub.Application\Hubs\" -Force

Write-Host "Adding missing Nuget Packages to Application..."
dotnet add TaskHub.Application\TaskHub.Application.csproj package Microsoft.Extensions.Logging.Abstractions -v 9.0.2
dotnet add TaskHub.Application\TaskHub.Application.csproj package Microsoft.Extensions.Configuration.Abstractions -v 9.0.2
dotnet add TaskHub.Application\TaskHub.Application.csproj package Microsoft.AspNetCore.SignalR -v 1.1.0 # Or just FrameworkReference

# Actually, a classlib can reference ASP.NET Core App to get all these
# Let's modify the Application csproj to use Web SDK or add FrameworkReference
$appCsprojPath = "TaskHub.Application\TaskHub.Application.csproj"
$appCsproj = Get-Content $appCsprojPath -Raw
$appCsproj = $appCsproj -replace '<Project Sdk="Microsoft\.NET\.Sdk">', '<Project Sdk="Microsoft.NET.Sdk.Web">'
$appCsproj = $appCsproj -replace '<OutputType>Exe</OutputType>', '' # In case it's there
Set-Content $appCsprojPath -Value $appCsproj -Encoding UTF8

Write-Host "Creating IAppDbContext..."
if (!(Test-Path "TaskHub.Application\Data")) { New-Item -ItemType Directory -Path "TaskHub.Application\Data" }
$iAppDbContent = @"
using Microsoft.EntityFrameworkCore;
using TaskHub.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace TaskHub.Application.Data
{
    public interface IAppDbContext
    {
        DbSet<User> Users { get; set; }
        DbSet<Workspace> Workspaces { get; set; }
        DbSet<WorkspaceMember> WorkspaceMembers { get; set; }
        DbSet<Project> Projects { get; set; }
        DbSet<ProjectMember> ProjectMembers { get; set; }
        DbSet<Board> Boards { get; set; }
        DbSet<BoardList> Lists { get; set; }
        DbSet<TaskItem> Tasks { get; set; }
        DbSet<TaskAttachment> TaskAttachments { get; set; }
        DbSet<Comment> Comments { get; set; }
        DbSet<RefreshToken> RefreshTokens { get; set; }
        DbSet<Notification> Notifications { get; set; }
        DbSet<NotificationType> NotificationTypes { get; set; }
        DbSet<UserNotificationPreference> UserNotificationPreferences { get; set; }
        DbSet<NotificationDelivery> NotificationDeliveries { get; set; }
        DbSet<NotificationTemplate> NotificationTemplates { get; set; }
        DbSet<ProjectInvitation> ProjectInvitations { get; set; }
        DbSet<ProjectActivityLog> ProjectActivityLogs { get; set; }
        DbSet<TaskActivityLog> TaskActivityLogs { get; set; }
        DbSet<Team> Teams { get; set; }
        DbSet<TeamMember> TeamMembers { get; set; }
        
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
"@
Set-Content "TaskHub.Application\Data\IAppDbContext.cs" -Value $iAppDbContent -Encoding UTF8

Write-Host "Updating AppDbContext to implement IAppDbContext..."
$dbContextPath = "TaskHub.Infrastructure\Data\AppDbContext.cs"
$dbContext = Get-Content $dbContextPath -Raw
$dbContext = $dbContext -replace "public class AppDbContext : DbContext", "public class AppDbContext : DbContext, TaskHub.Application.Data.IAppDbContext"
Set-Content $dbContextPath -Value $dbContext -Encoding UTF8

Write-Host "Replacing AppDbContext with IAppDbContext in Services..."
$services = Get-ChildItem -Path "TaskHub.Application\Services" -Filter "*.cs" -Recurse
foreach ($s in $services) {
    $content = Get-Content $s.FullName -Raw
    $content = $content -replace "AppDbContext", "IAppDbContext"
    $content = $content -replace "using TaskHub\.Infrastructure\.Data;", "using TaskHub.Application.Data;"
    Set-Content $s.FullName -Value $content -Encoding UTF8
}

Write-Host "Fixing missing namespaces..."
$files = Get-ChildItem -Recurse -Filter *.cs
foreach ($f in $files) {
    $content = Get-Content $f.FullName -Raw
    $content = $content -replace "using TaskHub\.API\.Hubs;", "using TaskHub.Application.Hubs;"
    $content = $content -replace "namespace TaskHub\.API\.Hubs", "namespace TaskHub.Application.Hubs"
    Set-Content $f.FullName -Value $content -Encoding UTF8
}

Write-Host "Done fixing architecture."
