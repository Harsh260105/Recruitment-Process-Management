using AutoMapper;
using Microsoft.Extensions.Logging;
using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Core.Interfaces;
using RecruitmentSystem.Services.Interfaces;
using RecruitmentSystem.Shared.DTOs;

namespace RecruitmentSystem.Services.Implementations
{
    public class StaffProfileService : IStaffProfileService
    {

        private readonly IStaffProfileRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<StaffProfileService> _logger;

        public StaffProfileService(
            IStaffProfileRepository repository,
            IMapper mapper,
            ILogger<StaffProfileService> logger)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<StaffProfileResponseDto> CreateProfileAsync(CreateStaffProfileDto dto, Guid userId)
        {
            try
            {
                if (await _repository.ExistsByUserIdAsync(userId))
                {
                    _logger.LogWarning("A staff profile already exists for user {UserId}.", userId);
                    throw new InvalidOperationException("A staff profile already exists for this user.");
                }

                var profile = _mapper.Map<StaffProfile>(dto);
                profile.UserId = userId;

                var createdProfile = await _repository.CreateAsync(profile);
                var profileWithUser = await _repository.GetByIdAsync(createdProfile.Id);
                return _mapper.Map<StaffProfileResponseDto>(profileWithUser);
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating staff profile for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> DeleteProfileAsync(Guid id)
        {
            try
            {
                var result = await _repository.DeleteAsync(id);

                if (!result)
                {
                    throw new KeyNotFoundException("Staff profile not found.");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting staff profile with ID {Id}", id);
                throw;
            }
        }

        public async Task<StaffProfileResponseDto?> GetByIdAsync(Guid id)
        {
            try
            {
                var profile = await _repository.GetByIdAsync(id);
                if (profile == null)
                {
                    _logger.LogWarning("Staff profile not found for ID {Id}.", id);
                    return null;
                }

                return _mapper.Map<StaffProfileResponseDto>(profile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving staff profile with ID {Id}.", id);
                throw;
            }
        }

        public async Task<StaffProfileResponseDto?> GetByUserIdAsync(Guid userId)
        {
            try
            {
                var profile = await _repository.GetByUserIdAsync(userId);
                if (profile == null)
                {
                    _logger.LogWarning("Staff profile not found for user ID {UserId}.", userId);
                    return null;
                }

                return _mapper.Map<StaffProfileResponseDto>(profile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving staff profile for user ID {UserId}.", userId);
                throw;
            }
        }

        public async Task<StaffProfileResponseDto?> UpdateProfileAsync(Guid id, UpdateStaffProfileDto dto)
        {
            try
            {
                var profile = await _repository.GetByIdAsync(id);
                if (profile == null)
                {
                    _logger.LogWarning("Staff profile not found for ID {Id}.", id);
                    return null;
                }

                _mapper.Map(dto, profile);
                var updatedProfile = await _repository.UpdateAsync(profile);
                var profileWithUser = await _repository.GetByIdAsync(updatedProfile.Id);
                return _mapper.Map<StaffProfileResponseDto>(profileWithUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating staff profile with ID {Id}.", id);
                throw;
            }
        }
    }
}