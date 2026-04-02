using RecruitmentSystem.Core.Entities;

namespace RecruitmentSystem.Core.Interfaces
{
    public interface INotificationRepository
    {
        Task<Notification> CreateAsync(Notification notification, IEnumerable<Guid> recipientUserIds);
        Task<List<Notification>> GetUnreadByUserAsync(Guid userId);
        Task<bool> MarkAsReadAsync(Guid userId, Guid notificationId);
        Task<int> MarkAllAsReadAsync(Guid userId);
    }
}