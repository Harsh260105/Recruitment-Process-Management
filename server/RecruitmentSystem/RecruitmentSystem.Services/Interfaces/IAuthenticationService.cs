using Microsoft.AspNetCore.Http;
using RecruitmentSystem.Shared.DTOs;

namespace RecruitmentSystem.Services.Interfaces
{
    public interface IAuthenticationService
    {
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
        Task<AuthResponseDto> RegisterStaffAsync(RegisterStaffDto registerDto);
        Task<RegisterResponseDto> RegisterCandidateAsync(CandidateRegisterDto registerDto);
        Task<AuthResponseDto> RegisterInitialSuperAdminAsync(InitialAdminDto registerDto);
        Task<List<RegisterResponseDto>> BulkRegisterCandidatesAsync(IFormFile file);
        Task<bool> HasSuperAdminAsync();
        Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordDto changePasswordDto);
        Task<UserProfileDto> GetUserProfileAsync(Guid userId);
        Task<bool> LogoutAsync(Guid userId);
        Task<List<string>> GetUserRolesAsync(Guid userId);
    }
}