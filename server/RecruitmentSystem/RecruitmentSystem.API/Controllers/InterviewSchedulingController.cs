using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecruitmentSystem.Services.Interfaces;
using RecruitmentSystem.Shared.DTOs;
using RecruitmentSystem.Shared.DTOs.Responses;
using System.Security.Claims;

namespace RecruitmentSystem.API.Controllers
{
    [Route("api/interviews")]
    [ApiController]
    [Authorize]
    public class InterviewSchedulingController : ControllerBase
    {
        private readonly IInterviewSchedulingService _schedulingService;
        private readonly IMapper _mapper;

        public InterviewSchedulingController(
            IInterviewSchedulingService schedulingService,
            IMapper mapper)
        {
            _schedulingService = schedulingService;
            _mapper = mapper;
        }

        #region Core Scheduling Operations

        /// <summary>
        /// Schedule a new interview
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,SuperAdmin,HR,Recruiter")]
        public async Task<ActionResult<ApiResponse<InterviewResponseDto>>> ScheduleInterview([FromBody] ScheduleInterviewDto dto)
        {
            try
            {
                var scheduledByUserId = GetCurrentUserId();
                var interview = await _schedulingService.ScheduleInterviewAsync(dto, scheduledByUserId);
                var responseDto = _mapper.Map<InterviewResponseDto>(interview);
                return Ok(ApiResponse<InterviewResponseDto>.SuccessResponse(responseDto, "Interview scheduled successfully"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<InterviewResponseDto>.FailureResponse(
                    new List<string> { ex.Message },
                    "Bad Request"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<InterviewResponseDto>.FailureResponse(
                    new List<string> { ex.Message },
                    "Bad Request"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, ApiResponse<InterviewResponseDto>.FailureResponse(
                    new List<string> { ex.Message },
                    "Forbidden"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<InterviewResponseDto>.FailureResponse(
                    new List<string> { ex.Message },
                    "Not Found"));
            }
        }

        /// <summary>
        /// Reschedule an existing interview
        /// </summary>
        [HttpPut("{interviewId:guid}/reschedule")]
        [Authorize(Roles = "Admin,SuperAdmin,HR,Recruiter")]
        public async Task<ActionResult<ApiResponse<InterviewResponseDto>>> RescheduleInterview(
            Guid interviewId,
            [FromBody] RescheduleInterviewDto dto)
        {
            try
            {
                var rescheduledByUserId = GetCurrentUserId();
                var interview = await _schedulingService.RescheduleInterviewAsync(interviewId, dto, rescheduledByUserId);
                var responseDto = _mapper.Map<InterviewResponseDto>(interview);
                return Ok(ApiResponse<InterviewResponseDto>.SuccessResponse(responseDto, "Interview rescheduled successfully"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse<InterviewResponseDto>.FailureResponse(
                    new List<string> { ex.Message },
                    "Bad Request"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<InterviewResponseDto>.FailureResponse(
                    new List<string> { ex.Message },
                    "Bad Request"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, ApiResponse<InterviewResponseDto>.FailureResponse(
                    new List<string> { ex.Message },
                    "Forbidden"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<InterviewResponseDto>.FailureResponse(
                    new List<string> { ex.Message },
                    "Not Found"));
            }
        }

        /// <summary>
        /// Cancel an interview
        /// </summary>
        [HttpPut("{interviewId:guid}/cancel")]
        [Authorize(Roles = "Admin,SuperAdmin,HR,Recruiter")]
        public async Task<ActionResult<ApiResponse<InterviewResponseDto>>> CancelInterview(
            Guid interviewId,
            [FromBody] CancelInterviewDto dto)
        {
            var cancelledByUserId = GetCurrentUserId();
            var interview = await _schedulingService.CancelInterviewAsync(interviewId, dto, cancelledByUserId);
            var responseDto = _mapper.Map<InterviewResponseDto>(interview);
            return Ok(ApiResponse<InterviewResponseDto>.SuccessResponse(responseDto, "Interview cancelled successfully"));
        }

        #endregion

        #region Status Management

        /// <summary>
        /// Mark interview as completed
        /// Only interview participants can mark as completed
        /// </summary>
        [HttpPut("{interviewId:guid}/complete")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<InterviewResponseDto>>> MarkInterviewAsCompleted(
            Guid interviewId,
            [FromBody] MarkInterviewCompletedDto dto)
        {
            var completedByUserId = GetCurrentUserId();

            var interview = await _schedulingService.MarkInterviewAsCompletedAsync(interviewId, dto, completedByUserId);
            var responseDto = _mapper.Map<InterviewResponseDto>(interview);
            return Ok(ApiResponse<InterviewResponseDto>.SuccessResponse(responseDto, "Interview marked as completed successfully"));
        }

        /// <summary>
        /// Mark interview as no-show
        /// </summary>
        [HttpPut("{interviewId:guid}/no-show")]
        [Authorize(Roles = "Admin,SuperAdmin,HR,Recruiter")]
        public async Task<ActionResult<ApiResponse<InterviewResponseDto>>> MarkNoShow(
            Guid interviewId,
            [FromBody] MarkInterviewNoShowDto dto)
        {
            var markedByUserId = GetCurrentUserId();
            var interview = await _schedulingService.MarkNoShowAsync(interviewId, dto, markedByUserId);
            var responseDto = _mapper.Map<InterviewResponseDto>(interview);
            return Ok(ApiResponse<InterviewResponseDto>.SuccessResponse(responseDto, "Interview marked as no-show successfully"));
        }

        #endregion

        #region Participant Management

        /// <summary>
        /// Get all participants for an interview
        /// </summary>
        [HttpGet("{interviewId:guid}/participants")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<List<InterviewParticipantResponseDto>>>> GetInterviewParticipants(Guid interviewId)
        {
            var requestingUserId = GetCurrentUserId();
            var participants = await _schedulingService.GetInterviewParticipantsAsync(interviewId, requestingUserId);
            var responseDtos = _mapper.Map<List<InterviewParticipantResponseDto>>(participants);
            return Ok(ApiResponse<List<InterviewParticipantResponseDto>>.SuccessResponse(responseDtos, "Participants retrieved successfully"));
        }

        #endregion

        #region Round Management

        /// <summary>
        /// Get the latest interview for a job application
        /// </summary>
        [HttpGet("applications/{jobApplicationId:guid}/latest")]
        [Authorize(Roles = "Admin,SuperAdmin,HR,Recruiter")]
        public async Task<ActionResult<ApiResponse<InterviewResponseDto>>> GetLatestInterviewForApplication(Guid jobApplicationId)
        {
            var interview = await _schedulingService.GetLatestInterviewForApplicationAsync(jobApplicationId);
            if (interview == null)
            {
                return NotFound(ApiResponse<InterviewResponseDto>.FailureResponse(
                    new List<string> { "No interviews found for this application" },
                    "Not Found"));
            }

            var responseDto = _mapper.Map<InterviewResponseDto>(interview);
            return Ok(ApiResponse<InterviewResponseDto>.SuccessResponse(responseDto, "Latest interview retrieved successfully"));
        }

        #endregion

        #region Validation

        /// <summary>
        /// Check if an interview can be scheduled for a job application
        /// </summary>
        [HttpGet("applications/{jobApplicationId:guid}/can-schedule")]
        [Authorize(Roles = "Admin,SuperAdmin,HR,Recruiter")]
        public async Task<ActionResult<ApiResponse<bool>>> CanScheduleInterview(Guid jobApplicationId)
        {
            var canSchedule = await _schedulingService.CanScheduleInterviewAsync(jobApplicationId);
            return Ok(ApiResponse<bool>.SuccessResponse(canSchedule, "Scheduling eligibility checked successfully"));
        }

        /// <summary>
        /// Check for conflicting interviews for a participant
        /// </summary>
        [HttpGet("conflicts")]
        [Authorize(Roles = "Admin,SuperAdmin,HR,Recruiter")]
        public async Task<ActionResult<ApiResponse<bool>>> CheckConflictingInterviews(
            [FromQuery] Guid participantUserId,
            [FromQuery] DateTime scheduledDateTime,
            [FromQuery] int durationMinutes)
        {
            var hasConflicts = await _schedulingService.HasConflictingInterviewsAsync(participantUserId, scheduledDateTime, durationMinutes);
            return Ok(ApiResponse<bool>.SuccessResponse(hasConflicts, "Conflict check completed successfully"));
        }

        /// <summary>
        /// Validate time slot against business rules
        /// </summary>
        [HttpPost("validate-time-slot")]
        [Authorize(Roles = "Admin,SuperAdmin,HR,Recruiter")]
        public ActionResult<ApiResponse> ValidateTimeSlot([FromBody] ValidateTimeSlotDto dto)
        {
            try
            {
                _schedulingService.ValidateTimeSlot(dto.ScheduledDateTime, dto.DurationMinutes);
                return Ok(ApiResponse.SuccessResponse("Time slot is valid"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponse.FailureResponse(new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Get available time slots for scheduling interviews
        /// Shows conflict-free slots within business hours (9 AM - 6 PM, weekdays only)
        /// If ParticipantUserIds is empty, checks availability for the current user only
        /// </summary>
        [HttpPost("available-slots")]
        [Authorize(Roles = "Admin,SuperAdmin,HR,Recruiter")]
        public async Task<ActionResult<ApiResponse<IEnumerable<AvailableTimeSlotDto>>>> GetAvailableTimeSlots(
            [FromBody] GetAvailableTimeSlotsRequestDto request)
        {
            // If no participants specified, use current user's availability
            var currentUserId = GetCurrentUserId();
            if (request.ParticipantUserIds == null || !request.ParticipantUserIds.Any())
            {
                request.ParticipantUserIds = new List<Guid> { currentUserId };
            }

            var availableSlots = await _schedulingService.GetAvailableTimeSlotsAsync(request);
            return Ok(ApiResponse<IEnumerable<AvailableTimeSlotDto>>.SuccessResponse(
                availableSlots,
                $"Found {availableSlots.Count()} available time slots"));
        }

        /// <summary>
        /// Get scheduled interviews (booked time slots) for a date range
        /// Shows exactly when interviews are already booked rather than available slots
        /// If ParticipantUserIds is empty, defaults to the current user
        /// </summary>
        [HttpPost("scheduled-interviews")]
        [Authorize(Roles = "Admin,SuperAdmin,HR,Recruiter")]
        public async Task<ActionResult<ApiResponse<IEnumerable<ScheduledInterviewSlotDto>>>> GetScheduledInterviews(
            [FromBody] GetScheduledInterviewsRequestDto request)
        {
            var currentUserId = GetCurrentUserId();
            if (request.ParticipantUserIds == null || !request.ParticipantUserIds.Any())
            {
                request.ParticipantUserIds = new List<Guid> { currentUserId };
            }

            var scheduledInterviews = await _schedulingService.GetScheduledInterviewsAsync(request);
            return Ok(ApiResponse<IEnumerable<ScheduledInterviewSlotDto>>.SuccessResponse(
                scheduledInterviews,
                $"Found {scheduledInterviews.Count()} scheduled interviews"));
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Gets the current user's ID from the JWT token
        /// </summary>
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

    #region Helper DTOs

    public class ValidateTimeSlotDto
    {
        public DateTime ScheduledDateTime { get; set; }
        public int DurationMinutes { get; set; }
    }

    #endregion
}