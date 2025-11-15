using System.ComponentModel.DataAnnotations;

namespace RecruitmentSystem.Core.Entities
{
    public class Skill
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public required string Name { get; set; }

        [StringLength(50)]
        public string? Category { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<CandidateSkill> CandidateSkills { get; set; } = new List<CandidateSkill>();
        public virtual ICollection<JobPositionSkill> JobPositionSkills { get; set; } = new List<JobPositionSkill>();
    }
}
