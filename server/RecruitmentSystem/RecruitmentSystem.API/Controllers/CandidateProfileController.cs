using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecruitmentSystem.Services.Interfaces;
using RecruitmentSystem.Shared.DTOs;
using RecruitmentSystem.Shared.DTOs.CandidateProfile;
using RecruitmentSystem.Shared.DTOs.Responses;
using System.Security.Claims;

namespace RecruitmentSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CandidateProfileController : ControllerBase
    {
        private readonly ICandidateProfileService _candidateProfileService;
        private readonly ILogger<CandidateProfileController> _logger;

        // Cache user ID
        private Guid? _currentUserId;

        public CandidateProfileController(
            ICandidateProfileService candidateProfileService,
            ILogger<CandidateProfileController> logger)
        {
            _candidateProfileService = candidateProfileService;
            _logger = logger;
        }

        #region Candidate Profile Management

        /// <summary>
        /// Get candidate profile by ID
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ApiResponse<CandidateProfileResponseDto>>> GetById(Guid id)
        {
            try
            {
                var profile = await _candidateProfileService.GetByIdAsync(id);
                if (profile == null)
                {
                    return NotFound(ApiResponse<CandidateProfileResponseDto>.FailureResponse(new List<string> { $"Candidate profile with ID {id} not found" }, "Not Found"));
                }

                if (!ValidateUserAccess(profile.UserId))
                {
                    return StatusCode(403, ApiResponse<CandidateProfileResponseDto>.FailureResponse(
                        new List<string> { "You don't have permission to access this profile" },
                        "Forbidden"));
                }

                return Ok(ApiResponse<CandidateProfileResponseDto>.SuccessResponse(profile, "Profile retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting candidate profile by ID: {Id}", id);
                return StatusCode(500, ApiResponse<CandidateProfileResponseDto>.FailureResponse(new List<string> { "An error occurred while retrieving the candidate profile" }, "Couldn't Retrieve Profile"));
            }
        }

        /// <summary>
        /// Get candidate profile by user ID
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<ApiResponse<CandidateProfileResponseDto>>> GetByUserId(Guid userId)
        {
            try
            {
                // Validate user access
                if (!ValidateUserAccess(userId))
                {
                    return StatusCode(403, ApiResponse<CandidateProfileResponseDto>.FailureResponse(
                        new List<string> { "You don't have permission to access this profile" },
                        "Forbidden"));
                }

                var profile = await _candidateProfileService.GetByUserIdAsync(userId);
                if (profile == null)
                {
                    return NotFound(ApiResponse<CandidateProfileResponseDto>.FailureResponse(new List<string> { $"No candidate profile found for user ID {userId}" }, "Couldn't retrieve Profile"));
                }

                return Ok(ApiResponse<CandidateProfileResponseDto>.SuccessResponse(profile, "Profile retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting candidate profile by user ID: {UserId}", userId);
                return StatusCode(500, ApiResponse<CandidateProfileResponseDto>.FailureResponse(new List<string> { "An error occurred while retrieving the candidate profile" }, "Couldn't Retrieve Profile"));
            }
        }

        /// <summary>
        /// Get current user's candidate profile
        /// </summary>
        [HttpGet("my-profile")]
        public async Task<ActionResult<ApiResponse<CandidateProfileResponseDto>>> GetMyProfile()
        {
            try
            {
                var profile = await GetCurrentUserProfileAsync();
                return Ok(ApiResponse<CandidateProfileResponseDto>.SuccessResponse(profile, "Profile retrieved successfully"));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(ApiResponse<CandidateProfileResponseDto>.FailureResponse(new List<string> { "No candidate profile found for the current user" }, "Not Found"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user's candidate profile");
                return StatusCode(500, ApiResponse<CandidateProfileResponseDto>.FailureResponse(new List<string> { "An error occurred while retrieving your profile" }, "Couldn't Retrieve Profile"));
            }
        }

        /// <summary>
        /// Create a new candidate profile
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<CandidateProfileResponseDto>>> CreateProfile([FromBody] CandidateProfileDto dto)
        {
            try
            {
                var createdBy = GetCurrentUserId();
                var profile = await _candidateProfileService.CreateProfileAsync(dto, createdBy);

                return CreatedAtAction(nameof(GetById), new { id = profile.Id }, ApiResponse<CandidateProfileResponseDto>.SuccessResponse(profile, "Profile created successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating candidate profile");
                return StatusCode(500, ApiResponse<CandidateProfileResponseDto>.FailureResponse(new List<string> { "An error occurred while creating the candidate profile" }, "Couldn't Create Profile"));
            }
        }

        /// <summary>
        /// Update candidate profile
        /// </summary>
        [HttpPatch("{id:guid}")]
        public async Task<ActionResult<ApiResponse<CandidateProfileResponseDto>>> UpdateProfile(Guid id, [FromBody] UpdateCandidateProfileDto dto)
        {
            try
            {
                // Validate ownership or admin privileges
                var userId = GetCurrentUserId();
                var candidateProfile = await _candidateProfileService.GetByIdAsync(id);
                if (candidateProfile == null || (candidateProfile.UserId != userId && !HasAdminPrivileges()))
                {
                    return StatusCode(403, ApiResponse<CandidateProfileResponseDto>.FailureResponse(
                        new List<string> { "You don't have permission to update this profile" },
                        "Forbidden"));
                }

                var profile = await _candidateProfileService.UpdateProfileAsync(id, dto);

                return Ok(ApiResponse<CandidateProfileResponseDto>.SuccessResponse(profile!, "Profile updated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating candidate profile with ID {Id}", id);
                return StatusCode(500, ApiResponse<CandidateProfileResponseDto>.FailureResponse(new List<string> { "An error occurred while updating the candidate profile" }, "Couldn't Update Profile"));
            }
        }

        /// <summary>
        /// Delete candidate profile
        /// </summary>
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<ApiResponse>> DeleteProfile(Guid id)
        {
            try
            {
                var userId = GetCurrentUserId();

                // Check if user owns the profile or has admin privileges
                var userProfile = await _candidateProfileService.GetByUserIdAsync(userId);
                if (userProfile == null || (userProfile.Id != id && !HasAdminPrivileges()))
                {
                    return StatusCode(403, ApiResponse.FailureResponse(
                        new List<string> { "You don't have permission to delete this profile" },
                        "Forbidden"));
                }

                var deleted = await _candidateProfileService.DeleteProfileAsync(id);

                return Ok(ApiResponse.SuccessResponse("Profile deleted successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting candidate profile with ID: {Id}", id);
                return StatusCode(500, ApiResponse.FailureResponse(new List<string> { "An error occurred while deleting the candidate profile" }, "Couldn't Delete Profile"));
            }
        }

        /// <summary>
        /// Grant or revoke candidate application limit override (Admin/HR only)
        /// </summary>
        [HttpPost("{candidateProfileId:guid}/application-override")]
        [Authorize(Roles = "SuperAdmin, Admin, HR")]
        public async Task<ActionResult<ApiResponse>> SetApplicationOverride(
            Guid candidateProfileId,
            [FromBody] CandidateApplicationOverrideRequestDto dto)
        {
            try
            {
                var approverId = GetCurrentUserId();
                await _candidateProfileService.SetApplicationOverrideAsync(candidateProfileId, dto, approverId);
                return Ok(ApiResponse.SuccessResponse("Application override updated successfully"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid application override request for candidate {CandidateProfileId}", candidateProfileId);
                return BadRequest(ApiResponse.FailureResponse(new List<string> { ex.Message }, "Invalid Override Request"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating application override for candidate {CandidateProfileId}", candidateProfileId);
                return StatusCode(500, ApiResponse.FailureResponse(new List<string> { "An error occurred while updating the override" }, "Couldn't Update Override"));
            }
        }

        #endregion

        #region My Skills Management

        /// <summary>
        /// Get current user's skills
        /// </summary>
        [HttpGet("my-skills")]
        public async Task<ActionResult<ApiResponse<List<CandidateSkillDto>>>> GetMySkills()
        {
            try
            {
                var userProfile = await GetCurrentUserProfileAsync();
                var skills = await _candidateProfileService.GetSkillsAsync(userProfile.Id);
                return Ok(ApiResponse<List<CandidateSkillDto>>.SuccessResponse(skills, "Skills retrieved successfully"));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(ApiResponse<List<CandidateSkillDto>>.FailureResponse(new List<string> { "No candidate profile found for the current user" }, "Not Found"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting skills for user {UserId}", GetCurrentUserId());
                return StatusCode(500, ApiResponse<List<CandidateSkillDto>>.FailureResponse(new List<string> { "An error occurred while retrieving skills" }, "Couldn't Retrieve Skills"));
            }
        }

        /// <summary>
        /// Add skills to current user's profile
        /// </summary>
        [HttpPost("my-skills")]
        public async Task<ActionResult<ApiResponse<List<CandidateSkillDto>>>> AddMySkills([FromBody] List<CreateCandidateSkillDto> skills)
        {
            try
            {
                var userProfile = await GetCurrentUserProfileAsync();
                var addedSkills = await _candidateProfileService.AddSkillsAsync(userProfile.Id, skills);
                return Ok(ApiResponse<List<CandidateSkillDto>>.SuccessResponse(addedSkills, "Skills added successfully"));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(ApiResponse<List<CandidateSkillDto>>.FailureResponse(new List<string> { "No candidate profile found for the current user" }, "Not Found"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding skills for user {UserId}", GetCurrentUserId());
                return StatusCode(500, ApiResponse<List<CandidateSkillDto>>.FailureResponse(new List<string> { "An unexpected error occurred while adding skills" }, "Internal Server Error"));
            }
        }

        /// <summary>
        /// Update a specific skill for current user
        /// </summary>
        [HttpPatch("my-skills/{skillId}")]
        public async Task<ActionResult<ApiResponse<CandidateSkillDto>>> UpdateMySkill(int skillId, [FromBody] UpdateCandidateSkillDto dto)
        {
            try
            {
                var userProfile = await GetCurrentUserProfileAsync();
                var updatedSkill = await _candidateProfileService.UpdateSkillAsync(userProfile.Id, skillId, dto);
                return Ok(ApiResponse<CandidateSkillDto>.SuccessResponse(updatedSkill, "Skill updated successfully"));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(ApiResponse<CandidateSkillDto>.FailureResponse(new List<string> { "Skill not found or no candidate profile found for the current user" }, "Not Found"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating skill for user {UserId}", GetCurrentUserId());
                return StatusCode(500, ApiResponse<CandidateSkillDto>.FailureResponse(new List<string> { "An unexpected error occurred while updating the skill" }, "Internal Server Error"));
            }
        }

        /// <summary>
        /// Remove a skill from current user's profile
        /// </summary>
        [HttpDelete("my-skills/{skillId}")]
        public async Task<ActionResult<ApiResponse>> RemoveMySkill(int skillId)
        {
            try
            {
                var userProfile = await GetCurrentUserProfileAsync();
                var removed = await _candidateProfileService.RemoveSkillAsync(userProfile.Id, skillId);
                if (!removed)
                {
                    return NotFound(ApiResponse.FailureResponse(new List<string> { $"Skill {skillId} not found" }, "Not Found"));
                }

                return Ok(ApiResponse.SuccessResponse("Skill removed successfully"));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(ApiResponse.FailureResponse(new List<string> { "Skill not found or no candidate profile found for the current user" }, "Not Found"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing skill for user {UserId}", GetCurrentUserId());
                return StatusCode(500, ApiResponse.FailureResponse(new List<string> { "An error occurred while removing the skill" }, "Couldn't Remove Skill"));
            }
        }

        #endregion

        #region My Education Management

        /// <summary>
        /// Get current user's education
        /// </summary>
        [HttpGet("my-education")]
        public async Task<ActionResult<ApiResponse<List<CandidateEducationDto>>>> GetMyEducation()
        {
            try
            {
                var userProfile = await GetCurrentUserProfileAsync();
                var education = await _candidateProfileService.GetEducationAsync(userProfile.Id);
                return Ok(ApiResponse<List<CandidateEducationDto>>.SuccessResponse(education, "Education retrieved successfully"));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(ApiResponse<List<CandidateEducationDto>>.FailureResponse(new List<string> { "No candidate profile found for the current user" }, "Not Found"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting education for user {UserId}", GetCurrentUserId());
                return StatusCode(500, ApiResponse<List<CandidateEducationDto>>.FailureResponse(new List<string> { "An error occurred while retrieving education" }, "Couldn't Retrieve Education"));
            }
        }

        /// <summary>
        /// Add education to current user's profile
        /// </summary>
        [HttpPost("my-education")]
        public async Task<ActionResult<ApiResponse<CandidateEducationDto>>> AddMyEducation([FromBody] CreateCandidateEducationDto dto)
        {
            try
            {
                var userProfile = await GetCurrentUserProfileAsync();
                var education = await _candidateProfileService.AddEducationAsync(userProfile.Id, dto);
                return CreatedAtAction(nameof(GetMyProfile), new { }, ApiResponse<CandidateEducationDto>.SuccessResponse(education, "Education added successfully"));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(ApiResponse<CandidateEducationDto>.FailureResponse(new List<string> { "No candidate profile found for the current user" }, "Not Found"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding education for user {UserId}", GetCurrentUserId());
                return StatusCode(500, ApiResponse<CandidateEducationDto>.FailureResponse(new List<string> { "An unexpected error occurred while adding education" }, "Internal Server Error"));
            }
        }

        /// <summary>
        /// Update education for current user
        /// </summary>
        [HttpPatch("my-education/{educationId}")]
        public async Task<ActionResult<ApiResponse<CandidateEducationDto>>> UpdateMyEducation(Guid educationId, [FromBody] UpdateCandidateEducationDto dto)
        {
            try
            {
                var userProfile = await GetCurrentUserProfileAsync();
                var updatedEducation = await _candidateProfileService.UpdateEducationAsync(educationId, dto);
                return Ok(ApiResponse<CandidateEducationDto>.SuccessResponse(updatedEducation, "Education updated successfully"));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(ApiResponse<CandidateEducationDto>.FailureResponse(new List<string> { "Education not found or no candidate profile found for the current user" }, "Not Found"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating education for user {UserId}", GetCurrentUserId());
                return StatusCode(500, ApiResponse<CandidateEducationDto>.FailureResponse(new List<string> { "An unexpected error occurred while updating education" }, "Internal Server Error"));
            }
        }

        /// <summary>
        /// Remove education from current user's profile
        /// </summary>
        [HttpDelete("my-education/{educationId}")]
        public async Task<ActionResult<ApiResponse>> RemoveMyEducation(Guid educationId)
        {
            try
            {
                var userProfile = await GetCurrentUserProfileAsync();
                var removed = await _candidateProfileService.RemoveEducationAsync(educationId);

                if (!removed)
                {
                    return NotFound(ApiResponse.FailureResponse(new List<string> { $"Education not found" }, "Not Found"));
                }

                return Ok(ApiResponse.SuccessResponse("Education removed successfully"));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(ApiResponse.FailureResponse(new List<string> { "Education not found or no candidate profile found for the current user" }, "Not Found"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, ApiResponse.FailureResponse(
                    new List<string> { ex.Message ?? "You don't have permission to remove this education" },
                    "Forbidden"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing education for user {UserId}", GetCurrentUserId());
                return StatusCode(500, ApiResponse.FailureResponse(new List<string> { "An error occurred while removing education" }, "Couldn't Remove Education"));
            }
        }

        #endregion

        #region My Work Experience Management

        /// <summary>
        /// Get current user's work experience
        /// </summary>
        [HttpGet("my-work-experience")]
        public async Task<ActionResult<ApiResponse<List<CandidateWorkExperienceDto>>>> GetMyWorkExperience()
        {
            try
            {
                var userProfile = await GetCurrentUserProfileAsync();
                var workExperience = await _candidateProfileService.GetWorkExperienceAsync(userProfile.Id);
                return Ok(ApiResponse<List<CandidateWorkExperienceDto>>.SuccessResponse(workExperience, "Work experience retrieved successfully"));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(ApiResponse<List<CandidateWorkExperienceDto>>.FailureResponse(new List<string> { "No candidate profile found for the current user" }, "Not Found"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting work experience for user {UserId}", GetCurrentUserId());
                return StatusCode(500, ApiResponse<List<CandidateWorkExperienceDto>>.FailureResponse(new List<string> { "An error occurred while retrieving work experience" }, "Couldn't Retrieve Work Experience"));
            }
        }

        /// <summary>
        /// Add work experience to current user's profile
        /// </summary>
        [HttpPost("my-work-experience")]
        public async Task<ActionResult<ApiResponse<CandidateWorkExperienceDto>>> AddMyWorkExperience([FromBody] CreateCandidateWorkExperienceDto dto)
        {
            try
            {
                var userProfile = await GetCurrentUserProfileAsync();
                var workExp = await _candidateProfileService.AddWorkExperienceAsync(userProfile.Id, dto);
                return CreatedAtAction(nameof(GetMyProfile), new { }, ApiResponse<CandidateWorkExperienceDto>.SuccessResponse(workExp, "Work experience added successfully"));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(ApiResponse<CandidateWorkExperienceDto>.FailureResponse(new List<string> { "No candidate profile found for the current user" }, "Not Found"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding work experience for user {UserId}", GetCurrentUserId());
                return StatusCode(500, ApiResponse<CandidateWorkExperienceDto>.FailureResponse(new List<string> { "An unexpected error occurred while adding work experience" }, "Internal Server Error"));
            }
        }

        /// <summary>
        /// Update work experience for current user
        /// </summary>
        [HttpPatch("my-work-experience/{workExperienceId}")]
        public async Task<ActionResult<ApiResponse<CandidateWorkExperienceDto>>> UpdateMyWorkExperience(Guid workExperienceId, [FromBody] UpdateCandidateWorkExperienceDto dto)
        {
            try
            {
                var userProfile = await GetCurrentUserProfileAsync();
                var updatedWorkExperience = await _candidateProfileService.UpdateWorkExperienceAsync(workExperienceId, dto);
                return Ok(ApiResponse<CandidateWorkExperienceDto>.SuccessResponse(updatedWorkExperience, "Work experience updated successfully"));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(ApiResponse<CandidateWorkExperienceDto>.FailureResponse(new List<string> { "Work experience not found or no candidate profile found for the current user" }, "Not Found"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating work experience for user {UserId}", GetCurrentUserId());
                return StatusCode(500, ApiResponse<CandidateWorkExperienceDto>.FailureResponse(new List<string> { "An unexpected error occurred while updating work experience" }, "Internal Server Error"));
            }
        }

        /// <summary>
        /// Remove work experience from current user's profile
        /// </summary>
        [HttpDelete("my-work-experience/{workExperienceId}")]
        public async Task<ActionResult<ApiResponse>> RemoveMyWorkExperience(Guid workExperienceId)
        {
            try
            {
                var userProfile = await GetCurrentUserProfileAsync();
                var removed = await _candidateProfileService.RemoveWorkExperienceAsync(workExperienceId);
                if (!removed)
                {
                    return NotFound(ApiResponse.FailureResponse(new List<string> { $"Work experience not found" }, "Not Found"));
                }

                return Ok(ApiResponse.SuccessResponse("Work experience removed successfully"));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(ApiResponse.FailureResponse(new List<string> { "Work experience not found or no candidate profile found for the current user" }, "Not Found"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, ApiResponse.FailureResponse(
                    new List<string> { ex.Message ?? "You don't have permission to remove this work experience" },
                    "Forbidden"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing work experience for user {UserId}", GetCurrentUserId());
                return StatusCode(500, ApiResponse.FailureResponse(new List<string> { "An error occurred while removing work experience" }, "Couldn't Remove Work Experience"));
            }
        }

        #endregion

        #region Resume Management

        [HttpPost("my-resume")]
        public async Task<ActionResult<ApiResponse<CandidateProfileResponseDto>>> UploadMyResume(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(ApiResponse<CandidateProfileResponseDto>.FailureResponse(new List<string> { "Please provide a valid resume file" }, "Invalid File"));
                }

                var userProfile = await GetCurrentUserProfileAsync();
                var profile = await _candidateProfileService.UploadResumeAsync(userProfile.Id, file);
                return Ok(ApiResponse<CandidateProfileResponseDto>.SuccessResponse(profile, "Resume uploaded successfully"));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(ApiResponse<CandidateProfileResponseDto>.FailureResponse(new List<string> { "No candidate profile found for the current user" }, "Not Found"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading resume for user {UserId}", GetCurrentUserId());
                return StatusCode(500, ApiResponse<CandidateProfileResponseDto>.FailureResponse(new List<string> { "An unexpected error occurred while uploading the resume" }, "Internal Server Error"));
            }
        }

        /// <summary>
        /// Get current user's resume download URL
        /// </summary>
        [HttpGet("my-resume")]
        public async Task<ActionResult<ApiResponse<string>>> GetMyResumeUrl()
        {
            try
            {
                var userProfile = await GetCurrentUserProfileAsync();
                var resumeUrl = await _candidateProfileService.GetResumeUrlAsync(userProfile.Id);
                if (string.IsNullOrEmpty(resumeUrl))
                {
                    return NotFound(ApiResponse<string>.FailureResponse(new List<string> { "No resume found for your profile" }, "Not Found"));
                }

                return Ok(ApiResponse<string>.SuccessResponse(resumeUrl, "Resume URL retrieved successfully"));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(ApiResponse<string>.FailureResponse(new List<string> { "No candidate profile found for the current user" }, "Not Found"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting resume URL for user {UserId}", GetCurrentUserId());
                return StatusCode(500, ApiResponse<string>.FailureResponse(new List<string> { "An error occurred while retrieving the resume URL" }, "Retrieval Failed"));
            }
        }

        /// <summary>
        /// Get resume URL for a specific candidate profile (admin/recruiter access)
        /// </summary>
        [HttpGet("{candidateId:guid}/resume")]
        public async Task<ActionResult<ApiResponse<string>>> GetCandidateResume(Guid candidateId)
        {
            try
            {
                var profile = await _candidateProfileService.GetByIdAsync(candidateId);
                if (profile == null)
                {
                    return NotFound(ApiResponse<string>.FailureResponse(new List<string> { $"Candidate profile with ID {candidateId} not found" }, "Not Found"));
                }

                if (!ValidateUserAccess(profile.UserId))
                {
                    return Forbid();
                }

                var resumeUrl = await _candidateProfileService.GetResumeUrlAsync(candidateId);
                if (string.IsNullOrEmpty(resumeUrl))
                {
                    return NotFound(ApiResponse<string>.FailureResponse(new List<string> { "No resume found for this candidate" }, "Not Found"));
                }

                return Ok(ApiResponse<string>.SuccessResponse(resumeUrl, "Resume URL retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting resume URL for candidate {CandidateId}", candidateId);
                return StatusCode(500, ApiResponse<string>.FailureResponse(new List<string> { "An error occurred while retrieving the resume URL" }, "Retrieval Failed"));
            }
        }

        /// <summary>
        /// Delete current user's resume
        /// </summary>
        [HttpDelete("my-resume")]
        public async Task<ActionResult<ApiResponse>> DeleteMyResume()
        {
            try
            {
                var userProfile = await GetCurrentUserProfileAsync();
                var deleted = await _candidateProfileService.DeleteResumeAsync(userProfile.Id);
                if (!deleted)
                {
                    return NotFound(ApiResponse.FailureResponse(new List<string> { "No resume found to delete" }, "Not Found"));
                }

                return Ok(ApiResponse.SuccessResponse("Resume deleted successfully"));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(ApiResponse.FailureResponse(new List<string> { "No candidate profile found for the current user" }, "Not Found"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting resume for user {UserId}", GetCurrentUserId());
                return StatusCode(500, ApiResponse.FailureResponse(new List<string> { "An error occurred while deleting the resume" }, "Delete Failed"));
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

        private async Task<CandidateProfileResponseDto> GetCurrentUserProfileAsync()
        {
            var userId = GetCurrentUserId();
            var userProfile = await _candidateProfileService.GetByUserIdAsync(userId);

            if (userProfile == null)
            {
                throw new KeyNotFoundException("No candidate profile found for the current user");
            }

            return userProfile;
        }

        private bool ValidateUserAccess(Guid targetUserId)
        {
            var currentUserId = GetCurrentUserId();
            // Allow access if user is accessing their own data OR has admin/HR/recruiter role
            return currentUserId == targetUserId || HasAdminPrivileges();
        }

        private bool HasAdminPrivileges()
        {
            return User.IsInRole("SuperAdmin") ||
                User.IsInRole("Admin") ||
                User.IsInRole("HR") ||
                User.IsInRole("Recruiter");
        }

        #endregion

        #region Search

        /// <summary>
        /// Search candidates with filters (Staff only)
        /// Recruiters can only see candidates from applications assigned to them
        /// SuperAdmin, Admin, and HR can see all candidates
        /// </summary>
        [HttpGet("search")]
        [Authorize(Roles = "SuperAdmin,Admin,HR,Recruiter")]
        public async Task<ActionResult<ApiResponse<PagedResult<CandidateSearchResultDto>>>> SearchCandidates(
            [FromQuery] CandidateSearchFilters filters)
        {
            try
            {
                // Validate pagination parameters
                if (filters.PageNumber < 1) filters.PageNumber = 1;
                if (filters.PageSize < 1) filters.PageSize = 25;
                if (filters.PageSize > 500) filters.PageSize = 500;

                // Recruiters can only see their assigned candidates
                Guid? assignedRecruiterId = null;
                if (User.IsInRole("Recruiter") &&
                    !User.IsInRole("SuperAdmin") &&
                    !User.IsInRole("Admin") &&
                    !User.IsInRole("HR"))
                {
                    assignedRecruiterId = GetCurrentUserId();
                }

                var result = await _candidateProfileService.SearchCandidatesAsync(filters, assignedRecruiterId);

                return Ok(ApiResponse<PagedResult<CandidateSearchResultDto>>.SuccessResponse(
                    result,
                    $"Found {result.TotalCount} candidates"
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching candidates");
                return StatusCode(500, ApiResponse<PagedResult<CandidateSearchResultDto>>.FailureResponse(
                    new List<string> { "An error occurred while searching candidates" },
                    "Search Failed"
                ));
            }
        }

        #endregion
    }
}
