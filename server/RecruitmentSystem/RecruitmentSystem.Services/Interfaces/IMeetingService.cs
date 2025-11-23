using RecruitmentSystem.Shared.DTOs;

namespace RecruitmentSystem.Services.Interfaces
{
    /// <summary>
    /// Service for integrating with third-party video conferencing platforms
    /// Supports Google Meet, Zoom, Microsoft Teams, etc.
    /// </summary>
    public interface IMeetingService
    {
        /// <summary>
        /// Creates a new video conference meeting
        /// </summary>
        /// <param name="request">Meeting details</param>
        /// <returns>Meeting credentials and access details</returns>
        Task<MeetingCredentialsDto> CreateMeetingAsync(CreateMeetingRequestDto request);

        /// <summary>
        /// Cancels/deletes a meeting
        /// </summary>
        /// <param name="meetingId">Meeting ID to cancel</param>
        /// <returns>True if successfully cancelled</returns>
        Task<bool> CancelMeetingAsync(string meetingId);

        /// <summary>
        /// Retrieves meeting details by ID
        /// </summary>
        /// <param name="meetingId">Meeting ID</param>
        /// <returns>Meeting credentials and details</returns>
        Task<MeetingCredentialsDto?> GetMeetingAsync(string meetingId);

        /// <summary>
        /// Checks if the meeting service is properly configured and available
        /// </summary>
        /// <returns>True if service is ready to use</returns>
        Task<bool> IsServiceAvailableAsync();

        /// <summary>
        /// Gets the supported meeting service type
        /// </summary>
        /// <returns>Service type (GoogleMeet, Zoom, etc.)</returns>
        string GetServiceType();
    }
}