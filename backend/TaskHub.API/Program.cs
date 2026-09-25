using System.Security.Claims;
using TaskHub.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Threading.RateLimiting;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TaskHub.Infrastructure.Data;
using TaskHub.API.Middleware;
using TaskHub.Application.Repositories.Interfaces;
using TaskHub.Infrastructure.Repositories;
using TaskHub.Application.Services.Interfaces;
using TaskHub.Application.Services;
using TaskHub.Application.Validators;

var builder = WebApplication.CreateBuilder(args);

// â”€â”€ 1. Database â”€â”€
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddScoped<TaskHub.Application.Data.IAppDbContext>(provider =>
    provider.GetRequiredService<AppDbContext>());

// â”€â”€ 2. CORS â”€â”€
builder.Services.AddCors(options =>
{
    options.AddPolicy("TaskHubCors",
        policy => policy
            .WithOrigins(
                "https://localhost:5001",
                "http://localhost:5000",
                "https://localhost:7000",
                "http://localhost:5173"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

// â”€â”€ 3. JWT Authentication â”€â”€


builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = JwtConfiguration.GetIssuer(builder.Configuration),
        ValidAudience = JwtConfiguration.GetAudience(builder.Configuration),
        IssuerSigningKey = new SymmetricSecurityKey(JwtConfiguration.GetSigningKey(builder.Configuration)),
        ClockSkew = TimeSpan.Zero
    };
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = context =>
        {
            if (!Guid.TryParse(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var subject) || subject == Guid.Empty)
                context.Fail("Invalid subject.");
            return Task.CompletedTask;
        },
        OnChallenge = async context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = 401;
            context.Response.Headers.WWWAuthenticate = "Bearer";
            await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail("Authentication required."));
        },
        OnForbidden = async context =>
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail("Access denied."));
        },
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/notification"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// â”€â”€ 4. Rate Limiting â”€â”€
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("AuthRateLimit", context => RateLimitPartition.GetFixedWindowLimiter(
        $"{context.Connection.RemoteIpAddress}:{context.Request.Path.Value?.TrimEnd('/').ToLowerInvariant()}",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
    options.RejectionStatusCode = 429;
    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry)
            ? Math.Ceiling(retry.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture) : "60";
        await context.HttpContext.Response.WriteAsJsonAsync(ApiResponse<object>.Fail("Too many attempts. Please try again later."), ct);
    };
});

// â”€â”€ 5. DI â€” Repositories â”€â”€
builder.Services.AddScoped<IBoardRepository, BoardRepository>();
builder.Services.AddScoped<IBoardReadRepository, BoardReadRepository>();
builder.Services.AddScoped<IBoardListRepository, BoardListRepository>();
builder.Services.AddScoped<ITaskItemRepository, TaskItemRepository>();
builder.Services.AddScoped<IAttachmentStorage, TaskHub.Infrastructure.Storage.LocalAttachmentStorage>();
builder.Services.AddScoped<ITaskCollaborationRepository, TaskCollaborationRepository>();
builder.Services.AddScoped<ITeamRepository, TeamRepository>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();

// â”€â”€ 6. DI â€” Services â”€â”€
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IGoogleIdentityVerifier, TaskHub.Infrastructure.Identity.GoogleIdentityVerifier>();
builder.Services.AddScoped<IBoardService, BoardService>();
builder.Services.AddScoped<ICollaborationReadRepository, CollaborationReadRepository>();
builder.Services.AddScoped<ICollaborationReadService, CollaborationReadService>();
builder.Services.AddScoped<IBoardListService, BoardListService>();
builder.Services.AddScoped<ITaskItemService, TaskItemService>();
builder.Services.AddScoped<ITeamService, TeamService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IRealtimeAccessService, RealtimeAccessService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IProjectService, ProjectService>();

// ── 6.1 DI — Phase 5 & 6 Services ──
builder.Services.AddScoped<ITimeTrackingRepository, TimeTrackingRepository>();
builder.Services.AddScoped<ITimeTrackingService, TimeTrackingService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();

// â”€â”€ 6.5 SignalR â”€â”€
builder.Services.AddSignalR();
builder.Services.AddSingleton<IProtectedHubContext, ProtectedHubContext>();

// â”€â”€ 7. FluentValidation â”€â”€
builder.Services.AddValidatorsFromAssemblyContaining<CreateTaskDtoValidator>();
builder.Services.AddFluentValidationAutoValidation();

// â”€â”€ 8. Controllers + JSON â”€â”€
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = context =>
        new BadRequestObjectResult(ApiResponse<object>.Fail("Invalid request.",
            context.ModelState.Where(entry => entry.Value?.Errors.Count > 0)
                .Select(entry => $"Invalid value for {entry.Key}.").ToList())))
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

//Logging 
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();
JwtConfiguration.GetSigningKey(app.Configuration);
app.Logger.LogInformation("JWT signing key loaded from configuration.");

// â”€â”€ Global Exception Middleware (first in pipeline) â”€â”€
app.UseMiddleware<GlobalExceptionMiddleware>();

// â”€â”€ Auto-migrate database â”€â”€
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("TaskHubCors");
app.UseRouting();

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers();
app.MapHub<TaskHub.Application.Hubs.NotificationHub>("/hubs/notification");

// âš ï¸ PRODUCTION REMINDER: Move Jwt:Key to environment variable or Azure Key Vault.
// Never store secrets in appsettings.json for production deployments.

app.Run();

public partial class Program { }



