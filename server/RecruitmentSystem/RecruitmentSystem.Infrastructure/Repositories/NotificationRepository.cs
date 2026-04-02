using Microsoft.EntityFrameworkCore;
using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Core.Interfaces;
using RecruitmentSystem.Infrastructure.Data;

namespace RecruitmentSystem.Infrastructure.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly ApplicationDbContext _context;

        public NotificationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Notification> CreateAsync(Notification notification, IEnumerable<Guid> recipientUserIds)
        {
            var recipients = recipientUserIds
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToList();

            if (recipients.Count == 0)
            {
                throw new ArgumentException("At least one recipient user ID is required.", nameof(recipientUserIds));
            }

            notification.CreatedAt = DateTime.UtcNow;
            notification.UpdatedAt = DateTime.UtcNow;

            _context.Notifications.Add(notification);

            foreach (var userId in recipients)
            {
                _context.UnreadNotifications.Add(new UnreadNotification
                {
                    NotificationId = notification.Id,
                    UserId = userId
                });
            }

            await _context.SaveChangesAsync();
            return notification;
        }

        public async Task<List<Notification>> GetUnreadByUserAsync(Guid userId)
        {
            return await _context.UnreadNotifications
                .AsNoTracking()
                .Where(un => un.UserId == userId)
                .Select(un => un.Notification)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> MarkAsReadAsync(Guid userId, Guid notificationId)
        {
            var unreadNotification = await _context.UnreadNotifications
                .FirstOrDefaultAsync(un => un.UserId == userId && un.NotificationId == notificationId);

            if (unreadNotification == null)
            {
                return false;
            }

            _context.UnreadNotifications.Remove(unreadNotification);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> MarkAllAsReadAsync(Guid userId)
        {
            var unreadNotifications = await _context.UnreadNotifications
                .Where(un => un.UserId == userId)
                .ToListAsync();

            if (unreadNotifications.Count == 0)
            {
                return 0;
            }

            _context.UnreadNotifications.RemoveRange(unreadNotifications);
            await _context.SaveChangesAsync();
            return unreadNotifications.Count;
        }
    }
}