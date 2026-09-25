using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using TaskHub.Application.DTOs;
using TaskHub.Application.Repositories.Interfaces;
using TaskHub.Application.Services;
using TaskHub.Application.Services.Interfaces;
using TaskHub.Domain.Entities;
using TaskHub.Domain.Exceptions;
using TaskHub.Infrastructure.Identity;
using Xunit;

namespace TaskHub.Tests.Services;

public class AuthRegressionTests
{
    // Boolean assertions deliberately keep signing material out of failure output.
#pragma warning disable xUnit2009
    private static IConfiguration Config(string? key, string? issuer = null, string? audience = null) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = key, ["Jwt:Issuer"] = issuer, ["Jwt:Audience"] = audience
        }).Build();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Jwt_RejectsAbsentKey(string? key) =>
        Assert.Throws<InvalidOperationException>(() => JwtConfiguration.GetSigningKey(Config(key)));

    [Fact]
    public void Jwt_RejectsShortKeyBeforeSigning()
    {
        var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        var service = new TokenService(Config(key), Substitute.For<IRefreshTokenRepository>());
        var exception = Assert.Throws<InvalidOperationException>(() => service.GenerateJwtToken(new AppUser()));
        Assert.True(exception.Message.Contains("at least 32 bytes"));
        Assert.False(exception.ToString().Contains(key));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", " ")]
    [InlineData("CustomIssuer", "CustomAudience")]
    public void Jwt_ValidatesSignatureLifetimeAndExistingClaims(string? issuer, string? audience)
    {
        var configuration = Config(Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)), issuer, audience);
        var user = new AppUser { Email = "test@example.invalid", Role = UserRole.Admin };
        var service = new TokenService(configuration, Substitute.For<IRefreshTokenRepository>());
        var encoded = service.GenerateJwtToken(user);
        var principal = new JwtSecurityTokenHandler().ValidateToken(encoded, new TokenValidationParameters
        {
            ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true,
            ValidIssuer = JwtConfiguration.GetIssuer(configuration), ValidAudience = JwtConfiguration.GetAudience(configuration),
            IssuerSigningKey = new SymmetricSecurityKey(JwtConfiguration.GetSigningKey(configuration)), ClockSkew = TimeSpan.Zero
        }, out var validated);
        Assert.Equal(user.Id.ToString(), principal.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal(user.Email, principal.FindFirstValue(ClaimTypes.Email));
        Assert.True(principal.IsInRole("Admin"));
        Assert.InRange(validated.ValidTo - DateTime.UtcNow, TimeSpan.FromMinutes(59), TimeSpan.FromHours(1));
    }

    [Fact]
    public async Task Login_InactiveUser_DoesNotIssueOrRevokeTokens()
    {
        var users = Substitute.For<IUserRepository>();
        var tokens = Substitute.For<ITokenService>();
        var password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(new AppUser
        {
            IsActive = false, PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
        });
        var service = new AuthService(users, tokens, NullLogger<AuthService>.Instance, Config(null), Substitute.For<IGoogleIdentityVerifier>());
        await Assert.ThrowsAsync<ForbiddenException>(() => service.LoginAsync(new LoginDto { Email = "test@example.invalid", Password = password }));
        Assert.Empty(tokens.ReceivedCalls());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Google_RequiresClientIdBeforeValidatingCredential(string? clientId)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Google:ClientId"] = clientId }).Build();
        var users = Substitute.For<IUserRepository>();
        var service = new AuthService(users, Substitute.For<ITokenService>(), NullLogger<AuthService>.Instance, configuration, Substitute.For<IGoogleIdentityVerifier>());
        await Assert.ThrowsAsync<ServiceUnavailableException>(() => service.GoogleLoginAsync(new GoogleAuthDto { Credential = Guid.NewGuid().ToString() }));
        Assert.Empty(users.ReceivedCalls());
    }

    [Fact]
    public async Task Google_ConfiguredClientIdWithoutClientSecret_RejectsMalformedCredential()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Google:ClientId"] = "test.apps.googleusercontent.com"
        }).Build();
        var users = Substitute.For<IUserRepository>();
        var service = new AuthService(users, Substitute.For<ITokenService>(), NullLogger<AuthService>.Instance, configuration, new GoogleIdentityVerifier());
        await Assert.ThrowsAsync<UnauthorizedException>(() => service.GoogleLoginAsync(
            new GoogleAuthDto { Credential = Guid.NewGuid().ToString() }));
        Assert.Empty(users.ReceivedCalls());
    }
}
