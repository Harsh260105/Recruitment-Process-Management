using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RecruitmentSystem.Core.Entities
{
    public class JobPositionSkill : BaseEntity
    {
        [ForeignKey("JobPosition")]
        public Guid JobPositionId { get; set; }

        [ForeignKey("Skill")]
        public int SkillId { get; set; }

        public bool IsRequired { get; set; } = true;

        [Range(0, 50)]
        public int MinimumExperience { get; set; } = 0;

        [Range(1, 5)]
        public int ProficiencyLevel { get; set; } = 1;

        public virtual JobPosition? JobPosition { get; set; }
        public virtual Skill? Skill { get; set; }
    }
}