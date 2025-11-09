using AutoMapper;
using Microsoft.Extensions.Logging;
using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Core.Enums;
using RecruitmentSystem.Core.Interfaces;
using RecruitmentSystem.Services.Interfaces;
using RecruitmentSystem.Shared.DTOs;

namespace RecruitmentSystem.Services.Implementations
{
    public class JobApplicationManagementService : IJobApplicationManagementService
    {
        private readonly IJobApplicationRepository _jobApplicationRepository;
        private readonly IJobPositionRepository _jobPositionRepository;
        private readonly IAuthenticationService _authenticationService;
        private readonly IMapper _mapper;
        private readonly ILogger<JobApplicationManagementService> _logger;

        public JobApplicationManagementService(
            IJobApplicationRepository jobApplicationRepository,
            IJobPositionRepository jobPositionRepository,
            IAuthenticationService authenticationService,
            IMapper mapper,
            ILogger<JobApplicationManagementService> logger)
        {
            _jobApplicationRepository = jobApplicationRepository;
            _jobPositionRepository = jobPositionRepository;
            _authenticationService = authenticationService;
            _mapper = mapper;
            _logger = logger;
        }

        #region Core CRUD Operations

        public async Task<JobApplicationDto> CreateApplicationAsync(JobApplicationCreateDto dto)
        {
            try
            {
                // Map DTO to entity using AutoMapper
                var application = _mapper.Map<JobApplication>(dto);

                // Set additional properties not in DTO
                application.Status = ApplicationStatus.Applied;
                application.AppliedDate = DateTime.UtcNow;
                application.IsActive = true;

                // Auto-assign a recruiter randomly from available recruiters
                var recruiters = await _authenticationService.GetAllRecruitersAsync();
                if (recruiters.Any())
                {
                    var random = new Random();
                    var assignedRecruiter = recruiters[random.Next(recruiters.Count)];
                    application.AssignedRecruiterId = assignedRecruiter.Id;
                }

                var createdApplication = await _jobApplicationRepository.CreateAsync(application);

                // Reload with full details including navigation properties for complete DTO
                var fullApplication = await _jobApplicationRepository.GetByIdWithDetailsAsync(createdApplication.Id);

                // Map entity back to DTO and return
                return _mapper.Map<JobApplicationDto>(fullApplication ?? createdApplication);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating job application for candidate {CandidateId} and job {JobId}",
                    dto.CandidateProfileId, dto.JobPositionId);
                throw;
            }
        }

        public async Task<JobApplicationDetailedDto?> GetApplicationWithDetailsAsync(Guid id)
        {
            try
            {
                var application = await _jobApplicationRepository.GetByIdWithDetailsAsync(id);
                return application == null ? null : _mapper.Map<JobApplicationDetailedDto>(application);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving job application with details for ID {ApplicationId}", id);
                throw;
            }
        }



        public async Task<IEnumerable<JobApplicationSummaryDto>> GetApplicationsByCandidateAsync(Guid candidateProfileId)
        {
            try
            {
                var applications = await _jobApplicationRepository.GetByCandidateIdAsync(candidateProfileId);
                return _mapper.Map<IEnumerable<JobApplicationSummaryDto>>(applications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving applications for candidate {CandidateProfileId}", candidateProfileId);
                throw;
            }
        }

        public async Task<IEnumerable<JobApplicationSummaryDto>> GetApplicationsByRecruiterAsync(Guid recruiterId)
        {
            try
            {
                var applications = await _jobApplicationRepository.GetByRecruiterAsync(recruiterId);
                return _mapper.Map<IEnumerable<JobApplicationSummaryDto>>(applications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving applications assigned to recruiter {RecruiterId}", recruiterId);
                throw;
            }
        }



        public async Task<JobApplicationDto> UpdateApplicationAsync(Guid id, JobApplicationUpdateDto dto)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    throw new ArgumentException("Application ID cannot be empty", nameof(id));
                }

                // Application existence already validated by controller
                var existingApplication = await _jobApplicationRepository.GetByIdAsync(id);

                // Map DTO properties to existing entity (AutoMapper handles null properties)
                _mapper.Map(dto, existingApplication!);

                var updatedApplication = await _jobApplicationRepository.UpdateAsync(existingApplication!);

                // Reload with full details for complete DTO
                var fullApplication = await _jobApplicationRepository.GetByIdWithDetailsAsync(updatedApplication.Id);

                return _mapper.Map<JobApplicationDto>(fullApplication ?? updatedApplication);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating job application with ID {ApplicationId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteApplicationAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to delete job application with empty GUID");
                    return false;
                }

                var result = await _jobApplicationRepository.DeleteAsync(id);

                if (result)
                {
                    _logger.LogInformation("Job application deleted successfully with ID {ApplicationId}", id);
                }
                else
                {
                    _logger.LogWarning("Job application with ID {ApplicationId} not found for deletion", id);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting job application with ID {ApplicationId}", id);
                throw;
            }
        }

