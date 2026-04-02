using RecruitmentSystem.Shared.DTOs;

namespace RecruitmentSystem.Services.Interfaces
{
    public interface INotificationService
    {
        Task<Guid> CreateAsync(string title, string message, string? type, IEnumerable<Guid> recipientUserIds);
        Task<List<NotificationResponseDto>> GetUnreadByUserAsync(Guid userId);
        Task<bool> MarkAsReadAsync(Guid userId, Guid notificationId);
        Task<int> MarkAllAsReadAsync(Guid userId);
    }
}