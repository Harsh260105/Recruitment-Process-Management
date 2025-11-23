using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RecruitmentSystem.Core.Enums;

namespace RecruitmentSystem.Core.Entities
{
    public class InterviewParticipant : BaseEntity
    {
        [ForeignKey("Interview")]
        public Guid InterviewId { get; set; }

        [ForeignKey("ParticipantUser")]
        public Guid ParticipantUserId { get; set; }

        [Required]
        public ParticipantRole Role { get; set; } = ParticipantRole.Interviewer;

        public bool IsLead { get; set; } = false; // Is this the lead interviewer

        [StringLength(500)]
        public string? Notes { get; set; }

        // Navigation Properties
        public virtual required Interview Interview { get; set; }
        public virtual required User ParticipantUser { get; set; }
    }
}