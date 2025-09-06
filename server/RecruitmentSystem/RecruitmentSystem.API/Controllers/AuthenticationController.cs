using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecruitmentSystem.Services.Interfaces;
using RecruitmentSystem.Shared.DTOs;
using System.Security.Claims;

namespace RecruitmentSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticationController : ControllerBase
    {
        private readonly IAuthenticationService _authenticationService;

        public AuthenticationController(IAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                var result = await _authenticationService.LoginAsync(loginDto);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // OPTION 1: Public candidate registration (no authorization required)
        [HttpPost("register/candidate")]
        public async Task<ActionResult<AuthResponseDto>> RegisterCandidate([FromBody] CandidateRegisterDto registerDto)
        {
            try
            {
                var result = await _authenticationService.RegisterCandidateAsync(registerDto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // OPTION 2: Admin-only registration for internal users (Recruiter, HR, etc.)
        [HttpPost("register/staff")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<ActionResult<AuthResponseDto>> RegisterStaff([FromBody] RegisterDto registerDto)
        {
            try
            {
                var result = await _authenticationService.RegisterAsync(registerDto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // OPTION 3: Initial Super Admin registration (only for system setup)
        [HttpPost("register/initial-admin")]
        public async Task<ActionResult<AuthResponseDto>> RegisterInitialAdmin([FromBody] InitialAdminDto registerDto)
        {
            try
            {
                // Check if any SuperAdmin exists
                var hasAdmin = await _authenticationService.HasSuperAdminAsync();
                if (hasAdmin)
                {
                    return Forbid("Super Admin already exists. Use staff registration endpoint.");
                }

                var result = await _authenticationService.RegisterInitialSuperAdminAsync(registerDto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var result = await _authenticationService.ChangePasswordAsync(userId, changePasswordDto);

                if (result)
                    return Ok(new { message = "Password changed successfully" });
                else
                    return BadRequest(new { message = "Password change failed" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("profile")]
        [Authorize]
        public async Task<ActionResult<UserProfileDto>> GetProfile()
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var profile = await _authenticationService.GetUserProfileAsync(userId);
                return Ok(profile);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<ActionResult> Logout()
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                await _authenticationService.LogoutAsync(userId);
                return Ok(new { message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("roles")]
        [Authorize]
        public async Task<ActionResult<List<string>>> GetUserRoles()
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var roles = await _authenticationService.GetUserRolesAsync(userId);
                return Ok(roles);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Helper endpoint to check if system needs initial setup
        [HttpGet("needs-setup")]
        public async Task<ActionResult<bool>> NeedsInitialSetup()
        {
            try
            {
                var hasAdmin = await _authenticationService.HasSuperAdminAsync();
                return Ok(!hasAdmin); // Returns true if no SuperAdmin exists
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}