using System.ComponentModel.DataAnnotations;
using RecruitmentSystem.Core.Enums;

namespace RecruitmentSystem.Shared.DTOs
{
    #region Core Interview DTOs

    /// <summary>
    /// Basic DTO for creating interview entity - used internally by InterviewService
    /// This is for system/service level operations, not user-facing scheduling
    /// </summary>
    public class CreateInterviewDto
    {
        [Required]
        public Guid JobApplicationId { get; set; }

        [Required]
        [StringLength(100)]
        public required string Title { get; set; }

        [Required]
        public InterviewType InterviewType { get; set; } = InterviewType.Technical;

        [Required]
        public int RoundNumber { get; set; } = 1;

        [Required]
        public DateTime ScheduledDateTime { get; set; }

        [Required]
        [Range(15, 720)] // 15 minutes to 12 hours
        public int DurationMinutes { get; set; } = 60;

        [Required]
        public InterviewMode Mode { get; set; } = InterviewMode.Online;

        [StringLength(500)]
        public string? MeetingDetails { get; set; }

        [StringLength(500)]
        public string? Instructions { get; set; }

        [Required]
        public Guid ScheduledByUserId { get; set; }
    }

    /// <summary>
    /// DTO for updating interview entity - used by InterviewService
    /// </summary>
    public class UpdateInterviewDto
    {
        [StringLength(100)]
        public string? Title { get; set; }

        public InterviewType? InterviewType { get; set; }

        public DateTime? ScheduledDateTime { get; set; }

        [Range(15, 720)]
        public int? DurationMinutes { get; set; }

        public InterviewMode? Mode { get; set; }

        [StringLength(500)]
        public string? MeetingDetails { get; set; }

        [StringLength(500)]
        public string? Instructions { get; set; }
    }

    #endregion

    #region Interview Scheduling DTOs

    /// <summary>
    /// Comprehensive DTO for scheduling interviews - used by InterviewSchedulingService
    /// This is the main user-facing DTO for creating AND scheduling interviews
    /// </summary>
    public class ScheduleInterviewDto
    {
        [Required]
        public Guid JobApplicationId { get; set; }

        [Required]
        [StringLength(100)]
        public required string Title { get; set; }

        [Required]
        public InterviewType InterviewType { get; set; } = InterviewType.Technical;

        [Required]
        public DateTime ScheduledDateTime { get; set; }

        [Required]
        [Range(15, 480)] // 15 minutes to 8 hours (for assessment centers)
        public int DurationMinutes { get; set; } = 60;

        [Required]
        public InterviewMode Mode { get; set; } = InterviewMode.Online;

        [StringLength(500)]
        public string? MeetingDetails { get; set; }

        [StringLength(500)]
        public string? Instructions { get; set; }

        [Required]
        public IEnumerable<Guid> ParticipantUserIds { get; set; } = new List<Guid>();
    }

    public class ScheduleInterviewWithParticipantsDto
    {
        [Required]
        public Guid JobApplicationId { get; set; }

        [Required]
        [StringLength(100)]
        public required string Title { get; set; }

        [Required]
        public InterviewType InterviewType { get; set; } = InterviewType.Technical;

        [Required]
        public DateTime ScheduledDateTime { get; set; }

        [Required]
        [Range(15, 720)]
        public int DurationMinutes { get; set; } = 60;

        [Required]
        public InterviewMode Mode { get; set; } = InterviewMode.Online;

        [StringLength(500)]
        public string? MeetingDetails { get; set; }

        [StringLength(500)]
        public string? Instructions { get; set; }

        [Required]
        public IEnumerable<InterviewParticipantDto> Participants { get; set; } = new List<InterviewParticipantDto>();
    }

    public class InterviewParticipantDto
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        public ParticipantRole Role { get; set; } = ParticipantRole.Interviewer;

