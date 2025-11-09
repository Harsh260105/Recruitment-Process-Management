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
    [Route("api/job-applications")]
    [ApiController]
    [Authorize]
    public class JobApplicationManagementController : ControllerBase
    {
        private readonly IJobApplicationManagementService _jobApplicationService;
        private readonly ICandidateProfileService _candidateProfileService;
        private readonly IAuthenticationService _authenticationService;
        private readonly IMapper _mapper;
        private readonly ILogger<JobApplicationManagementController> _logger;

        // Cache user ID and roles
        private Guid? _currentUserId;
        private List<string>? _currentUserRoles;

        public JobApplicationManagementController(
            IJobApplicationManagementService jobApplicationService,
            ICandidateProfileService candidateProfileService,
            IAuthenticationService authenticationService,
            IMapper mapper,
            ILogger<JobApplicationManagementController> logger)
        {
            _jobApplicationService = jobApplicationService;
            _candidateProfileService = candidateProfileService;
            _authenticationService = authenticationService;
            _mapper = mapper;
            _logger = logger;
        }

        #region Basic CRUD Operations

        /// <summary>
        /// Get job application by ID with full details (Candidates can see their own, Staff can see assigned/all)
        /// Returns role-appropriate view: CandidateViewDto for candidates, StaffViewDto for staff
        /// </summary>
        [HttpGet("{id:guid}")]
        [Authorize(Roles = "Candidate, Recruiter, HR, Admin, SuperAdmin")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var entity = await _jobApplicationService.GetApplicationEntityByIdAsync(id);
                if (entity == null)
                {
                    return NotFound(ApiResponse<object>.FailureResponse(
                        new List<string> { $"Job application with ID {id} not found" },
                        "Not Found"));
                }

                if (!await CanAccessApplication(entity))
                {
                    return Forbid();
                }

                var detailedApplication = await _jobApplicationService.GetApplicationWithDetailsAsync(id);

                // Determine user role and return appropriate DTO
                var currentUserRoles = await GetCurrentUserRolesAsync();
                var isStaff = currentUserRoles.Contains("Recruiter") ||
                              currentUserRoles.Contains("HR") ||
                              currentUserRoles.Contains("Admin") ||
                              currentUserRoles.Contains("SuperAdmin");

                if (isStaff)
                {
                    var staffDto = _mapper.Map<JobApplicationStaffViewDto>(detailedApplication);
                    return Ok(ApiResponse<JobApplicationStaffViewDto>.SuccessResponse(staffDto, "Application retrieved successfully"));
                }
                else
                {
                    var candidateDto = _mapper.Map<JobApplicationCandidateViewDto>(detailedApplication);
                    return Ok(ApiResponse<JobApplicationCandidateViewDto>.SuccessResponse(candidateDto, "Application retrieved successfully"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting job application by ID: {Id}", id);
                return StatusCode(500, ApiResponse<object>.FailureResponse(
                    new List<string> { "An error occurred while retrieving the job application" },
                    "Couldn't Retrieve Application"));
            }
        }

        /// <summary>
        /// Get applications by job position (Staff only)
        /// </summary>
        [HttpGet("job/{jobPositionId:guid}")]
        [Authorize(Roles = "Recruiter, HR, Admin, SuperAdmin")]
        public async Task<ActionResult<PagedResult<JobApplicationSummaryDto>>> GetByJobPosition(
            Guid jobPositionId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRoles = await GetCurrentUserRolesAsync();

                var pagedResult = await _jobApplicationService.GetApplicationsByJobForUserAsync(
                    jobPositionId, currentUserId, currentUserRoles, pageNumber, pageSize);

                return Ok(ApiResponse<PagedResult<JobApplicationSummaryDto>>.SuccessResponse(pagedResult, "Applications retrieved successfully"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument for paged applications by job position: {JobPositionId}", jobPositionId);
                return BadRequest(ApiResponse<PagedResult<JobApplicationSummaryDto>>.FailureResponse(
                    new List<string> { "An error occurred while processing your request" },
                    "Invalid Request"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paged applications by job position: {JobPositionId}", jobPositionId);
                return StatusCode(500, ApiResponse<PagedResult<JobApplicationSummaryDto>>.FailureResponse(
                    new List<string> { "An error occurred while retrieving applications" },
                    "Couldn't Retrieve Applications"));
            }
        }

        /// <summary>
        /// Get applications by candidate (Candidates can see their own, Admin staff can see all)
        /// </summary>
        [HttpGet("candidate/{candidateProfileId:guid}")]
        [Authorize(Roles = "Candidate, Recruiter, HR, Admin, SuperAdmin")]
        public async Task<ActionResult<List<JobApplicationSummaryDto>>> GetByCandidate(Guid candidateProfileId)
        {
            try
            {
                if (!await CanAccessCandidateApplications(candidateProfileId))
                {
                    return Forbid();
                }

                var dtos = await _jobApplicationService.GetApplicationsByCandidateAsync(candidateProfileId);

                return Ok(ApiResponse<List<JobApplicationSummaryDto>>.SuccessResponse(dtos.ToList(), "Applications retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting applications by candidate: {CandidateProfileId}", candidateProfileId);
                return StatusCode(500, ApiResponse<List<JobApplicationSummaryDto>>.FailureResponse(new List<string> { "An error occurred while retrieving applications" }, "Couldn't Retrieve Applications"));
            }
        }



        /// <summary>
        /// Get applications assigned to current recruiter
        /// </summary>
        [HttpGet("my-assigned")]
        [Authorize(Roles = "Recruiter, HR, Admin, SuperAdmin")]
        public async Task<ActionResult<List<JobApplicationSummaryDto>>> GetMyAssignedApplications()
        {
            try
            {
                var recruiterId = GetCurrentUserId();
                var dtos = await _jobApplicationService.GetApplicationsByRecruiterAsync(recruiterId);

                return Ok(ApiResponse<List<JobApplicationSummaryDto>>.SuccessResponse(dtos.ToList(), "Assigned applications retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting assigned applications for recruiter: {RecruiterId}", GetCurrentUserId());
                return StatusCode(500, ApiResponse<List<JobApplicationSummaryDto>>.FailureResponse(new List<string> { "An error occurred while retrieving assigned applications" }, "Couldn't Retrieve Applications"));
            }
        }

        /// <summary>
        /// Get applications by status
        /// </summary>
        [HttpGet("status/{status}")]
        [Authorize(Roles = "Recruiter, HR, Admin, SuperAdmin")]
        public async Task<ActionResult<PagedResult<JobApplicationSummaryDto>>> GetByStatus(
            ApplicationStatus status,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 25)
        {
            try
            {
                var pagedResult = await _jobApplicationService.GetApplicationsByStatusAsync(status, pageNumber, pageSize);

                return Ok(ApiResponse<PagedResult<JobApplicationSummaryDto>>.SuccessResponse(pagedResult, "Applications retrieved successfully"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument for paged applications by status: {Status}", status);
                return BadRequest(ApiResponse<PagedResult<JobApplicationSummaryDto>>.FailureResponse(
                    new List<string> { "An error occurred while processing your request" },
                    "Invalid Request"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paged applications by status: {Status}", status);
                return StatusCode(500, ApiResponse<PagedResult<JobApplicationSummaryDto>>.FailureResponse(
                    new List<string> { "An error occurred while retrieving applications" },
                    "Couldn't Retrieve Applications"));
            }
        }

        /// <summary>
        /// Create a new job application (Candidates and Staff)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Candidate, Recruiter, HR, Admin, SuperAdmin")]
        public async Task<ActionResult<JobApplicationDto>> CreateApplication([FromBody] JobApplicationCreateDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<JobApplicationDto>.FailureResponse(errors, "Validation Failed"));
                }

                // Check if candidate can apply to this job
                var canApply = await _jobApplicationService.CanApplyToJobAsync(dto.JobPositionId, dto.CandidateProfileId);
                if (!canApply)
                {
                    return BadRequest(ApiResponse<JobApplicationDto>.FailureResponse(
                        new List<string> { "Cannot apply to this job position. You may have already applied or the position is closed." },
                        "Invalid Application"));
                }

                // Check if user owns the candidate profile
                if (!await CanAccessCandidateApplications(dto.CandidateProfileId))
                {
                    return Forbid();
                }

                var resultDto = await _jobApplicationService.CreateApplicationAsync(dto);

                return CreatedAtAction(nameof(GetById), new { id = resultDto.Id },
                    ApiResponse<JobApplicationDto>.SuccessResponse(resultDto, "Application created successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating job application");
                return StatusCode(500, ApiResponse<JobApplicationDto>.FailureResponse(
                    new List<string> { "An error occurred while creating the job application" },
                    "Couldn't Create Application"));
            }
        }

        /// <summary>
        /// Update job application (Candidates can update their own, Staff can update assigned/all)
        /// </summary>
        [HttpPatch("{id:guid}")]
        [Authorize(Roles = "Candidate, Recruiter, HR, Admin, SuperAdmin")]
        public async Task<ActionResult<JobApplicationDto>> UpdateApplication(Guid id, [FromBody] JobApplicationUpdateDto dto)
        {
            try
            {
                // Validate model state
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<JobApplicationDto>.FailureResponse(errors, "Validation Failed"));
                }

                // Check if application exists and get entity for authorization
                var entity = await _jobApplicationService.GetApplicationEntityByIdAsync(id);
                if (entity == null)
                {
                    return NotFound(ApiResponse<JobApplicationDto>.FailureResponse(
                        new List<string> { $"Job application with ID {id} not found" },
                        "Not Found"));
                }

                if (!await CanAccessApplication(entity))
                {
                    return Forbid();
                }

                var resultDto = await _jobApplicationService.UpdateApplicationAsync(id, dto);

                return Ok(ApiResponse<JobApplicationDto>.SuccessResponse(resultDto, "Application updated successfully"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument when updating job application with ID: {Id}", id);
                return NotFound(ApiResponse<JobApplicationDto>.FailureResponse(
                    new List<string> { "An error occurred while processing your request" },
                    "Not Found"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating job application with ID: {Id}", id);
                return StatusCode(500, ApiResponse<JobApplicationDto>.FailureResponse(
                    new List<string> { "An error occurred while updating the job application" },
                    "Couldn't Update Application"));
            }
        }

        /// <summary>
        /// Delete job application (Candidates can delete their own, Staff can delete assigned/all)
        /// </summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Candidate, Recruiter, HR, Admin, SuperAdmin")]
        public async Task<ActionResult> DeleteApplication(Guid id)
        {
            try
            {
                // Get entity for authorization
                var entity = await _jobApplicationService.GetApplicationEntityByIdAsync(id);
                if (entity == null)
                {
                    return NotFound(ApiResponse.FailureResponse(new List<string> { $"Job application with ID {id} not found" }, "Not Found"));
                }

                if (!await CanAccessApplication(entity))
                {
                    return Forbid();
                }

                var deleted = await _jobApplicationService.DeleteApplicationAsync(id);

                if (!deleted)
                {
                    return NotFound(ApiResponse.FailureResponse(new List<string> { $"Job application with ID {id} not found" }, "Not Found"));
                }

                return Ok(ApiResponse.SuccessResponse("Application deleted successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting job application with ID: {Id}", id);
                return StatusCode(500, ApiResponse.FailureResponse(new List<string> { "An error occurred while deleting the job application" }, "Couldn't Delete Application"));
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

            if (currentUserRoles.Contains("SuperAdmin") || currentUserRoles.Contains("Admin") || currentUserRoles.Contains("HR"))
                return true;

            if (currentUserRoles.Contains("Recruiter") && application.AssignedRecruiterId == currentUserId)
                return true;

            if (application.CandidateProfile != null && application.CandidateProfile.UserId == currentUserId)
                return true;

            return false;
        }

        private async Task<bool> CanAccessCandidateApplications(Guid candidateProfileId)
        {
            var currentUserId = GetCurrentUserId();
            var currentUserRoles = await GetCurrentUserRolesAsync();

            if (currentUserRoles.Contains("SuperAdmin") || currentUserRoles.Contains("Admin") || currentUserRoles.Contains("HR"))
                return true;

            try
            {
                var candidateProfile = await _candidateProfileService.GetByIdAsync(candidateProfileId);
                if (candidateProfile != null && candidateProfile.UserId == currentUserId)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking candidate profile ownership for profile ID: {ProfileId}", candidateProfileId);
            }

            return false;
        }

        #endregion
    }
}