using System.ComponentModel.DataAnnotations;

namespace RecruitmentSystem.Core.Entities
{
    public class Notification : BaseEntity
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Message { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Type { get; set; }

        public virtual ICollection<UnreadNotification> UnreadNotifications { get; set; } = new List<UnreadNotification>();
    }
}