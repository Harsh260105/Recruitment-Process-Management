using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Core.Enums;
using RecruitmentSystem.Services.Interfaces;
using RecruitmentSystem.Shared.DTOs;
using RecruitmentSystem.Shared.DTOs.Responses;
using System.Security.Claims;

namespace RecruitmentSystem.API.Controllers
{
    [Route("api/job-offers")]
    [ApiController]
    [Authorize]
    public class JobOfferController : ControllerBase
    {
        private readonly IJobOfferService _jobOfferService;
        private readonly IAuthenticationService _authenticationService;
        private readonly IMapper _mapper;
        private readonly ILogger<JobOfferController> _logger;

        // Cache user ID and roles
        private Guid? _currentUserId;
        private List<string>? _currentUserRoles;

        public JobOfferController(
            IJobOfferService jobOfferService,
            IAuthenticationService authenticationService,
            IMapper mapper,
            ILogger<JobOfferController> logger)
        {
            _jobOfferService = jobOfferService;
            _authenticationService = authenticationService;
            _mapper = mapper;
            _logger = logger;
        }

        #region Job Offer CRUD Operations

        /// <summary>
        /// Get job offer by ID (Role-based access)
        /// </summary>
        [HttpGet("{id:guid}")]
        [Authorize(Roles = "Candidate, HR, Admin, SuperAdmin")]
        public async Task<ActionResult<ApiResponse<JobOfferDto>>> GetById(Guid id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRoles = await GetCurrentUserRolesAsync();

                // Only candidates need ownership check, HR/Admin have full access
                if (currentUserRoles.Contains("Candidate") && !await _jobOfferService.CanCandidateAccessOfferAsync(id, currentUserId))
                {
                    return StatusCode(403, ApiResponse<JobOfferDto>.FailureResponse(new List<string> { "Access denied" }, "Forbidden"));
                }

                var offer = await _jobOfferService.GetOfferByIdAsync(id);
                if (offer == null)
                {
                    return NotFound(ApiResponse<JobOfferDto>.FailureResponse(new List<string> { "Job offer not found" }, "Not Found"));
                }

                var offerDto = _mapper.Map<JobOfferDto>(offer);
                return Ok(ApiResponse<JobOfferDto>.SuccessResponse(offerDto, "Job offer retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting job offer by ID: {Id}", id);
                return StatusCode(500, ApiResponse<JobOfferDto>.FailureResponse(new List<string> { "An error occurred while retrieving the offer" }, "Internal Server Error"));
            }
        }

        /// <summary>
        /// Get job offer by application ID (Role-based access)
        /// </summary>
        [HttpGet("application/{applicationId:guid}")]
        [Authorize(Roles = "Candidate, HR, Admin, SuperAdmin")]
        public async Task<ActionResult<ApiResponse<JobOfferDto>>> GetByApplicationId(Guid applicationId)
        {
            try
            {
                var offer = await _jobOfferService.GetOfferByApplicationIdAsync(applicationId);
                if (offer == null)
                {
                    return NotFound(ApiResponse<JobOfferDto>.FailureResponse(new List<string> { "Job offer not found" }, "Not Found"));
                }

                var currentUserId = GetCurrentUserId();
                var currentUserRoles = await GetCurrentUserRolesAsync();

                // Only candidates need ownership check, HR/Admin have full access
                if (currentUserRoles.Contains("Candidate") && !await _jobOfferService.CanCandidateAccessOfferAsync(offer.Id, currentUserId))
                {
                    return StatusCode(403, ApiResponse<JobOfferDto>.FailureResponse(new List<string> { "Access denied" }, "Forbidden"));
                }

                var offerDto = _mapper.Map<JobOfferDto>(offer);
                return Ok(ApiResponse<JobOfferDto>.SuccessResponse(offerDto, "Job offer retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting job offer by application ID: {ApplicationId}", applicationId);
                return StatusCode(500, ApiResponse<JobOfferDto>.FailureResponse(new List<string> { "An error occurred while retrieving the offer" }, "Internal Server Error"));
            }
        }

        #endregion

        #region HR Offer Management

        /// <summary>
        /// Extend job offer (HR/Admin only)
        /// </summary>
        [HttpPost("extend")]
        [Authorize(Roles = "HR, Admin, SuperAdmin")]
        public async Task<ActionResult<ApiResponse<JobOfferDto>>> ExtendOffer([FromBody] JobOfferExtendDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<JobOfferDto>.FailureResponse(new List<string> { "Invalid request data" }, "Bad Request"));
                }

