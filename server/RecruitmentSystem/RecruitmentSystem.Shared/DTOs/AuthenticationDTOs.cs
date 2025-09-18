using System.ComponentModel.DataAnnotations;

namespace RecruitmentSystem.Shared.DTOs
{
    public class LoginDto
    {
        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        [Required]
        [MinLength(6)]
        public required string Password { get; set; }

        public bool RememberMe { get; set; } = false;
    }

    public class CandidateRegisterDto
    {
        [Required]
        [StringLength(100)]
        public required string FirstName { get; set; }

        [Required]
        [StringLength(100)]
        public required string LastName { get; set; }

        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        [Required]
        [MinLength(6)]
        public required string Password { get; set; }

        [Required]
        [Compare("Password")]
        public required string ConfirmPassword { get; set; }

        [Phone]
        public string? PhoneNumber { get; set; }
    }

    // initial super admin
    public class InitialAdminDto
    {
        [Required]
        [StringLength(100)]
        public required string FirstName { get; set; }

        [Required]
        [StringLength(100)]
        public required string LastName { get; set; }

        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        [Required]
        [MinLength(8)]
        public required string Password { get; set; }

        [Required]
        [Compare("Password")]
        public required string ConfirmPassword { get; set; }

        [Phone]
        public string? PhoneNumber { get; set; }

        [StringLength(200)]
        public string? CompanyName { get; set; }
    }

    public class RegisterDto
    {
        [Required]
        [StringLength(100)]
        public required string FirstName { get; set; }

        [Required]
        [StringLength(100)]
        public required string LastName { get; set; }

        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        [Required]
        [MinLength(6)]
        public required string Password { get; set; }

        [Required]
        [Compare("Password")]
        public required string ConfirmPassword { get; set; }

        [Phone]
        public string? PhoneNumber { get; set; }

        [Required]
        public List<string> Roles { get; set; } = new List<string>();
    }

    public class AuthResponseDto
    {
        public required string Token { get; set; }
        public DateTime Expiration { get; set; }
        public UserProfileDto? User { get; set; }
    }

    public class UserProfileDto
    {
        public Guid Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public bool IsActive { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
    }

    public class ChangePasswordDto
    {
        [Required]
        public required string CurrentPassword { get; set; }

        [Required]
        [MinLength(6)]
        public required string NewPassword { get; set; }

        [Required]
        [Compare("NewPassword")]
        public required string ConfirmNewPassword { get; set; }
    }

    public class ForgotPasswordDto
    {
        [Required]
        [EmailAddress]
        public required string Email { get; set; }
    }

    public class ResetPasswordDto
    {
        [Required]
        public required string UserId { get; set; }

        [Required]
        public required string Token { get; set; }

        [Required]
        [MinLength(6)]
        public required string NewPassword { get; set; }

        [Required]
        [Compare("NewPassword")]
        public required string ConfirmNewPassword { get; set; }
    }

    public class ConfirmEmailDto
    {
        [Required]
        public required string UserId { get; set; }

        [Required]
        public required string Token { get; set; }
    }

    public class ResendVerificationDto
    {
        [Required]
        [EmailAddress]
        public required string Email { get; set; }
    }
}