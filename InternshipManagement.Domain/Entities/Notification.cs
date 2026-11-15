namespace InternshipManagement.Domain.Entities
{
    /// <summary>One message shown to one user in the site's notification list.</summary>
    public class Notification
    {
        public long Id { get; set; }
        public int UserId { get; set; }

        /// <summary>A NotificationTypes value, used for the icon on the page.</summary>
        public string Type { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;

        /// <summary>A path on the website (for example /Student/ApplicationDetails/5), never an outside address.</summary>
        public string? LinkPath { get; set; }

        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAt { get; set; }
    }
}