                var currentUserId = GetCurrentUserId();

                if (!await _jobOfferService.CanExtendOfferAsync(request.JobApplicationId))
                {
                    return BadRequest(ApiResponse<JobOfferDto>.FailureResponse(new List<string> { "Cannot extend offer for this application" }, "Invalid Operation"));
                }

                var offer = await _jobOfferService.ExtendOfferAsync(
                    request.JobApplicationId,
                    request.OfferedSalary,
                    request.Benefits,
                    request.JobTitle,
                    request.ExpiryDate,
                    request.JoiningDate,
                    currentUserId,
                    request.Notes);

                // Send notification email
                await _jobOfferService.SendOfferNotificationAsync(offer.Id);

                var offerDto = _mapper.Map<JobOfferDto>(offer);
                return Ok(ApiResponse<JobOfferDto>.SuccessResponse(offerDto, "Job offer extended successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extending job offer for application: {ApplicationId}", request.JobApplicationId);
                return StatusCode(500, ApiResponse<JobOfferDto>.FailureResponse(new List<string> { "An error occurred while extending the offer" }, "Internal Server Error"));
            }
        }

        /// <summary>
        /// Withdraw job offer (HR/Admin only)
        /// </summary>
        [HttpPut("{id:guid}/withdraw")]
        [Authorize(Roles = "HR, Admin, SuperAdmin")]
        public async Task<ActionResult<ApiResponse<JobOfferDto>>> WithdrawOffer(Guid id, [FromBody] JobOfferWithdrawDto request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                var offer = await _jobOfferService.WithdrawOfferAsync(id, currentUserId, request?.Reason);
                var offerDto = _mapper.Map<JobOfferDto>(offer);

                return Ok(ApiResponse<JobOfferDto>.SuccessResponse(offerDto, "Job offer withdrawn successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error withdrawing job offer: {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        #endregion

        #region Candidate Offer Actions

        /// <summary>
        /// Accept job offer (Candidate only - own offers)
        /// </summary>
        [HttpPut("{id:guid}/accept")]
        [Authorize(Roles = "Candidate")]
        public async Task<ActionResult<ApiResponse<JobOfferDto>>> AcceptOffer(Guid id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                var offer = await _jobOfferService.AcceptOfferAsync(id, currentUserId);
                var offerDto = _mapper.Map<JobOfferDto>(offer);

                return Ok(ApiResponse<JobOfferDto>.SuccessResponse(offerDto, "Job offer accepted successfully"));
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(403, ApiResponse<JobOfferDto>.FailureResponse(new List<string> { "Access denied" }, "Forbidden"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting job offer: {Id}", id);
                return StatusCode(500, ApiResponse<JobOfferDto>.FailureResponse(new List<string> { "An error occurred while accepting the offer" }, "Internal Server Error"));
            }
        }

        /// <summary>
        /// Reject job offer (Candidate only - own offers)
        /// </summary>
        [HttpPut("{id:guid}/reject")]
        [Authorize(Roles = "Candidate")]
        public async Task<ActionResult<ApiResponse<JobOfferDto>>> RejectOffer(Guid id, [FromBody] JobOfferRejectDto? request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                var offer = await _jobOfferService.RejectOfferAsync(id, currentUserId, request?.RejectionReason);
                var offerDto = _mapper.Map<JobOfferDto>(offer);

                return Ok(ApiResponse<JobOfferDto>.SuccessResponse(offerDto, "Job offer rejected successfully"));
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(403, ApiResponse<JobOfferDto>.FailureResponse(new List<string> { "Access denied" }, "Forbidden"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting job offer: {Id}", id);
                return StatusCode(500, ApiResponse<JobOfferDto>.FailureResponse(new List<string> { "An error occurred while rejecting the offer" }, "Internal Server Error"));
            }
        }

        /// <summary>
        /// Submit counter offer (Candidate only - own offers)
        /// </summary>
        [HttpPut("{id:guid}/counter")]
        [Authorize(Roles = "Candidate")]
        public async Task<ActionResult<ApiResponse<JobOfferDto>>> CounterOffer(Guid id, [FromBody] JobOfferCounterDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<JobOfferDto>.FailureResponse(new List<string> { "Invalid request data" }, "Bad Request"));
                }

                var currentUserId = GetCurrentUserId();

                var offer = await _jobOfferService.CounterOfferAsync(id, request.CounterAmount, request.CounterNotes, currentUserId);
                var offerDto = _mapper.Map<JobOfferDto>(offer);

                return Ok(ApiResponse<JobOfferDto>.SuccessResponse(offerDto, "Counter offer submitted successfully"));
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(403, ApiResponse<JobOfferDto>.FailureResponse(new List<string> { "Access denied" }, "Forbidden"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting counter offer: {Id}", id);
                return StatusCode(500, ApiResponse<JobOfferDto>.FailureResponse(new List<string> { "An error occurred while submitting the counter offer" }, "Internal Server Error"));
            }
        }

        #endregion

        #region HR Negotiation Management

        /// <summary>
        /// Respond to counter offer (HR/Admin only)
        /// </summary>
        [HttpPut("{id:guid}/respond-counter")]
        [Authorize(Roles = "HR, Admin, SuperAdmin")]
        public async Task<ActionResult<ApiResponse<JobOfferDto>>> RespondToCounterOffer(Guid id, [FromBody] JobOfferRespondToCounterDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<JobOfferDto>.FailureResponse(new List<string> { "Invalid request data" }, "Bad Request"));
                }

                var currentUserId = GetCurrentUserId();

                var offer = await _jobOfferService.RespondToCounterOfferAsync(
                    id, request.Accepted, currentUserId, request.RevisedSalary, request.Response);

                var offerDto = _mapper.Map<JobOfferDto>(offer);
                return Ok(ApiResponse<JobOfferDto>.SuccessResponse(offerDto, "Counter offer response submitted successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error responding to counter offer: {Id}", id);
                return StatusCode(500, ApiResponse<JobOfferDto>.FailureResponse(new List<string> { "An error occurred while responding to the counter offer" }, "Internal Server Error"));
            }
        }

        /// <summary>
        /// Extend offer expiry date (HR/Admin only)
        /// </summary>
        [HttpPut("{id:guid}/extend-expiry")]
        [Authorize(Roles = "HR, Admin, SuperAdmin")]
        public async Task<ActionResult<ApiResponse<JobOfferDto>>> ExtendOfferExpiry(Guid id, [FromBody] JobOfferExtendExpiryDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<JobOfferDto>.FailureResponse(new List<string> { "Invalid request data" }, "Bad Request"));
                }

                var currentUserId = GetCurrentUserId();

                var offer = await _jobOfferService.ExtendOfferExpiryAsync(id, request.NewExpiryDate, currentUserId, request.Reason);
                var offerDto = _mapper.Map<JobOfferDto>(offer);

                return Ok(ApiResponse<JobOfferDto>.SuccessResponse(offerDto, "Offer expiry extended successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extending offer expiry: {Id}", id);
                return StatusCode(500, ApiResponse<JobOfferDto>.FailureResponse(new List<string> { "An error occurred while extending the offer expiry" }, "Internal Server Error"));
            }
        }

