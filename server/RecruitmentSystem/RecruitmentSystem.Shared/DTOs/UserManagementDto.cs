namespace RecruitmentSystem.Shared.DTOs
{
    public class UserSearchFilters
    {
        public string? Search { get; set; }
        public List<string>? Roles { get; set; }
        public bool? IsActive { get; set; }
        public bool? HasProfile { get; set; } // Only applies to Candidate role
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 25;
    }

    public class UserSummaryDto
    {
        public Guid UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public bool IsActive { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public bool HasCandidateProfile { get; set; }
        public bool HasStaffProfile { get; set; }
        public DateTime RegisteredAt { get; set; }
        public bool IsCurrentlyLockedOut { get; set; }
    }

    public class UserDetailsDto
    {
        public Guid UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public bool IsActive { get; set; }
        public bool EmailConfirmed { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public bool HasCandidateProfile { get; set; }
        public bool HasStaffProfile { get; set; }
        public DateTime RegisteredAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int AccessFailedCount { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public bool IsCurrentlyLockedOut { get; set; }
    }

    // Bulk update user status
    public class UpdateUserStatusRequest
    {
        public List<Guid> UserIds { get; set; } = new List<Guid>();
        public bool IsActive { get; set; }
    }

    public class UpdateUserStatusResult
    {
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }

    // Bulk admin password reset
    public class AdminResetPasswordRequest
    {
        public List<Guid> UserIds { get; set; } = new List<Guid>();
        public bool SendEmail { get; set; } = true;
    }

    public class AdminResetPasswordResult
    {
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<PasswordResetInfo> ResetInfo { get; set; } = new List<PasswordResetInfo>();
        public List<string> Errors { get; set; } = new List<string>();
    }

    public class PasswordResetInfo
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? TemporaryPassword { get; set; }
    }

    // Single user update
    public class UpdateUserInfoRequest
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? PhoneNumber { get; set; }
    }

    // Bulk role management
    public class ManageUserRolesRequest
    {
        public List<Guid> UserIds { get; set; } = new List<Guid>();
        public List<string> RolesToAdd { get; set; } = new List<string>();
        public List<string> RolesToRemove { get; set; } = new List<string>();
    }

    public class ManageUserRolesResult
    {
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }

    // End user lockout
    public class EndUserLockoutRequest
    {
        public List<Guid> UserIds { get; set; } = new List<Guid>();
    }

    public class EndUserLockoutResult
    {
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
}
