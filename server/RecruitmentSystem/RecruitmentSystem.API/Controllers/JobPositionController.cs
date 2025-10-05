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
        public async Task<ActionResult<JobPositionResponseDto>> CreateJob([FromBody] CreateJobPositionDto dto)
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
        public async Task<ActionResult<JobPositionResponseDto>> GetJobById(Guid id)
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

        /// <summary>
        /// Get all Job Positions with optional filters
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<JobPositionResponseDto>>> GetAllJobs([FromQuery] JobPositionQueryDto query)
        {
            try
            {
                var jobs = await _service.GetJobsWithFiltersAsync(query.Status, query.Department, query.Location, query.ExperienceLevel, query.SkillIds, query.CreatedFromDate, query.CreatedToDate, query.DeadlineFromDate, query.DeadlineToDate);

                return Ok(ApiResponse<List<JobPositionResponseDto>>.SuccessResponse(jobs, jobs.Count == 0 ? "No jobs found matching the criteria" : "Jobs retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retriving Jobs");
                return StatusCode(500, ApiResponse<JobPositionResponseDto>.FailureResponse(new List<string> { "An Error occurred while retriving the jobs." }, "Internal Server Error"));
            }
        }

        /// <summary>
        /// Get all Active Job Positions
        /// </summary>
        [HttpGet("active")]
        public async Task<ActionResult<List<JobPositionResponseDto>>> GetActiveJobs()
        {
            try
            {
                var jobs = await _service.GetActiveJobsAsync();

                return Ok(ApiResponse<List<JobPositionResponseDto>>.SuccessResponse(jobs, jobs.Count == 0 ? "No active jobs found" : "Active jobs retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active Jobs");
                return StatusCode(500, ApiResponse<List<JobPositionResponseDto>>.FailureResponse(new List<string> { "An Error occurred while retrieving the active jobs." }, "Internal Server Error"));
            }
        }

        /// <summary>
        /// Search Job Positions by term with optional filters
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<List<JobPositionResponseDto>>> SearchJobs([FromQuery] string searchTerm, string? department = null, string? status = null)
        {
            try
            {
                var jobs = await _service.SearchJobsAsync(searchTerm, department, status);

                return Ok(ApiResponse<List<JobPositionResponseDto>>.SuccessResponse(jobs, jobs.Count == 0 ? "No jobs found matching the search criteria" : "Jobs retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching Jobs");
                return StatusCode(500, ApiResponse<List<JobPositionResponseDto>>.FailureResponse(new List<string> { "An Error occurred while searching the jobs." }, "Internal Server Error"));
            }
        }

        /// <summary>
        /// Get Job Positions by Department
        /// </summary>
        [HttpGet("by-department/{department}")]
        public async Task<ActionResult<List<JobPositionResponseDto>>> GetJobsByDepartment(string department)
        {
            try
            {
                var jobs = await _service.GetJobsByDepartmentAsync(department);

                return Ok(ApiResponse<List<JobPositionResponseDto>>.SuccessResponse(jobs, jobs.Count == 0 ? $"No jobs found for department {department}" : "Jobs retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Jobs for Department {department}", department);
                return StatusCode(500, ApiResponse<List<JobPositionResponseDto>>.FailureResponse(new List<string> { "An Error occurred while retrieving the jobs for the department." }, "Internal Server Error"));
            }

        }

        /// <summary>
        /// Get Job Positions by Status 
        /// </summary>
        [HttpGet("by-status/{status}")]
        public async Task<ActionResult<List<JobPositionResponseDto>>> GetJobsByStatus(string status)
        {
            try
            {
                var jobs = await _service.GetJobsByStatusAsync(status);

                return Ok(ApiResponse<List<JobPositionResponseDto>>.SuccessResponse(jobs, jobs.Count == 0 ? $"No jobs found with status {status}" : "Jobs retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Jobs for Status {status}", status);
                return StatusCode(500, ApiResponse<List<JobPositionResponseDto>>.FailureResponse(new List<string> { "An Error occurred while retrieving the jobs for provided status." }, "Internal Server Error"));
            }
        }

        /// <summary>
        /// Update an existing Job Position (partial update)
        /// </summary>
        [HttpPatch("{id:guid}")]
        [Authorize(Roles = "SuperAdmin,Admin,HR")]
        public async Task<ActionResult<JobPositionResponseDto>> UpdateJob(Guid id, [FromBody] UpdateJobPositionDto dto)
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
        public async Task<IActionResult> DeleteJob(Guid id)
        {
            try
            {
                var existingJob = await _service.GetJobByIdAsync(id);
                if (existingJob == null)
                {
                    return NotFound(ApiResponse.FailureResponse(new List<string> { $"Job with ID {id} not found" }, "Not Found"));
                }

                await _service.DeleteJobAsync(id);

                return NoContent();
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
        public async Task<IActionResult> CloseJob(Guid id)
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