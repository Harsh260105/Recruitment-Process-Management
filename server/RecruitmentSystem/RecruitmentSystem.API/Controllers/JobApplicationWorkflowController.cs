using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Core.Enums;
using RecruitmentSystem.Services.Interfaces;
using RecruitmentSystem.Shared.DTOs;
using RecruitmentSystem.Shared.DTOs.Responses;
using System.Security.Claims;

namespace RecruitmentSystem.API.Controllers
{
    [Route("api/job-applications")]
    [ApiController]
    [Authorize]
    public class JobApplicationWorkflowController : ControllerBase
    {
        private readonly IJobApplicationWorkflowService _workflowService;
        private readonly IJobApplicationManagementService _managementService;
        private readonly IAuthenticationService _authenticationService;
        private readonly IMapper _mapper;
        private readonly ILogger<JobApplicationWorkflowController> _logger;

        // Cache user ID and roles
        private Guid? _currentUserId;
        private List<string>? _currentUserRoles;

        public JobApplicationWorkflowController(
            IJobApplicationWorkflowService workflowService,
            IJobApplicationManagementService managementService,
            IAuthenticationService authenticationService,
            IMapper mapper,
            ILogger<JobApplicationWorkflowController> logger)
        {
            _workflowService = workflowService;
            _managementService = managementService;
            _authenticationService = authenticationService;
            _mapper = mapper;
            _logger = logger;
        }

        #region Status Management

