using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Core.Interfaces;
using RecruitmentSystem.Services.Interfaces;
using RecruitmentSystem.Shared.DTOs;

namespace RecruitmentSystem.Services.Implementations
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;

        public NotificationService(INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        public async Task<Guid> CreateAsync(string title, string message, string? type, IEnumerable<Guid> recipientUserIds)
        {
            var notification = new Notification
            {
                Title = title,
                Message = message,
                Type = type
            };

            var createdNotification = await _notificationRepository.CreateAsync(notification, recipientUserIds);
            return createdNotification.Id;
        }

        public async Task<List<NotificationResponseDto>> GetUnreadByUserAsync(Guid userId)
        {
            var notifications = await _notificationRepository.GetUnreadByUserAsync(userId);

            return notifications.Select(n => new NotificationResponseDto
            {
                NotificationId = n.Id,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                CreatedAt = n.CreatedAt
            }).ToList();
        }

        public Task<bool> MarkAsReadAsync(Guid userId, Guid notificationId)
            => _notificationRepository.MarkAsReadAsync(userId, notificationId);

        public Task<int> MarkAllAsReadAsync(Guid userId)
            => _notificationRepository.MarkAllAsReadAsync(userId);
    }
}