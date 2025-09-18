using RecruitmentSystem.Shared.DTOs;

namespace RecruitmentSystem.Services.Interfaces
{
    public interface IAuthenticationService
    {
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
        Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
        Task<AuthResponseDto> RegisterCandidateAsync(CandidateRegisterDto registerDto);
        Task<AuthResponseDto> RegisterInitialSuperAdminAsync(InitialAdminDto registerDto);
        Task<bool> HasSuperAdminAsync();
        Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordDto changePasswordDto);
        Task<UserProfileDto> GetUserProfileAsync(Guid userId);
        Task<bool> LogoutAsync(Guid userId);
        Task<List<string>> GetUserRolesAsync(Guid userId);
    }
}