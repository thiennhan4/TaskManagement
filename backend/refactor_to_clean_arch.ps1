$ErrorActionPreference = "Stop"

$backendDir = "d:\web\TaskHub\backend"
Set-Location $backendDir

Write-Host "Creating new projects..."
dotnet new sln -n TaskHub -o . --force

dotnet new classlib -n TaskHub.Domain -o TaskHub.Domain
dotnet new classlib -n TaskHub.Application -o TaskHub.Application
dotnet new classlib -n TaskHub.Infrastructure -o TaskHub.Infrastructure
dotnet new webapi -n TaskHub.API -o TaskHub.API

# Remove default Class1.cs
Remove-Item TaskHub.Domain\Class1.cs -ErrorAction SilentlyContinue
Remove-Item TaskHub.Application\Class1.cs -ErrorAction SilentlyContinue
Remove-Item TaskHub.Infrastructure\Class1.cs -ErrorAction SilentlyContinue

Write-Host "Adding projects to solution..."
dotnet sln add TaskHub.Domain\TaskHub.Domain.csproj
dotnet sln add TaskHub.Application\TaskHub.Application.csproj
dotnet sln add TaskHub.Infrastructure\TaskHub.Infrastructure.csproj
dotnet sln add TaskHub.API\TaskHub.API.csproj

Write-Host "Setting up project references..."
dotnet add TaskHub.Application\TaskHub.Application.csproj reference TaskHub.Domain\TaskHub.Domain.csproj
dotnet add TaskHub.Infrastructure\TaskHub.Infrastructure.csproj reference TaskHub.Domain\TaskHub.Domain.csproj
dotnet add TaskHub.Infrastructure\TaskHub.Infrastructure.csproj reference TaskHub.Application\TaskHub.Application.csproj
dotnet add TaskHub.API\TaskHub.API.csproj reference TaskHub.Application\TaskHub.Application.csproj
dotnet add TaskHub.API\TaskHub.API.csproj reference TaskHub.Infrastructure\TaskHub.Infrastructure.csproj

Write-Host "Moving files to Domain..."
if (Test-Path "Models") { Move-Item -Path "Models" -Destination "TaskHub.Domain\Entities" }
if (Test-Path "Exceptions") { Move-Item -Path "Exceptions" -Destination "TaskHub.Domain\Exceptions" }

Write-Host "Moving files to Application..."
if (Test-Path "DTOs") { Move-Item -Path "DTOs" -Destination "TaskHub.Application\DTOs" }
if (Test-Path "Services") { Move-Item -Path "Services" -Destination "TaskHub.Application\Services" }
if (Test-Path "Validators") { Move-Item -Path "Validators" -Destination "TaskHub.Application\Validators" }

Write-Host "Moving files to Infrastructure..."
if (Test-Path "Repositories") { Move-Item -Path "Repositories" -Destination "TaskHub.Infrastructure\Repositories" }
if (Test-Path "Data") { Move-Item -Path "Data" -Destination "TaskHub.Infrastructure\Data" }
if (Test-Path "Migrations") { Move-Item -Path "Migrations" -Destination "TaskHub.Infrastructure\Migrations" }

Write-Host "Moving files to API..."
if (Test-Path "Controllers") { Move-Item -Path "Controllers" -Destination "TaskHub.API\Controllers" -Force }
if (Test-Path "Hubs") { Move-Item -Path "Hubs" -Destination "TaskHub.API\Hubs" -Force }
if (Test-Path "Middleware") { Move-Item -Path "Middleware" -Destination "TaskHub.API\Middleware" -Force }
if (Test-Path "Program.cs") { Move-Item -Path "Program.cs" -Destination "TaskHub.API\Program.cs" -Force }
if (Test-Path "appsettings.json") { Move-Item -Path "appsettings.json" -Destination "TaskHub.API\appsettings.json" -Force }
if (Test-Path "appsettings.Development.json") { Move-Item -Path "appsettings.Development.json" -Destination "TaskHub.API\appsettings.Development.json" -Force }
if (Test-Path "TaskHub.http") { Move-Item -Path "TaskHub.http" -Destination "TaskHub.API\TaskHub.http" -Force }
if (Test-Path "Properties") { Move-Item -Path "Properties" -Destination "TaskHub.API\Properties" -Force }

