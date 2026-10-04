namespace TaskHub.Application.Services.Interfaces;
public record VerifiedGoogleIdentity(string Subject, string Email, bool EmailVerified, string? HostedDomain, string? Name, string? Picture);
public interface IGoogleIdentityVerifier
{
    Task<VerifiedGoogleIdentity> VerifyAsync(string credential, string audience, CancellationToken ct = default);
}
