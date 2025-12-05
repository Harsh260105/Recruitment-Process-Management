using RecruitmentSystem.Core.Enums;

namespace RecruitmentSystem.Core.Entities.Projections
{
    public class InterviewSummaryProjection
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
}