        /// <summary>
        /// Update application status
        /// </summary>
        [HttpPatch("{id:guid}/status")]
        [Authorize(Roles = "Recruiter, HR, Admin, SuperAdmin")]
        public async Task<ActionResult<ApiResponse<JobApplicationStaffViewDto>>> UpdateStatus(Guid id, [FromBody] JobApplicationStatusUpdateDto dto)
        {
            try
            {
                var application = await _managementService.GetApplicationEntityByIdAsync(id);
                if (application == null)
                {
                    return NotFound(ApiResponse<JobApplicationStaffViewDto>.FailureResponse(new List<string> { $"Job application with ID {id} not found" }, "Not Found"));
                }

                if (!await CanModifyApplication(application))
                {
                    return StatusCode(403, ApiResponse<JobApplicationStaffViewDto>.FailureResponse(
                        new List<string> { "You don't have permission to modify this application" },
                        "Forbidden"));
                }

                var updatedApplication = await _workflowService.UpdateApplicationStatusAsync(id, dto.Status, GetCurrentUserId(), dto.Comments);
                var resultDto = _mapper.Map<JobApplicationStaffViewDto>(updatedApplication);

                return Ok(ApiResponse<JobApplicationStaffViewDto>.SuccessResponse(resultDto, "Application status updated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for application ID: {Id}", id);
                return StatusCode(500, ApiResponse<JobApplicationStaffViewDto>.FailureResponse(new List<string> { "An error occurred while updating the application status" }, "Couldn't Update Status"));
            }
        }

        /// <summary>
        /// Shortlist application
        /// </summary>
        [HttpPatch("{id:guid}/shortlist")]
        [Authorize(Roles = "Recruiter, HR, Admin, SuperAdmin")]
        public async Task<ActionResult<ApiResponse<JobApplicationStaffViewDto>>> ShortlistApplication(Guid id, [FromBody] string? comments = null)
        {
            try
            {
                var application = await _managementService.GetApplicationEntityByIdAsync(id);
                if (application == null)
                {
                    return NotFound(ApiResponse<JobApplicationStaffViewDto>.FailureResponse(new List<string> { $"Job application with ID {id} not found" }, "Not Found"));
                }

                if (!await CanModifyApplication(application))
                {
                    return StatusCode(403, ApiResponse<JobApplicationStaffViewDto>.FailureResponse(
                        new List<string> { "You don't have permission to shortlist this application" },
                        "Forbidden"));
                }

                var updatedApplication = await _workflowService.ShortlistApplicationAsync(id, GetCurrentUserId(), comments);
                var resultDto = _mapper.Map<JobApplicationStaffViewDto>(updatedApplication);

                return Ok(ApiResponse<JobApplicationStaffViewDto>.SuccessResponse(resultDto, "Application shortlisted successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error shortlisting application ID: {Id}", id);
                return StatusCode(500, ApiResponse<JobApplicationStaffViewDto>.FailureResponse(new List<string> { "An error occurred while shortlisting the application" }, "Couldn't Shortlist Application"));
            }
        }

        /// <summary>
        /// Reject application
        /// </summary>
        [HttpPatch("{id:guid}/reject")]
        [Authorize(Roles = "Recruiter, HR, Admin, SuperAdmin")]
        public async Task<ActionResult<ApiResponse<JobApplicationStaffViewDto>>> RejectApplication(Guid id, [FromBody] string? comments)
        {
            try
            {
                var application = await _managementService.GetApplicationEntityByIdAsync(id);
                if (application == null)
                {
                    return NotFound(ApiResponse<JobApplicationStaffViewDto>.FailureResponse(new List<string> { $"Job application with ID {id} not found" }, "Not Found"));
                }

                if (!await CanModifyApplication(application))
                {
                    return Forbid();
                }

                var updatedApplication = await _workflowService.RejectApplicationAsync(id, comments ?? "No reason provided", GetCurrentUserId());
                var resultDto = _mapper.Map<JobApplicationStaffViewDto>(updatedApplication);

                return Ok(ApiResponse<JobApplicationStaffViewDto>.SuccessResponse(resultDto, "Application rejected successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting application ID: {Id}", id);
                return StatusCode(500, ApiResponse<JobApplicationStaffViewDto>.FailureResponse(new List<string> { "An error occurred while rejecting the application" }, "Couldn't Reject Application"));
            }
        }

        /// <summary>
        /// Withdraw application
        /// </summary>
        [HttpPatch("{id:guid}/withdraw")]
        public async Task<ActionResult<ApiResponse<JobApplicationCandidateViewDto>>> WithdrawApplication(Guid id)
        {
            try
            {
                var application = await _managementService.GetApplicationEntityByIdAsync(id);
                if (application == null)
                {
                    return NotFound(ApiResponse<JobApplicationCandidateViewDto>.FailureResponse(new List<string> { $"Job application with ID {id} not found" }, "Not Found"));
                }

                if (!await CanAccessApplication(application))
                {
                    return Forbid();
                }

                var updatedApplication = await _workflowService.WithdrawApplicationAsync(id, GetCurrentUserId());
                var resultDto = _mapper.Map<JobApplicationCandidateViewDto>(updatedApplication);

                return Ok(ApiResponse<JobApplicationCandidateViewDto>.SuccessResponse(resultDto, "Application withdrawn successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error withdrawing application ID: {Id}", id);
                return StatusCode(500, ApiResponse<JobApplicationCandidateViewDto>.FailureResponse(new List<string> { "An error occurred while withdrawing the application" }, "Couldn't Withdraw Application"));
            }
        }

        /// <summary>
        /// Put application on hold
        /// </summary>
        [HttpPatch("{id:guid}/hold")]
        [Authorize(Roles = "Recruiter, HR, Admin, SuperAdmin")]
        public async Task<ActionResult<ApiResponse<JobApplicationStaffViewDto>>> PutOnHold(Guid id, [FromBody] string? comments = null)
        {
            try
            {
                var application = await _managementService.GetApplicationEntityByIdAsync(id);
                if (application == null)
                {
                    return NotFound(ApiResponse<JobApplicationStaffViewDto>.FailureResponse(new List<string> { $"Job application with ID {id} not found" }, "Not Found"));
                }

                if (!await CanModifyApplication(application))
                {
                    return Forbid();
                }

                var updatedApplication = await _workflowService.PutOnHoldAsync(id, comments ?? "No reason provided", GetCurrentUserId());
                var resultDto = _mapper.Map<JobApplicationStaffViewDto>(updatedApplication);

                return Ok(ApiResponse<JobApplicationStaffViewDto>.SuccessResponse(resultDto, "Application put on hold successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error putting application on hold ID: {Id}", id);
                return StatusCode(500, ApiResponse<JobApplicationStaffViewDto>.FailureResponse(new List<string> { "An error occurred while putting the application on hold" }, "Couldn't Put On Hold"));
            }
        }

        #endregion

        #region Assignment and Testing

        /// <summary>
        /// Assign recruiter to application
        /// </summary>
        [HttpPatch("{id:guid}/assign-recruiter/{recruiterId:guid}")]
        [Authorize(Roles = "HR, Admin, SuperAdmin")]
        public async Task<ActionResult<ApiResponse<JobApplicationStaffViewDto>>> AssignRecruiter(Guid id, Guid recruiterId)
        {
            try
            {
                var application = await _managementService.GetApplicationEntityByIdAsync(id);
                if (application == null)
                {
                    return NotFound(ApiResponse<JobApplicationStaffViewDto>.FailureResponse(new List<string> { $"Job application with ID {id} not found" }, "Not Found"));
                }

                var updatedApplication = await _workflowService.AssignRecruiterAsync(id, recruiterId);
                var resultDto = _mapper.Map<JobApplicationStaffViewDto>(updatedApplication);

                return Ok(ApiResponse<JobApplicationStaffViewDto>.SuccessResponse(resultDto, "Recruiter assigned successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning recruiter to application ID: {Id}", id);
                return StatusCode(500, ApiResponse<JobApplicationStaffViewDto>.FailureResponse(new List<string> { "An error occurred while assigning the recruiter" }, "Couldn't Assign Recruiter"));
            }
        }

        /// <summary>
        /// Send test invitation
        /// </summary>
        [HttpPatch("{id:guid}/send-test")]
        [Authorize(Roles = "Recruiter, HR, Admin, SuperAdmin")]
        public async Task<ActionResult<ApiResponse<JobApplicationStaffViewDto>>> SendTestInvitation(Guid id)
        {
            try
            {
                var application = await _managementService.GetApplicationEntityByIdAsync(id);
                if (application == null)
                {
                    return NotFound(ApiResponse<JobApplicationStaffViewDto>.FailureResponse(new List<string> { $"Job application with ID {id} not found" }, "Not Found"));
                }

                if (!await CanModifyApplication(application))
                {
                    return Forbid();
                }

                var updatedApplication = await _workflowService.SendTestInvitationAsync(id, GetCurrentUserId());
                var resultDto = _mapper.Map<JobApplicationStaffViewDto>(updatedApplication);

                return Ok(ApiResponse<JobApplicationStaffViewDto>.SuccessResponse(resultDto, "Test invitation sent successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending test invitation for application ID: {Id}", id);
                return StatusCode(500, ApiResponse<JobApplicationStaffViewDto>.FailureResponse(new List<string> { "An error occurred while sending the test invitation" }, "Couldn't Send Test Invitation"));
            }
        }

        /// <summary>
        /// Mark test as completed
        /// </summary>
        [HttpPatch("{id:guid}/complete-test")]
        [Authorize(Roles = "Recruiter, HR, Admin, SuperAdmin")]
        public async Task<ActionResult<ApiResponse<JobApplicationStaffViewDto>>> CompleteTest(Guid id, [FromBody] int score)
        {
            try
            {
                var application = await _managementService.GetApplicationEntityByIdAsync(id);
                if (application == null)
                {
                    return NotFound(ApiResponse<JobApplicationStaffViewDto>.FailureResponse(new List<string> { $"Job application with ID {id} not found" }, "Not Found"));
                }

                if (!await CanModifyApplication(application))
                {
                    return Forbid();
                }

                var updatedApplication = await _workflowService.MarkTestCompletedAsync(id, score, GetCurrentUserId());
                var resultDto = _mapper.Map<JobApplicationStaffViewDto>(updatedApplication);

                return Ok(ApiResponse<JobApplicationStaffViewDto>.SuccessResponse(resultDto, "Test marked as completed successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing test for application ID: {Id}", id);
                return StatusCode(500, ApiResponse<JobApplicationStaffViewDto>.FailureResponse(new List<string> { "An error occurred while completing the test" }, "Couldn't Complete Test"));
            }
        }

        /// <summary>
        /// Move application to review
        /// </summary>
        [HttpPatch("{id:guid}/move-to-review")]
        [Authorize(Roles = "Recruiter, HR, Admin, SuperAdmin")]
        public async Task<ActionResult<ApiResponse<JobApplicationStaffViewDto>>> MoveToReview(Guid id)
        {
            try
            {
                var application = await _managementService.GetApplicationEntityByIdAsync(id);
                if (application == null)
                {
                    return NotFound(ApiResponse<JobApplicationStaffViewDto>.FailureResponse(new List<string> { $"Job application with ID {id} not found" }, "Not Found"));
                }

                if (!await CanModifyApplication(application))
                {
                    return Forbid();
                }

                var updatedApplication = await _workflowService.MoveToReviewAsync(id, GetCurrentUserId());
                var resultDto = _mapper.Map<JobApplicationStaffViewDto>(updatedApplication);

                return Ok(ApiResponse<JobApplicationStaffViewDto>.SuccessResponse(resultDto, "Application moved to review successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving application to review ID: {Id}", id);
                return StatusCode(500, ApiResponse<JobApplicationStaffViewDto>.FailureResponse(new List<string> { "An error occurred while moving the application to review" }, "Couldn't Move To Review"));
            }
        }

        /// <summary>
        /// Add or update internal notes (Staff only)
        /// </summary>
        [HttpPatch("{id:guid}/internal-notes")]
        [Authorize(Roles = "Recruiter, HR, Admin, SuperAdmin")]
        public async Task<ActionResult<ApiResponse<JobApplicationStaffViewDto>>> AddInternalNotes(Guid id, [FromBody] string notes)
        {
            try
            {
                var application = await _managementService.GetApplicationEntityByIdAsync(id);
                if (application == null)
                {
                    return NotFound(ApiResponse<JobApplicationStaffViewDto>.FailureResponse(new List<string> { $"Job application with ID {id} not found" }, "Not Found"));
                }

                if (!await CanModifyApplication(application))
                {
                    return Forbid();
                }

                var updatedApplication = await _workflowService.AddInternalNotesAsync(id, notes);
                var resultDto = _mapper.Map<JobApplicationStaffViewDto>(updatedApplication);

                return Ok(ApiResponse<JobApplicationStaffViewDto>.SuccessResponse(resultDto, "Internal notes updated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding internal notes to application ID: {Id}", id);
                return StatusCode(500, ApiResponse<JobApplicationStaffViewDto>.FailureResponse(new List<string> { "An error occurred while adding internal notes" }, "Couldn't Update Notes"));
            }
        }

        #endregion

        #region Private Methods

        private Guid GetCurrentUserId()
        {
            if (_currentUserId == null)
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    throw new UnauthorizedAccessException("User ID not found in token");
                }
                _currentUserId = userId;
            }
            return _currentUserId.Value;
        }

        private async Task<List<string>> GetCurrentUserRolesAsync()
        {
            if (_currentUserRoles == null)
            {
                var userId = GetCurrentUserId();
                _currentUserRoles = await _authenticationService.GetUserRolesAsync(userId);
            }
            return _currentUserRoles;
        }

        private async Task<bool> CanAccessApplication(JobApplication application)
        {
            var currentUserId = GetCurrentUserId();
            var currentUserRoles = await GetCurrentUserRolesAsync();

            // SuperAdmins, Admins, and HR can access all applications
            if (currentUserRoles.Contains("SuperAdmin") || currentUserRoles.Contains("Admin") || currentUserRoles.Contains("HR"))
                return true;

            // Recruiters can access applications assigned to them
            if (currentUserRoles.Contains("Recruiter") && application.AssignedRecruiterId == currentUserId)
                return true;

            // Candidates can access their own applications
            if (application.CandidateProfile != null && application.CandidateProfile.UserId == currentUserId)
                return true;

            return false;
        }

        private async Task<bool> CanModifyApplication(JobApplication application)
        {
            var currentUserId = GetCurrentUserId();
            var currentUserRoles = await GetCurrentUserRolesAsync();

            // SuperAdmins, Admins, and HR can modify all applications
            if (currentUserRoles.Contains("SuperAdmin") || currentUserRoles.Contains("Admin") || currentUserRoles.Contains("HR"))
                return true;

            // Recruiters can modify applications assigned to them
            if (currentUserRoles.Contains("Recruiter") && application.AssignedRecruiterId == currentUserId)
                return true;

            return false;
        }

        #endregion
    }
}
