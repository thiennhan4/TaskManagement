using System.Net;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace TaskHub.Tests.Integration;

public class JwtConfigurationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t ")]
    [InlineData("invalid-short-test-value")]
    public void Startup_RejectsMissingOrUndersizedSigningKey(string? key)
    {
        using var factory = Factory(key);
        var error = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());
        Assert.Contains("Jwt:Key", error.Message);
    }

    [Fact]
    public async Task Startup_AcceptsExternalSigningKey()
    {
        await using var factory = Factory(Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)));
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/v1/dashboard/velocity");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static WebApplicationFactory<Program> Factory(string? key) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["Jwt:Key"] = key }));
        });
}
