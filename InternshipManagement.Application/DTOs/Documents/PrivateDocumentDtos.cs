namespace InternshipManagement.Application.DTOs.Documents
{
    public sealed record PrivateDocumentResponse(
        Guid Id,
        string OriginalFileName,
        string Category,
        string ContentType,
        long Length,
        string RetentionState,
        DateTimeOffset CreatedAt);

    public sealed record DocumentAccessLinkResponse(string Url, DateTimeOffset ExpiresAt);
}
