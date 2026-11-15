namespace InternshipManagement.Domain.Entities
{
    /// <summary>A formal warning an administrator sent to a company about breaking platform policy.</summary>
    public class CompanyWarning
    {
        public int Id { get; set; }
        public int CompanyProfileId { get; set; }
        public int AdminUserId { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
