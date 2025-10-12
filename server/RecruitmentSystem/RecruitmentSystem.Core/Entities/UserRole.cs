
using Microsoft.AspNetCore.Identity;

namespace RecruitmentSystem.Core.Entities
{
    public class UserRole : IdentityUserRole<Guid>
    {
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public required virtual User User { get; set; }
        public required virtual Role Role { get; set; }
    }
}
