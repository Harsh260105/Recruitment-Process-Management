using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RecruitmentSystem.Core.Enums;

namespace RecruitmentSystem.Core.Entities
{
    public class InterviewEvaluation : BaseEntity
    {
        [ForeignKey("Interview")]
        public Guid InterviewId { get; set; }

        [ForeignKey("EvaluatorUser")]
        public Guid EvaluatorUserId { get; set; } // Interviewer who provided evaluation

        [Range(1, 5)]
        public int? OverallRating { get; set; } // Simple 1-5 rating

        [StringLength(2000)]
        public string? Strengths { get; set; }

        [StringLength(2000)]
        public string? Concerns { get; set; }

        [StringLength(1000)]
        public string? AdditionalComments { get; set; }

        [Required]
        public EvaluationRecommendation Recommendation { get; set; } = EvaluationRecommendation.Maybe;

        // Navigation Properties
        public virtual required Interview Interview { get; set; }
        public virtual required User EvaluatorUser { get; set; }
    }
}