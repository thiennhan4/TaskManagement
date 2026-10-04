namespace TaskHub.Application.Services.Interfaces;
public record StoredAttachment(string Key, string ContentType, long Length);
public interface IAttachmentStorage
{
    Task<StoredAttachment> StoreAsync(Stream content, string fileName, CancellationToken ct);
    Task<Stream> OpenAsync(string key, CancellationToken ct);
    Task DeleteAsync(string key, CancellationToken ct);
}
