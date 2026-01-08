using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Core.Enums;
using RecruitmentSystem.Shared.DTOs;

namespace RecruitmentSystem.Services.Interfaces
{
    /// <summary>
    /// Handles interview scheduling, rescheduling, and participant management
    /// </summary>
    public interface IInterviewSchedulingService
    {
        // Core Scheduling Operations
        Task<Interview> ScheduleInterviewAsync(ScheduleInterviewDto dto, Guid scheduledByUserId);
        Task<Interview> RescheduleInterviewAsync(Guid interviewId, RescheduleInterviewDto dto, Guid rescheduledByUserId);
        Task<Interview> CancelInterviewAsync(Guid interviewId, CancelInterviewDto dto, Guid cancelledByUserId);

        // Status Management
        Task<Interview> MarkInterviewAsCompletedAsync(Guid interviewId, MarkInterviewCompletedDto dto, Guid completedByUserId);
        Task<Interview> MarkNoShowAsync(Guid interviewId, MarkInterviewNoShowDto dto, Guid markedByUserId);

        // Participant Management
        Task<IEnumerable<InterviewParticipant>> GetInterviewParticipantsAsync(Guid interviewId, Guid requestingUserId);

        // Round Management (moved from IInterviewService - scheduling responsibility)
        Task<Interview?> GetLatestInterviewForApplicationAsync(Guid jobApplicationId);

        // Validation
        Task<bool> CanScheduleInterviewAsync(Guid jobApplicationId);
        Task<bool> HasConflictingInterviewsAsync(Guid participantUserId, DateTime scheduledDateTime, int durationMinutes, Guid? excludingInterviewId = null);

        // Time Slot Validation - Centralized business rules
        void ValidateTimeSlot(DateTime scheduledDateTime, int durationMinutes);

        // Available Time Slots
        Task<IEnumerable<AvailableTimeSlotDto>> GetAvailableTimeSlotsAsync(GetAvailableTimeSlotsRequestDto request);

        // Automation helpers
        Task<int> SendUpcomingInterviewRemindersAsync(int hoursAhead = 4);
        Task<int> SendPendingEvaluationRemindersAsync(int hoursAfterCompletion = 24);
    }
}