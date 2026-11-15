namespace InternshipManagement.Domain.Entities
{
    /// <summary>The contract a company attaches to an offer, so the student can read it before answering.</summary>
    public class OfferContract
    {
        public int Id { get; set; }
        public int InternshipApplicationId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long Length { get; set; }
        public byte[] Content { get; set; } = Array.Empty<byte>();
        public int UploadedByUserId { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
