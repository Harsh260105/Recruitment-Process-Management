using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecruitmentSystem.Services.Interfaces;
using RecruitmentSystem.Shared.DTOs;
using RecruitmentSystem.Shared.DTOs.Responses;
using System.Security.Claims;

namespace RecruitmentSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    public class JobPositionController : ControllerBase
    {
        private readonly IJobPositionService _service;
        private readonly ILogger<JobPositionController> _logger;

        public JobPositionController(
            IJobPositionService service,
            ILogger<JobPositionController> logger
            )
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// Create a new Job Positionn
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "SuperAdmin,Admin,HR")]
        public async Task<ActionResult<ApiResponse<JobPositionResponseDto>>> CreateJob([FromBody] CreateJobPositionDto dto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var creatorId))
                {
                    return BadRequest(ApiResponse<JobPositionResponseDto>.FailureResponse(new List<string> { "Invalid or missing user Id" }, "Authentication Error"));
                }

                var createdJob = await _service.CreateJobAsync(dto, creatorId);

                return CreatedAtAction(nameof(GetJobById), new { id = createdJob.Id }, ApiResponse<JobPositionResponseDto>.SuccessResponse(createdJob, "Job created successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating new Job.");
                return StatusCode(500, ApiResponse<JobPositionResponseDto>.FailureResponse(new List<string> { "An Error occurred while creating new Job." }, "Internal Server Error"));
            }
        }

        /// <summary>
        /// Get Job Position by ID
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ApiResponse<JobPositionResponseDto>>> GetJobById(Guid id)
        {
            try
            {
                var job = await _service.GetJobByIdAsync(id);
                if (job == null)
                {
                    return NotFound(ApiResponse<JobPositionResponseDto>.FailureResponse(new List<string> { $"Job for Job Id {id} not found!" }, "Not Found"));
                }

                return Ok(ApiResponse<JobPositionResponseDto>.SuccessResponse(job, "Job Retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retriving job for ID {id}", id);
                return StatusCode(500, ApiResponse<JobPositionResponseDto>.FailureResponse(new List<string> { "An error occurred while retrieving the job." }, "Internal Server Error"));
            }
        }

        [HttpGet("summaries/public")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<PagedResult<JobPositionPublicSummaryDto>>>> GetPublicJobSummaries(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 25,
            [FromQuery] JobPositionQueryDto? query = null)
        {
            try
            {
                var pagedJobs = await _service.GetJobSummariesWithFiltersAsync<JobPositionPublicSummaryDto>(
                    pageNumber, 
                    pageSize,
                    "Active", query?.Department, query?.Location, query?.ExperienceLevel,
                    query?.SkillIds, query?.CreatedFromDate, query?.CreatedToDate,
                    query?.DeadlineFromDate, query?.DeadlineToDate);

                return Ok(ApiResponse<PagedResult<JobPositionPublicSummaryDto>>.SuccessResponse(
                    pagedJobs,
                    pagedJobs.Items.Count == 0 ? "No jobs found matching the criteria" : "Jobs retrieved successfully"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid pagination parameters for public summaries");
                return BadRequest(ApiResponse<PagedResult<JobPositionPublicSummaryDto>>.FailureResponse(
                    new List<string> { ex.Message }, "Invalid Request"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving public job summaries");
                return StatusCode(500, ApiResponse<PagedResult<JobPositionPublicSummaryDto>>.FailureResponse(
                    new List<string> { "An error occurred while retrieving the jobs." },
                    "Internal Server Error"));
            }
        }

        [HttpGet("summaries/staff")]
        [Authorize(Roles = "SuperAdmin,Admin,HR,Recruiter")]
        public async Task<ActionResult<ApiResponse<PagedResult<JobPositionStaffSummaryDto>>>> GetStaffJobSummaries(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 25,
            [FromQuery] JobPositionQueryDto? query = null)
        {
            try
            {
                var pagedJobs = await _service.GetJobSummariesWithFiltersAsync<JobPositionStaffSummaryDto>(
                    pageNumber, pageSize,
                    query?.Status, query?.Department, query?.Location, query?.ExperienceLevel,
                    query?.SkillIds, query?.CreatedFromDate, query?.CreatedToDate,
                    query?.DeadlineFromDate, query?.DeadlineToDate);

                return Ok(ApiResponse<PagedResult<JobPositionStaffSummaryDto>>.SuccessResponse(
                    pagedJobs,
                    pagedJobs.Items.Count == 0 ? "No jobs found matching the criteria" : "Jobs retrieved successfully"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid pagination parameters for staff summaries");
                return BadRequest(ApiResponse<PagedResult<JobPositionStaffSummaryDto>>.FailureResponse(
                    new List<string> { ex.Message }, "Invalid Request"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving staff job summaries");
                return StatusCode(500, ApiResponse<PagedResult<JobPositionStaffSummaryDto>>.FailureResponse(
                    new List<string> { "An error occurred while retrieving the jobs." },
                    "Internal Server Error"));
            }
        }

        [HttpGet("summaries/public/active")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<PagedResult<JobPositionPublicSummaryDto>>>> GetPublicActiveJobs(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var pagedJobs = await _service.GetActiveJobSummariesAsync<JobPositionPublicSummaryDto>(pageNumber, pageSize);

                return Ok(ApiResponse<PagedResult<JobPositionPublicSummaryDto>>.SuccessResponse(
                    pagedJobs,
                    pagedJobs.Items.Count == 0 ? "No active jobs found" : "Active jobs retrieved successfully"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid pagination parameters for public active summaries");
                return BadRequest(ApiResponse<PagedResult<JobPositionPublicSummaryDto>>.FailureResponse(
                    new List<string> { ex.Message }, "Invalid Request"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving public active job summaries");
                return StatusCode(500, ApiResponse<PagedResult<JobPositionPublicSummaryDto>>.FailureResponse(
                    new List<string> { "An error occurred while retrieving the active jobs." },
                    "Internal Server Error"));
            }
        }

        [HttpGet("summaries/staff/active")]
        [Authorize(Roles = "SuperAdmin,Admin,HR,Recruiter")]
        public async Task<ActionResult<ApiResponse<PagedResult<JobPositionStaffSummaryDto>>>> GetStaffActiveJobs(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var pagedJobs = await _service.GetActiveJobSummariesAsync<JobPositionStaffSummaryDto>(pageNumber, pageSize);

                return Ok(ApiResponse<PagedResult<JobPositionStaffSummaryDto>>.SuccessResponse(
                    pagedJobs,
                    pagedJobs.Items.Count == 0 ? "No active jobs found" : "Active jobs retrieved successfully"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid pagination parameters for staff active summaries");
                return BadRequest(ApiResponse<PagedResult<JobPositionStaffSummaryDto>>.FailureResponse(
                    new List<string> { ex.Message }, "Invalid Request"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving staff active job summaries");
                return StatusCode(500, ApiResponse<PagedResult<JobPositionStaffSummaryDto>>.FailureResponse(
                    new List<string> { "An error occurred while retrieving the active jobs." },
                    "Internal Server Error"));
            }
        }

        [HttpGet("summaries/public/search")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<PagedResult<JobPositionPublicSummaryDto>>>> SearchPublicJobSummaries(
            [FromQuery] string searchTerm,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 15,
            [FromQuery] string? department = null,
            [FromQuery] string? status = null)
        {
            try
            {
                var pagedJobs = await _service.SearchJobSummariesAsync<JobPositionPublicSummaryDto>(
                    searchTerm, pageNumber, pageSize, department, status);

                return Ok(ApiResponse<PagedResult<JobPositionPublicSummaryDto>>.SuccessResponse(
                    pagedJobs,
                    pagedJobs.Items.Count == 0 ? "No jobs found matching the search criteria" : "Jobs retrieved successfully"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid search parameters for public job summaries");
                return BadRequest(ApiResponse<PagedResult<JobPositionPublicSummaryDto>>.FailureResponse(
                    new List<string> { ex.Message }, "Invalid Request"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching public job summaries");
                return StatusCode(500, ApiResponse<PagedResult<JobPositionPublicSummaryDto>>.FailureResponse(
                    new List<string> { "An error occurred while searching the jobs." },
                    "Internal Server Error"));
            }
        }

        [HttpGet("summaries/staff/search")]
        [Authorize(Roles = "SuperAdmin,Admin,HR,Recruiter")]
        public async Task<ActionResult<ApiResponse<PagedResult<JobPositionStaffSummaryDto>>>> SearchStaffJobSummaries(
            [FromQuery] string searchTerm,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 15,
            [FromQuery] string? department = null,
            [FromQuery] string? status = null)
        {
            try
            {
                var pagedJobs = await _service.SearchJobSummariesAsync<JobPositionStaffSummaryDto>(
                    searchTerm, pageNumber, pageSize, department, status);

                return Ok(ApiResponse<PagedResult<JobPositionStaffSummaryDto>>.SuccessResponse(
                    pagedJobs,
                    pagedJobs.Items.Count == 0 ? "No jobs found matching the search criteria" : "Jobs retrieved successfully"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid search parameters for staff job summaries");
                return BadRequest(ApiResponse<PagedResult<JobPositionStaffSummaryDto>>.FailureResponse(
                    new List<string> { ex.Message }, "Invalid Request"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching staff job summaries");
                return StatusCode(500, ApiResponse<PagedResult<JobPositionStaffSummaryDto>>.FailureResponse(
                    new List<string> { "An error occurred while searching the jobs." },
                    "Internal Server Error"));
            }
        }

        // [HttpGet("summaries/public/by-department/{department}")]
        // [Authorize]
        // public async Task<ActionResult<ApiResponse<PagedResult<JobPositionPublicSummaryDto>>>> GetPublicJobsByDepartmentSummary(
        //     string department,
        //     [FromQuery] int pageNumber = 1,
        //     [FromQuery] int pageSize = 15)
        // {
        //     try
        //     {
        //         var pagedJobs = await _service.GetJobSummariesByDepartmentAsync<JobPositionPublicSummaryDto>(department, pageNumber, pageSize);

        //         return Ok(ApiResponse<PagedResult<JobPositionPublicSummaryDto>>.SuccessResponse(
        //             pagedJobs,
        //             pagedJobs.Items.Count == 0 ? $"No jobs found for department {department}" : "Jobs retrieved successfully"));
        //     }
        //     catch (ArgumentException ex)
        //     {
        //         _logger.LogWarning(ex, "Invalid department parameters for public job summaries");
        //         return BadRequest(ApiResponse<PagedResult<JobPositionPublicSummaryDto>>.FailureResponse(
        //             new List<string> { ex.Message }, "Invalid Request"));
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "Error retrieving public job summaries for department {department}", department);
        //         return StatusCode(500, ApiResponse<PagedResult<JobPositionPublicSummaryDto>>.FailureResponse(
        //             new List<string> { "An error occurred while retrieving the jobs for the department." },
        //             "Internal Server Error"));
        //     }
        // }

        // [HttpGet("summaries/staff/by-department/{department}")]
        // [Authorize(Roles = "SuperAdmin,Admin,HR,Recruiter")]
        // public async Task<ActionResult<ApiResponse<PagedResult<JobPositionStaffSummaryDto>>>> GetStaffJobsByDepartmentSummary(
        //     string department,
        //     [FromQuery] int pageNumber = 1,
        //     [FromQuery] int pageSize = 15)
        // {
        //     try
        //     {
        //         var pagedJobs = await _service.GetJobSummariesByDepartmentAsync<JobPositionStaffSummaryDto>(department, pageNumber, pageSize);

        //         return Ok(ApiResponse<PagedResult<JobPositionStaffSummaryDto>>.SuccessResponse(
        //             pagedJobs,
        //             pagedJobs.Items.Count == 0 ? $"No jobs found for department {department}" : "Jobs retrieved successfully"));
        //     }
        //     catch (ArgumentException ex)
        //     {
        //         _logger.LogWarning(ex, "Invalid department parameters for staff job summaries");
        //         return BadRequest(ApiResponse<PagedResult<JobPositionStaffSummaryDto>>.FailureResponse(
        //             new List<string> { ex.Message }, "Invalid Request"));
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "Error retrieving staff job summaries for department {department}", department);
        //         return StatusCode(500, ApiResponse<PagedResult<JobPositionStaffSummaryDto>>.FailureResponse(
        //             new List<string> { "An error occurred while retrieving the jobs for the department." },
        //             "Internal Server Error"));
        //     }
        // }

        // [HttpGet("summaries/public/by-status/{status}")]
        // [Authorize]
        // public async Task<ActionResult<ApiResponse<PagedResult<JobPositionPublicSummaryDto>>>> GetPublicJobsByStatusSummary(
        //     string status,
        //     [FromQuery] int pageNumber = 1,
        //     [FromQuery] int pageSize = 15)
        // {
        //     try
        //     {
        //         var pagedJobs = await _service.GetJobSummariesByStatusAsync<JobPositionPublicSummaryDto>(status, pageNumber, pageSize);

        //         return Ok(ApiResponse<PagedResult<JobPositionPublicSummaryDto>>.SuccessResponse(
        //             pagedJobs,
        //             pagedJobs.Items.Count == 0 ? $"No jobs found with status {status}" : "Jobs retrieved successfully"));
        //     }
        //     catch (ArgumentException ex)
        //     {
        //         _logger.LogWarning(ex, "Invalid status parameters for public job summaries");
        //         return BadRequest(ApiResponse<PagedResult<JobPositionPublicSummaryDto>>.FailureResponse(
        //             new List<string> { ex.Message }, "Invalid Request"));
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "Error retrieving public job summaries for status {status}", status);
        //         return StatusCode(500, ApiResponse<PagedResult<JobPositionPublicSummaryDto>>.FailureResponse(
        //             new List<string> { "An error occurred while retrieving the jobs for the status." },
        //             "Internal Server Error"));
        //     }
        // }

        // [HttpGet("summaries/staff/by-status/{status}")]
        // [Authorize(Roles = "SuperAdmin,Admin,HR,Recruiter")]
        // public async Task<ActionResult<ApiResponse<PagedResult<JobPositionStaffSummaryDto>>>> GetStaffJobsByStatusSummary(
        //     string status,
        //     [FromQuery] int pageNumber = 1,
        //     [FromQuery] int pageSize = 15)
        // {
        //     try
        //     {
        //         var pagedJobs = await _service.GetJobSummariesByStatusAsync<JobPositionStaffSummaryDto>(status, pageNumber, pageSize);

        //         return Ok(ApiResponse<PagedResult<JobPositionStaffSummaryDto>>.SuccessResponse(
        //             pagedJobs,
        //             pagedJobs.Items.Count == 0 ? $"No jobs found with status {status}" : "Jobs retrieved successfully"));
        //     }
        //     catch (ArgumentException ex)
        //     {
        //         _logger.LogWarning(ex, "Invalid status parameters for staff job summaries");
        //         return BadRequest(ApiResponse<PagedResult<JobPositionStaffSummaryDto>>.FailureResponse(
        //             new List<string> { ex.Message }, "Invalid Request"));
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "Error retrieving staff job summaries for status {status}", status);
        //         return StatusCode(500, ApiResponse<PagedResult<JobPositionStaffSummaryDto>>.FailureResponse(
        //             new List<string> { "An error occurred while retrieving the jobs for the status." },
        //             "Internal Server Error"));
        //     }
        // }

        /// <summary>
        /// Update an existing Job Position (partial update)
        /// </summary>
        [HttpPatch("{id:guid}")]
        [Authorize(Roles = "SuperAdmin,Admin,HR")]
        public async Task<ActionResult<ApiResponse<JobPositionResponseDto>>> UpdateJob(Guid id, [FromBody] UpdateJobPositionDto dto)
        {
            try
            {
                var existingJob = await _service.GetJobByIdAsync(id);
                if (existingJob == null)
                {
                    return NotFound(ApiResponse<JobPositionResponseDto>.FailureResponse(new List<string> { $"Job with ID {id} not found" }, "Not Found"));
                }

                var updatedJob = await _service.UpdateJobAsync(id, dto);

                return Ok(ApiResponse<JobPositionResponseDto>.SuccessResponse(updatedJob!, "Job updated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating the job with ID {id}", id);
                return StatusCode(500, ApiResponse<JobPositionResponseDto>.FailureResponse(new List<string> { "An Error occurred while updating the job" }, "Internal Server Error"));
            }
        }

        /// <summary>
        /// Delete a Job Position   
        /// </summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "SuperAdmin,Admin,HR")]
        public async Task<ActionResult<ApiResponse>> DeleteJob(Guid id)
        {
            try
            {
                var existingJob = await _service.GetJobByIdAsync(id);
                if (existingJob == null)
                {
                    return NotFound(ApiResponse.FailureResponse(new List<string> { $"Job with ID {id} not found" }, "Not Found"));
                }

                await _service.DeleteJobAsync(id);

                return Ok(ApiResponse.SuccessResponse("Job deleted successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting job with ID {id}", id);
                return StatusCode(500, ApiResponse.FailureResponse(new List<string> { "An error occurred while deleting the job" }, "Internal Server Error"));
            }
        }

        /// <summary>
        /// Close a Job Position
        /// </summary>
        [HttpPut("{id:guid}/close")]
        [Authorize(Roles = "SuperAdmin,Admin,HR")]
        public async Task<ActionResult<ApiResponse>> CloseJob(Guid id)
        {
            try
            {
                var existingJob = await _service.GetJobByIdAsync(id);
                if (existingJob == null)
                {
                    return NotFound(ApiResponse.FailureResponse(new List<string> { $"Job with ID {id} not found" }, "Not Found"));
                }

                await _service.CloseJobAsync(id);

                return Ok(ApiResponse.SuccessResponse("Job closed successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing job with ID {id}", id);
                return StatusCode(500, ApiResponse.FailureResponse(new List<string> { "An error occurred while closing the job" }, "Internal Server Error"));
            }
        }

        /// <summary>
        /// Check if a Job Position exists by ID
        /// </summary>
        [HttpHead("{id:guid}/exists")]
        public async Task<IActionResult> JobExists(Guid id)
        {
            try
            {
                var exists = await _service.ExistsAsync(id);
                if (exists)
                {
                    return Ok();
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking existence of job with ID {id}", id);
                return StatusCode(500);
            }
        }
    }
}