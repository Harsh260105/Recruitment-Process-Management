using System.ComponentModel.DataAnnotations;

namespace RecruitmentSystem.Shared.DTOs
{
    public class CreateJobPositionDto
    {
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
        public decimal? MinExperience { get; set; }

        [StringLength(1000)]
        public string? JobResponsibilities { get; set; }

        [StringLength(1000)]
        public string? RequiredQualifications { get; set; }

        public List<CreateJobPositionSkillDto> JobPositionSkills { get; set; } = new List<CreateJobPositionSkillDto>();
    }

    public class CreateJobPositionSkillDto
    {
        [Required]
        public int SkillId { get; set; }

        public bool IsRequired { get; set; } = true;

        [Range(0, 50)]
        public int MinimumExperience { get; set; } = 0;

        [Range(1, 5)]
        public int ProficiencyLevel { get; set; } = 1;
    }

    public class JobPositionResponseDto
    {
        public Guid Id { get; set; }
        public Guid CreatedByUserId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Department { get; set; }
        public string? Location { get; set; }
        public string? EmploymentType { get; set; }
        public string? ExperienceLevel { get; set; }
        public string? SalaryRange { get; set; }
        public string? Status { get; set; }
        public DateTime? ApplicationDeadline { get; set; }
        public decimal? MinExperience { get; set; }
        public string? JobResponsibilities { get; set; }
        public string? RequiredQualifications { get; set; }
        public DateTime? ClosedDate { get; set; }
        public int TotalApplicants { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Creator info
        public string? CreatorFirstName { get; set; }
        public string? CreatorLastName { get; set; }
        public string? CreatorEmail { get; set; }

        public List<JobPositionSkillResponseDto> Skills { get; set; } = new List<JobPositionSkillResponseDto>();
    }

    public class JobPositionSummarySkillDto
    {
        public int SkillId { get; set; }
        public string? SkillName { get; set; }
        public bool IsRequired { get; set; }
    }

    public class JobPositionPublicSummaryDto
    {
        public Guid Id { get; set; }
        public string? Title { get; set; }
        public string? Department { get; set; }
        public string? Location { get; set; }
        public string? EmploymentType { get; set; }
        public string? ExperienceLevel { get; set; }
        public string? SalaryRange { get; set; }
        public DateTime? ApplicationDeadline { get; set; }
        public decimal? MinExperience { get; set; }
        public List<JobPositionSummarySkillDto> Skills { get; set; } = new();
    }

    public class JobPositionStaffSummaryDto : JobPositionPublicSummaryDto
    {
        public string? Status { get; set; }
        public int TotalApplicants { get; set; }
        public Guid CreatedByUserId { get; set; }
        public string? CreatorFirstName { get; set; }
        public string? CreatorLastName { get; set; }
        public string? CreatorEmail { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class JobPositionSkillResponseDto
    {
        public int SkillId { get; set; }
        public string? SkillName { get; set; }
        public string? SkillCategory { get; set; }
        public bool IsRequired { get; set; }
        public int MinimumExperience { get; set; }
        public int ProficiencyLevel { get; set; }
    }

    public class UpdateJobPositionDto
    {
        [StringLength(255)]
        public string? Title { get; set; }

        [StringLength(5000)]
        public string? Description { get; set; }

        [StringLength(100)]
        public string? Department { get; set; }

        [StringLength(100)]
        public string? Location { get; set; }

        [StringLength(50)]
        public string? EmploymentType { get; set; }

        [StringLength(50)]
        public string? ExperienceLevel { get; set; }

        [StringLength(100)]
        public string? SalaryRange { get; set; }

        [StringLength(20)]
        public string? Status { get; set; }

        public DateTime? ApplicationDeadline { get; set; }

        [Range(0, 50)]
        public decimal? MinExperience { get; set; }

        [StringLength(1000)]
        public string? JobResponsibilities { get; set; }

        [StringLength(1000)]
        public string? RequiredQualifications { get; set; }

        public List<UpdateJobPositionSkillDto>? Skills { get; set; }
    }

    public class UpdateJobPositionSkillDto
    {
        [Required]
        public int SkillId { get; set; }

        public bool? IsRequired { get; set; }

        [Range(0, 50)]
        public int? MinimumExperience { get; set; }

        [Range(1, 5)]
        public int? ProficiencyLevel { get; set; }
    }

    public class JobPositionQueryDto
    {
        public string? Status { get; set; }
        public string? Department { get; set; }
        public string? Location { get; set; }
        public string? ExperienceLevel { get; set; }
        public List<int>? SkillIds { get; set; }
        public DateTime? CreatedFromDate { get; set; }
        public DateTime? CreatedToDate { get; set; }
        public DateTime? DeadlineFromDate { get; set; }
        public DateTime? DeadlineToDate { get; set; }
    }
}