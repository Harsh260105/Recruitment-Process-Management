using System.ComponentModel.DataAnnotations;

namespace RecruitmentSystem.Shared.DTOs
{
    #region Scheduled Interviews DTOs

    /// <summary>
    /// Request DTO for getting scheduled interviews in a date range
    /// Shows what times are already booked rather than available slots
    /// </summary>
    public class GetScheduledInterviewsRequestDto
    {
        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Optional: Filter by specific participant user IDs
        /// If provided, only returns interviews where these users are participants
        /// </summary>
        public List<Guid> ParticipantUserIds { get; set; } = new();

        /// <summary>
        /// Optional: Job application ID to exclude from results
        /// </summary>
        public Guid? ExcludeJobApplicationId { get; set; }
    }

    /// <summary>
    /// Scheduled interview slot information
    /// Shows exactly when interviews are booked
    /// </summary>
    public class ScheduledInterviewSlotDto
    {
        public Guid InterviewId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public int DurationMinutes { get; set; }
        public string InterviewType { get; set; } = string.Empty;
        public string Mode { get; set; } = string.Empty;
        public string CandidateName { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public List<string> Participants { get; set; } = new();
    }

    #endregion
}
