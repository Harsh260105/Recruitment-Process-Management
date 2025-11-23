using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RecruitmentSystem.Core.Entities
{
    public class RefreshToken : BaseEntity
    {
        [Required]
        [MaxLength(256)]
        public string TokenHash { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        [MaxLength(64)]
        public string? CreatedByIp { get; set; }
        [MaxLength(512)]
        public string? UserAgent { get; set; }
        public DateTime? RevokedAt { get; set; }
        [MaxLength(64)]
        public string? RevokedByIp { get; set; }
        [MaxLength(256)]
        public string? ReplacedByTokenHash { get; set; }
        [MaxLength(256)]
        public string? ReasonRevoked { get; set; }

        public Guid UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        public bool IsActive => RevokedAt == null && DateTime.UtcNow <= ExpiresAt;
    }
}
