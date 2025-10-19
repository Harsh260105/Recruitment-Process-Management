using Microsoft.Extensions.Logging;
using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Core.Enums;
using RecruitmentSystem.Core.Interfaces;
using RecruitmentSystem.Services.Interfaces;

namespace RecruitmentSystem.Services.Implementations
{
    public class JobApplicationWorkflowService : IJobApplicationWorkflowService
    {
        private readonly IJobApplicationRepository _jobApplicationRepository;
        private readonly IApplicationStatusHistoryRepository _statusHistoryRepository;
        private readonly ILogger<JobApplicationWorkflowService> _logger;

        public JobApplicationWorkflowService(
            IJobApplicationRepository jobApplicationRepository,
            IApplicationStatusHistoryRepository statusHistoryRepository,
            ILogger<JobApplicationWorkflowService> logger)
        {
            _jobApplicationRepository = jobApplicationRepository;
            _statusHistoryRepository = statusHistoryRepository;
            _logger = logger;
        }

        #region Business Logic Operations

        public async Task<JobApplication> UpdateApplicationStatusAsync(Guid applicationId, ApplicationStatus newStatus, Guid changedByUserId, string? comments = null)
        {
            try
            {
                var currentStatus = await _jobApplicationRepository.GetStatusByIdAsync(applicationId);
                if (currentStatus == null)
                {
                    throw new InvalidOperationException($"Job application with ID {applicationId} not found");
                }

                var isValidTransition = await ValidateStatusTransitionAsync(currentStatus.Value, newStatus);
                if (!isValidTransition)
                {
                    throw new InvalidOperationException($"Invalid status transition from {currentStatus.Value} to {newStatus}");
                }

                // Update the status (repository fetches the full entity here)
                await _jobApplicationRepository.UpdateStatusAsync(applicationId, newStatus, changedByUserId, comments);

                // Fetch the updated application with all navigation properties for proper DTO mapping
                var updatedApplicationWithDetails = await _jobApplicationRepository.GetByIdWithDetailsAsync(applicationId);

                return updatedApplicationWithDetails!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating application {ApplicationId} status to {NewStatus}",
                    applicationId, newStatus);
                throw;
            }
        }

