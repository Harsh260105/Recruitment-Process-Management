using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RecruitmentSystem.Core.Entities
{
    public class CandidateProfile : BaseEntity
    {
        public Guid UserId { get; set; }

        public Guid CreatedBy { get; set; }

        [StringLength(100)]
        public string? CurrentLocation { get; set; }

        [Column(TypeName = "decimal(4,2)")]
        public decimal? TotalExperience { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? CurrentCTC { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? ExpectedCTC { get; set; }

        public int? NoticePeriod { get; set; } // in days

        [StringLength(255)]
        public string? LinkedInProfile { get; set; }

        [StringLength(255)]
        public string? GitHubProfile { get; set; }

        [StringLength(255)]
        public string? PortfolioUrl { get; set; }

        [StringLength(255)]
        public string? College { get; set; }

        [StringLength(100)]
        public string? Degree { get; set; }

        public int? GraduationYear { get; set; }

        [StringLength(255)]
        public string? ResumeFileName { get; set; }

        [StringLength(500)]
        public string? ResumeFilePath { get; set; }

        [StringLength(50)]
        public string? Source { get; set; } = "Portal";

        public bool IsOpenToRelocation { get; set; } = false;

        public bool CanBypassApplicationLimits { get; set; }

        public DateTime? OverrideExpiresAt { get; set; }

        // Navigation Properties
        public virtual required User User { get; set; }
        public virtual required User CreatedByUser { get; set; }
        public virtual ICollection<CandidateSkill> CandidateSkills { get; set; } = new List<CandidateSkill>();
        public virtual ICollection<CandidateEducation> CandidateEducations { get; set; } = new List<CandidateEducation>();
        public virtual ICollection<CandidateWorkExperience> CandidateWorkExperiences { get; set; } = new List<CandidateWorkExperience>();
    }
}