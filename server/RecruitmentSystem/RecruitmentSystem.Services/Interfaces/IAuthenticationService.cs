using Microsoft.AspNetCore.Http;
using RecruitmentSystem.Shared.DTOs;

namespace RecruitmentSystem.Services.Interfaces
{
    public interface IAuthenticationService
    {
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto, string ipAddress, string? userAgent);
        Task<AuthResponseDto> RegisterStaffAsync(RegisterStaffDto registerDto, string ipAddress, string? userAgent);
        Task<RegisterResponseDto> RegisterCandidateAsync(CandidateRegisterDto registerDto);
        Task<AuthResponseDto> RegisterInitialSuperAdminAsync(InitialAdminDto registerDto, string ipAddress, string? userAgent);
        Task<List<RegisterResponseDto>> BulkRegisterCandidatesAsync(IFormFile file);
        Task<bool> HasSuperAdminAsync();
        Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordDto changePasswordDto);
        Task<UserProfileDto> GetUserProfileAsync(Guid userId);
        Task LogoutAsync(Guid userId, string? refreshToken, string ipAddress);
        Task<List<string>> GetUserRolesAsync(Guid userId);
        Task<List<UserProfileDto>> GetAllRecruitersAsync();
        Task<AuthResponseDto> RefreshTokenAsync(string refreshToken, string ipAddress, string? userAgent);
        Task RevokeRefreshTokenAsync(string refreshToken, string ipAddress, string? reason = null);
    }
}