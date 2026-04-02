using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecruitmentSystem.Services.Interfaces;
using RecruitmentSystem.Shared.DTOs;
using RecruitmentSystem.Shared.DTOs.Responses;

namespace RecruitmentSystem.API.Controllers
{
    [Route("api/notifications")]
    [ApiController]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(
            INotificationService notificationService,
            ILogger<NotificationController> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        [HttpPost]
        [Authorize(Roles = "SuperAdmin,Admin,HR,Recruiter")]
        public async Task<ActionResult<ApiResponse<NotificationCreateResponseDto>>> Create([FromBody] CreateNotificationDto dto)
        {
            try
            {
                var id = await _notificationService.CreateAsync(dto.Title, dto.Message, dto.Type, dto.RecipientUserIds);

                return Ok(ApiResponse<NotificationCreateResponseDto>.SuccessResponse(
                    new NotificationCreateResponseDto { NotificationId = id },
                    "Notification created successfully"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<NotificationCreateResponseDto>.FailureResponse(
                    new List<string> { ex.Message },
                    "Invalid request"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification");
                return StatusCode(500, ApiResponse<NotificationCreateResponseDto>.FailureResponse(
                    new List<string> { "An error occurred while creating notification" },
                    "Internal Server Error"));
            }
        }

        [HttpGet("me")]
        public async Task<ActionResult<ApiResponse<List<NotificationResponseDto>>>> GetMyNotifications()
        {
            try
            {
                var userId = GetCurrentUserId();
                var notifications = await _notificationService.GetUnreadByUserAsync(userId);

                return Ok(ApiResponse<List<NotificationResponseDto>>.SuccessResponse(
                    notifications,
                    "Notifications retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notifications for current user");
                return StatusCode(500, ApiResponse<List<NotificationResponseDto>>.FailureResponse(
                    new List<string> { "An error occurred while retrieving notifications" },
                    "Internal Server Error"));
            }
        }

        [HttpPatch("{notificationId:guid}/read")]
        public async Task<ActionResult<ApiResponse>> MarkAsRead(Guid notificationId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var marked = await _notificationService.MarkAsReadAsync(userId, notificationId);

                if (!marked)
                {
                    return NotFound(ApiResponse.FailureResponse(
                        new List<string> { "Unread notification not found" },
                        "Not Found"));
                }

                return Ok(ApiResponse.SuccessResponse("Notification marked as read"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read: {NotificationId}", notificationId);
                return StatusCode(500, ApiResponse.FailureResponse(
                    new List<string> { "An error occurred while marking notification as read" },
                    "Internal Server Error"));
            }
        }

        [HttpPatch("read-all")]
        public async Task<ActionResult<ApiResponse<MarkAllNotificationsReadResponseDto>>> MarkAllAsRead()
        {
            try
            {
                var userId = GetCurrentUserId();
                var count = await _notificationService.MarkAllAsReadAsync(userId);

                return Ok(ApiResponse<MarkAllNotificationsReadResponseDto>.SuccessResponse(
                    new MarkAllNotificationsReadResponseDto { MarkedCount = count },
                    "All notifications marked as read"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read");
                return StatusCode(500, ApiResponse<MarkAllNotificationsReadResponseDto>.FailureResponse(
                    new List<string> { "An error occurred while marking all notifications as read" },
                    "Internal Server Error"));
            }
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("Invalid authenticated user context");
            }

            return userId;
        }
    }
}