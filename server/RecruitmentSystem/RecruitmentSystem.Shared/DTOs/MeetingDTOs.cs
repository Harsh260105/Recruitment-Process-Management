using System.ComponentModel.DataAnnotations;

namespace RecruitmentSystem.Shared.DTOs
{
    /// <summary>
    /// Request DTO for creating a video conference meeting
    /// </summary>
    public class CreateMeetingRequestDto
    {
        [Required]
        [StringLength(200)]
        public required string Title { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        public DateTime StartDateTime { get; set; }

        [Required]
        [Range(15, 480)] // 15 minutes to 8 hours
        public int DurationMinutes { get; set; }

        [Required]
        [EmailAddress]
        public required string OrganizerEmail { get; set; }

        public List<string> AttendeeEmails { get; set; } = new();
    }

    /// <summary>
    /// Response DTO containing meeting details and credentials
    /// </summary>
    public class MeetingCredentialsDto
    {
        public required string MeetingId { get; set; }
        public required string MeetingLink { get; set; }
        public required string Title { get; set; }
        public DateTime StartDateTime { get; set; }
        public int DurationMinutes { get; set; }
        public string? Password { get; set; }
        public string? DialInNumber { get; set; }
        public string? AccessCode { get; set; }
        public string? Description { get; set; }
        public List<string> AttendeeEmails { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Configuration settings for meeting service
    /// </summary>
    public class MeetingServiceConfiguration
    {
        public required string ServiceType { get; set; } // "JitsiMeet", "Zoom", "Teams"
        public required string ClientId { get; set; }
        public required string ClientSecret { get; set; }
        public string? RefreshToken { get; set; }
        public string? ServiceAccountKeyPath { get; set; }
        public bool IsEnabled { get; set; } = true;
    }

    /// <summary>
    /// Meeting status update DTO
    /// </summary>
    public class UpdateMeetingStatusDto
    {
        public required string MeetingId { get; set; }
        public required string Status { get; set; } // "Started", "Ended", "Cancelled"
        public DateTime UpdatedAt { get; set; }
    }
}