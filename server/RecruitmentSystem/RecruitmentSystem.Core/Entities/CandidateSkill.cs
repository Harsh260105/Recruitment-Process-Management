using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace RecruitmentSystem.Core.Entities
{
    public class CandidateSkill : BaseEntity
    {
        public Guid CandidateProfileId { get; set; }
        
        public int SkillId { get; set; }

        [Column(TypeName = "decimal(4,2)")]
        [Range(0, 50)]
        public decimal YearsOfExperience { get; set; }

        [Range(1, 5)]
        public int ProficiencyLevel { get; set; }

        // Navigation Properties

        public virtual required CandidateProfile CandidateProfile { get; set; }
        
        public virtual required Skill Skill { get; set; }
    }
}
