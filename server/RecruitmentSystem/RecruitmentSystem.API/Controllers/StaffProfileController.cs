using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Services.Interfaces;
using RecruitmentSystem.Shared.DTOs;
using RecruitmentSystem.Shared.DTOs.Responses;

namespace RecruitmentSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin, Admin, HR, Recruiter")]
    public class StaffProfileController : ControllerBase
    {
        private readonly IStaffProfileService _staffProfileService;
        private readonly ILogger<StaffProfileController> _logger;
        private readonly UserManager<User> _userManager;

        public StaffProfileController(
            IStaffProfileService staffProfileService,
            ILogger<StaffProfileController> logger,
            UserManager<User> userManager)
        {
            _staffProfileService = staffProfileService;
            _logger = logger;
            _userManager = userManager;
        }

        /// <summary>
        /// Get staff profile by Id
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<StaffProfileResponseDto>> GetById(Guid id)
        {
            try
            {
                var profile = await _staffProfileService.GetByIdAsync(id);
                if (profile == null)
                {
                    return NotFound(ApiResponse<CandidateRegisterDto>.FailureResponse(new List<string> { $"Staff profile with ID {id} not found!" }, "Not Found"));
                }

                // Check ownership/permission
                if (!CanAccessProfile(profile.UserId))
                {
                    return Forbid();
                }

                return Ok(ApiResponse<StaffProfileResponseDto>.SuccessResponse(profile, "Profile retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retriving staff profile with ID {Id}", id);
                return StatusCode(500, ApiResponse<CandidateRegisterDto>.FailureResponse(new List<string> { "An error occurred while retrieving the staff profile." }, "Internal Server Error"));
            }
        }

        /// <summary>
        /// Get staff profile by UserId
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<StaffProfileResponseDto>> GetByUserId(Guid userId)
        {
            try
            {
                var profile = await _staffProfileService.GetByUserIdAsync(userId);
                if (profile == null)
                {
                    return NotFound(ApiResponse<CandidateRegisterDto>.FailureResponse(new List<string> { $"Staff profile for User ID {userId} not found!" }, "Not Found"));
                }

                // Check ownership/permission
                if (!CanAccessProfile(profile.UserId))
                {
                    return Forbid();
                }

                return Ok(ApiResponse<StaffProfileResponseDto>.SuccessResponse(profile, "Profile retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving staff profile for User ID {UserId}", userId);
                return StatusCode(500, ApiResponse<CandidateRegisterDto>.FailureResponse(new List<string> { "An error occurred while retrieving the staff profile." }, "Internal Server Error"));
            }
        }

        /// <summary>
        /// Get Current user's staff profile
        /// </summary>
        [HttpGet("my-profile")]
        public async Task<ActionResult<StaffProfileResponseDto>> GetMyProfile()
        {
            try
            {
                var userId = GetCurrentUserId();
                var profile = await _staffProfileService.GetByUserIdAsync(userId);
                if (profile == null)
                {
                    return NotFound(ApiResponse<CandidateRegisterDto>.FailureResponse(new List<string> { "Staff profile not found for the current user!" }, "Not Found"));
                }

                return Ok(ApiResponse<StaffProfileResponseDto>.SuccessResponse(profile, "Profile retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving current user's staff profile");
                return StatusCode(500, ApiResponse<CandidateRegisterDto>.FailureResponse(new List<string> { "An error occurred while retrieving the staff profile." }, "Internal Server Error"));
            }
        }

        /// <summary>
        /// Create a new staff profile
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<StaffProfileResponseDto>> CreateProfile([FromBody] CreateStaffProfileDto dto)
        {
            Guid userId = Guid.Empty;
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<StaffProfileResponseDto>.FailureResponse(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList(), "Invalid Data"));
                }

                userId = GetCurrentUserId();
                var profile = await _staffProfileService.CreateProfileAsync(dto, userId);

                return CreatedAtAction(nameof(GetById), new { id = profile.Id }, ApiResponse<StaffProfileResponseDto>.SuccessResponse(profile, "Profile created successfully"));

            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Business rule violation while creating staff profile for user {UserId}", userId);
                return Conflict(ApiResponse<StaffProfileResponseDto>.FailureResponse(new List<string> { ex.Message }, "Conflict"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating staff profile for user {UserId}", userId);
                return StatusCode(500, ApiResponse<StaffProfileResponseDto>.FailureResponse(new List<string> { "An error occurred while creating the staff profile." }, "Internal Server Error"));
            }
        }

        /// <summary>
        /// Update an existing staff profile
        /// </summary>
        [HttpPatch("{id}")]
        public async Task<ActionResult<StaffProfileResponseDto>> UpdateProfile(Guid id, [FromBody] UpdateStaffProfileDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<StaffProfileResponseDto>.FailureResponse(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList(), "Invalid Data"));
                }

                var profile = await _staffProfileService.UpdateProfileAsync(id, dto);
                if (profile == null)
                {
                    return NotFound(ApiResponse<StaffProfileResponseDto>.FailureResponse(new List<string> { $"Staff profile with ID {id} not found" }, "Not Found"));
                }

                // Check ownership/permission
                if (!CanAccessProfile(profile.UserId))
                {
                    return Forbid();
                }

                return Ok(ApiResponse<StaffProfileResponseDto>.SuccessResponse(profile, "Profile updated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating staff profile with ID {Id}", id);
                return StatusCode(500, ApiResponse<StartupBase>.FailureResponse(new List<string> { "An error occurred while updating the staff profile" }, "Couldn't Update Profile"));
            }
        }

        /// <summary>
        /// Delete a staff profile
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteProfile(Guid id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUser = await _userManager.FindByIdAsync(currentUserId.ToString());
                if (currentUser == null)
                {
                    return Unauthorized(ApiResponse.FailureResponse(new List<string> { "Current user not found." }));
                }

                var profile = await _staffProfileService.GetByIdAsync(id);
                if (profile == null)
                {
                    return NotFound(ApiResponse.FailureResponse(new List<string> { $"Staff profile with ID {id} not found." }));
                }

                var targetUser = await _userManager.FindByIdAsync(profile.UserId.ToString());
                if (targetUser == null)
                {
                    return NotFound(ApiResponse.FailureResponse(new List<string> { "Associated user not found." }));
                }

                var targetRoles = await _userManager.GetRolesAsync(targetUser);

                // SuperAdmin profiles can only be deleted by themselves
                if (targetRoles.Contains("SuperAdmin") && currentUserId != profile.UserId)
                {
                    return Forbid("Cannot delete SuperAdmin profiles.");
                }

                // Allow deletion if deleting self or has admin privileges
                var currentRoles = await _userManager.GetRolesAsync(currentUser);
                bool isAuthorized = currentUserId == profile.UserId || currentRoles.Contains("SuperAdmin") || currentRoles.Contains("Admin") || currentRoles.Contains("HR");

                if (!isAuthorized)
                {
                    return Forbid("You do not have permission to delete this profile.");
                }

                var deleted = await _staffProfileService.DeleteProfileAsync(id);
                if (!deleted)
                {
                    return NotFound(ApiResponse.FailureResponse(new List<string> { $"Staff profile with ID {id} not found or could not be deleted." }));
                }

                return Ok(ApiResponse.SuccessResponse("Profile deleted successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting staff profile with ID {Id}", id);
                return StatusCode(500, ApiResponse.FailureResponse(new List<string> { "An error occurred while deleting the staff profile." }));
            }
        }

        #region Private Methods

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("User Id not found in token");
            }
            return userId;
        }

        private bool CanAccessProfile(Guid profileUserId)
        {
            var currentUserId = GetCurrentUserId();
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            // SuperAdmins and Admins can access all profiles
            if (currentUserRole == "SuperAdmin" || currentUserRole == "Admin" || currentUserRole == "HR")
                return true;

            // Recruiters can only access their own profile
            return profileUserId == currentUserId;
        }

        #endregion
    }
}