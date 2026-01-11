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
        private readonly ICandidateProfileRepository _candidateProfileRepository;
        private readonly IAuthenticationService _authenticationService;
        private readonly IMapper _mapper;
        private readonly ILogger<JobApplicationManagementService> _logger;
        private static readonly TimeSpan ReapplyCooldownWindow = TimeSpan.FromDays(60);
        private static readonly HashSet<ApplicationStatus> CooldownEligibleStatuses = new()
        {
            ApplicationStatus.Rejected,
            ApplicationStatus.Withdrawn
        };
        private const int MaxActiveApplicationsPerCandidate = 3;
        private static readonly HashSet<ApplicationStatus> ActiveApplicationStatuses = new()
        {
            ApplicationStatus.Applied,
            ApplicationStatus.TestInvited,
            ApplicationStatus.TestCompleted,
            ApplicationStatus.UnderReview,
            ApplicationStatus.Shortlisted,
            ApplicationStatus.Interview,
            ApplicationStatus.Selected,
            ApplicationStatus.OnHold
        };

        private static readonly HashSet<ApplicationStatus> AllowedCoverLetterStatuses = new()
        {
            ApplicationStatus.Applied,
            ApplicationStatus.TestInvited,
            ApplicationStatus.TestCompleted,
            ApplicationStatus.UnderReview,
            ApplicationStatus.OnHold
        };

        private static readonly HashSet<ApplicationStatus> AllowedInternalNotesStatuses = new()
        {
            ApplicationStatus.Applied,
            ApplicationStatus.TestInvited,
            ApplicationStatus.TestCompleted,
            ApplicationStatus.UnderReview,
            ApplicationStatus.Shortlisted,
            ApplicationStatus.Interview,
            ApplicationStatus.Selected,
            ApplicationStatus.OnHold
        };

        public JobApplicationManagementService(
            IJobApplicationRepository jobApplicationRepository,
            IJobPositionRepository jobPositionRepository,
            ICandidateProfileRepository candidateProfileRepository,
            IAuthenticationService authenticationService,
            IMapper mapper,
            ILogger<JobApplicationManagementService> logger)
        {
            _jobApplicationRepository = jobApplicationRepository;
            _jobPositionRepository = jobPositionRepository;
            _candidateProfileRepository = candidateProfileRepository;
            _authenticationService = authenticationService;
            _mapper = mapper;
            _logger = logger;
        }

        #region Core CRUD Operations

        public async Task<JobApplicationDto> CreateApplicationAsync(JobApplicationCreateDto dto, bool consumeOverride = false)
        {
            try
            {
                var existingApplication = await _jobApplicationRepository.GetByJobAndCandidateAsync(dto.JobPositionId, dto.CandidateProfileId);

                if (existingApplication != null)
                {
                    bool isTerminalState = existingApplication.Status == ApplicationStatus.Rejected ||
                                          existingApplication.Status == ApplicationStatus.Withdrawn;

                    if (!isTerminalState)
                    {
                        throw new InvalidOperationException(
                            $"You have already applied for this position. Your application status is '{existingApplication.Status}'. " +
                            "Please wait for the current application to be processed.");
                    }

                    var candidateProfile = await _candidateProfileRepository.GetByIdAsync(dto.CandidateProfileId);
                    bool hasBypassOverride = candidateProfile?.CanBypassApplicationLimits == true &&
                                            (candidateProfile.OverrideExpiresAt == null ||
                                             candidateProfile.OverrideExpiresAt > DateTime.UtcNow);

                    if (!hasBypassOverride)
                    {
                        throw new InvalidOperationException(
                            $"You have already applied for this position. Your previous application was {existingApplication.Status}. " +
                            "Reapplication is not allowed. Please contact HR if you believe this is an error.");
                    }

                    _logger.LogInformation(
                        "Candidate {CandidateId} has bypass override. Deleting existing {Status} application {ApplicationId} to allow reapplication for job {JobId}",
                        dto.CandidateProfileId, existingApplication.Status, existingApplication.Id, dto.JobPositionId);

                    await _jobApplicationRepository.DeleteAsync(existingApplication.Id);
                }

                var application = _mapper.Map<JobApplication>(dto);

                application.Status = ApplicationStatus.Applied;
                application.AppliedDate = DateTime.UtcNow;
                application.IsActive = true;

                var recruiters = await _authenticationService.GetAllRecruitersAsync();
                if (recruiters.Any())
                {
                    var random = new Random();
                    var assignedRecruiter = recruiters[random.Next(recruiters.Count)];
                    application.AssignedRecruiterId = assignedRecruiter.Id;
                }

                var createdApplication = await _jobApplicationRepository.CreateAsync(application);

                await _jobPositionRepository.IncrementTotalApplicantsAsync(dto.JobPositionId);

                if (consumeOverride)
                {
                    var overrideCleared = await _candidateProfileRepository.UpdateApplicationOverrideAsync(
                        dto.CandidateProfileId,
                        false,
                        null);

                    if (overrideCleared)
                    {
                        _logger.LogInformation(
                            "Consumed application override for candidate {CandidateProfileId} after creating application {ApplicationId}",
                            dto.CandidateProfileId,
                            createdApplication.Id);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Attempted to consume override for candidate {CandidateProfileId}, but profile was not found when creating application {ApplicationId}",
                            dto.CandidateProfileId,
                            createdApplication.Id);
                    }
                }

                var fullApplication = await _jobApplicationRepository.GetByIdWithDetailsAsync(createdApplication.Id);

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



        public async Task<JobApplicationDto> UpdateApplicationAsync(Guid id, JobApplicationUpdateDto dto, List<string> userRoles)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    throw new ArgumentException("Application ID cannot be empty", nameof(id));
                }

                var existingApplication = await _jobApplicationRepository.GetByIdAsync(id);

                await ValidateApplicationUpdateAsync(dto, userRoles, existingApplication!.Status);

                _mapper.Map(dto, existingApplication!);

                var updatedApplication = await _jobApplicationRepository.UpdateAsync(existingApplication!);
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

        private async Task ValidateApplicationUpdateAsync(JobApplicationUpdateDto dto, List<string> userRoles, ApplicationStatus currentStatus)
        {
            var isStaffUser = userRoles.Contains("Recruiter") || userRoles.Contains("HR") || userRoles.Contains("Admin") || userRoles.Contains("SuperAdmin");
            var isCandidateUser = userRoles.Contains("Candidate");

            // Validate CoverLetter updates
            if (dto.CoverLetter != null)
            {
                if (!isCandidateUser)
                {
                    throw new InvalidOperationException("Only candidates can update the cover letter");
                }

                if (!AllowedCoverLetterStatuses.Contains(currentStatus))
                {
                    throw new InvalidOperationException($"Cover letter cannot be updated when application status is {currentStatus}");
                }
            }

            // Validate InternalNotes updates
            if (dto.InternalNotes != null)
            {
                if (!isStaffUser)
                {
                    throw new InvalidOperationException("Only staff members can update internal notes");
                }

                if (!AllowedInternalNotesStatuses.Contains(currentStatus))
                {
                    throw new InvalidOperationException($"Internal notes cannot be updated when application status is {currentStatus}");
                }
            }

            // Validate AssignedRecruiterId updates
            if (dto.AssignedRecruiterId.HasValue)
            {
                if (!(userRoles.Contains("HR") || userRoles.Contains("Admin") || userRoles.Contains("SuperAdmin")))
                {
                    throw new InvalidOperationException("Only HR, Admin, or SuperAdmin can assign recruiters");
                }

                var recruiterRoles = await _authenticationService.GetUserRolesAsync(dto.AssignedRecruiterId.Value);
                if (!recruiterRoles.Contains("Recruiter"))
                {
                    throw new InvalidOperationException("Assigned user is not a recruiter");
                }

                if (currentStatus == ApplicationStatus.Rejected || currentStatus == ApplicationStatus.Withdrawn)
                {
                    throw new InvalidOperationException($"Recruiter assignment cannot be changed when application status is {currentStatus}");
                }
            }
        }

        public async Task<JobApplicationEligibilityResult> CanApplyToJobAsync(
            Guid jobPositionId,
            Guid candidateProfileId)
        {
            try
            {
                // Validate inputs
                if (jobPositionId == Guid.Empty || candidateProfileId == Guid.Empty)
                {
                    return JobApplicationEligibilityResult.Forbidden("Invalid job or candidate information.");
                }

                // Check if the job position is available for applications
                var isJobAvailable = await _jobPositionRepository.IsJobPositionAvailableForApplicationAsync(jobPositionId);
                if (!isJobAvailable)
                {
                    return JobApplicationEligibilityResult.Forbidden("This job is no longer accepting applications.");
                }

                var candidateProfile = await _candidateProfileRepository.GetByIdAsync(candidateProfileId);
                if (candidateProfile == null)
                {
                    return JobApplicationEligibilityResult.Forbidden("Candidate profile not found.");
                }

                // Check if override is expired and disable it if so
                var isOverrideExpired = candidateProfile.CanBypassApplicationLimits &&
                    candidateProfile.OverrideExpiresAt.HasValue &&
                    candidateProfile.OverrideExpiresAt.Value < DateTime.UtcNow;

                if (isOverrideExpired)
                {

                    // Disable the expired override
                    await _candidateProfileRepository.UpdateApplicationOverrideAsync(
                        candidateProfileId,
                        false,
                        null);

                    // Update the local object for the rest of this method
                    candidateProfile.CanBypassApplicationLimits = false;
                    candidateProfile.OverrideExpiresAt = null;
                }

                var hasOverridePrivilege = candidateProfile.CanBypassApplicationLimits &&
                    (!candidateProfile.OverrideExpiresAt.HasValue || candidateProfile.OverrideExpiresAt.Value >= DateTime.UtcNow);

                var overrideUsed = false;

                var activeCount = await _jobApplicationRepository.GetActiveApplicationCountAsync(candidateProfileId, ActiveApplicationStatuses);

                if (activeCount >= MaxActiveApplicationsPerCandidate)
                {
                    if (hasOverridePrivilege)
                    {
                        overrideUsed = true;
                        _logger.LogInformation(
                            "Candidate override bypassed active application cap for candidate {CandidateId}. Current active applications: {Count}",
                            candidateProfileId,
                            activeCount);
                    }
                    else
                    {
                        var capReason = $"You already have {activeCount} active applications. The maximum is {MaxActiveApplicationsPerCandidate}.";
                        return JobApplicationEligibilityResult.Forbidden(capReason);
                    }
                }

                // Check latest application for candidate & job
                var latestApplication = await _jobApplicationRepository.GetByJobAndCandidateAsync(jobPositionId, candidateProfileId);

                if (latestApplication == null)
                {
                    return JobApplicationEligibilityResult.Allowed(overrideUsed);
                }

                if (!CooldownEligibleStatuses.Contains(latestApplication.Status))
                {
                    _logger.LogDebug("Candidate {CandidateId} has already applied to job {JobId}",
                        candidateProfileId, jobPositionId);
                    return JobApplicationEligibilityResult.Forbidden("You already have an active application for this job.");
                }

                var referenceDate = latestApplication.UpdatedAt == default
                    ? latestApplication.AppliedDate
                    : latestApplication.UpdatedAt;
                var cooldownEndsAt = referenceDate.Add(ReapplyCooldownWindow);

                if (DateTime.UtcNow >= cooldownEndsAt)
                {
                    return JobApplicationEligibilityResult.Allowed(overrideUsed);
                }

                if (hasOverridePrivilege)
                {
                    overrideUsed = true;
                    _logger.LogInformation(
                        "Candidate override bypassed reapplication cooldown for candidate {CandidateId} on job {JobId}. " +
                        "Existing application {ApplicationId} status {Status}, cooldown until {CooldownEndsAt}",
                        candidateProfileId,
                        jobPositionId,
                        latestApplication.Id,
                        latestApplication.Status,
                        cooldownEndsAt);

                    return JobApplicationEligibilityResult.Allowed(overrideUsed);
                }

                var remaining = cooldownEndsAt - DateTime.UtcNow;
                var daysRemaining = Math.Ceiling(remaining.TotalDays);
                var reason = daysRemaining <= 1
                    ? "Please wait 1 more day before applying to this job again."
                    : $"Please wait {daysRemaining} days before applying to this job again.";

                return JobApplicationEligibilityResult.Cooldown(reason, cooldownEndsAt);

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