using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Services.Interfaces;
using RecruitmentSystem.Shared.DTOs;
using RecruitmentSystem.Shared.DTOs.Responses;
using System.Security.Claims;

namespace RecruitmentSystem.API.Controllers
{
    [Route("api/job-applications")]
    [ApiController]
    [Authorize]
    public class JobApplicationHistoryController : ControllerBase
    {
        private readonly IJobApplicationManagementService _managementService;
        private readonly IJobApplicationWorkflowService _workflowService;
        private readonly IAuthenticationService _authenticationService;
        private readonly IMapper _mapper;
        private readonly ILogger<JobApplicationHistoryController> _logger;

        // Cache user ID and roles
        private Guid? _currentUserId;
        private List<string>? _currentUserRoles;

        public JobApplicationHistoryController(
            IJobApplicationManagementService managementService,
            IJobApplicationWorkflowService workflowService,
            IAuthenticationService authenticationService,
            IMapper mapper,
            ILogger<JobApplicationHistoryController> logger)
        {
            _managementService = managementService;
            _workflowService = workflowService;
            _authenticationService = authenticationService;
            _mapper = mapper;
            _logger = logger;
        }

        #region Status History

        /// <summary>
        /// Get application status history (paginated)
        /// </summary>
        [HttpGet("{id:guid}/history")]
        [Authorize(Roles = "Candidate, Recruiter, HR, Admin, SuperAdmin")]
        public async Task<ActionResult<ApiResponse<PagedResult<JobApplicationStatusHistoryDto>>>> GetApplicationHistory(
            Guid id,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var application = await _managementService.GetApplicationEntityByIdAsync(id);
                if (application == null)
                {
                    return NotFound(ApiResponse<PagedResult<JobApplicationStatusHistoryDto>>.FailureResponse(new List<string> { $"Job application with ID {id} not found" }, "Not Found"));
                }

                if (!await CanAccessApplication(application))
                {
                    return Forbid();
                }

                var (history, totalCount) = await _workflowService.GetApplicationStatusHistoryPagedAsync(id, pageNumber, pageSize);
                var historyDtos = _mapper.Map<List<JobApplicationStatusHistoryDto>>(history);
                var pagedResult = PagedResult<JobApplicationStatusHistoryDto>.Create(historyDtos, totalCount, pageNumber, pageSize);

                return Ok(ApiResponse<PagedResult<JobApplicationStatusHistoryDto>>.SuccessResponse(pagedResult, "Application history retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting application history for ID: {Id}", id);
                return StatusCode(500, ApiResponse<PagedResult<JobApplicationStatusHistoryDto>>.FailureResponse(new List<string> { "An error occurred while retrieving application history" }, "Couldn't Get History"));
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

        #endregion
    }
}
