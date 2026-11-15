using InternshipManagement.Application.DTOs.Notifications;

namespace InternshipManagement.Application.Interfaces
{
    /// <summary>Notification types, used by the page to pick an icon.</summary>
    public static class NotificationTypes
    {
        public const string Application = "Application";
        public const string Interview = "Interview";
        public const string Offer = "Offer";
        public const string Placement = "Placement";
        public const string Report = "Report";
        public const string Extension = "Extension";
        public const string Withdrawal = "Withdrawal";
        public const string Verification = "Verification";
        public const string Moderation = "Moderation";
        public const string Evaluation = "Evaluation";
        public const string Recommendation = "Recommendation";
        public const string General = "General";
    }

    public interface INotificationService
    {
        /// <summary>
        /// Adds a notification for a user and, when asked, emails it too. This never throws: a failed
        /// notification must not undo or break the action that caused it.
        /// </summary>
        Task NotifyAsync(
            int userId, string type, string title, string message,
            string? linkPath = null, bool sendEmail = false, CancellationToken cancellationToken = default);

        Task<List<NotificationResponse>> GetAsync(int userId, bool unreadOnly, int take, CancellationToken cancellationToken = default);

        Task<int> CountUnreadAsync(int userId, CancellationToken cancellationToken = default);

        /// <summary>Marks one of the user's own notifications as read. False if it is not theirs or does not exist.</summary>
        Task<bool> MarkReadAsync(int userId, long id, CancellationToken cancellationToken = default);

        Task<int> MarkAllReadAsync(int userId, CancellationToken cancellationToken = default);
    }
}
