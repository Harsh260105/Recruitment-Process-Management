using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RecruitmentSystem.Core.Enums;

namespace RecruitmentSystem.Core.Entities
{
    public class JobOffer : BaseEntity
    {
        [ForeignKey("JobApplication")]
        public Guid JobApplicationId { get; set; }

        [Required]
        [Column(TypeName = "decimal(12,2)")]
        public decimal OfferedSalary { get; set; }

        [StringLength(1000)]
        public string? Benefits { get; set; } // Health insurance, PTO, etc.

        [StringLength(100)]
        public string? JobTitle { get; set; } // Might differ from original posting

        public DateTime OfferDate { get; set; } = DateTime.UtcNow;

        public DateTime ExpiryDate { get; set; }

        [Required]
        public OfferStatus Status { get; set; } = OfferStatus.Pending;

        [ForeignKey("ExtendedByUser")]
        public Guid ExtendedByUserId { get; set; } // HR/Manager who extended the offer

        [StringLength(1000)]
        public string? Notes { get; set; }

        public DateTime? JoiningDate { get; set; } // Negotiated start date

        [Column(TypeName = "decimal(12,2)")]
        public decimal? CounterOfferAmount { get; set; } // If candidate countered

        [StringLength(500)]
        public string? CounterOfferNotes { get; set; }

        public DateTime? ResponseDate { get; set; } // When candidate responded to offer

        // Navigation Properties
        public virtual required JobApplication JobApplication { get; set; }
        public virtual required User ExtendedByUser { get; set; }
    }
}