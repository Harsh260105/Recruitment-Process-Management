using Microsoft.AspNetCore.Http;
using RecruitmentSystem.Shared.DTOs;
using RecruitmentSystem.Shared.DTOs.CandidateProfile;

namespace RecruitmentSystem.Services.Interfaces
{
    public interface ICandidateProfileService
    {
        Task<CandidateProfileResponseDto> CreateProfileAsync(CandidateProfileDto dto, Guid createdBy);
        Task<CandidateProfileResponseDto?> GetByIdAsync(Guid id);
        Task<CandidateProfileResponseDto?> GetByUserIdAsync(Guid userId);
        Task<CandidateProfileResponseDto?> UpdateProfileAsync(Guid id, UpdateCandidateProfileDto dto);
        Task<bool> DeleteProfileAsync(Guid id);
        Task SetApplicationOverrideAsync(Guid candidateProfileId, CandidateApplicationOverrideRequestDto dto, Guid approvedByUserId);

        // Skills management
        Task<List<CandidateSkillDto>> AddSkillsAsync(Guid candidateProfileId, List<CreateCandidateSkillDto> skills);
        Task<CandidateSkillDto> UpdateSkillAsync(Guid candidateProfileId, int skillId, UpdateCandidateSkillDto skill);
        Task<bool> RemoveSkillAsync(Guid candidateProfileId, int skillId);
        Task<List<CandidateSkillDto>> GetSkillsAsync(Guid candidateProfileId);

        // Education management
        Task<CandidateEducationDto> AddEducationAsync(Guid candidateProfileId, CreateCandidateEducationDto education);
        Task<CandidateEducationDto> UpdateEducationAsync(Guid educationId, UpdateCandidateEducationDto education);
        Task<bool> RemoveEducationAsync(Guid educationId);
        Task<List<CandidateEducationDto>> GetEducationAsync(Guid candidateProfileId);

        // Work experience management
        Task<CandidateWorkExperienceDto> AddWorkExperienceAsync(Guid candidateProfileId, CreateCandidateWorkExperienceDto workExperience);
        Task<CandidateWorkExperienceDto> UpdateWorkExperienceAsync(Guid workExperienceId, UpdateCandidateWorkExperienceDto workExperience);
        Task<bool> RemoveWorkExperienceAsync(Guid workExperienceId);
        Task<List<CandidateWorkExperienceDto>> GetWorkExperienceAsync(Guid candidateProfileId);

        // Resume management
        Task<CandidateProfileResponseDto> UploadResumeAsync(Guid candidateProfileId, IFormFile file);
        Task<string?> GetResumeUrlAsync(Guid candidateProfileId);
        Task<bool> DeleteResumeAsync(Guid candidateProfileId);

        // Search
        Task<PagedResult<CandidateSearchResultDto>> SearchCandidatesAsync(CandidateSearchFilters filters, Guid? assignedRecruiterId = null);
    }
}