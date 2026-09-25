using Google.Apis.Auth;
using TaskHub.Application.Services.Interfaces;
using TaskHub.Domain.Exceptions;
namespace TaskHub.Infrastructure.Identity;
public sealed class GoogleIdentityVerifier : IGoogleIdentityVerifier
{
    public async Task<VerifiedGoogleIdentity> VerifyAsync(string credential, string audience, CancellationToken ct = default)
    {
        try
        {
            var payload=await GoogleJsonWebSignature.ValidateAsync(credential,new GoogleJsonWebSignature.ValidationSettings { Audience=new[] { audience } });
            ct.ThrowIfCancellationRequested();
            return new(payload.Subject,payload.Email,payload.EmailVerified,payload.HostedDomain,payload.Name,payload.Picture);
        }
        catch(InvalidJwtException) { throw new UnauthorizedException("Invalid Google token."); }
    }
}
