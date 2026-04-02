using System.ComponentModel.DataAnnotations.Schema;

namespace RecruitmentSystem.Core.Entities
{
    public class UnreadNotification
    {
        [ForeignKey("Notification")]
        public Guid NotificationId { get; set; }

        [ForeignKey("User")]
        public Guid UserId { get; set; }

        public virtual Notification Notification { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}