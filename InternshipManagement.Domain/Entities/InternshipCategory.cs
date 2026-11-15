namespace InternshipManagement.Domain.Entities
{
    /// <summary>A field or industry that internships can be listed under. Administrators manage the list.</summary>
    public class InternshipCategory
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
