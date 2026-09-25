using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using TaskHub.Application.DTOs;
using TaskHub.Application.Repositories.Interfaces;
using TaskHub.Application.Services;
using TaskHub.Application.Services.Interfaces;
using TaskHub.Domain.Entities;
using TaskHub.Domain.Exceptions;
using Xunit;

namespace TaskHub.Tests.Services;

public class GoogleIdentityPolicyTests
{
    [Theory]
    [InlineData("gmail.com", null, true, "subject", true)]
    [InlineData("example.invalid", "example.invalid", true, "subject", true)]
    [InlineData("example.invalid", null, true, "subject", false)]
    [InlineData("example.invalid", "other.invalid", true, "subject", false)]
    [InlineData("gmail.com", null, false, "subject", false)]
    [InlineData("gmail.com", null, true, "", false)]
    public async Task OnlyVerifiedGoogleAuthoritativeEmailCanAuthenticate(string domain, string? hostedDomain, bool verified, string subject, bool allowed)
    {
        var users = Substitute.For<IUserRepository>();
        var tokens = Substitute.For<ITokenService>();
        var verifier = Substitute.For<IGoogleIdentityVerifier>();
        var email = $"test@{domain}";
        var user = new AppUser { Email = email, FullName = "Test", IsActive = true };
        users.GetByEmailAsync(email, Arg.Any<CancellationToken>()).Returns(user);
        tokens.GenerateJwtToken(user).Returns("synthetic-access");
        tokens.GenerateRefreshTokenAsync(user.Id, Arg.Any<CancellationToken>()).Returns(new RefreshToken { Token = "synthetic-refresh" });
        verifier.VerifyAsync(Arg.Any<string>(), "synthetic-client", Arg.Any<CancellationToken>())
            .Returns(new VerifiedGoogleIdentity(subject, email, verified, hostedDomain, "Test", null));
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Google:ClientId"] = "synthetic-client" }).Build();
        var service = new AuthService(users, tokens, NullLogger<AuthService>.Instance, config, verifier);
        if (allowed)
        {
            var result = await service.GoogleLoginAsync(new GoogleAuthDto { Credential = "synthetic-input" });
            Assert.Equal(user.Id, result.User.Id);
        }
        else
        {
            await Assert.ThrowsAnyAsync<AppException>(() => service.GoogleLoginAsync(new GoogleAuthDto { Credential = "synthetic-input" }));
            Assert.Empty(tokens.ReceivedCalls());
            Assert.Empty(users.ReceivedCalls());
        }
    }
}
