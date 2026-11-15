using InternshipManagement.Application.DTOs.Notifications;

namespace InternshipManagement.Application.Interfaces
{
    /// <summary>Pushes a freshly saved notification to the user's open browser tabs. Never throws and never waits.</summary>
    public interface INotificationPublisher
    {
        void Publish(int userId, NotificationResponse notification, int unreadCount);
    }
}
