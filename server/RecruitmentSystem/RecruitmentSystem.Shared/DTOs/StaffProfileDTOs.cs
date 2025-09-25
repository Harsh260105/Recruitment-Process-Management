using System.ComponentModel.DataAnnotations;

namespace RecruitmentSystem.Shared.DTOs
{
    public class CreateStaffProfileDto
    {
        [Required]
        [StringLength(50)]
        public string? EmployeeCode { get; set; }

        [Required]
        [StringLength(100)]
        public string? Department { get; set; }

        [Required]
        [StringLength(100)]
        public string? Location { get; set; }

        [Required]
        [StringLength(50)]
        public string? Status { get; set; }

        public DateTime? JoinedDate { get; set; }
    }

    public class StaffProfileResponseDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string? EmployeeCode { get; set; }
        public string? Department { get; set; }
        public string? Location { get; set; }
        public string? Status { get; set; }
        public DateTime? JoinedDate { get; set; }

        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
    }

    public class UpdateStaffProfileDto
    {
        [StringLength(100)]
        public string? Department { get; set; }

        [StringLength(100)]
        public string? Location { get; set; }

        [StringLength(50)]
        public string? Status { get; set; }

        public DateTime? JoinedDate { get; set; }
    }
}