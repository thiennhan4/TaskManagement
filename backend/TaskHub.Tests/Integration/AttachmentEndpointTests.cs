using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using TaskHub.Tests.Fixtures;
using Xunit;

namespace TaskHub.Tests.Integration;

public class AttachmentEndpointTests
{
    private static MultipartFormDataContent Upload(byte[] content, string name)
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        form.Add(file, "file", name);
        return form;
    }

    [Fact]
    public async Task Attachment_IsPrivate_ParentBound_AndDownloadableOnlyWithAccess()
    {
        await using var factory = new Phase1ApiFactory();
        var seed = await factory.SeedTeamAsync();
        var r = seed.Resources;
        using var owner = factory.Client(r.Owner);
        var bytes = Encoding.UTF8.GetBytes("Synthetic file content");
        using var form = Upload(bytes, "test.txt");
        using var uploaded = await owner.PostAsync($"/api/tasks/{r.Task}/attachments", form);
        Assert.Equal(HttpStatusCode.OK, uploaded.StatusCode);
        var body = await uploaded.Content.ReadAsStringAsync();
        Assert.False(body.Contains(factory.AttachmentRoot, StringComparison.OrdinalIgnoreCase));
        using var json = JsonDocument.Parse(body);
        var attachment = json.RootElement.GetProperty("data");
        var id = attachment.GetProperty("id").GetGuid();
        var url = attachment.GetProperty("fileUrl").GetString()!;
        Assert.Equal($"/api/v1/tasks/{r.Task}/attachments/{id}/download", url);
        Assert.Equal(url, attachment.GetProperty("filePath").GetString());
        using var download = await owner.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal(bytes, await download.Content.ReadAsByteArrayAsync());
        Assert.Equal("text/plain", download.Content.Headers.ContentType!.MediaType);
        Assert.Equal("attachment", download.Content.Headers.ContentDisposition!.DispositionType);
        Assert.True(download.Headers.CacheControl!.NoStore);
        using var outsider = factory.Client(r.Outsider);
        using var denied = await outsider.GetAsync(url);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        using var anonymous = factory.Client();
        using var unauthenticated = await anonymous.GetAsync(url);
        Assert.Equal(HttpStatusCode.Unauthorized, unauthenticated.StatusCode);
        using var otherTask = await owner.PostAsJsonAsync($"/api/tasks/lists/{r.List}/tasks", new { title="Other parent" });
        otherTask.EnsureSuccessStatusCode();
        using var otherJson = JsonDocument.Parse(await otherTask.Content.ReadAsStringAsync());
        var otherId = otherJson.RootElement.GetProperty("data").GetProperty("id").GetGuid();
        using var existingWrongParent = await owner.GetAsync($"/api/v1/tasks/{otherId}/attachments/{id}/download");
        Assert.Equal(HttpStatusCode.NotFound, existingWrongParent.StatusCode);
        using var existingWrongDelete = await owner.DeleteAsync($"/api/tasks/{otherId}/attachments/{id}");
        Assert.Equal(HttpStatusCode.NotFound, existingWrongDelete.StatusCode);
        using var wrongParent = await owner.GetAsync($"/api/v1/tasks/{Guid.NewGuid()}/attachments/{id}/download");
        Assert.Equal(HttpStatusCode.NotFound, wrongParent.StatusCode);
        using var member = factory.Client(seed.Member);
        using var metadata = await member.GetAsync($"/api/tasks/{r.Task}/attachments");
        using var metadataJson = JsonDocument.Parse(await metadata.Content.ReadAsStringAsync());
        Assert.False(metadataJson.RootElement.GetProperty("data")[0].GetProperty("canDelete").GetBoolean());
        using var deleteDenied = await member.DeleteAsync($"/api/tasks/{r.Task}/attachments/{id}");
        Assert.Equal(HttpStatusCode.Forbidden, deleteDenied.StatusCode);
        using var wrongDelete = await owner.DeleteAsync($"/api/tasks/{Guid.NewGuid()}/attachments/{id}");
        Assert.Equal(HttpStatusCode.NotFound, wrongDelete.StatusCode);
        using var removed = await owner.DeleteAsync($"/api/tasks/{r.Task}/attachments/{id}");
        Assert.Equal(HttpStatusCode.OK, removed.StatusCode);
        using var missing = await owner.GetAsync(url);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Theory]
    [InlineData("fake.png", 4)]
    [InlineData("script.html", 4)]
    [InlineData("large.txt", 10485761)]
    [InlineData("empty.txt", 0)]
    public async Task Upload_RejectsInvalidContentAndSize(string name, int length)
    {
        await using var factory = new Phase1ApiFactory();
        var seed = await factory.SeedAsync();
        using var client = factory.Client(seed.Owner);
        using var form = Upload(new byte[length], name);
        using var response = await client.PostAsync($"/api/tasks/{seed.Task}/attachments", form);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.False(Directory.Exists(factory.AttachmentRoot));
    }
}
