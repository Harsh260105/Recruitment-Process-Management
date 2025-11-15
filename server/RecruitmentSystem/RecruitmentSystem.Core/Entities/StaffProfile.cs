using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RecruitmentSystem.Core.Entities
{
    public class StaffProfile : BaseEntity
    {
        // Foreign key to User
        [Required]
        public Guid UserId { get; set; }

        [Required]
        [StringLength(50)]
        public required string EmployeeCode { get; set; }

        [Required]
        [StringLength(100)]
        public required string Department { get; set; }

        [Required]
        [StringLength(100)]
        public required string Location { get; set; }

        [Required]
        [StringLength(50)]
        public required string Status { get; set; }

        public DateTime? JoinedDate { get; set; }

        // Navigation property
        [ForeignKey("UserId")]
        public virtual required User User { get; set; }
    }
}