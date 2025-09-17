using System;
using System.ComponentModel.DataAnnotations;

namespace RecruitmentSystem.Core.Entities
{
    public class CandidateWorkExperience : BaseEntity
    {
        public Guid CandidateProfileId { get; set; }

        [Required]
        [StringLength(255)]
        public required string CompanyName { get; set; }

        [Required]
        [StringLength(100)]
        public required string JobTitle { get; set; }

        [StringLength(50)]
        public string? EmploymentType { get; set; }

        [Required]
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; } 

        public bool IsCurrentJob { get; set; } = false;

        [StringLength(100)]
        public string? Location { get; set; }

        [StringLength(1000)]
        public string? JobDescription { get; set; }

        // Navigation Property
        public virtual required CandidateProfile CandidateProfile { get; set; }
    }
}
