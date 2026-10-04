using System.Text;
using Microsoft.Extensions.Configuration;

namespace TaskHub.Application.Services;

public static class JwtConfiguration
{
    public static byte[] GetSigningKey(IConfiguration configuration)
    {
        var value = configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException(
                "Jwt:Key must be configured through User Secrets or deployment environment configuration.");

        // Preserve the existing encoding for compatibility with issued tokens.
        var key = Encoding.ASCII.GetBytes(value);
        if (key.Length < 32)
            throw new InvalidOperationException(
                "Jwt:Key must contain at least 32 bytes for HS256. Configure a cryptographically random signing key.");
        return key;
    }

    public static string GetIssuer(IConfiguration configuration) =>
        string.IsNullOrWhiteSpace(configuration["Jwt:Issuer"]) ? "TaskHubServer" : configuration["Jwt:Issuer"]!;

    public static string GetAudience(IConfiguration configuration) =>
        string.IsNullOrWhiteSpace(configuration["Jwt:Audience"]) ? "TaskHubClient" : configuration["Jwt:Audience"]!;
}
