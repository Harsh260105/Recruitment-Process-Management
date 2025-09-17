using System.ComponentModel.DataAnnotations;

namespace RecruitmentSystem.Shared.DTOs.CandidateProfile
{
    public class CandidateProfileResponseDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string? CurrentLocation { get; set; }
        public decimal? TotalExperience { get; set; }
        public decimal? CurrentCTC { get; set; }
        public decimal? ExpectedCTC { get; set; }
        public int? NoticePeriod { get; set; }
        public string? LinkedInProfile { get; set; }
        public string? GitHubProfile { get; set; }
        public string? PortfolioUrl { get; set; }
        public string? College { get; set; }
        public string? Degree { get; set; }
        public int? GraduationYear { get; set; }
        public string? ResumeFileName { get; set; }
        public string? ResumeFilePath { get; set; }
        public string? Source { get; set; }
        public bool IsOpenToRelocation { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // User basic info
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }

        // Related data
        public List<CandidateSkillDto> Skills { get; set; } = new List<CandidateSkillDto>();
        public List<CandidateEducationDto> Education { get; set; } = new List<CandidateEducationDto>();
        public List<CandidateWorkExperienceDto> WorkExperience { get; set; } = new List<CandidateWorkExperienceDto>();
    }

    public class CandidateProfileDto
    {
        [StringLength(100)]
        public string? CurrentLocation { get; set; }

        [Range(0, 50)]
        public decimal? TotalExperience { get; set; }

        [Range(0, 10000000)]
        public decimal? CurrentCTC { get; set; }

        [Range(0, 10000000)]
        public decimal? ExpectedCTC { get; set; }

        [Range(0, 365)]
        public int? NoticePeriod { get; set; }

        [StringLength(255)]
        [Url]
        public string? LinkedInProfile { get; set; }

        [StringLength(255)]
        [Url]
        public string? GitHubProfile { get; set; }

        [StringLength(255)]
        [Url]
        public string? PortfolioUrl { get; set; }

        [StringLength(255)]
        public string? College { get; set; }

        [StringLength(100)]
        public string? Degree { get; set; }

        [Range(1990, 2030)]
        public int? GraduationYear { get; set; }

        [StringLength(50)]
        public string? Source { get; set; } = "Portal";

        public bool IsOpenToRelocation { get; set; } = false;

        public List<CreateCandidateSkillDto> Skills { get; set; } = new List<CreateCandidateSkillDto>();
        public List<CreateCandidateEducationDto> Education { get; set; } = new List<CreateCandidateEducationDto>();
        public List<CreateCandidateWorkExperienceDto> WorkExperience { get; set; } = new List<CreateCandidateWorkExperienceDto>();
    }

    public class UpdateCandidateProfileDto
    {
        [StringLength(100)]
        public string? CurrentLocation { get; set; }

        [Range(0, 50)]
        public decimal? TotalExperience { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? CurrentCTC { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? ExpectedCTC { get; set; }

        [Range(0, 365)]
        public int? NoticePeriod { get; set; }

        [Url]
        public string? LinkedInProfile { get; set; }

        [Url]
        public string? GitHubProfile { get; set; }

        [Url]
        public string? PortfolioUrl { get; set; }

        [StringLength(255)]
        public string? College { get; set; }

        [StringLength(100)]
        public string? Degree { get; set; }

        [Range(1950, 2030)]
        public int? GraduationYear { get; set; }

        public bool IsOpenToRelocation { get; set; } = false;
    }

    // Skill DTOs
    public class CandidateSkillDto
    {
        public Guid Id { get; set; }
        public int SkillId { get; set; }
        public required string SkillName { get; set; }
        public required string Category { get; set; }
        public decimal YearsOfExperience { get; set; }
        public int ProficiencyLevel { get; set; }
    }

    public class CreateCandidateSkillDto
    {
        [Required]
        public int SkillId { get; set; }

        [Range(0, 50)]
        public decimal YearsOfExperience { get; set; }

        [Range(1, 5)]
        public int ProficiencyLevel { get; set; }
    }

    public class UpdateCandidateSkillDto
    {
        [Range(0, 50)]
        public decimal? YearsOfExperience { get; set; }

        [Range(1, 5)]
        public int? ProficiencyLevel { get; set; }
    }

    // Education DTOs
    public class CandidateEducationDto
    {
        public Guid Id { get; set; }
        public string? InstitutionName { get; set; }
        public string? Degree { get; set; }
        public string? FieldOfStudy { get; set; }
        public int StartYear { get; set; }
        public int? EndYear { get; set; }
        public string? GPAScale { get; set; }
        public decimal? GPA { get; set; }
        public string? EducationType { get; set; }
    }

    public class CreateCandidateEducationDto
    {
        [Required]
        [StringLength(255)]
        public string? InstitutionName { get; set; }

        [Required]
        [StringLength(100)]
        public string? Degree { get; set; }

        [StringLength(100)]
        public string? FieldOfStudy { get; set; }

        [Required]
        [Range(1950, 2030)]
        public required int StartYear { get; set; }

        [Range(1950, 2030)]
        public int? EndYear { get; set; }

        [StringLength(10)]
        public string? GPAScale { get; set; }

        [Required]
        [Range(0, 100)]
        public required decimal GPA { get; set; }

        [StringLength(50)]
        public string? EducationType { get; set; }
    }

    public class UpdateCandidateEducationDto
    {
        [StringLength(255)]
        public string? InstitutionName { get; set; }

        [StringLength(100)]
        public string? Degree { get; set; }

        [StringLength(100)]
        public string? FieldOfStudy { get; set; }

        [Range(1950, 2030)]
        public int? StartYear { get; set; }

        [Range(1950, 2030)]
        public int? EndYear { get; set; }

        [StringLength(10)]
        public string? GPAScale { get; set; }

        [Range(0, 100)]
        public decimal? GPA { get; set; }

        [StringLength(50)]
        public string? EducationType { get; set; }
    }

    // Work Experience DTOs
    public class CandidateWorkExperienceDto
    {
        public Guid Id { get; set; }
        public required string CompanyName { get; set; }
        public required string JobTitle { get; set; }
        public string? EmploymentType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsCurrentJob { get; set; }
        public string? Location { get; set; }
        public string? JobDescription { get; set; }
    }

    public class CreateCandidateWorkExperienceDto
    {
        [Required]
        [StringLength(255)]
        public required string CompanyName { get; set; }

        [Required]
        [StringLength(100)]
        public required string JobTitle { get; set; }

        [StringLength(50)]
        public string? EmploymentType { get; set; }

        [Required]
        public required DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public bool IsCurrentJob { get; set; } = false;

        [StringLength(100)]
        public string? Location { get; set; }

        [StringLength(1000)]
        public string? JobDescription { get; set; }
    }

    public class UpdateCandidateWorkExperienceDto
    {
        [StringLength(255)]
        public string? CompanyName { get; set; }

        [StringLength(100)]
        public string? JobTitle { get; set; }

        [StringLength(50)]
        public string? EmploymentType { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public bool? IsCurrentJob { get; set; }

        [StringLength(100)]
        public string? Location { get; set; }

        [StringLength(1000)]
        public string? JobDescription { get; set; }
    }
}
