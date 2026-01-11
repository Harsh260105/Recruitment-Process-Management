using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Core.Interfaces;
using RecruitmentSystem.Services.Interfaces;
using RecruitmentSystem.Shared.DTOs.CandidateProfile;
using RecruitmentSystem.Shared.DTOs;

namespace RecruitmentSystem.Services.Implementations
{
    public class CandidateProfileService : ICandidateProfileService
    {
        #region Dependencies & Constructor

        private readonly ICandidateProfileRepository _repository;
        private readonly IS3Service _s3Service;
        private readonly IMapper _mapper;
        private readonly ILogger<CandidateProfileService> _logger;

        public CandidateProfileService(
            ICandidateProfileRepository repository,
            IS3Service s3Service,
            IMapper mapper,
            ILogger<CandidateProfileService> logger)
        {
            _repository = repository;
            _s3Service = s3Service;
            _mapper = mapper;
            _logger = logger;
        }

        #endregion

        #region Profile CRUD Operations

        public async Task<CandidateProfileResponseDto> CreateProfileAsync(CandidateProfileDto dto, Guid createdBy)
        {
            try
            {
                var profile = _mapper.Map<CandidateProfile>(dto);
                profile.UserId = createdBy;
                profile.CreatedBy = createdBy;

                var createdProfile = await _repository.CreateAsync(profile);

                if (dto.Skills?.Any() == true)
                {
                    foreach (var skillDto in dto.Skills)
                    {
                        var skill = _mapper.Map<CandidateSkill>(skillDto);
                        skill.CandidateProfileId = createdProfile.Id;
                        await _repository.AddSkillAsync(skill);
                    }
                }

                if (dto.Education?.Any() == true)
                {
                    foreach (var educationDto in dto.Education)
                    {
                        var education = _mapper.Map<CandidateEducation>(educationDto);
                        education.CandidateProfileId = createdProfile.Id;
                        await _repository.AddEducationAsync(education);
                    }
                }

                if (dto.WorkExperience?.Any() == true)
                {
                    foreach (var workExpDto in dto.WorkExperience)
                    {
                        var workExp = _mapper.Map<CandidateWorkExperience>(workExpDto);
                        workExp.CandidateProfileId = createdProfile.Id;
                        await _repository.AddWorkExperienceAsync(workExp);
                    }
                }

                // new object with all details
                var fullProfile = await _repository.GetByIdAsync(createdProfile.Id);
                return _mapper.Map<CandidateProfileResponseDto>(fullProfile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating candidate profile");
                throw;
            }
        }

        public async Task<CandidateProfileResponseDto?> GetByIdAsync(Guid id)
        {
            try
            {
                var profile = await _repository.GetByIdAsync(id);
                return profile == null ? null : _mapper.Map<CandidateProfileResponseDto>(profile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting candidate profile by ID: {Id}", id);
                throw;
            }
        }

        public async Task<CandidateProfileResponseDto?> GetByUserIdAsync(Guid userId)
        {
            try
            {
                var profile = await _repository.GetByUserIdAsync(userId);
                return profile == null ? null : _mapper.Map<CandidateProfileResponseDto>(profile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting candidate profile by user ID: {UserId}", userId);
                throw;
            }
        }

        public async Task<CandidateProfileResponseDto?> UpdateProfileAsync(Guid id, UpdateCandidateProfileDto dto)
        {
            try
            {
                var existingProfile = await _repository.GetByIdAsync(id);

                // Applying partial updates using AutoMapper with null-safe mapping
                _mapper.Map(dto, existingProfile!);

                var updatedProfile = await _repository.UpdateAsync(existingProfile!);

                return _mapper.Map<CandidateProfileResponseDto>(updatedProfile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating candidate profile with ID: {Id}", id);
                throw;
            }
        }

        public async Task<bool> DeleteProfileAsync(Guid id)
        {
            try
            {
                var exists = await _repository.DeleteAsync(id);
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting candidate profile with ID: {Id}", id);
                throw;
            }
        }

        public async Task SetApplicationOverrideAsync(
            Guid candidateProfileId,
            CandidateApplicationOverrideRequestDto dto,
            Guid approvedByUserId)
        {
            try
            {
                
                if (dto.CanBypassApplicationLimits)
                {
                    if (dto.OverrideExpiresAt.HasValue && dto.OverrideExpiresAt.Value <= DateTime.UtcNow)
                    {
                        throw new ArgumentException("Override expiration must be in the future.", nameof(dto.OverrideExpiresAt));
                    }
                }

                var success = await _repository.UpdateApplicationOverrideAsync(
                    candidateProfileId,
                    dto.CanBypassApplicationLimits,
                    dto.CanBypassApplicationLimits ? dto.OverrideExpiresAt : null);

                if (!success)
                {
                    throw new ArgumentException($"Candidate profile {candidateProfileId} not found", nameof(candidateProfileId));
                }

                _logger.LogInformation(
                    "User {UserId} updated application override for candidate {CandidateId}. Enabled: {Enabled}, ExpiresAt: {ExpiresAt}",
                    approvedByUserId,
                    candidateProfileId,
                    dto.CanBypassApplicationLimits,
                    dto.CanBypassApplicationLimits ? dto.OverrideExpiresAt : null);
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating application override for candidate {CandidateId}", candidateProfileId);
                throw;
            }
        }

        #endregion

        #region Skills Management

        // Skills management
        public async Task<List<CandidateSkillDto>> AddSkillsAsync(Guid candidateProfileId, List<CreateCandidateSkillDto> skills)
        {
            try
            {
                var addedSkills = new List<CandidateSkillDto>();

                foreach (var skillDto in skills)
                {
                    var skill = _mapper.Map<CandidateSkill>(skillDto);
                    skill.CandidateProfileId = candidateProfileId;

                    var addedSkill = await _repository.AddSkillAsync(skill);
                    addedSkills.Add(_mapper.Map<CandidateSkillDto>(addedSkill));
                }

                return addedSkills;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding skills to candidate {CandidateId}", candidateProfileId);
                throw;
            }
        }

        public async Task<CandidateSkillDto> UpdateSkillAsync(Guid candidateProfileId, int skillId, UpdateCandidateSkillDto dto)
        {
            try
            {
                var existingSkill = await _repository.GetSkillAsync(candidateProfileId, skillId);
                if (existingSkill == null)
                {
                    throw new ArgumentException($"Skill not found for candidate {candidateProfileId} and skill {skillId}");
                }

                _mapper.Map(dto, existingSkill);
                var updatedSkill = await _repository.UpdateSkillAsync(existingSkill);

                return _mapper.Map<CandidateSkillDto>(updatedSkill);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating skill {SkillId} for candidate {CandidateId}", skillId, candidateProfileId);
                throw;
            }
        }

        public async Task<bool> RemoveSkillAsync(Guid candidateProfileId, int skillId)
        {
            try
            {
                return await _repository.RemoveSkillAsync(candidateProfileId, skillId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing skill {SkillId} from candidate {CandidateId}", skillId, candidateProfileId);
                throw;
            }
        }

        public async Task<List<CandidateSkillDto>> GetSkillsAsync(Guid candidateProfileId)
        {
            try
            {
                var skills = await _repository.GetSkillsAsync(candidateProfileId);
                return _mapper.Map<List<CandidateSkillDto>>(skills);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting skills for candidate {CandidateId}", candidateProfileId);
                throw;
            }
        }

        #endregion

        #region Education Management

        // Education management
        public async Task<CandidateEducationDto> AddEducationAsync(Guid candidateProfileId, CreateCandidateEducationDto dto)
        {
            try
            {
                var education = _mapper.Map<CandidateEducation>(dto);
                education.CandidateProfileId = candidateProfileId;

                var addedEducation = await _repository.AddEducationAsync(education);
                return _mapper.Map<CandidateEducationDto>(addedEducation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding education to candidate {CandidateId}", candidateProfileId);
                throw;
            }
        }

        public async Task<bool> RemoveEducationAsync(Guid educationId)
        {
            try
            {
                return await _repository.RemoveEducationAsync(educationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing education {EducationId}", educationId);
                throw;
            }
        }

        public async Task<List<CandidateEducationDto>> GetEducationAsync(Guid candidateProfileId)
        {
            try
            {
                var educationList = await _repository.GetEducationAsync(candidateProfileId);
                return _mapper.Map<List<CandidateEducationDto>>(educationList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting education for candidate {CandidateId}", candidateProfileId);
                throw;
            }
        }

        public async Task<CandidateEducationDto> UpdateEducationAsync(Guid educationId, UpdateCandidateEducationDto dto)
        {
            try
            {
                var existingEducation = await _repository.GetEducationByIdAsync(educationId);
                if (existingEducation == null)
                {
                    throw new ArgumentException($"Education record not found: {educationId}");
                }

                _mapper.Map(dto, existingEducation);
                var updatedEducation = await _repository.UpdateEducationAsync(existingEducation);

                return _mapper.Map<CandidateEducationDto>(updatedEducation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating education {EducationId}", educationId);
                throw;
            }
        }

        #endregion

        #region Work Experience Management

        // Work experience management
        public async Task<CandidateWorkExperienceDto> AddWorkExperienceAsync(Guid candidateProfileId, CreateCandidateWorkExperienceDto dto)
        {
            try
            {
                var workExp = _mapper.Map<CandidateWorkExperience>(dto);
                workExp.CandidateProfileId = candidateProfileId;

                var addedWorkExp = await _repository.AddWorkExperienceAsync(workExp);
                return _mapper.Map<CandidateWorkExperienceDto>(addedWorkExp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding work experience to candidate {CandidateId}", candidateProfileId);
                throw;
            }
        }

        public async Task<bool> RemoveWorkExperienceAsync(Guid workExperienceId)
        {
            try
            {
                return await _repository.RemoveWorkExperienceAsync(workExperienceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing work experience {WorkExperienceId}", workExperienceId);
                throw;
            }
        }

        public async Task<List<CandidateWorkExperienceDto>> GetWorkExperienceAsync(Guid candidateProfileId)
        {
            try
            {
                var workExperienceList = await _repository.GetWorkExperienceAsync(candidateProfileId);
                return _mapper.Map<List<CandidateWorkExperienceDto>>(workExperienceList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting work experience for candidate {CandidateId}", candidateProfileId);
                throw;
            }
        }

        public async Task<CandidateWorkExperienceDto> UpdateWorkExperienceAsync(Guid workExperienceId, UpdateCandidateWorkExperienceDto dto)
        {
            try
            {
                var existingWorkExperience = await _repository.GetWorkExperienceByIdAsync(workExperienceId);
                if (existingWorkExperience == null)
                {
                    throw new ArgumentException($"Work experience record not found: {workExperienceId}");
                }

                _mapper.Map(dto, existingWorkExperience);
                var updatedWorkExperience = await _repository.UpdateWorkExperienceAsync(existingWorkExperience);

                return _mapper.Map<CandidateWorkExperienceDto>(updatedWorkExperience);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating work experience {WorkExperienceId}", workExperienceId);
                throw;
            }
        }

        #endregion

        #region Resume Management

        public async Task<CandidateProfileResponseDto> UploadResumeAsync(Guid candidateProfileId, IFormFile file)
        {
            try
            {
                var existingProfile = await _repository.GetByIdAsync(candidateProfileId);

                // Delete old resume if exists
                if (!string.IsNullOrEmpty(existingProfile!.ResumeFilePath))
                {
                    await _s3Service.DeleteResumeAsync(existingProfile!.ResumeFilePath);
                }

                var fileKey = await _s3Service.UploadResumeAsync(file, existingProfile!.UserId.ToString());

                existingProfile!.ResumeFileName = file.FileName;
                existingProfile!.ResumeFilePath = fileKey;

                var updatedProfile = await _repository.UpdateAsync(existingProfile!);
                return _mapper.Map<CandidateProfileResponseDto>(updatedProfile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading resume for candidate {CandidateId}", candidateProfileId);
                throw;
            }
        }

        public async Task<string?> GetResumeUrlAsync(Guid candidateProfileId)
        {
            try
            {
                var profile = await _repository.GetByIdAsync(candidateProfileId);
                if (string.IsNullOrEmpty(profile!.ResumeFilePath))
                {
                    return null;
                }

                return await _s3Service.GetResumeUrlAsync(profile!.ResumeFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting resume URL for candidate {CandidateId}", candidateProfileId);
                return null;
            }
        }

        public async Task<bool> DeleteResumeAsync(Guid candidateProfileId)
        {
            try
            {
                var existingProfile = await _repository.GetByIdAsync(candidateProfileId);
                if (string.IsNullOrEmpty(existingProfile!.ResumeFilePath))
                {
                    return false;
                }

                var deleted = await _s3Service.DeleteResumeAsync(existingProfile!.ResumeFilePath);
                if (deleted)
                {
                    existingProfile!.ResumeFileName = null;
                    existingProfile!.ResumeFilePath = null;
                    await _repository.UpdateAsync(existingProfile!);
                }

                return deleted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting resume for candidate {CandidateId}", candidateProfileId);
                return false;
            }
        }

        #endregion

        #region Search

        public async Task<PagedResult<CandidateSearchResultDto>> SearchCandidatesAsync(CandidateSearchFilters filters, Guid? assignedRecruiterId = null)
        {
            try
            {
                var (items, totalCount) = await _repository.SearchCandidatesAsync(
                    filters.Query,
                    filters.Skills,
                    filters.Location,
                    filters.MinExperience,
                    filters.MaxExperience,
                    filters.MinExpectedCTC,
                    filters.MaxExpectedCTC,
                    filters.MaxNoticePeriod,
                    filters.IsOpenToRelocation,
                    filters.Degree,
                    filters.MinGraduationYear,
                    filters.MaxGraduationYear,
                    filters.PageNumber,
                    filters.PageSize,
                    assignedRecruiterId
                );

                var searchResults = _mapper.Map<List<CandidateSearchResultDto>>(items);

                return PagedResult<CandidateSearchResultDto>.Create(
                    searchResults,
                    totalCount,
                    filters.PageNumber,
                    filters.PageSize
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching candidates with filters: {@Filters}", filters);
                throw;
            }
        }

        #endregion
    }
}