        // Helper method for authorization - returns entity without mapping
        public async Task<JobApplication?> GetApplicationEntityByIdAsync(Guid id)
        {
            try
            {
                return await _jobApplicationRepository.GetByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving job application entity with ID {ApplicationId}", id);
                throw;
            }
        }

        #endregion

        #region Pagination Methods

        public async Task<PagedResult<JobApplicationSummaryDto>> GetApplicationsByJobForUserAsync(
            Guid jobPositionId, Guid userId, List<string> userRoles, int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                if (jobPositionId == Guid.Empty)
                {
                    throw new ArgumentException("Invalid job position ID", nameof(jobPositionId));
                }

                if (pageNumber < 1)
                {
                    throw new ArgumentException("Page number must be greater than 0", nameof(pageNumber));
                }

                if (pageSize < 1 || pageSize > 100)
                {
                    throw new ArgumentException("Page size must be between 1 and 100", nameof(pageSize));
                }

                var (items, totalCount) = await _jobApplicationRepository.GetByJobPositionIdForUserAsync(
                    jobPositionId, userId, userRoles, pageNumber, pageSize);

                var dtos = _mapper.Map<List<JobApplicationSummaryDto>>(items);

                return PagedResult<JobApplicationSummaryDto>.Create(dtos, totalCount, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving filtered applications for job position {JobPositionId} and user {UserId}",
                    jobPositionId, userId);
                throw;
            }
        }

        public async Task<PagedResult<JobApplicationSummaryDto>> GetApplicationsByStatusAsync(
            ApplicationStatus status, int pageNumber = 1, int pageSize = 25)
        {
            try
            {
                if (pageNumber < 1)
                {
                    throw new ArgumentException("Page number must be greater than 0", nameof(pageNumber));
                }

                if (pageSize < 1 || pageSize > 100)
                {
                    throw new ArgumentException("Page size must be between 1 and 100", nameof(pageSize));
                }

                var (items, totalCount) = await _jobApplicationRepository.GetByStatusAsync(
                    status, pageNumber, pageSize);

                var dtos = _mapper.Map<List<JobApplicationSummaryDto>>(items);
                return PagedResult<JobApplicationSummaryDto>.Create(dtos, totalCount, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paged applications with status {Status}", status);
                throw;
            }
        }

        #endregion

        #region Validation Methods

        public async Task<bool> CanApplyToJobAsync(Guid jobPositionId, Guid candidateProfileId)
        {
            try
            {
                // Validate inputs
                if (jobPositionId == Guid.Empty || candidateProfileId == Guid.Empty)
                {
                    _logger.LogWarning("Invalid Guid provided for job position or candidate profile");
                    return false;
                }

                // Check if the job position is available for applications
                var isJobAvailable = await _jobPositionRepository.IsJobPositionAvailableForApplicationAsync(jobPositionId);
                if (!isJobAvailable)
                {
                    _logger.LogDebug("Job position {JobId} is not available for applications", jobPositionId);
                    return false;
                }

                // Check if candidate has already applied
                var hasApplied = await _jobApplicationRepository.HasCandidateAppliedAsync(jobPositionId, candidateProfileId);

                if (hasApplied)
                {
                    _logger.LogDebug("Candidate {CandidateId} has already applied to job {JobId}",
                        candidateProfileId, jobPositionId);
                    return false;
                }

                // Additional validation logic can be added here
                // e.g., check if candidate meets requirements, etc.

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if candidate {CandidateId} can apply to job {JobId}",
                    candidateProfileId, jobPositionId);
                throw;
            }
        }

        #endregion
    }
}