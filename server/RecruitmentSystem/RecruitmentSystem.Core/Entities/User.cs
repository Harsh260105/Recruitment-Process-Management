using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace RecruitmentSystem.Core.Entities
{
    public class User : IdentityUser<Guid>
    {
        [Required]
        [StringLength(100)]
        public required string FirstName { get; set; }

        [Required]
        [StringLength(100)]
        public required string LastName { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public virtual CandidateProfile? CandidateProfile { get; set; }
        public virtual StaffProfile? StaffProfile { get; set; }
    }
}