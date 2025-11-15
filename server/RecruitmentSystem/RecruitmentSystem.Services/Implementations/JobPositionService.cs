using AutoMapper;
using Microsoft.Extensions.Logging;
using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Core.Interfaces;
using RecruitmentSystem.Services.Interfaces;
using RecruitmentSystem.Shared.DTOs;

namespace RecruitmentSystem.Services.Implementations
{
    public class JobPositionService : IJobPositionService
    {
        private readonly IJobPositionRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<JobPositionService> _logger;

        public JobPositionService(
            IJobPositionRepository repository,
            IMapper mapper,
            ILogger<JobPositionService> logger)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task CloseJobAsync(Guid id)
        {
            try
            {
                var job = await _repository.GetByIdAsync(id);

                job!.Status = "Closed";
                await _repository.UpdateAsync(job!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing job with ID {JobId}.", id);
                throw;
            }
        }

        public async Task<JobPositionResponseDto> CreateJobAsync(CreateJobPositionDto dto, Guid creatorId)
        {
            try
            {
                var job = _mapper.Map<JobPosition>(dto);
                job.CreatedByUserId = creatorId;

                await _repository.CreateAsync(job);

                if (dto.JobPositionSkills != null && dto.JobPositionSkills.Any())
                {
                    var jobSkills = _mapper.Map<List<JobPositionSkill>>(dto.JobPositionSkills);
                    foreach (var skill in jobSkills)
                    {
                        skill.JobPositionId = job.Id;
                    }
                    await _repository.AddSkillsAsync(jobSkills);
                }

                var createdJob = await _repository.GetByIdAsync(job.Id);
                return _mapper.Map<JobPositionResponseDto>(createdJob);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating job for creator {CreatorId}.", creatorId);
                throw;
            }
        }

        public async Task DeleteJobAsync(Guid id)
        {
            try
            {
                await _repository.DeleteAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting job with ID {JobId}.", id);
                throw;
            }
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            try
            {
                return await _repository.ExistsAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking existence of job with ID {JobId}.", id);
                throw;
            }
        }

        public async Task<List<JobPositionResponseDto>> GetActiveJobsAsync()
        {
            try
            {
                var activeJobs = await _repository.GetActiveAsync();
                return _mapper.Map<List<JobPositionResponseDto>>(activeJobs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active jobs.");
                throw;
            }
        }

        public async Task<JobPositionResponseDto?> GetJobByIdAsync(Guid id)
        {
            try
            {
                var job = await _repository.GetByIdAsync(id);
                return _mapper.Map<JobPositionResponseDto>(job);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving job with ID {JobId}.", id);
                throw;
            }
        }

        public async Task<List<JobPositionResponseDto>> GetJobsByDepartmentAsync(string department)
        {
            try
            {
                var jobs = await _repository.GetByDepartmentAsync(department);
                return _mapper.Map<List<JobPositionResponseDto>>(jobs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving jobs for department {Department}.", department);
                throw;
            }
        }

        public async Task<List<JobPositionResponseDto>> GetJobsByStatusAsync(string status)
        {
            try
            {
                var jobs = await _repository.GetByStatusAsync(status);
                return _mapper.Map<List<JobPositionResponseDto>>(jobs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving jobs for status {Status}.", status);
                throw;
            }
        }

        public async Task<List<JobPositionResponseDto>> GetJobsWithFiltersAsync(string? status = null, string? department = null, string? location = null, string? experienceLevel = null, List<int>? skillIds = null, DateTime? createdFromDate = null, DateTime? createdToDate = null, DateTime? deadlineFromDate = null, DateTime? deadlineToDate = null)
        {
            try
            {
                var jobs = await _repository.GetPositionsWithFiltersAsync(status, department, location, experienceLevel, skillIds, createdFromDate, createdToDate, deadlineFromDate, deadlineToDate);
                return _mapper.Map<List<JobPositionResponseDto>>(jobs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving jobs with filters.");
                throw;
            }
        }

        public async Task<List<JobPositionResponseDto>> SearchJobsAsync(string searchTerm, string? department = null, string? status = null)
        {
            try
            {
                var jobs = await _repository.SearchPositionsAsync(searchTerm, department, status);
                return _mapper.Map<List<JobPositionResponseDto>>(jobs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching jobs with term {SearchTerm}.", searchTerm);
                throw;
            }
        }

        public async Task<JobPositionResponseDto> UpdateJobAsync(Guid id, UpdateJobPositionDto dto)
        {
            try
            {
                var existingJob = await _repository.GetByIdAsync(id);

                _mapper.Map(dto, existingJob!);
                await _repository.UpdateAsync(existingJob!);

                if (dto.Skills != null)
                {
                    await _repository.RemoveSkillsAsync(id);

                    var newSkills = _mapper.Map<List<JobPositionSkill>>(dto.Skills);
                    foreach (var skill in newSkills)
                    {
                        skill.JobPositionId = id;
                    }
                    await _repository.AddSkillsAsync(newSkills);
                }

                var updatedJob = await _repository.GetByIdAsync(id);
                return _mapper.Map<JobPositionResponseDto>(updatedJob!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating job with ID {JobId}.", id);
                throw;
            }
        }
    }
}