        public bool IsLead { get; set; } = false;
    }

    public class RescheduleInterviewDto
    {
        [Required]
        public DateTime NewDateTime { get; set; }

        [StringLength(500)]
        public string? Reason { get; set; }
    }

    public class CancelInterviewDto
    {
        [StringLength(500)]
        public string? Reason { get; set; }
    }

    public class MarkInterviewCompletedDto
    {
        [StringLength(2000)]
        public string? SummaryNotes { get; set; }
    }

    public class MarkInterviewNoShowDto
    {
        [StringLength(500)]
        public string? Notes { get; set; }
    }

    #endregion

    #region Interview Evaluation DTOs

    public class SubmitEvaluationDto
    {
        [Required]
        public Guid InterviewId { get; set; }

        [Required]
        [Range(1, 5)]
        public int? OverallRating { get; set; }

        [StringLength(2000)]
        public string? Strengths { get; set; }

        [StringLength(2000)]
        public string? Concerns { get; set; }

        [StringLength(1000)]
        public string? AdditionalComments { get; set; }

        [Required]
        public EvaluationRecommendation Recommendation { get; set; } = EvaluationRecommendation.Maybe;
    }

    public class UpdateEvaluationDto
    {
        [Range(1, 5)]
        public int? OverallRating { get; set; }

        [StringLength(2000)]
        public string? Strengths { get; set; }

        [StringLength(2000)]
        public string? Concerns { get; set; }

        [StringLength(1000)]
        public string? AdditionalComments { get; set; }

        public EvaluationRecommendation? Recommendation { get; set; }
    }

    public class SetInterviewOutcomeDto
    {
        [Required]
        public InterviewOutcome Outcome { get; set; }
    }

    public class CompleteInterviewWithEvaluationDto
    {
        [Required]
        public InterviewOutcome Outcome { get; set; }

        [StringLength(2000)]
        public string? SummaryNotes { get; set; }
    }

    #endregion

    #region Interview Reporting DTOs

    public class InterviewSearchDto
    {
        public InterviewStatus? Status { get; set; }
        public InterviewType? InterviewType { get; set; }
        public InterviewMode? Mode { get; set; }
        public DateTime? ScheduledFromDate { get; set; }
        public DateTime? ScheduledToDate { get; set; }
        public Guid? ParticipantUserId { get; set; }
        public Guid? JobApplicationId { get; set; }
        public int? PageNumber { get; set; } = 1;
        public int? PageSize { get; set; } = 20;
    }

    public class InterviewAnalyticsDto
    {
        public Dictionary<string, int> StatusDistribution { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> TypeDistribution { get; set; } = new Dictionary<string, int>();
        public int TotalInterviews { get; set; }
        public int UpcomingInterviews { get; set; }
        public int CompletedInterviews { get; set; }
        public int CancelledInterviews { get; set; }
        public double AverageInterviewDuration { get; set; }
    }

    #endregion

    #region Interview Response DTOs

    public class InterviewResponseDto
    {
        public Guid Id { get; set; }
        public Guid JobApplicationId { get; set; }
        public string? Title { get; set; }
        public InterviewType InterviewType { get; set; }
        public int RoundNumber { get; set; }
        public InterviewStatus Status { get; set; }
        public DateTime ScheduledDateTime { get; set; }
        public int DurationMinutes { get; set; }
        public InterviewMode Mode { get; set; }
        public string? MeetingDetails { get; set; }
        public string? Instructions { get; set; }
        public Guid ScheduledByUserId { get; set; }
        public string? ScheduledByUserName { get; set; }
        public InterviewOutcome? Outcome { get; set; }
        public string? SummaryNotes { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Related data
        public JobApplicationSummaryDto? JobApplication { get; set; }
        public List<InterviewParticipantResponseDto> Participants { get; set; } = new List<InterviewParticipantResponseDto>();
        public List<InterviewEvaluationResponseDto> Evaluations { get; set; } = new List<InterviewEvaluationResponseDto>();
    }

    public class InterviewParticipantResponseDto
    {
        public Guid Id { get; set; }
        public Guid InterviewId { get; set; }
        public Guid ParticipantUserId { get; set; }
        public string? ParticipantUserName { get; set; }
        public string? ParticipantUserEmail { get; set; }
        public ParticipantRole Role { get; set; }
        public bool IsLead { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class InterviewEvaluationResponseDto
    {
        public Guid Id { get; set; }
        public Guid InterviewId { get; set; }
        public Guid EvaluatorUserId { get; set; }
        public string? EvaluatorUserName { get; set; }
        public string? EvaluatorUserEmail { get; set; }
        public int? OverallRating { get; set; }
        public string? Strengths { get; set; }
        public string? Concerns { get; set; }
        public string? AdditionalComments { get; set; }
        public EvaluationRecommendation Recommendation { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class InterviewSummaryDto
    {
        public Guid Id { get; set; }
        public Guid JobApplicationId { get; set; }
        public string? Title { get; set; }
        public InterviewType InterviewType { get; set; }
        public int RoundNumber { get; set; }
        public InterviewStatus Status { get; set; }
        public DateTime ScheduledDateTime { get; set; }
        public InterviewMode Mode { get; set; }
        public InterviewOutcome? Outcome { get; set; }
        public int ParticipantCount { get; set; }
        public int EvaluationCount { get; set; }
        public double? AverageRating { get; set; }
    }

    public class InterviewPublicSummaryDto
    {
        public Guid Id { get; set; }
        public Guid JobApplicationId { get; set; }
        public string? Title { get; set; }
        public InterviewType InterviewType { get; set; }
        public int RoundNumber { get; set; }
        public InterviewStatus Status { get; set; }
        public DateTime ScheduledDateTime { get; set; }
        public InterviewMode Mode { get; set; }
    }

    public class InterviewWorkflowDto
    {
        public Guid JobApplicationId { get; set; }
        public List<InterviewSummaryDto> Interviews { get; set; } = new List<InterviewSummaryDto>();
        public bool IsProcessComplete { get; set; }
        public InterviewOutcome? FinalOutcome { get; set; }
        public int CurrentRound { get; set; }
        public int TotalRounds { get; set; }
    }

    #endregion

    #region Available Time Slots DTOs

    /// <summary>
    /// Request DTO for getting available time slots
    /// </summary>
    public class GetAvailableTimeSlotsRequestDto
    {
        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        [Range(15, 720)]
        public int DurationMinutes { get; set; } = 60;

        /// <summary>
        /// Optional: Participant user IDs to check availability for
        /// If provided, only time slots where all participants are available will be returned
        /// </summary>
        public List<Guid> ParticipantUserIds { get; set; } = new();

        /// <summary>
        /// Optional: Job application ID to exclude its interviews from conflict check
        /// </summary>
        public Guid? ExcludeJobApplicationId { get; set; }
    }

    /// <summary>
    /// Available time slot information
    /// </summary>
    public class AvailableTimeSlotDto
    {
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public int DurationMinutes { get; set; }
        public bool IsRecommended { get; set; }
        public List<string> AvailableParticipants { get; set; } = new();
        public List<string> UnavailableParticipants { get; set; } = new();
    }

    #endregion
}