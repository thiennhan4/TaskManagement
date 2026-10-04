using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using TaskHub.Application.Repositories.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TaskHub.Domain.Entities;
using TaskHub.Application.Services.Interfaces;

namespace TaskHub.Application.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly IRefreshTokenRepository _repository;

    public TokenService(IConfiguration configuration, IRefreshTokenRepository repository)
    {
        _configuration = configuration;
        _repository = repository;
    }

    public string GenerateJwtToken(AppUser user)
    {
        var key = JwtConfiguration.GetSigningKey(_configuration);

        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),  // sub
                new Claim(ClaimTypes.Email, user.Email),                    // email
                new Claim(ClaimTypes.Role, user.Role.ToString())            // role
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = JwtConfiguration.GetIssuer(_configuration),
            Audience = JwtConfiguration.GetAudience(_configuration),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public async Task<RefreshToken> GenerateRefreshTokenAsync(Guid userId, CancellationToken ct = default)
    {
        var refreshToken = new RefreshToken
        {
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(refreshToken, ct);
        return refreshToken;
    }

    public async Task<RefreshToken?> RotateRefreshTokenAsync(string original, Guid userId, CancellationToken ct = default)
    {
        var replacement = new RefreshToken { UserId = userId,
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)), ExpiresAt = DateTime.UtcNow.AddDays(7) };
        return await _repository.TryRotateAsync(original, replacement, ct) ? replacement : null;
    }

    public Task<RefreshToken?> ValidateRefreshTokenAsync(string token, CancellationToken ct = default) =>
        _repository.GetValidAsync(token, ct);

    public Task RevokeRefreshTokenAsync(string token, CancellationToken ct = default) =>
        _repository.RevokeAsync(token, ct);

    public Task RevokeAllUserTokensAsync(Guid userId, CancellationToken ct = default) =>
        _repository.RevokeAllAsync(userId, ct);
}
