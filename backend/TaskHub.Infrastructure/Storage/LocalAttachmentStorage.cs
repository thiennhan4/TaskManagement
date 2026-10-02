using System.IO.Compression;
using System.Text;
using Microsoft.Extensions.Configuration;
using TaskHub.Application.Services.Interfaces;
using TaskHub.Domain.Exceptions;
namespace TaskHub.Infrastructure.Storage;

public sealed class LocalAttachmentStorage(IConfiguration config) : IAttachmentStorage
{
    private const int Limit=10*1024*1024;
    private readonly string _root=Path.GetFullPath(config["Attachments:PrivateRoot"] ?? Path.Combine(AppContext.BaseDirectory,"private-attachments"));
    private string Resolve(string key)
    {
        if(string.IsNullOrWhiteSpace(key) || Path.GetFileName(key)!=key || !Guid.TryParseExact(Path.GetFileNameWithoutExtension(key),"N",out _))
            throw new NotFoundException("Attachment file unavailable.");
        var path=Path.GetFullPath(Path.Combine(_root,key));
        if(!path.StartsWith(_root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)) throw new NotFoundException("Attachment file unavailable.");
        if(File.Exists(path) && (File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0) throw new NotFoundException("Attachment file unavailable.");
        return path;
    }
    public async Task<StoredAttachment> StoreAsync(Stream content,string fileName,CancellationToken ct)
    {
        using var buffer=new MemoryStream();
        var block=new byte[81920];
        int read;
        while((read=await content.ReadAsync(block,ct))>0)
        {
            if(buffer.Length+read>Limit) throw new BusinessValidationException("File exceeds 10MB limit.");
            await buffer.WriteAsync(block.AsMemory(0,read),ct);
        }
        var bytes=buffer.ToArray();
        if(bytes.Length==0) throw new BusinessValidationException("File is empty.");
        var extension=Path.GetExtension(fileName).ToLowerInvariant();
        var mime=Validate(bytes,extension);
        Directory.CreateDirectory(_root);
        var key=Guid.NewGuid().ToString("N")+extension;
        var path = Resolve(key);
        try
        {
            await File.WriteAllBytesAsync(path, bytes, ct);
        }
        catch
        {
            // Store has not returned a key yet, so the service cannot compensate this write.
            File.Delete(path);
            throw;
        }
        return new(key,mime,bytes.Length);
    }
    private static string Validate(byte[] bytes,string extension)
    {
        bool Starts(params byte[] prefix)=>bytes.AsSpan().StartsWith(prefix);
        switch(extension)
        {
            case ".pdf" when Starts(0x25,0x50,0x44,0x46,0x2D): return "application/pdf";
            case ".png" when Starts(137,80,78,71,13,10,26,10): return "image/png";
            case ".jpg" or ".jpeg" when Starts(255,216,255): return "image/jpeg";
            case ".doc" when Starts(208,207,17,224,161,177,26,225): return "application/msword";
            case ".docx":
                try
                {
                    using var zip=new ZipArchive(new MemoryStream(bytes),ZipArchiveMode.Read);
                    if(zip.GetEntry("[Content_Types].xml")!=null && zip.GetEntry("word/document.xml")!=null && !zip.Entries.Any(e=>e.FullName.EndsWith("vbaProject.bin",StringComparison.OrdinalIgnoreCase)))
                        return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                }
                catch(InvalidDataException) { }
                break;
            case ".txt":
                try { var text=new UTF8Encoding(false,true).GetString(bytes); if(!text.Contains('\0')) return "text/plain"; }
                catch(DecoderFallbackException) { }
                break;
        }
        throw new BusinessValidationException("Unsupported file type or file contents do not match its extension.");
    }
    public Task<Stream> OpenAsync(string key,CancellationToken ct)
    {
        var path=Resolve(key);
        if(!File.Exists(path)) throw new NotFoundException("Attachment file unavailable.");
        return Task.FromResult<Stream>(new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read,81920,FileOptions.Asynchronous));
    }
    public Task DeleteAsync(string key,CancellationToken ct)
    {
        File.Delete(Resolve(key)); return Task.CompletedTask;
    }
}
