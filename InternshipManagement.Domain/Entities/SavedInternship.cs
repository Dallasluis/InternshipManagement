namespace InternshipManagement.Domain.Entities
{
    /// <summary>An internship a student bookmarked to come back to.</summary>
    public class SavedInternship
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int InternshipId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