        public async Task<JobApplication> AssignRecruiterAsync(Guid applicationId, Guid recruiterId)
        {
            try
            {
                var application = await _jobApplicationRepository.GetByIdAsync(applicationId);
                application!.AssignedRecruiterId = recruiterId;
                await _jobApplicationRepository.UpdateAsync(application);

                // Fetch the updated application with all navigation properties for proper DTO mapping
                var updatedApplicationWithDetails = await _jobApplicationRepository.GetByIdWithDetailsAsync(applicationId);

                return updatedApplicationWithDetails!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning recruiter {RecruiterId} to application {ApplicationId}",
                    recruiterId, applicationId);
                throw;
            }
        }

        public async Task<JobApplication> AddInternalNotesAsync(Guid applicationId, string notes)
        {
            try
            {
                var application = await _jobApplicationRepository.GetByIdAsync(applicationId);
                application!.InternalNotes = notes;
                await _jobApplicationRepository.UpdateAsync(application);

                // Fetch the updated application with all navigation properties for proper DTO mapping
                var updatedApplicationWithDetails = await _jobApplicationRepository.GetByIdWithDetailsAsync(applicationId);

                return updatedApplicationWithDetails!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding internal notes to application {ApplicationId}",
                    applicationId);
                throw;
            }
        }

        #endregion

        #region Workflow Management

        public async Task<JobApplication> SendTestInvitationAsync(Guid applicationId, Guid sentByUserId)
        {
            try
            {
                // Update status to TestInvited
                var updatedApplication = await UpdateApplicationStatusAsync(
                    applicationId,
                    ApplicationStatus.TestInvited,
                    sentByUserId,
                    "Test invitation sent to candidate");

                return updatedApplication;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending test invitation for application {ApplicationId}",
                    applicationId);
                throw;
            }
        }

        public async Task<JobApplication> MarkTestCompletedAsync(Guid applicationId, int score, Guid updatedByUserId)
        {
            try
            {
                // Lightweight query - only fetch Status field for validation
                var currentStatus = await _jobApplicationRepository.GetStatusByIdAsync(applicationId);
                if (currentStatus == null)
                {
                    throw new InvalidOperationException($"Job application with ID {applicationId} not found");
                }

                if (currentStatus.Value != ApplicationStatus.TestInvited)
                {
                    throw new InvalidOperationException($"Cannot complete test for application in status {currentStatus.Value}. Application must be in TestInvited status.");
                }

                var newStatus = ApplicationStatus.TestCompleted;
                var statusComment = $"Test completed with score {score}%";

                await _jobApplicationRepository.CompleteTestWithScoreAsync(applicationId, score, newStatus, updatedByUserId, statusComment);

                // Fetch the updated application with all navigation properties for proper DTO mapping
                var updatedApplicationWithDetails = await _jobApplicationRepository.GetByIdWithDetailsAsync(applicationId);

                return updatedApplicationWithDetails!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking test as completed for application {ApplicationId}",
                    applicationId);
                throw;
            }
        }

        public async Task<JobApplication> MoveToReviewAsync(Guid applicationId, Guid reviewedByUserId)
        {
            try
            {
                var updatedApplication = await UpdateApplicationStatusAsync(
                    applicationId,
                    ApplicationStatus.UnderReview,
                    reviewedByUserId,
                    "Application moved to review phase");

                return updatedApplication;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving application {ApplicationId} to review",
                    applicationId);
                throw;
            }
        }

        public async Task<JobApplication> ShortlistApplicationAsync(Guid applicationId, Guid shortlistedByUserId, string? comments = null)
        {
            try
            {
                var statusComment = comments ?? "Application shortlisted for further consideration";

                var updatedApplication = await UpdateApplicationStatusAsync(
                    applicationId,
                    ApplicationStatus.Shortlisted,
                    shortlistedByUserId,
                    statusComment);

                return updatedApplication;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error shortlisting application {ApplicationId}",
                    applicationId);
                throw;
            }
        }

        public async Task<JobApplication> RejectApplicationAsync(Guid applicationId, string rejectionReason, Guid rejectedByUserId)
        {
            try
            {
                var application = await _jobApplicationRepository.GetByIdAsync(applicationId);
                application!.RejectionReason = rejectionReason;
                await _jobApplicationRepository.UpdateAsync(application);

                var updatedApplication = await UpdateApplicationStatusAsync(
                    applicationId,
                    ApplicationStatus.Rejected,
                    rejectedByUserId,
                    rejectionReason);

                return updatedApplication;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting application {ApplicationId}",
                    applicationId);
                throw;
            }
        }

        public async Task<JobApplication> WithdrawApplicationAsync(Guid applicationId, Guid withdrawnByUserId)
        {
            try
            {
                var updatedApplication = await UpdateApplicationStatusAsync(
                    applicationId,
                    ApplicationStatus.Withdrawn,
                    withdrawnByUserId,
                    "Application withdrawn by candidate");

                return updatedApplication;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error withdrawing application {ApplicationId}",
                    applicationId);
                throw;
            }
        }

        public async Task<JobApplication> PutOnHoldAsync(Guid applicationId, string reason, Guid putOnHoldByUserId)
        {
            try
            {
                var updatedApplication = await UpdateApplicationStatusAsync(
                    applicationId,
                    ApplicationStatus.OnHold,
                    putOnHoldByUserId,
                    reason);

                return updatedApplication;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error putting application {ApplicationId} on hold",
                    applicationId);
                throw;
            }
        }

        #endregion

        #region Status History and Audit

        public async Task<IEnumerable<ApplicationStatusHistory>> GetApplicationStatusHistoryAsync(Guid applicationId)
        {
            try
            {
                _logger.LogDebug("Retrieving status history for application {ApplicationId}", applicationId);
                return await _statusHistoryRepository.GetByApplicationAsync(applicationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving status history for application {ApplicationId}",
                    applicationId);
                throw;
            }
        }

        public async Task<ApplicationStatusHistory?> GetLatestStatusChangeAsync(Guid applicationId)
        {
            try
            {
                _logger.LogDebug("Retrieving latest status change for application {ApplicationId}", applicationId);
                var history = await _statusHistoryRepository.GetByApplicationAsync(applicationId, 1);
                return history.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving latest status change for application {ApplicationId}",
                    applicationId);
                throw;
            }
        }

        #endregion

        #region Validation Methods

        public Task<bool> ValidateStatusTransitionAsync(ApplicationStatus fromStatus, ApplicationStatus toStatus)
        {
            try
            {
                // Define valid status transitions with business rules
                var validTransitions = new Dictionary<ApplicationStatus, (ApplicationStatus[] AllowedStatuses, Func<ApplicationStatus, ApplicationStatus, bool>? BusinessRule)>
                {
                    [ApplicationStatus.Applied] = (
                    [
                        ApplicationStatus.UnderReview,
                        ApplicationStatus.TestInvited,
                        ApplicationStatus.Rejected,
                        ApplicationStatus.Withdrawn,
                        ApplicationStatus.OnHold
                    ], null),
                    [ApplicationStatus.TestInvited] = (
                    [
                        ApplicationStatus.TestCompleted,
                        ApplicationStatus.Rejected,
                        ApplicationStatus.Withdrawn
                    ], null),
                    [ApplicationStatus.TestCompleted] = (
                    [
                        ApplicationStatus.UnderReview,
                        ApplicationStatus.Shortlisted,
                        ApplicationStatus.Rejected,
                        ApplicationStatus.TestInvited // Allow retake
                    ], null),
                    [ApplicationStatus.UnderReview] = (
                    [
                        ApplicationStatus.Shortlisted,
                        ApplicationStatus.Rejected,
                        ApplicationStatus.TestInvited,
                        ApplicationStatus.OnHold
                    ], null),
                    [ApplicationStatus.Shortlisted] = (
                    [
                        ApplicationStatus.Rejected,
                        ApplicationStatus.OnHold
                    ], null),
                    [ApplicationStatus.OnHold] = (
                    [
                        ApplicationStatus.UnderReview,
                        ApplicationStatus.Shortlisted,
                        ApplicationStatus.Rejected,
                        ApplicationStatus.TestInvited
                    ], null),
                    [ApplicationStatus.Rejected] = ([ApplicationStatus.Applied], null), // Allow reapplication
                    [ApplicationStatus.Withdrawn] = ([ApplicationStatus.Applied], null)  // Allow reapplication
                };

                if (!validTransitions.ContainsKey(fromStatus))
                {
                    _logger.LogWarning("Unknown status {FromStatus}", fromStatus);
                    return Task.FromResult(false);
                }

                var (allowedStatuses, businessRule) = validTransitions[fromStatus];
                var isValidTransition = allowedStatuses.Contains(toStatus);

                // Apply additional business rules if they exist
                var isValidBusinessRule = businessRule == null || businessRule(fromStatus, toStatus);

                var isValid = isValidTransition && isValidBusinessRule;

                return Task.FromResult(isValid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating status transition from {FromStatus} to {ToStatus}",
                    fromStatus, toStatus);
                throw;
            }
        }

        #endregion
    }
}