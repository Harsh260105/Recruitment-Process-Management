using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Core.Enums;
using RecruitmentSystem.Core.Interfaces;
using RecruitmentSystem.Services.Interfaces;
using RecruitmentSystem.Shared.DTOs;
using RecruitmentSystem.Shared.DTOs.Responses;
using System.Security.Claims;

namespace RecruitmentSystem.API.Controllers
{
    [Route("api/interviews")]
    [ApiController]
    [Authorize]
    public class InterviewReportingController : ControllerBase
    {
        private readonly IInterviewReportingService _reportingService;
        private readonly IJobApplicationRepository _jobApplicationRepository;

        public InterviewReportingController(
            IInterviewReportingService reportingService,
            IJobApplicationRepository jobApplicationRepository)
        {
            _reportingService = reportingService;
            _jobApplicationRepository = jobApplicationRepository;
        }

        #region Analytics and Statistics

        /// <summary>
        /// Get interview status distribution with optional date filtering
        /// </summary>
        [HttpGet("analytics/status-distribution")]
        [Authorize(Roles = "Admin,SuperAdmin,HR")]
        public async Task<ActionResult<ApiResponse<Dictionary<InterviewStatus, int>>>> GetInterviewStatusDistribution(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            var distribution = await _reportingService.GetInterviewStatusDistributionAsync(fromDate, toDate);
            return Ok(ApiResponse<Dictionary<InterviewStatus, int>>.SuccessResponse(distribution, "Status distribution retrieved successfully"));
        }

        /// <summary>
        /// Get interview type distribution with optional date filtering
        /// </summary>
        [HttpGet("analytics/type-distribution")]
        [Authorize(Roles = "Admin,SuperAdmin,HR")]
        public async Task<ActionResult<ApiResponse<Dictionary<InterviewType, int>>>> GetInterviewTypeDistribution(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            var distribution = await _reportingService.GetInterviewTypeDistributionAsync(fromDate, toDate);
            return Ok(ApiResponse<Dictionary<InterviewType, int>>.SuccessResponse(distribution, "Type distribution retrieved successfully"));
        }

        /// <summary>
        /// Get comprehensive interview analytics
        /// </summary>
        [HttpGet("analytics")]
        [Authorize(Roles = "Admin,SuperAdmin,HR")]
        public async Task<ActionResult<ApiResponse<InterviewAnalyticsDto>>> GetInterviewAnalytics(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            var analytics = await _reportingService.GetInterviewAnalyticsAsync(fromDate, toDate);
            return Ok(ApiResponse<InterviewAnalyticsDto>.SuccessResponse(analytics, "Analytics retrieved successfully"));
        }

        #endregion

        #region Search and Filtering

        /// <summary>
        /// Search interviews with advanced filtering and pagination
        /// </summary>
        [HttpPost("search")]
        [Authorize(Roles = "Admin,SuperAdmin,HR")]
        public async Task<ActionResult<ApiResponse<PagedResult<InterviewSummaryDto>>>> SearchInterviews([FromBody] InterviewSearchDto searchDto)
        {
            var result = await _reportingService.SearchInterviewsAsync(searchDto);
            return Ok(ApiResponse<PagedResult<InterviewSummaryDto>>.SuccessResponse(result, "Interviews retrieved successfully"));
        }