Write-Host "Adding nuget packages..."
# Extract packages from old csproj
$oldCsproj = Get-Content "TaskHub.csproj" -Raw
if ($oldCsproj -match 'PackageReference Include="BCrypt.Net-Next"') { dotnet add TaskHub.Infrastructure\TaskHub.Infrastructure.csproj package BCrypt.Net-Next -v 4.1.0 }
if ($oldCsproj -match 'FluentValidation.AspNetCore') { dotnet add TaskHub.Application\TaskHub.Application.csproj package FluentValidation.AspNetCore -v 11.3.1 }
if ($oldCsproj -match 'Microsoft.EntityFrameworkCore.SqlServer') { dotnet add TaskHub.Infrastructure\TaskHub.Infrastructure.csproj package Microsoft.EntityFrameworkCore.SqlServer -v 9.0.2 }
if ($oldCsproj -match 'Microsoft.EntityFrameworkCore.Tools') { dotnet add TaskHub.Infrastructure\TaskHub.Infrastructure.csproj package Microsoft.EntityFrameworkCore.Tools -v 9.0.2 }
if ($oldCsproj -match 'Microsoft.AspNetCore.Authentication.JwtBearer') { dotnet add TaskHub.API\TaskHub.API.csproj package Microsoft.AspNetCore.Authentication.JwtBearer -v 9.0.2 }

Write-Host "Updating Namespaces in C# files..."
$files = Get-ChildItem -Recurse -Filter *.cs
foreach ($f in $files) {
    $content = Get-Content $f.FullName -Raw
    # Simple replace
    $content = $content -replace "namespace TaskHub\.Models", "namespace TaskHub.Domain.Entities"
    $content = $content -replace "using TaskHub\.Models", "using TaskHub.Domain.Entities"
    
    $content = $content -replace "namespace TaskHub\.Exceptions", "namespace TaskHub.Domain.Exceptions"
    $content = $content -replace "using TaskHub\.Exceptions", "using TaskHub.Domain.Exceptions"
    
    $content = $content -replace "namespace TaskHub\.DTOs", "namespace TaskHub.Application.DTOs"
    $content = $content -replace "using TaskHub\.DTOs", "using TaskHub.Application.DTOs"
    
    $content = $content -replace "namespace TaskHub\.Services", "namespace TaskHub.Application.Services"
    $content = $content -replace "using TaskHub\.Services", "using TaskHub.Application.Services"
    
    $content = $content -replace "namespace TaskHub\.Validators", "namespace TaskHub.Application.Validators"
    $content = $content -replace "using TaskHub\.Validators", "using TaskHub.Application.Validators"
    
    $content = $content -replace "namespace TaskHub\.Repositories", "namespace TaskHub.Infrastructure.Repositories"
    $content = $content -replace "using TaskHub\.Repositories", "using TaskHub.Infrastructure.Repositories"
    
    $content = $content -replace "namespace TaskHub\.Data", "namespace TaskHub.Infrastructure.Data"
    $content = $content -replace "using TaskHub\.Data", "using TaskHub.Infrastructure.Data"
    
    $content = $content -replace "namespace TaskHub\.Controllers", "namespace TaskHub.API.Controllers"
    
    # Ensure they also use the correct namespace across boundaries
    Set-Content -Path $f.FullName -Value $content -Encoding UTF8
}

Write-Host "Cleanup old files..."
Remove-Item -Force TaskHub.csproj
Remove-Item -Recurse -Force bin, obj -ErrorAction SilentlyContinue

Write-Host "Done!"