        /// <summary>
        /// Revise offer terms (HR/Admin only)
        /// </summary>
        [HttpPut("{id:guid}/revise")]
        [Authorize(Roles = "HR, Admin, SuperAdmin")]
        public async Task<ActionResult<ApiResponse<JobOfferDto>>> ReviseOffer(Guid id, [FromBody] JobOfferReviseDto request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                var offer = await _jobOfferService.ReviseOfferAsync(id, request.NewSalary, request.NewBenefits, request.NewJoiningDate, currentUserId);
                var offerDto = _mapper.Map<JobOfferDto>(offer);

                return Ok(ApiResponse<JobOfferDto>.SuccessResponse(offerDto, "Offer revised successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revising offer: {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        #endregion

        #region Search and Filtering

        /// <summary>
        /// Search offers with filters (HR/Admin only)
        /// </summary>
        [HttpGet("search")]
        [Authorize(Roles = "HR, Admin, SuperAdmin")]
        public async Task<ActionResult<ApiResponse<PagedResult<JobOfferSummaryDto>>>> SearchOffers(
            [FromQuery] OfferStatus? status = null,
            [FromQuery] Guid? extendedByUserId = null,
            [FromQuery] DateTime? offerFromDate = null,
            [FromQuery] DateTime? offerToDate = null,
            [FromQuery] DateTime? expiryFromDate = null,
            [FromQuery] DateTime? expiryToDate = null,
            [FromQuery] decimal? minSalary = null,
            [FromQuery] decimal? maxSalary = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var offers = await _jobOfferService.SearchOffersAsync(
                    status, extendedByUserId, offerFromDate, offerToDate,
                    expiryFromDate, expiryToDate, minSalary, maxSalary, pageNumber, pageSize);

                return Ok(ApiResponse<PagedResult<JobOfferSummaryDto>>.SuccessResponse(offers, "Offers retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching offers");
                return StatusCode(500, ApiResponse<PagedResult<JobOfferSummaryDto>>.FailureResponse(new List<string> { "An error occurred while searching offers" }, "Internal Server Error"));
            }
        }

        /// <summary>
        /// Get offers by status (HR/Admin only)
        /// </summary>
        [HttpGet("status/{status}")]
        [Authorize(Roles = "HR, Admin, SuperAdmin")]
        public async Task<ActionResult<ApiResponse<PagedResult<JobOfferSummaryDto>>>> GetOffersByStatus(
            OfferStatus status,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var offers = await _jobOfferService.GetOffersByStatusPagedAsync(status, pageNumber, pageSize);

                return Ok(ApiResponse<PagedResult<JobOfferSummaryDto>>.SuccessResponse(offers, $"Offers with status {status} retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting offers by status: {Status}", status);
                return StatusCode(500, ApiResponse<PagedResult<JobOfferSummaryDto>>.FailureResponse(new List<string> { "An error occurred while retrieving offers" }, "Internal Server Error"));
            }
        }

        /// <summary>
        /// Get offers extended by a specific user (HR/Admin only)
        /// </summary>
        [HttpGet("extended-by/{extendedByUserId:guid}")]
        [Authorize(Roles = "HR, Admin, SuperAdmin")]
        public async Task<ActionResult<ApiResponse<PagedResult<JobOfferSummaryDto>>>> GetOffersExtendedByUser(
            Guid extendedByUserId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var offers = await _jobOfferService.GetOffersByExtendedByUserPagedAsync(extendedByUserId, pageNumber, pageSize);

                return Ok(ApiResponse<PagedResult<JobOfferSummaryDto>>.SuccessResponse(offers, "Offers extended by user retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting offers extended by user: {UserId}", extendedByUserId);
                return StatusCode(500, ApiResponse<PagedResult<JobOfferSummaryDto>>.FailureResponse(new List<string> { "An error occurred while retrieving offers extended by this user" }, "Internal Server Error"));
            }
        }

        /// <summary>
        /// Get offers requiring action (HR/Admin only)
        /// </summary>
        [HttpGet("requiring-action")]
        [Authorize(Roles = "HR, Admin, SuperAdmin")]
        public async Task<ActionResult<ApiResponse<PagedResult<JobOfferSummaryDto>>>> GetOffersRequiringAction(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var offers = await _jobOfferService.GetOffersRequiringActionAsync(null, pageNumber, pageSize);

                return Ok(ApiResponse<PagedResult<JobOfferSummaryDto>>.SuccessResponse(offers, "Offers requiring action retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting offers requiring action");
                return StatusCode(500, ApiResponse<PagedResult<JobOfferSummaryDto>>.FailureResponse(new List<string> { "An error occurred while retrieving offers requiring action" }, "Internal Server Error"));
            }
        }

        /// <summary>
        /// Get candidate's own offers (Candidate only)
        /// </summary>
        [HttpGet("my-offers")]
        [Authorize(Roles = "Candidate")]
        public async Task<ActionResult<ApiResponse<PagedResult<JobOfferSummaryDto>>>> GetMyOffers(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                var offers = await _jobOfferService.GetOffersByCandidateUserIdAsync(currentUserId, pageNumber, pageSize);
                return Ok(ApiResponse<PagedResult<JobOfferSummaryDto>>.SuccessResponse(offers, "Your offers retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting candidate offers for user: {UserId}", GetCurrentUserId());
                return StatusCode(500, ApiResponse<PagedResult<JobOfferSummaryDto>>.FailureResponse(new List<string> { "An error occurred while retrieving your offers" }, "Internal Server Error"));
            }
        }

        #endregion

        #region Expiration Management

        /// <summary>
        /// Get expiring offers (HR/Admin only)
        /// </summary>
        [HttpGet("expiring")]
        [Authorize(Roles = "HR, Admin, SuperAdmin")]
        public async Task<ActionResult<ApiResponse<PagedResult<JobOfferSummaryDto>>>> GetExpiringOffers(
            [FromQuery] int daysAhead = 3,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var offers = await _jobOfferService.GetExpiringOffersAsync(daysAhead, pageNumber, pageSize);

                return Ok(ApiResponse<PagedResult<JobOfferSummaryDto>>.SuccessResponse(
                    offers, $"Offers expiring within {daysAhead} days retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting expiring offers");
                return StatusCode(500, ApiResponse<PagedResult<JobOfferSummaryDto>>.FailureResponse(new List<string> { "An error occurred while retrieving expiring offers" }, "Internal Server Error"));
            }
        }

        /// <summary>
        /// Get expired offers (HR/Admin only)
        /// </summary>
        [HttpGet("expired")]
        [Authorize(Roles = "HR, Admin, SuperAdmin")]
        public async Task<ActionResult<ApiResponse<PagedResult<JobOfferSummaryDto>>>> GetExpiredOffers(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var offers = await _jobOfferService.GetExpiredOffersAsync(pageNumber, pageSize);

                return Ok(ApiResponse<PagedResult<JobOfferSummaryDto>>.SuccessResponse(
                    offers, "Expired offers retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting expired offers");
                return StatusCode(500, ApiResponse<PagedResult<JobOfferSummaryDto>>.FailureResponse(new List<string> { "An error occurred while retrieving expired offers" }, "Internal Server Error"));
            }
        }

        /// <summary>
        /// Mark offer as expired (HR/Admin only)
        /// </summary>
        [HttpPut("{id:guid}/mark-expired")]
        [Authorize(Roles = "HR, Admin, SuperAdmin")]
        public async Task<ActionResult<ApiResponse<JobOfferDto>>> MarkOfferExpired(Guid id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                var offer = await _jobOfferService.MarkOfferExpiredAsync(id, currentUserId);
                var offerDto = _mapper.Map<JobOfferDto>(offer);

                return Ok(ApiResponse<JobOfferDto>.SuccessResponse(offerDto, "Offer marked as expired successfully"));
            }
            catch (ArgumentException)
            {
                return NotFound(ApiResponse<JobOfferDto>.FailureResponse(new List<string> { "Job offer not found" }, "Not Found"));
            }
            catch (InvalidOperationException)
            {
                return BadRequest(ApiResponse<JobOfferDto>.FailureResponse(new List<string> { "Cannot mark this offer as expired" }, "Invalid Operation"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking offer as expired: {Id}", id);
                return StatusCode(500, ApiResponse<JobOfferDto>.FailureResponse(new List<string> { "An error occurred while marking the offer as expired" }, "Internal Server Error"));
            }
        }

        /// <summary>
        /// Send expiry reminder (HR/Admin only)
        /// </summary>
        // [HttpPost("{id:guid}/send-reminder")]
        // [Authorize(Roles = "HR, Admin, SuperAdmin")]
        // public async Task<ActionResult<ApiResponse<object?>>> SendExpiryReminder(Guid id, [FromQuery] int daysBefore = 1)
        // {
        //     try
        //     {
        //         var success = await _jobOfferService.SendExpiryReminderAsync(id, daysBefore); if (success)
        //         {
        //             return Ok(ApiResponse<object?>.SuccessResponse(null, "Expiry reminder sent successfully"));
        //         }
        //         else
        //         {
        //             return BadRequest(ApiResponse<object?>.FailureResponse(new List<string> { "Failed to send expiry reminder" }, "Reminder Not Sent"));
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "Error sending expiry reminder for offer: {Id}", id);
        //         return StatusCode(500, ApiResponse<object?>.FailureResponse(new List<string> { "An error occurred while sending the expiry reminder" }, "Internal Server Error"));
        //     }
        // }

        /// <summary>
        /// Process and mark expired offers in bulk (Admin/SuperAdmin only)
        /// </summary>
        // [HttpPost("process-expired")]
        // [Authorize(Roles = "Admin, SuperAdmin")]
        // public async Task<ActionResult<ApiResponse<int>>> ProcessExpiredOffers()
        // {
        //     try
        //     {
        //         var currentUserId = GetCurrentUserId();
        //         var processedCount = await _jobOfferService.ProcessExpiredOffersAsync(currentUserId);
        //         return Ok(ApiResponse<int>.SuccessResponse(processedCount, "Expired offers processed successfully"));
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "Error processing expired offers");
        //         return StatusCode(500, ApiResponse<int>.FailureResponse(new List<string> { "An error occurred while processing expired offers" }, "Internal Server Error"));
        //     }
        // }
        #endregion

        #region Analytics and Reporting

        /// <summary>
        /// Get offer status distribution (HR/Admin only)
        /// </summary>
        [HttpGet("analytics/status-distribution")]
        [Authorize(Roles = "HR, Admin, SuperAdmin")]
        public async Task<ActionResult<ApiResponse<Dictionary<OfferStatus, int>>>> GetOfferStatusDistribution()
        {
            try
            {
                var distribution = await _jobOfferService.GetOfferStatusDistributionAsync();
                return Ok(ApiResponse<Dictionary<OfferStatus, int>>.SuccessResponse(
                    distribution, "Offer status distribution retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting offer status distribution");
                return StatusCode(500, ApiResponse<Dictionary<OfferStatus, int>>.FailureResponse(new List<string> { "An error occurred while retrieving offer status distribution" }, "Internal Server Error"));
            }
        }

        /// <summary>
        /// Get average offer amount (HR/Admin only)
        /// </summary>
        [HttpGet("analytics/average-amount")]
        [Authorize(Roles = "HR, Admin, SuperAdmin")]
        public async Task<ActionResult<ApiResponse<decimal>>> GetAverageOfferAmount([FromQuery] Guid? jobPositionId = null)
        {
            try
            {
                var average = await _jobOfferService.GetAverageOfferAmountAsync(jobPositionId);
                return Ok(ApiResponse<decimal>.SuccessResponse(average, "Average offer amount retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting average offer amount");
                return StatusCode(500, ApiResponse<decimal>.FailureResponse(new List<string> { "An error occurred while retrieving average offer amount" }, "Internal Server Error"));
            }
        }

        /// <summary>
        /// Get offer acceptance rate (HR/Admin only)
        /// </summary>
        [HttpGet("analytics/acceptance-rate")]
        [Authorize(Roles = "HR, Admin, SuperAdmin")]
        public async Task<ActionResult<ApiResponse<double>>> GetOfferAcceptanceRate(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var rate = await _jobOfferService.GetOfferAcceptanceRateAsync(fromDate, toDate);
                return Ok(ApiResponse<double>.SuccessResponse(rate, "Offer acceptance rate retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting offer acceptance rate");
                return StatusCode(500, ApiResponse<double>.FailureResponse(new List<string> { "An error occurred while retrieving offer acceptance rate" }, "Internal Server Error"));
            }
        }

        /// <summary>
        /// Get average offer response time (HR/Admin only)
        /// </summary>
        [HttpGet("analytics/response-time")]
        [Authorize(Roles = "HR, Admin, SuperAdmin")]
        public async Task<ActionResult<ApiResponse<TimeSpan>>> GetAverageOfferResponseTime()
        {
            try
            {
                var responseTime = await _jobOfferService.GetAverageOfferResponseTimeAsync();
                return Ok(ApiResponse<TimeSpan>.SuccessResponse(responseTime, "Average offer response time retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting average offer response time");
                return StatusCode(500, ApiResponse<TimeSpan>.FailureResponse(new List<string> { "An error occurred while retrieving average offer response time" }, "Internal Server Error"));
            }
        }

        /// <summary>
        /// Get offer trends within a date range (HR/Admin only)
        /// </summary>
        [HttpGet("analytics/trends")]
        [Authorize(Roles = "HR, Admin, SuperAdmin")]
        public async Task<ActionResult<ApiResponse<PagedResult<JobOfferSummaryDto>>>> GetOfferTrends(
            [FromQuery] DateTime fromDate,
            [FromQuery] DateTime toDate,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                if (fromDate == default || toDate == default || fromDate > toDate)
                {
                    return BadRequest(ApiResponse<PagedResult<JobOfferSummaryDto>>.FailureResponse(new List<string> { "Invalid date range supplied" }, "Bad Request"));
                }

                var trends = await _jobOfferService.GetOfferTrendsAsync(fromDate, toDate, pageNumber, pageSize);
                return Ok(ApiResponse<PagedResult<JobOfferSummaryDto>>.SuccessResponse(trends, "Offer trends retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting offer trends between {FromDate} and {ToDate}", fromDate, toDate);
                return StatusCode(500, ApiResponse<PagedResult<JobOfferSummaryDto>>.FailureResponse(new List<string> { "An error occurred while retrieving offer trends" }, "Internal Server Error"));
            }
        }

        #endregion

        #region Private Helper Methods

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

        #endregion
    }

}