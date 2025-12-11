using System.ComponentModel.DataAnnotations;
using RecruitmentSystem.Core.Enums;

namespace RecruitmentSystem.Shared.DTOs
{
    public class JobApplicationDto
    {
        public Guid Id { get; set; }
        public Guid CandidateProfileId { get; set; }
        public string? CandidateName { get; set; }
        public Guid JobPositionId { get; set; }
        public string? JobTitle { get; set; }
        public ApplicationStatus Status { get; set; }
        public string? CoverLetter { get; set; }
        public string? InternalNotes { get; set; }
        public DateTime AppliedDate { get; set; }
        public Guid? AssignedRecruiterId { get; set; }
        public string? AssignedRecruiterName { get; set; }
        public int? TestScore { get; set; }
        public DateTime? TestCompletedAt { get; set; }
        public string? RejectionReason { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class JobApplicationCreateDto
    {
        [Required]
        public Guid CandidateProfileId { get; set; }

        [Required]
        public Guid JobPositionId { get; set; }

        [StringLength(2000)]
        public string? CoverLetter { get; set; }
    }

    public class JobApplicationUpdateDto
    {
        [StringLength(2000)]
        public string? CoverLetter { get; set; }

        [StringLength(1000)]
        public string? InternalNotes { get; set; }

        public Guid? AssignedRecruiterId { get; set; }
    }

    public class JobApplicationStatusUpdateDto
    {
        [Required]
        public ApplicationStatus Status { get; set; }

        [StringLength(500)]
        public string? Comments { get; set; }
    }

    public class JobApplicationSummaryDto
    {
        public Guid Id { get; set; }
        public string? CandidateName { get; set; }
        public string? JobTitle { get; set; }
        public ApplicationStatus Status { get; set; }
        public DateTime AppliedDate { get; set; }
        public Guid? AssignedRecruiterId { get; set; }
        public string? AssignedRecruiterName { get; set; }
    }

    /// <summary>
    /// Detailed DTO with full navigation properties - used when fetching complete application details
    /// </summary>
    public class JobApplicationDetailedDto
    {
        public Guid Id { get; set; }
        public Guid CandidateProfileId { get; set; }
        public Guid JobPositionId { get; set; }
        public ApplicationStatus Status { get; set; }
        public string? CoverLetter { get; set; }
        public string? InternalNotes { get; set; }
        public DateTime AppliedDate { get; set; }
        public Guid? AssignedRecruiterId { get; set; }
        public int? TestScore { get; set; }
        public DateTime? TestCompletedAt { get; set; }
        public string? RejectionReason { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Detailed Candidate Information
        public JobApplicationCandidateDto Candidate { get; set; } = new();

        // Detailed Job Information  
        public JobApplicationJobPositionDto JobPosition { get; set; } = new();

        // Assigned Recruiter Information
        public JobApplicationRecruiterDto? AssignedRecruiter { get; set; }

        // Recent Status History
        public List<JobApplicationStatusHistoryDto> StatusHistory { get; set; } = new();

        // Job Offer Information (if any)
        public JobApplicationOfferDto? JobOffer { get; set; }
    }

    /// <summary>
    /// Candidate information within job application context
    /// </summary>
    public class JobApplicationCandidateDto
    {
        public Guid Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? CurrentLocation { get; set; }
        public decimal? TotalExperience { get; set; }
        public decimal? CurrentCTC { get; set; }
        public decimal? ExpectedCTC { get; set; }
        public int? NoticePeriod { get; set; }
        public string? LinkedInProfile { get; set; }
        public string? ResumeFileName { get; set; }
    }

    /// <summary>
    /// Job position information within job application context
    /// </summary>
    public class JobApplicationJobPositionDto
    {
        public Guid Id { get; set; }
        public string? Title { get; set; }
        public string? Department { get; set; }
        public string? Location { get; set; }
        public string? EmploymentType { get; set; }
        public string? ExperienceLevel { get; set; }
        public string? SalaryRange { get; set; }
        public decimal? MinExperience { get; set; }
    }

    /// <summary>
    /// Recruiter information within job application context
    /// </summary>
    public class JobApplicationRecruiterDto
    {
        public Guid Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
    }

    /// <summary>
    /// Status history information within job application context
    /// </summary>
    public class JobApplicationStatusHistoryDto
    {
        public Guid Id { get; set; }
        public ApplicationStatus FromStatus { get; set; }
        public ApplicationStatus ToStatus { get; set; }
        public DateTime ChangedAt { get; set; }
        public string? Comments { get; set; }
        public string? ChangedByName { get; set; }
    }

    /// <summary>
    /// Job offer information within job application context
    /// </summary>
    public class JobApplicationOfferDto
    {
        public Guid Id { get; set; }
        public decimal OfferedSalary { get; set; }
        public string? OfferStatus { get; set; }
        public DateTime OfferDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public DateTime? ResponseDate { get; set; }
    }

    /// <summary>
    /// Candidate-facing view - excludes sensitive internal information
    /// </summary>
    public class JobApplicationCandidateViewDto
    {
        public Guid Id { get; set; }
        public Guid JobPositionId { get; set; }
        public ApplicationStatus Status { get; set; }
        public string? CoverLetter { get; set; }
        public DateTime AppliedDate { get; set; }
        public int? TestScore { get; set; }
        public DateTime? TestCompletedAt { get; set; }
        public string? RejectionReason { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Job Position Information
        public JobApplicationJobPositionDto JobPosition { get; set; } = new();

        // Assigned Recruiter (basic info only)
        public string? AssignedRecruiterName { get; set; }

        // Limited Status History (candidates see what happened, not internal comments)
        public List<JobApplicationStatusHistoryDto> StatusHistory { get; set; } = new();

        // Job Offer Information (if any)
        public JobApplicationOfferDto? JobOffer { get; set; }
    }

    /// <summary>
    /// Staff-facing view - includes internal information for recruiters/HR
    /// </summary>
    public class JobApplicationStaffViewDto
    {
        public Guid Id { get; set; }
        public Guid CandidateProfileId { get; set; }
        public Guid JobPositionId { get; set; }
        public ApplicationStatus Status { get; set; }
        public string? CoverLetter { get; set; }
        public string? InternalNotes { get; set; } // Staff can see internal notes
        public DateTime AppliedDate { get; set; }
        public Guid? AssignedRecruiterId { get; set; }
        public int? TestScore { get; set; }
        public DateTime? TestCompletedAt { get; set; }
        public string? RejectionReason { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Detailed Candidate Information
        public JobApplicationCandidateDto Candidate { get; set; } = new();

        // Job Position Information  
        public JobApplicationJobPositionDto JobPosition { get; set; } = new();

        // Assigned Recruiter Information
        public JobApplicationRecruiterDto? AssignedRecruiter { get; set; }

        // Full Status History with internal comments
        public List<JobApplicationStatusHistoryDto> StatusHistory { get; set; } = new();

        // Job Offer Information
        public JobApplicationOfferDto? JobOffer { get; set; }
    }
}