        /// <summary>
        /// Get upcoming interviews for current user
        /// </summary>
        [HttpGet("upcoming")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<PagedResult<InterviewPublicSummaryDto>>>> GetUpcomingInterviews(
            [FromQuery] int days = 7,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            var userId = GetCurrentUserId();
            var result = await _reportingService.GetPublicUpcomingInterviewsForUserAsync(userId, days, pageNumber, pageSize);
            return Ok(ApiResponse<PagedResult<InterviewPublicSummaryDto>>.SuccessResponse(result, "Upcoming interviews retrieved successfully"));
        }

        /// <summary>
        /// Get upcoming interviews for a specific user (Admin/HR only)
        /// </summary>
        [HttpGet("users/{userId:guid}/upcoming")]
        [Authorize(Roles = "Admin,SuperAdmin,HR")]
        public async Task<ActionResult<ApiResponse<PagedResult<InterviewSummaryDto>>>> GetUpcomingInterviewsForUser(
            Guid userId,
            [FromQuery] int days = 7,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            var result = await _reportingService.GetUpcomingInterviewsForUserAsync(userId, days, pageNumber, pageSize);
            return Ok(ApiResponse<PagedResult<InterviewSummaryDto>>.SuccessResponse(result, "Upcoming interviews retrieved successfully"));
        }

        /// <summary>
        /// Get today's interviews (optionally filtered by participant)
        /// </summary>
        [HttpGet("today")]
        [Authorize(Roles = "Admin,SuperAdmin,HR")]
        public async Task<ActionResult<ApiResponse<PagedResult<InterviewSummaryDto>>>> GetTodayInterviews(
            [FromQuery] Guid? participantUserId = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            var result = await _reportingService.GetTodayInterviewsAsync(participantUserId, pageNumber, pageSize);
            return Ok(ApiResponse<PagedResult<InterviewSummaryDto>>.SuccessResponse(result, "Today's interviews retrieved successfully"));
        }

        /// <summary>
        /// Get interviews requiring action
        /// </summary>
        [HttpGet("requiring-action")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<PagedResult<InterviewPublicSummaryDto>>>> GetInterviewsRequiringAction(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            var isStaff = User.IsInRole("Admin") || User.IsInRole("SuperAdmin") || User.IsInRole("HR");

            if (isStaff)
            {
                var staffResult = await _reportingService.GetInterviewsNeedingActionAsync(null, pageNumber, pageSize);
                return Ok(ApiResponse<PagedResult<InterviewSummaryDto>>.SuccessResponse(staffResult, "Interviews requiring action retrieved successfully"));
            }

            var userId = GetCurrentUserId();
            var publicResult = await _reportingService.GetPublicInterviewsNeedingActionAsync(userId, pageNumber, pageSize);
            return Ok(ApiResponse<PagedResult<InterviewPublicSummaryDto>>.SuccessResponse(publicResult, "Interviews requiring action retrieved successfully"));
        }

        #endregion

        #region Application-specific Queries

        /// <summary>
        /// Get all interviews for a specific job application
        /// Recruiters can only view interviews for their assigned applications
        /// </summary>
        [HttpGet("applications/{jobApplicationId:guid}")]
        [Authorize(Roles = "Admin,SuperAdmin,HR,Recruiter")]
        public async Task<ActionResult<ApiResponse<PagedResult<InterviewSummaryDto>>>> GetInterviewsByApplication(
            Guid jobApplicationId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            var currentUserId = GetCurrentUserId();

            // Recruiters can only access their assigned applications
            if (!User.IsInRole("Admin") && !User.IsInRole("SuperAdmin") && User.IsInRole("Recruiter"))
            {
                var jobApplication = await _jobApplicationRepository.GetByIdAsync(jobApplicationId);
                if (jobApplication == null || jobApplication.AssignedRecruiterId != currentUserId)
                {
                    return Forbid("You don't have access to this application's interviews");
                }
            }

            var result = await _reportingService.GetInterviewsByApplicationAsync(jobApplicationId, pageNumber, pageSize);
            return Ok(ApiResponse<PagedResult<InterviewSummaryDto>>.SuccessResponse(result, "Interviews for application retrieved successfully"));
        }

        /// <summary>
        /// Get interviews where current user was a participant
        /// </summary>
        [HttpGet("my-participations")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<PagedResult<InterviewPublicSummaryDto>>>> GetMyInterviewParticipations(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            var userId = GetCurrentUserId();
            var result = await _reportingService.GetPublicInterviewsByParticipantAsync(userId, pageNumber, pageSize);
            return Ok(ApiResponse<PagedResult<InterviewPublicSummaryDto>>.SuccessResponse(result, "Interview participations retrieved successfully"));
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Gets the current user's ID from the JWT token
        /// </summary>
        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("User ID not found in token");
            }
            return userId;
        }

        #endregion
    }
}