namespace InternshipManagement.Web.Models.Documents
{
    public sealed class PrivateDocumentResponse
    {
        public Guid Id { get; set; }
        public string OriginalFileName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long Length { get; set; }
        public string RetentionState { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
    }

    public sealed class DocumentAccessLinkResponse
    {
        public string Url { get; set; } = string.Empty;
        public DateTimeOffset ExpiresAt { get; set; }
    }

    public sealed class DocumentFileResponse
    {
        public byte[] Content { get; set; } = Array.Empty<byte>();
        public string ContentType { get; set; } = "application/octet-stream";
        public string FileName { get; set; } = "document";
    }
}
