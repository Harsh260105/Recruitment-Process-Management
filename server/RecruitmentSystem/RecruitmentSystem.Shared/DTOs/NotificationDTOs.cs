using System.ComponentModel.DataAnnotations;

namespace RecruitmentSystem.Shared.DTOs
{
    public class CreateNotificationDto
    {
        [Required]
        [StringLength(200)]
        public required string Title { get; set; }

        [Required]
        [StringLength(2000)]
        public required string Message { get; set; }

        [StringLength(100)]
        public string? Type { get; set; }

        [Required]
        [MinLength(1)]
        public required List<Guid> RecipientUserIds { get; set; }
    }

    public class NotificationCreateResponseDto
    {
        public Guid NotificationId { get; set; }
    }

    public class NotificationResponseDto
    {
        public Guid NotificationId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Type { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class MarkAllNotificationsReadResponseDto
    {
        public int MarkedCount { get; set; }
    }
}