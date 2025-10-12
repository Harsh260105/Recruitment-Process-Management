using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RecruitmentSystem.Core.Enums;

namespace RecruitmentSystem.Core.Entities
{
    public class Interview : BaseEntity
    {
        [ForeignKey("JobApplication")]
        public Guid JobApplicationId { get; set; }

        [Required]
        [StringLength(100)]
        public required string Title { get; set; } // "Round 1 - Technical", "Round 2 - Manager Discussion"

        public InterviewType InterviewType { get; set; } = InterviewType.Technical;

        public int RoundNumber { get; set; } = 1; // Allows flexible number of rounds

        public InterviewStatus Status { get; set; } = InterviewStatus.Scheduled;

        public DateTime ScheduledDateTime { get; set; }

        public int DurationMinutes { get; set; } = 60;

        public InterviewMode Mode { get; set; } = InterviewMode.Online;

        [StringLength(500)]
        public string? MeetingDetails { get; set; } // Room number, video link, phone number

        [StringLength(500)]
        public string? Instructions { get; set; } // What candidate should prepare

        [ForeignKey("ScheduledByUser")]
        public Guid ScheduledByUserId { get; set; } // Who scheduled this interview

        public InterviewOutcome? Outcome { get; set; }

        [StringLength(2000)]
        public string? SummaryNotes { get; set; } // Overall interview summary

        public bool IsActive { get; set; } = true;

        // Navigation Properties
        public virtual required JobApplication JobApplication { get; set; }
        public virtual required User ScheduledByUser { get; set; }
        public virtual ICollection<InterviewEvaluation> Evaluations { get; set; } = new List<InterviewEvaluation>();
        public virtual ICollection<InterviewParticipant> Participants { get; set; } = new List<InterviewParticipant>();
    }
}