using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TaskHub.backend.Data;
using TaskHub.backend.Middleware;
using TaskHub.backend.Repositories.Interfaces;
using TaskHub.backend.Repositories;
using TaskHub.backend.Services.Interfaces;
using TaskHub.backend.Services;

var builder = WebApplication.CreateBuilder(args);

// ── 1. Database ──
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// ── 2. CORS ──
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

// ── 3. JWT Authentication ──
var keyString = builder.Configuration["Jwt:Key"] ?? "";
var key = Encoding.ASCII.GetBytes(keyString);

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
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "TaskHubServer",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "TaskHubClient",
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// ── 4. Rate Limiting ──
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("AuthRateLimit", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });
    options.RejectionStatusCode = 429;
});

// ── 5. DI — Repositories ──
builder.Services.AddScoped<IBoardRepository, BoardRepository>();
builder.Services.AddScoped<IBoardListRepository, BoardListRepository>();
builder.Services.AddScoped<ITaskItemRepository, TaskItemRepository>();

// ── 6. DI — Services ──
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IBoardService, BoardService>();
builder.Services.AddScoped<IBoardListService, BoardListService>();
builder.Services.AddScoped<ITaskItemService, TaskItemService>();

// ── 7. Controllers + JSON ──
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// ── Global Exception Middleware (first in pipeline) ──
app.UseMiddleware<GlobalExceptionMiddleware>();

// ── Auto-migrate database ──
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
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ⚠️ PRODUCTION REMINDER: Move Jwt:Key to environment variable or Azure Key Vault.
// Never store secrets in appsettings.json for production deployments.

app.Run();
