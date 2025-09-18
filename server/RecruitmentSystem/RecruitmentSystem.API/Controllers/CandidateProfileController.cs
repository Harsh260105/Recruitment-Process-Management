using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RecruitmentSystem.Services.Interfaces;
using RecruitmentSystem.Shared.DTOs.CandidateProfile;
using RecruitmentSystem.Shared.DTOs.Responses;
using System.Collections.Generic;
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
        [HttpGet("{id}")]
        public async Task<ActionResult<CandidateProfileResponseDto>> GetById(Guid id)
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
                    return Forbid();
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
        public async Task<ActionResult<CandidateProfileResponseDto>> GetByUserId(Guid userId)
        {
            try
            {
                // Validate user access
                if (!ValidateUserAccess(userId))
                {
                    return Forbid();
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
        public async Task<ActionResult<CandidateProfileResponseDto>> GetMyProfile()
        {
            try
            {
                var profile = await GetCurrentUserProfileAsync();
                return Ok(ApiResponse<CandidateProfileResponseDto>.SuccessResponse(profile, "Profile retrieved successfully"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<CandidateProfileResponseDto>.FailureResponse(new List<string> { ex.Message }, "Not Found"));
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
        public async Task<ActionResult<CandidateProfileResponseDto>> CreateProfile([FromBody] CandidateProfileDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<CandidateProfileResponseDto>.FailureResponse(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList(), "Invalid Data"));
                }

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
        [HttpPatch("{id}")]
        public async Task<ActionResult<CandidateProfileResponseDto>> UpdateProfile(Guid id, [FromBody] UpdateCandidateProfileDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<CandidateProfileResponseDto>.FailureResponse(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList(), "Invalid Data"));
                }

                // Validate ownership or admin privileges
                var userId = GetCurrentUserId();
                var candidateProfile = await _candidateProfileService.GetByIdAsync(id);
                if (candidateProfile == null || (candidateProfile.UserId != userId && !HasAdminPrivileges()))
                {
                    return Forbid();
                }

                var profile = await _candidateProfileService.UpdateProfileAsync(id, dto);
                if (profile == null)
                {
                    return NotFound(ApiResponse<CandidateProfileResponseDto>.FailureResponse(new List<string> { $"Candidate profile with ID {id} not found" }, "Not Found"));
                }

                return Ok(ApiResponse<CandidateProfileResponseDto>.SuccessResponse(profile, "Profile updated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating candidate profile");
                return StatusCode(500, ApiResponse<CandidateProfileResponseDto>.FailureResponse(new List<string> { "An error occurred while updating the candidate profile" }, "Couldn't Update Profile"));
            }
        }

        /// <summary>
        /// Delete candidate profile
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteProfile(Guid id)
        {
            try
            {
                var userId = GetCurrentUserId();

                // Check if user owns the profile or has admin privileges
                var userProfile = await _candidateProfileService.GetByUserIdAsync(userId);
                if (userProfile == null || (userProfile.Id != id && !HasAdminPrivileges()))
                {
                    return Forbid();
                }

                var deleted = await _candidateProfileService.DeleteProfileAsync(id);
                if (!deleted)
                {
                    return NotFound(ApiResponse.FailureResponse(new List<string> { $"Candidate profile with ID {id} not found or could not be deleted" }, "Not Found"));
                }

                return Ok(ApiResponse.SuccessResponse("Profile deleted successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting candidate profile with ID: {Id}", id);
                return StatusCode(500, ApiResponse.FailureResponse(new List<string> { "An error occurred while deleting the candidate profile" }, "Couldn't Delete Profile"));
            }
        }

        #endregion

        #region My Skills Management

        /// <summary>
        /// Get current user's skills
        /// </summary>
        [HttpGet("my-skills")]
        public async Task<ActionResult<List<CandidateSkillDto>>> GetMySkills()
        {
            try
            {
                var userProfile = await GetCurrentUserProfileAsync();
                var skills = await _candidateProfileService.GetSkillsAsync(userProfile.Id);
                return Ok(ApiResponse<List<CandidateSkillDto>>.SuccessResponse(skills, "Skills retrieved successfully"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<List<CandidateSkillDto>>.FailureResponse(new List<string> { ex.Message }, "Not Found"));
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
        public async Task<ActionResult<List<CandidateSkillDto>>> AddMySkills([FromBody] List<CreateCandidateSkillDto> skills)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<List<CandidateSkillDto>>.FailureResponse(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList(), "Invalid Data"));
                }

                var userProfile = await GetCurrentUserProfileAsync();
                var addedSkills = await _candidateProfileService.AddSkillsAsync(userProfile.Id, skills);
                return Ok(ApiResponse<List<CandidateSkillDto>>.SuccessResponse(addedSkills, "Skills added successfully"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<List<CandidateSkillDto>>.FailureResponse(new List<string> { ex.Message }, "Not Found"));
            }
            catch (ArgumentException ex)
            {
                return NotFound(ApiResponse<List<CandidateSkillDto>>.FailureResponse(new List<string> { ex.Message }, "Not Found"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding skills for user {UserId}", GetCurrentUserId());
                return StatusCode(500, ApiResponse<List<CandidateSkillDto>>.FailureResponse(new List<string> { "An error occurred while adding skills" }, "Couldn't Add Skills"));
            }
        }

        /// <summary>
        /// Update a specific skill for current user
        /// </summary>
        [HttpPatch("my-skills/{skillId}")]
        public async Task<ActionResult<CandidateSkillDto>> UpdateMySkill(int skillId, [FromBody] UpdateCandidateSkillDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<CandidateSkillDto>.FailureResponse(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList(), "Invalid Data"));
                }

                var userProfile = await GetCurrentUserProfileAsync();
                var updatedSkill = await _candidateProfileService.UpdateSkillAsync(userProfile.Id, skillId, dto);
                return Ok(ApiResponse<CandidateSkillDto>.SuccessResponse(updatedSkill, "Skill updated successfully"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<CandidateSkillDto>.FailureResponse(new List<string> { ex.Message }, "Not Found"));
            }
            catch (ArgumentException ex)
            {
                return NotFound(ApiResponse<CandidateSkillDto>.FailureResponse(new List<string> { ex.Message }, "Not Found"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating skill for user {UserId}", GetCurrentUserId());
                return StatusCode(500, ApiResponse<CandidateSkillDto>.FailureResponse(new List<string> { "An error occurred while updating the skill" }, "Couldn't Update Skill"));
            }
        }

        /// <summary>
        /// Remove a skill from current user's profile
        /// </summary>
        [HttpDelete("my-skills/{skillId}")]
        public async Task<ActionResult> RemoveMySkill(int skillId)
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
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse.FailureResponse(new List<string> { ex.Message }, "Not Found"));
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
        public async Task<ActionResult<List<CandidateEducationDto>>> GetMyEducation()
        {
            try
            {
                var userProfile = await GetCurrentUserProfileAsync();
                var education = await _candidateProfileService.GetEducationAsync(userProfile.Id);
                return Ok(ApiResponse<List<CandidateEducationDto>>.SuccessResponse(education, "Education retrieved successfully"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<List<CandidateEducationDto>>.FailureResponse(new List<string> { ex.Message }, "Not Found"));
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
        public async Task<ActionResult<CandidateEducationDto>> AddMyEducation([FromBody] CreateCandidateEducationDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<CandidateEducationDto>.FailureResponse(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList(), "Invalid Data"));
                }

                var userProfile = await GetCurrentUserProfileAsync();
                var education = await _candidateProfileService.AddEducationAsync(userProfile.Id, dto);
                return CreatedAtAction(nameof(GetMyProfile), new { }, ApiResponse<CandidateEducationDto>.SuccessResponse(education, "Education added successfully"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<CandidateEducationDto>.FailureResponse(new List<string> { ex.Message }, "Not Found"));
            }
            catch (ArgumentException ex)
            {
                return NotFound(ApiResponse<CandidateEducationDto>.FailureResponse(new List<string> { ex.Message }, "Not Found"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding education for user {UserId}", GetCurrentUserId());
                return StatusCode(500, ApiResponse<CandidateEducationDto>.FailureResponse(new List<string> { "An error occurred while adding education" }, "Couldn't Add Education"));
            }
        }

        /// <summary>
        /// Update education for current user
        /// </summary>
        [HttpPatch("my-education/{educationId}")]
        public async Task<ActionResult<CandidateEducationDto>> UpdateMyEducation(Guid educationId, [FromBody] UpdateCandidateEducationDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<CandidateEducationDto>.FailureResponse(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList(), "Invalid Data"));
                }

                var userProfile = await GetCurrentUserProfileAsync();
                var updatedEducation = await _candidateProfileService.UpdateEducationAsync(educationId, dto);
                return Ok(ApiResponse<CandidateEducationDto>.SuccessResponse(updatedEducation, "Education updated successfully"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<CandidateEducationDto>.FailureResponse(new List<string> { ex.Message }, "Not Found"));
            }
            catch (ArgumentException ex)
            {
                return NotFound(ApiResponse<CandidateEducationDto>.FailureResponse(new List<string> { ex.Message }, "Not Found"));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating education for user {UserId}", GetCurrentUserId());
                return StatusCode(500, ApiResponse<CandidateEducationDto>.FailureResponse(new List<string> { "An error occurred while updating education" }, "Couldn't Update Education"));
            }
        }

        /// <summary>
        /// Remove education from current user's profile
        /// </summary>
        [HttpDelete("my-education/{educationId}")]
        public async Task<ActionResult> RemoveMyEducation(Guid educationId)
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
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse.FailureResponse(new List<string> { ex.Message }, "Not Found"));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
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
        public async Task<ActionResult<List<CandidateWorkExperienceDto>>> GetMyWorkExperience()
        {
            try
            {
                var userProfile = await GetCurrentUserProfileAsync();
                var workExperience = await _candidateProfileService.GetWorkExperienceAsync(userProfile.Id);
                return Ok(ApiResponse<List<CandidateWorkExperienceDto>>.SuccessResponse(workExperience, "Work experience retrieved successfully"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<List<CandidateWorkExperienceDto>>.FailureResponse(new List<string> { ex.Message }, "Not Found"));
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
        public async Task<ActionResult<CandidateWorkExperienceDto>> AddMyWorkExperience([FromBody] CreateCandidateWorkExperienceDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<CandidateWorkExperienceDto>.FailureResponse(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList(), "Invalid Data"));
                }

                var userProfile = await GetCurrentUserProfileAsync();
                var workExp = await _candidateProfileService.AddWorkExperienceAsync(userProfile.Id, dto);
                return CreatedAtAction(nameof(GetMyProfile), new { }, ApiResponse<CandidateWorkExperienceDto>.SuccessResponse(workExp, "Work experience added successfully"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<CandidateWorkExperienceDto>.FailureResponse(new List<string> { ex.Message }, "Not Found"));
            }
            catch (ArgumentException ex)
            {
                return NotFound(ApiResponse<CandidateWorkExperienceDto>.FailureResponse(new List<string> { ex.Message }, "Not Found"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding work experience for user {UserId}", GetCurrentUserId());
                return StatusCode(500, ApiResponse<CandidateWorkExperienceDto>.FailureResponse(new List<string> { "An error occurred while adding work experience" }, "Couldn't Add Work Experience"));
            }
        }

        /// <summary>
        /// Update work experience for current user
        /// </summary>
        [HttpPatch("my-work-experience/{workExperienceId}")]
        public async Task<ActionResult<CandidateWorkExperienceDto>> UpdateMyWorkExperience(Guid workExperienceId, [FromBody] UpdateCandidateWorkExperienceDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<CandidateWorkExperienceDto>.FailureResponse(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList(), "Invalid Data"));
                }

                var userProfile = await GetCurrentUserProfileAsync();
                var updatedWorkExperience = await _candidateProfileService.UpdateWorkExperienceAsync(workExperienceId, dto);
                return Ok(ApiResponse<CandidateWorkExperienceDto>.SuccessResponse(updatedWorkExperience, "Work experience updated successfully"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<CandidateWorkExperienceDto>.FailureResponse(new List<string> { ex.Message }, "Not Found"));
            }
            catch (ArgumentException ex)
            {
                return NotFound(ApiResponse<CandidateWorkExperienceDto>.FailureResponse(new List<string> { ex.Message }, "Not Found"));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating work experience for user {UserId}", GetCurrentUserId());
                return StatusCode(500, ApiResponse<CandidateWorkExperienceDto>.FailureResponse(new List<string> { "An error occurred while updating work experience" }, "Couldn't Update Work Experience"));
            }
        }

        /// <summary>
        /// Remove work experience from current user's profile
        /// </summary>
        [HttpDelete("my-work-experience/{workExperienceId}")]
        public async Task<ActionResult> RemoveMyWorkExperience(Guid workExperienceId)
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
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse.FailureResponse(new List<string> { ex.Message }, "Not Found"));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
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
        public async Task<ActionResult<CandidateProfileResponseDto>> UploadMyResume(IFormFile file)
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
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<CandidateProfileResponseDto>.FailureResponse(new List<string> { ex.Message }, "Not Found"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<CandidateProfileResponseDto>.FailureResponse(new List<string> { ex.Message }, "Invalid Request"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading resume for user {UserId}", GetCurrentUserId());
                return StatusCode(500, ApiResponse<CandidateProfileResponseDto>.FailureResponse(new List<string> { "An error occurred while uploading the resume" }, "Upload Failed"));
            }
        }

        /// <summary>
        /// Get current user's resume download URL
        /// </summary>
        [HttpGet("my-resume")]
        public async Task<ActionResult<string>> GetMyResumeUrl()
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
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<string>.FailureResponse(new List<string> { ex.Message }, "Not Found"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting resume URL for user {UserId}", GetCurrentUserId());
                return StatusCode(500, ApiResponse<string>.FailureResponse(new List<string> { "An error occurred while retrieving the resume URL" }, "Retrieval Failed"));
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
            return User.IsInRole("Admin") ||
                   User.IsInRole("HR") ||
                   User.IsInRole("Recruiter");
        }

        #endregion
    }
}
