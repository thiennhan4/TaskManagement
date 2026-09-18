using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace TaskHub.Tests.Integration;

public class JwtConfigurationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t ")]
    public async Task Startup_RejectsMissingEmptyOrWhitespaceSigningKey(string? key)
    {
        // Run the actual entry point with isolated process configuration. Factory
        // configuration callbacks run after the early startup validation.
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../..")),
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        startInfo.ArgumentList.Add(typeof(Program).Assembly.Location);
        startInfo.Environment["DOTNET_ENVIRONMENT"] = "Testing";
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Testing";
        startInfo.Environment.Remove("Jwt__Key");
        startInfo.Environment.Remove("Jwt:Key");
        if (key is not null)
        {
            startInfo.Environment["Jwt__Key"] = key;
        }

        using var process = Process.Start(startInfo)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }

        var startupOutput = await output + await error;

        Assert.NotEqual(0, process.ExitCode);
        Assert.True(startupOutput.Contains(
            "Jwt:Key must be configured through User Secrets or deployment environment configuration."),
            "Startup must fail with the expected configuration error.");
    }

    [Fact]
    public async Task Startup_AcceptsSigningKeyFromProcessEnvironment()
    {
        Assert.False(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("Jwt__Key")),
            "Run integration tests with an ephemeral Jwt__Key process environment variable.");
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/v1/dashboard/velocity");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
