namespace TaskHub.Application.DTOs;

// Public DTOs contain only the authenticated download route, never the private storage key.
public static class AttachmentLinks
{
    public static string Download(Guid taskId, Guid attachmentId) =>
        $"/api/v1/tasks/{taskId}/attachments/{attachmentId}/download";
}
