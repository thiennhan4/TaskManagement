$ErrorActionPreference = "Stop"
Set-Location "d:\web\TaskHub\backend"

Write-Host "=== STEP 1: Fix IAppDbContext to match actual DbSets in AppDbContext ===" -ForegroundColor Cyan

$iDbContextContent = @'
using Microsoft.EntityFrameworkCore;
using TaskHub.Domain.Entities;

namespace TaskHub.Application.Data;

public interface IAppDbContext
{
    // Phase 1
    DbSet<AppUser> Users { get; set; }
    DbSet<RefreshToken> RefreshTokens { get; set; }
    DbSet<Board> Boards { get; set; }
    DbSet<BoardList> Lists { get; set; }
    DbSet<TaskItem> Tasks { get; set; }
    DbSet<AuditLog> AuditLogs { get; set; }

    // Phase 2
    DbSet<Team> Teams { get; set; }
    DbSet<TeamMember> TeamMembers { get; set; }
    DbSet<Comment> Comments { get; set; }
    DbSet<TaskAttachment> TaskAttachments { get; set; }
    DbSet<TaskActivityLog> TaskActivityLogs { get; set; }
    DbSet<NotificationType> NotificationTypes { get; set; }
    DbSet<NotificationTemplate> NotificationTemplates { get; set; }
    DbSet<UserNotificationPreference> UserNotificationPreferences { get; set; }
    DbSet<Notification> Notifications { get; set; }
    DbSet<NotificationDelivery> NotificationDeliveries { get; set; }
    DbSet<EmailTrackingToken> EmailTrackingTokens { get; set; }
    DbSet<TaskInvitation> TaskInvitations { get; set; }

    // Phase 3
    DbSet<Project> Projects { get; set; }
    DbSet<ProjectMember> ProjectMembers { get; set; }
    DbSet<ProjectInvitation> ProjectInvitations { get; set; }
    DbSet<ProjectActivityLog> ProjectActivityLogs { get; set; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
'@

Set-Content "TaskHub.Application\Data\IAppDbContext.cs" -Value $iDbContextContent -Encoding UTF8
Write-Host "  [OK] IAppDbContext.cs rewritten" -ForegroundColor Green

Write-Host "=== STEP 2: Fix Repository Interface namespaces (Application, not Infrastructure) ===" -ForegroundColor Cyan

$repoInterfaceFiles = Get-ChildItem -Path "TaskHub.Application\Repositories\Interfaces" -Filter "*.cs"
foreach ($f in $repoInterfaceFiles) {
    $content = Get-Content $f.FullName -Raw
    $content = $content -replace "namespace TaskHub\.Infrastructure\.Repositories\.Interfaces", "namespace TaskHub.Application.Repositories.Interfaces"
    Set-Content $f.FullName -Value $content -Encoding UTF8
    Write-Host "  [OK] Fixed namespace: $($f.Name)" -ForegroundColor Green
}

Write-Host "=== STEP 3: Fix Infrastructure Repositories to implement Application interfaces ===" -ForegroundColor Cyan

$infraRepoFiles = Get-ChildItem -Path "TaskHub.Infrastructure\Repositories" -Filter "*.cs" -File
foreach ($f in $infraRepoFiles) {
    $content = Get-Content $f.FullName -Raw
    # Fix using statements
    $content = $content -replace "using TaskHub\.Infrastructure\.Repositories\.Interfaces;", "using TaskHub.Application.Repositories.Interfaces;"
    # Fix namespace from Infrastructure to keep Infrastructure (that's correct - only the interface moves)
    Set-Content $f.FullName -Value $content -Encoding UTF8
    Write-Host "  [OK] Fixed repo: $($f.Name)" -ForegroundColor Green
}

Write-Host "=== STEP 4: Fix ALL using statements in Application Services ===" -ForegroundColor Cyan

$serviceFiles = Get-ChildItem -Path "TaskHub.Application\Services" -Filter "*.cs" -Recurse
foreach ($f in $serviceFiles) {
    $content = Get-Content $f.FullName -Raw
    # Fix using statements
    $content = $content -replace "using TaskHub\.Infrastructure\.Data;", "using TaskHub.Application.Data;"
    $content = $content -replace "using TaskHub\.Infrastructure\.Repositories\.Interfaces;", "using TaskHub.Application.Repositories.Interfaces;"
    # Fix AppDbContext references to use IAppDbContext namespace
    Set-Content $f.FullName -Value $content -Encoding UTF8
    Write-Host "  [OK] Fixed service: $($f.Name)" -ForegroundColor Green
}

Write-Host "=== STEP 5: Fix ProjectService.cs char literal (re-check) ===" -ForegroundColor Cyan

$psPath = "TaskHub.Application\Services\ProjectService.cs"
$psContent = Get-Content $psPath -Raw
if ($psContent -match "Replace\('...',") {
    Write-Host "  [WARN] Possible corrupt char literals detected, rewriting RemoveDiacritics" -ForegroundColor Yellow
}
# Use byte-safe approach - replace via regex any corrupted variants
$psContent = $psContent -replace "value\.Replace\([^)]*\)\.Replace\([^)]*\)\.Normalize", "value.Replace('`u{0111}', 'd').Replace('`u{0110}', 'D').Normalize"
Set-Content $psPath -Value $psContent -Encoding UTF8
Write-Host "  [OK] ProjectService.cs char literals checked" -ForegroundColor Green

Write-Host "=== STEP 6: Ensure Infrastructure.csproj references Application ===" -ForegroundColor Cyan
$infraCsproj = "TaskHub.Infrastructure\TaskHub.Infrastructure.csproj"
$infraContent = Get-Content $infraCsproj -Raw
if ($infraContent -notmatch "TaskHub\.Application") {
    $infraContent = $infraContent -replace "(<ProjectReference.*TaskHub\.Domain.*/>)", '$1
    <ProjectReference Include="..\TaskHub.Application\TaskHub.Application.csproj" />'
    Set-Content $infraCsproj -Value $infraContent -Encoding UTF8
    Write-Host "  [OK] Added Application reference to Infrastructure.csproj" -ForegroundColor Green
} else {
    Write-Host "  [SKIP] Application reference already exists in Infrastructure.csproj" -ForegroundColor Gray
}

Write-Host "`n=== ALL DONE. Running build... ===" -ForegroundColor Cyan
