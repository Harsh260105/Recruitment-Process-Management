using RecruitmentSystem.Core.Enums;

namespace RecruitmentSystem.Shared.DTOs
{
    /// <summary>
    /// Lightweight response for status-only operations
    /// </summary>
    public class JobApplicationStatusResponseDto
    {
        public Guid Id { get; set; }
        public ApplicationStatus Status { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? LastUpdatedBy { get; set; }
        public string? Comments { get; set; }
        public bool IsActive { get; set; }

        // For workflow-specific fields that might change with status
        public Guid? AssignedRecruiterId { get; set; }
        public string? AssignedRecruiterName { get; set; }
        public int? TestScore { get; set; }
        public DateTime? TestCompletedAt { get; set; }
        public string? RejectionReason { get; set; }
    }
}