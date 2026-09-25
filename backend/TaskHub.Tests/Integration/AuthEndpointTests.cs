using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TaskHub.Infrastructure.Data;
using Xunit;
using NSubstitute;
using TaskHub.Application.Services.Interfaces;

namespace TaskHub.Tests.Integration;

public class AuthEndpointTests
{
    // Do not let assertion diagnostics dump cookies or token-bearing collections.
#pragma warning disable xUnit2012
    private static string RandomValue() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    [Fact]
    public async Task RegisterLoginRefreshPasswordAndLogout_PreserveAuthenticationLifecycle()
    {
        await using var factory = new AuthApiFactory();
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
        var email = $"{Guid.NewGuid():N}@example.invalid";
        var password = RandomValue();
        var registration = new { email, password, fullName = "Auth Test" };
        using var register = await client.PostAsJsonAsync("/api/auth/register", registration);
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);
        var registered = await ReadAuth(register);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.SingleAsync(u => u.Email == email);
            Assert.True(BCrypt.Net.BCrypt.Verify(password, user.PasswordHash));
            var token = await db.RefreshTokens.SingleAsync(t => t.UserId == user.Id);
            Assert.True(token.Token == registered.Refresh);
            Assert.Equal(64, Convert.FromBase64String(token.Token).Length);
            Assert.InRange(token.ExpiresAt - DateTime.UtcNow, TimeSpan.FromDays(6.99), TimeSpan.FromDays(7));
        }
        using var duplicate = await client.PostAsJsonAsync("/api/auth/register", registration);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        using var wrong = await client.PostAsJsonAsync("/api/auth/login", new { email, password = RandomValue() });
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        using var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var loggedIn = await ReadAuth(login);
        Assert.True(loggedIn.Refresh != registered.Refresh);
        using var oldRegistration = await Refresh(client, registered.Refresh);
        Assert.Equal(HttpStatusCode.Unauthorized, oldRegistration.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loggedIn.Access);
        using var me = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        using var refresh = await Refresh(client, loggedIn.Refresh);
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        var rotated = await ReadAuth(refresh);
        Assert.True(rotated.Refresh != loggedIn.Refresh);
        using var replay = await Refresh(client, loggedIn.Refresh);
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", rotated.Access);
        using var wrongCurrent = await client.PutAsJsonAsync("/api/settings/password", new { currentPassword = RandomValue(), newPassword = RandomValue() });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, wrongCurrent.StatusCode);
        var newPassword = RandomValue();
        using var changed = await client.PutAsJsonAsync("/api/settings/password", new { currentPassword = password, newPassword });
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        using var revoked = await Refresh(client, rotated.Refresh);
        Assert.Equal(HttpStatusCode.Unauthorized, revoked.StatusCode);
        using var oldPassword = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.Unauthorized, oldPassword.StatusCode);
        using var newLogin = await client.PostAsJsonAsync("/api/auth/login", new { email, password = newPassword });
        Assert.Equal(HttpStatusCode.OK, newLogin.StatusCode);
        var final = await ReadAuth(newLogin);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", final.Access);
        using var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        logoutRequest.Headers.Add("Cookie", $"refreshToken={final.Refresh}");
        using var logout = await client.SendAsync(logoutRequest);
        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);
        Assert.True(logout.Headers.GetValues("Set-Cookie").Any(c => c.Contains("expires=Thu, 01 Jan 1970")));
        using var loggedOut = await Refresh(client, final.Refresh);
        Assert.Equal(HttpStatusCode.Unauthorized, loggedOut.StatusCode);
    }

    [Fact]
    public async Task Refresh_RejectsMissingInvalidAndExpiredTokens()
    {
        await using var factory = new AuthApiFactory();
        using var client = factory.CreateClient(new() { HandleCookies = false });
        using var missing = await client.PostAsync("/api/auth/refresh", null);
        Assert.Equal(HttpStatusCode.Unauthorized, missing.StatusCode);
        using var invalid = await Refresh(client, RandomValue());
        Assert.Equal(HttpStatusCode.Unauthorized, invalid.StatusCode);
        using var register = await client.PostAsJsonAsync("/api/auth/register", new { email = $"{Guid.NewGuid():N}@example.invalid", password = RandomValue(), fullName = "Expiry Test" });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);
        var auth = await ReadAuth(register);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.RefreshTokens.SingleAsync()).ExpiresAt = DateTime.UtcNow.AddSeconds(-1);
            await db.SaveChangesAsync();
        }
        using var expired = await Refresh(client, auth.Refresh);
        Assert.Equal(HttpStatusCode.Unauthorized, expired.StatusCode);
    }

    [Fact]
    public async Task Google_MissingConfiguration_ReturnsControlledServiceUnavailable()
    {
        await using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/auth/google", new { credential = RandomValue() });
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Google login is not configured.", json.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Google_Success_ReturnsOnlyPublicAuthFields_AndUsableRefreshCookie()
    {
        await using var factory = new AuthApiFactory();
        var verifier = Substitute.For<IGoogleIdentityVerifier>();
        verifier.VerifyAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new VerifiedGoogleIdentity("synthetic-subject", "google@example.invalid", true,
                "example.invalid", "Google Test", null));
        await using var configured = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["Google:ClientId"] = "synthetic-client" }));
            builder.ConfigureTestServices(services => services.AddSingleton(verifier));
        });
        using var client = configured.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
        using var response = await client.PostAsJsonAsync("/api/auth/google", new { credential = RandomValue() });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var auth = await ReadAuth(response);
        using var refreshed = await Refresh(client, auth.Refresh);
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        await ReadAuth(refreshed);
        await verifier.Received(1).VerifyAsync(Arg.Any<string>(), "synthetic-client", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Google_MalformedToken_ReturnsControlledUnauthorized_WithoutCookie()
    {
        await using var factory = new AuthApiFactory();
        await using var configured = factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["Google:ClientId"] = "synthetic-client" })));
        using var client = configured.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/auth/google", new { credential = Guid.NewGuid().ToString() });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.False(response.Headers.Contains("Set-Cookie"));
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.False(json.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("Invalid Google token.", json.RootElement.GetProperty("message").GetString());
    }

    private static async Task<(string Access, string Refresh)> ReadAuth(HttpResponseMessage response)
    {
        var cookie = response.Headers.GetValues("Set-Cookie").Single();
        Assert.True(cookie.Contains("httponly", StringComparison.OrdinalIgnoreCase));
        Assert.True(cookie.Contains("secure", StringComparison.OrdinalIgnoreCase));
        Assert.True(cookie.Contains("samesite=strict", StringComparison.OrdinalIgnoreCase));
        Assert.True(cookie.Contains("path=/", StringComparison.OrdinalIgnoreCase));
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var data = json.RootElement.GetProperty("data");
        Assert.False(data.TryGetProperty("refreshToken", out _));
        Assert.Equal(new[] { "token", "user" }, data.EnumerateObject().Select(p => p.Name).OrderBy(n => n));
        var allowedUserFields = new[] { "id", "email", "fullName", "avatarUrl", "role", "isActive", "createdAt" };
        Assert.All(data.GetProperty("user").EnumerateObject(), p => Assert.Contains(p.Name, allowedUserFields));
        var refresh = Uri.UnescapeDataString(cookie.Split(';')[0].Substring("refreshToken=".Length));
        Assert.False((await response.Content.ReadAsStringAsync()).Contains(refresh, StringComparison.Ordinal));
        return (data.GetProperty("token").GetString()!, refresh);
    }

    private static async Task<HttpResponseMessage> Refresh(HttpClient client, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie", $"refreshToken={token}");
        return await client.SendAsync(request);
    }

    private sealed class AuthApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _database = Guid.NewGuid().ToString();
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["Google:ClientId"] = "", ["Jwt:Key"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)) }));
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(_database));
            });
        }
    }
}
