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
    public class JobApplicationAnalyticsController : ControllerBase
    {
        private readonly IJobApplicationAnalyticsService _analyticsService;
        private readonly IMapper _mapper;
        private readonly ILogger<JobApplicationAnalyticsController> _logger;

        public JobApplicationAnalyticsController(
            IJobApplicationAnalyticsService analyticsService,
            IMapper mapper,
            ILogger<JobApplicationAnalyticsController> logger)
        {
            _analyticsService = analyticsService;
            _mapper = mapper;
            _logger = logger;
        }

        #region Search and Filtering

        /// <summary>
        /// Search applications with filters (paginated)
        /// </summary>
        [HttpGet("search")]
        [Authorize(Roles = "Recruiter, HR, Admin, SuperAdmin")]
        [ResponseCache(Duration = 60, VaryByQueryKeys = new[] { "status", "jobPositionId", "candidateProfileId", "assignedRecruiterId", "pageNumber", "pageSize" })]
        public async Task<ActionResult<PagedResult<JobApplicationSummaryDto>>> SearchApplications(
            [FromQuery] ApplicationStatus? status,
            [FromQuery] Guid? jobPositionId,
            [FromQuery] Guid? candidateProfileId,
            [FromQuery] Guid? assignedRecruiterId,
            [FromQuery] DateTime? appliedFromDate,
            [FromQuery] DateTime? appliedToDate,
            [FromQuery] int? minTestScore,
            [FromQuery] int? maxTestScore,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var (applications, totalCount) = await _analyticsService.SearchApplicationsAsync(
                    status, jobPositionId, candidateProfileId, assignedRecruiterId,
                    appliedFromDate, appliedToDate, minTestScore, maxTestScore, pageNumber, pageSize);

                var dtos = _mapper.Map<List<JobApplicationSummaryDto>>(applications);
                var pagedResult = PagedResult<JobApplicationSummaryDto>.Create(dtos, totalCount, pageNumber, pageSize);

                return Ok(ApiResponse<PagedResult<JobApplicationSummaryDto>>.SuccessResponse(pagedResult, "Applications retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching applications");
                return StatusCode(500, ApiResponse<PagedResult<JobApplicationSummaryDto>>.FailureResponse(new List<string> { "An error occurred while searching applications" }, "Couldn't Search Applications"));
            }
        }

        #endregion

        #region Statistics and Analytics

        /// <summary>
        /// Get application count by job position
        /// </summary>
        [HttpGet("stats/job/{jobPositionId:guid}/count")]
        [Authorize(Roles = "Recruiter, HR, Admin, SuperAdmin")]
        [ResponseCache(Duration = 120, VaryByQueryKeys = new[] { "jobPositionId" })]
        public async Task<ActionResult<int>> GetApplicationCountByJob(Guid jobPositionId)
        {
            try
            {
                var count = await _analyticsService.GetApplicationCountByJobAsync(jobPositionId);
                return Ok(ApiResponse<int>.SuccessResponse(count, "Application count retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting application count for job: {JobPositionId}", jobPositionId);
                return StatusCode(500, ApiResponse<int>.FailureResponse(new List<string> { "An error occurred while retrieving application count" }, "Couldn't Get Count"));
            }
        }

        /// <summary>
        /// Get application count by status
        /// </summary>
        [HttpGet("stats/status/{status}/count")]
        [Authorize(Roles = "Recruiter, HR, Admin, SuperAdmin")]
        [ResponseCache(Duration = 120, VaryByQueryKeys = new[] { "status" })]
        public async Task<ActionResult<int>> GetApplicationCountByStatus(ApplicationStatus status)
        {
            try
            {
                var count = await _analyticsService.GetApplicationCountByStatusAsync(status);
                return Ok(ApiResponse<int>.SuccessResponse(count, "Application count retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting application count for status: {Status}", status);
                return StatusCode(500, ApiResponse<int>.FailureResponse(new List<string> { "An error occurred while retrieving application count" }, "Couldn't Get Count"));
            }
        }

        /// <summary>
        /// Get recent applications (paginated)
        /// </summary>
        [HttpGet("recent")]
        [Authorize(Roles = "Recruiter, HR, Admin, SuperAdmin")]
        [ResponseCache(Duration = 30, VaryByQueryKeys = new[] { "pageNumber", "pageSize" })]
        public async Task<ActionResult<PagedResult<JobApplicationSummaryDto>>> GetRecentApplications(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var (applications, totalCount) = await _analyticsService.GetRecentApplicationsAsync(pageNumber, pageSize);
                var dtos = _mapper.Map<List<JobApplicationSummaryDto>>(applications);
                var pagedResult = PagedResult<JobApplicationSummaryDto>.Create(dtos, totalCount, pageNumber, pageSize);

                return Ok(ApiResponse<PagedResult<JobApplicationSummaryDto>>.SuccessResponse(pagedResult, "Recent applications retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent applications");
                return StatusCode(500, ApiResponse<PagedResult<JobApplicationSummaryDto>>.FailureResponse(new List<string> { "An error occurred while retrieving recent applications" }, "Couldn't Get Recent Applications"));
            }
        }

        /// <summary>
        /// Get applications requiring action (paginated)
        /// </summary>
        [HttpGet("requiring-action")]
        [Authorize(Roles = "Recruiter, HR, Admin, SuperAdmin")]
        public async Task<ActionResult<PagedResult<JobApplicationSummaryDto>>> GetApplicationsRequiringAction(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var recruiterId = User.IsInRole("Recruiter") ? (Guid?)GetCurrentUserId() : null;
                var (applications, totalCount) = await _analyticsService.GetApplicationsRequiringActionAsync(recruiterId, pageNumber, pageSize);
                var dtos = _mapper.Map<List<JobApplicationSummaryDto>>(applications);
                var pagedResult = PagedResult<JobApplicationSummaryDto>.Create(dtos, totalCount, pageNumber, pageSize);

                return Ok(ApiResponse<PagedResult<JobApplicationSummaryDto>>.SuccessResponse(pagedResult, "Applications requiring action retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting applications requiring action");
                return StatusCode(500, ApiResponse<PagedResult<JobApplicationSummaryDto>>.FailureResponse(new List<string> { "An error occurred while retrieving applications requiring action" }, "Couldn't Get Applications"));
            }
        }

        /// <summary>
        /// Get application status distribution
        /// </summary>
        [HttpGet("stats/status-distribution")]
        [Authorize(Roles = "Recruiter, HR, Admin, SuperAdmin")]
        [ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "jobPositionId" })]
        public async Task<ActionResult<Dictionary<ApplicationStatus, int>>> GetStatusDistribution([FromQuery] Guid? jobPositionId = null)
        {
            try
            {
                var distribution = await _analyticsService.GetApplicationStatusDistributionAsync(jobPositionId);
                return Ok(ApiResponse<Dictionary<ApplicationStatus, int>>.SuccessResponse(distribution, "Status distribution retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting status distribution");
                return StatusCode(500, ApiResponse<Dictionary<ApplicationStatus, int>>.FailureResponse(new List<string> { "An error occurred while retrieving status distribution" }, "Couldn't Get Distribution"));
            }
        }

        #endregion

        #region Private Methods

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