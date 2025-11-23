using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RecruitmentSystem.Core.Enums;

namespace RecruitmentSystem.Core.Entities
{
    public class JobApplication : BaseEntity
    {
        [ForeignKey("CandidateProfile")]
        public Guid CandidateProfileId { get; set; }

        [ForeignKey("JobPosition")]
        public Guid JobPositionId { get; set; }

        [Required]
        public ApplicationStatus Status { get; set; } = ApplicationStatus.Applied;

        [StringLength(2000)]
        public string? CoverLetter { get; set; }

        [StringLength(1000)]
        public string? InternalNotes { get; set; } // Recruiter notes - not visible to candidate

        public DateTime AppliedDate { get; set; } = DateTime.UtcNow;

        [ForeignKey("AssignedRecruiter")]
        public Guid? AssignedRecruiterId { get; set; } // Lead recruiter handling this application

        // Initial Screening Test (from external test module)
        [Range(0, 100)]
        public int? TestScore { get; set; } // Score from screening test (0-100)

        public DateTime? TestCompletedAt { get; set; } // When test was completed

        [StringLength(500)]
        public string? RejectionReason { get; set; }

        public bool IsActive { get; set; } = true; // Can be withdrawn or closed

        // Navigation Properties
        public virtual required CandidateProfile CandidateProfile { get; set; }
        public virtual required JobPosition JobPosition { get; set; }
        public virtual User? AssignedRecruiter { get; set; }
        public virtual ICollection<Interview> Interviews { get; set; } = new List<Interview>();
        public virtual ICollection<ApplicationStatusHistory> StatusHistory { get; set; } = new List<ApplicationStatusHistory>();
        public virtual JobOffer? JobOffer { get; set; } // One-to-one relationship
    }
}