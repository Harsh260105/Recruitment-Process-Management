using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RecruitmentSystem.Core.Entities
{
    public class CandidateEducation : BaseEntity
    {
        public Guid CandidateProfileId { get; set; }

        [Required]
        [StringLength(255)]
        public string? InstitutionName { get; set; }

        [Required]
        [StringLength(100)]
        public string? Degree { get; set; }

        [StringLength(100)]
        public string? FieldOfStudy { get; set; }

        [Range(1950, 2030)]
        public int StartYear { get; set; }

        [Range(1950, 2030)]
        public int? EndYear { get; set; }

        [StringLength(10)]
        public string? GPAScale { get; set; }

        [Required]
        [Column(TypeName = "decimal(4,2)")]
        [Range(0, 100)]
        public required decimal GPA { get; set; }

        [StringLength(50)]
        public string? EducationType { get; set; }

        // Navigation Property
        public virtual CandidateProfile? CandidateProfile { get; set; }
    }
}
