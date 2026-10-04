namespace TaskHub.Application.Models;

// Internal stream result; the controller owns disposal through FileStreamResult.
public sealed record AttachmentDownload(Stream Content, string FileName, string ContentType);
