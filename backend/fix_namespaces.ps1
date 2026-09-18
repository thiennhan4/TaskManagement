$ErrorActionPreference = "Stop"
Set-Location "d:\web\TaskHub\backend"

$files = Get-ChildItem -Recurse -Filter *.cs
foreach ($f in $files) {
    $content = Get-Content $f.FullName -Raw
    $content = $content -replace "TaskHub\.backend\.Models", "TaskHub.Domain.Entities"
    $content = $content -replace "TaskHub\.backend\.Exceptions", "TaskHub.Domain.Exceptions"
    $content = $content -replace "TaskHub\.backend\.DTOs", "TaskHub.Application.DTOs"
    $content = $content -replace "TaskHub\.backend\.Services", "TaskHub.Application.Services"
    $content = $content -replace "TaskHub\.backend\.Validators", "TaskHub.Application.Validators"
    $content = $content -replace "TaskHub\.backend\.Repositories", "TaskHub.Infrastructure.Repositories"
    $content = $content -replace "TaskHub\.backend\.Data", "TaskHub.Infrastructure.Data"
    $content = $content -replace "TaskHub\.backend\.Controllers", "TaskHub.API.Controllers"
    $content = $content -replace "TaskHub\.backend\.Hubs", "TaskHub.Application.Hubs"
    $content = $content -replace "TaskHub\.backend", "TaskHub.API" # generic fallback for Program.cs etc
    Set-Content $f.FullName -Value $content -Encoding UTF8
}

Write-Host "Namespaces updated successfully."
