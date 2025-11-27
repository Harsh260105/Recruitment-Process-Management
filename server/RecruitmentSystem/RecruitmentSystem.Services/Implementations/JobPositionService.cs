using AutoMapper;
using Microsoft.Extensions.Logging;
using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Core.Entities.Projections;
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
                job!.ClosedDate = DateTime.UtcNow;
                job!.UpdatedAt = DateTime.UtcNow;
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

        public async Task<PagedResult<TSummary>> GetJobSummariesWithFiltersAsync<TSummary>(
            int pageNumber = 1, int pageSize = 25,
            string? status = null,
            string? department = null,
            string? location = null,
            string? experienceLevel = null,
            List<int>? skillIds = null,
            DateTime? createdFromDate = null,
            DateTime? createdToDate = null,
            DateTime? deadlineFromDate = null,
            DateTime? deadlineToDate = null) where TSummary : JobPositionPublicSummaryDto
        {
            if (pageNumber < 1)
                throw new ArgumentException("Page number must be greater than 0", nameof(pageNumber));

            if (pageSize < 1 || pageSize > 100)
                throw new ArgumentException("Page size must be between 1 and 100", nameof(pageSize));

            var resultTask = _repository.GetPositionSummariesWithFiltersAsync(
                pageNumber, pageSize, status, department, location, experienceLevel,
                skillIds, createdFromDate, createdToDate, deadlineFromDate, deadlineToDate);

            return await MapSummaryResultAsync<TSummary>(resultTask, pageNumber, pageSize);
        }

        public async Task<PagedResult<TSummary>> GetActiveJobSummariesAsync<TSummary>(int pageNumber = 1, int pageSize = 20)
            where TSummary : JobPositionPublicSummaryDto
        {
            if (pageNumber < 1)
                throw new ArgumentException("Page number must be greater than 0", nameof(pageNumber));

            if (pageSize < 1 || pageSize > 100)
                throw new ArgumentException("Page size must be between 1 and 100", nameof(pageSize));

            var resultTask = _repository.GetActiveSummariesAsync(pageNumber, pageSize);
            return await MapSummaryResultAsync<TSummary>(resultTask, pageNumber, pageSize);
        }

        public async Task<PagedResult<TSummary>> SearchJobSummariesAsync<TSummary>(
            string searchTerm, int pageNumber = 1, int pageSize = 15,
            string? department = null,
            string? status = null) where TSummary : JobPositionPublicSummaryDto
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                throw new ArgumentException("Search term cannot be empty", nameof(searchTerm));

            if (pageNumber < 1)
                throw new ArgumentException("Page number must be greater than 0", nameof(pageNumber));

            if (pageSize < 1 || pageSize > 100)
                throw new ArgumentException("Page size must be between 1 and 100", nameof(pageSize));

            var resultTask = _repository.SearchPositionSummariesAsync(searchTerm, pageNumber, pageSize, department, status);
            return await MapSummaryResultAsync<TSummary>(resultTask, pageNumber, pageSize);
        }

        // public async Task<PagedResult<TSummary>> GetJobSummariesByDepartmentAsync<TSummary>(string department, int pageNumber = 1, int pageSize = 15)
        //     where TSummary : JobPositionPublicSummaryDto
        // {
        //     if (string.IsNullOrWhiteSpace(department))
        //         throw new ArgumentException("Department cannot be empty", nameof(department));

        //     if (pageNumber < 1)
        //         throw new ArgumentException("Page number must be greater than 0", nameof(pageNumber));

        //     if (pageSize < 1 || pageSize > 100)
        //         throw new ArgumentException("Page size must be between 1 and 100", nameof(pageSize));

        //     var resultTask = _repository.GetSummaryByDepartmentAsync(department, pageNumber, pageSize);
        //     return await MapSummaryResultAsync<TSummary>(resultTask, pageNumber, pageSize);
        // }

        // public async Task<PagedResult<TSummary>> GetJobSummariesByStatusAsync<TSummary>(string status, int pageNumber = 1, int pageSize = 15)
        //     where TSummary : JobPositionPublicSummaryDto
        // {
        //     if (string.IsNullOrWhiteSpace(status))
        //         throw new ArgumentException("Status cannot be empty", nameof(status));

        //     if (pageNumber < 1)
        //         throw new ArgumentException("Page number must be greater than 0", nameof(pageNumber));

        //     if (pageSize < 1 || pageSize > 100)
        //         throw new ArgumentException("Page size must be between 1 and 100", nameof(pageSize));

        //     var resultTask = _repository.GetSummaryByStatusAsync(status, pageNumber, pageSize);
        //     return await MapSummaryResultAsync<TSummary>(resultTask, pageNumber, pageSize);
        // }

        private async Task<PagedResult<TSummary>> MapSummaryResultAsync<TSummary>(
            Task<(List<JobPositionSummaryProjection> Items, int TotalCount)> fetchTask,
            int pageNumber,
            int pageSize) where TSummary : JobPositionPublicSummaryDto
        {
            try
            {
                var (items, totalCount) = await fetchTask;
                var dtos = _mapper.Map<List<TSummary>>(items);
                return PagedResult<TSummary>.Create(dtos, totalCount, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving job summaries for {SummaryType}", typeof(TSummary).Name);
                throw;
            }
        }
    }
}