using RecruitmentSystem.Core.Enums;

namespace RecruitmentSystem.Core.Entities.Projections
{
    public class InterviewDetailProjection
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
        public InterviewDetailJobApplicationProjection JobApplication { get; set; } = null!;
        public List<InterviewDetailParticipantProjection> Participants { get; set; } = new();
        public List<InterviewDetailEvaluationProjection> Evaluations { get; set; } = new();
    }

    public class InterviewDetailJobApplicationProjection
    {
        public Guid JobApplicationId { get; set; }
        public Guid CandidateProfileId { get; set; }
        public Guid CandidateUserId { get; set; }
        public string? CandidateFirstName { get; set; }
        public string? CandidateLastName { get; set; }
        public string? CandidateEmail { get; set; }
        public Guid? AssignedRecruiterId { get; set; }
        public string? AssignedRecruiterName { get; set; }
        public Guid JobPositionId { get; set; }
        public string? JobPositionTitle { get; set; }
        public string? JobPositionDepartment { get; set; }
        public string? JobPositionLocation { get; set; }
    }

    public class InterviewDetailParticipantProjection
    {
        public Guid Id { get; set; }
        public Guid ParticipantUserId { get; set; }
        public string? ParticipantName { get; set; }
        public string? ParticipantEmail { get; set; }
        public ParticipantRole Role { get; set; }
        public bool IsLead { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Notes { get; set; }
    }

    public class InterviewDetailEvaluationProjection
    {
        public Guid Id { get; set; }
        public Guid EvaluatorUserId { get; set; }
        public string? EvaluatorName { get; set; }
        public string? EvaluatorEmail { get; set; }
        public int? OverallRating { get; set; }
        public string? Strengths { get; set; }
        public string? Concerns { get; set; }
        public string? AdditionalComments { get; set; }
        public EvaluationRecommendation Recommendation { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
