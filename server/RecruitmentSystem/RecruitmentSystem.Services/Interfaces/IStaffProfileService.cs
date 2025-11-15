using RecruitmentSystem.Shared.DTOs;

namespace RecruitmentSystem.Services.Interfaces
{
    public interface IStaffProfileService
    {
        Task<StaffProfileResponseDto> CreateProfileAsync(CreateStaffProfileDto dto, Guid userId);
        Task<StaffProfileResponseDto?> GetByIdAsync(Guid id);
        Task<StaffProfileResponseDto?> GetByUserIdAsync(Guid userId);
        Task<StaffProfileResponseDto?> UpdateProfileAsync(Guid id, UpdateStaffProfileDto dto);
        Task<bool> DeleteProfileAsync(Guid id);
    }
}