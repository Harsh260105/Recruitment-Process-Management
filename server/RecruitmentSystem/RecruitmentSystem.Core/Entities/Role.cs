using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace RecruitmentSystem.Core.Entities
{
    public class Role : IdentityRole<Guid>
    {
        [StringLength(255)]
        public string Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}
