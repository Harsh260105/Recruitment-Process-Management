using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RecruitmentSystem.Core.Enums;

namespace RecruitmentSystem.Core.Entities
{
    public class ApplicationStatusHistory : BaseEntity
    {
        [ForeignKey("JobApplication")]
        public Guid JobApplicationId { get; set; }

        [Required]
        public ApplicationStatus FromStatus { get; set; }

        [Required]
        public ApplicationStatus ToStatus { get; set; }

        [ForeignKey("ChangedByUser")]
        public Guid ChangedByUserId { get; set; } // Who made the status change

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        [StringLength(500)]
        public string? Comments { get; set; } // Brief reason for change

        // Navigation Properties
        public virtual required JobApplication JobApplication { get; set; }
        public virtual required User ChangedByUser { get; set; }
    }
}