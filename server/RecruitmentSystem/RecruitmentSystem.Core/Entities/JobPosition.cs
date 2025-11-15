using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RecruitmentSystem.Core.Entities
{
    public class JobPosition : BaseEntity
    {
        [ForeignKey("CreatedByUser")]
        public Guid CreatedByUserId { get; set; }

        [Required]
        [StringLength(255)]
        public required string Title { get; set; }

        [Required]
        [StringLength(5000)]
        public required string Description { get; set; }

        [Required]
        [StringLength(100)]
        public required string Department { get; set; }

        [Required]
        [StringLength(100)]
        public required string Location { get; set; }

        [Required]
        [StringLength(50)]
        public required string EmploymentType { get; set; } = "Full-Time";

        [Required]
        [StringLength(50)]
        public required string ExperienceLevel { get; set; } = "Entry";

        [StringLength(100)]
        public string? SalaryRange { get; set; }

        [StringLength(20)]
        public string? Status { get; set; } = "Active";

        public DateTime? ApplicationDeadline { get; set; }

        [Range(0, 50)]
        public decimal? MinExperience { get; set; } = 0;

        [StringLength(1000)]
        public string? JobResponsibilities { get; set; }

        [StringLength(1000)]
        public string? RequiredQualifications { get; set; }

        public DateTime? ClosedDate { get; set; }

        public int TotalApplicants { get; set; } = 0;

        public virtual User? CreatedByUser { get; set; }
        public virtual ICollection<JobPositionSkill> JobPositionSkills { get; set; } = new List<JobPositionSkill>();
    }
}