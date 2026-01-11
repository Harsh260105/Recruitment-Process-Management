using Microsoft.Extensions.Logging;
using RecruitmentSystem.Services.Interfaces;
using RecruitmentSystem.Shared.DTOs;

namespace RecruitmentSystem.Services.Implementations
{
    public class JitsiMeetService : IMeetingService
    {
        private readonly ILogger<JitsiMeetService> _logger;

        public JitsiMeetService(ILogger<JitsiMeetService> logger)
        {
            _logger = logger;
        }
        public Task<MeetingCredentialsDto> CreateMeetingAsync(CreateMeetingRequestDto request)
        {
            try
            {
                var uniqueId = Guid.NewGuid().ToString("N")[..12];
                var sanitizedTitle = SanitizeForUrl(request.Title);
                var roomName = $"ROIMA-Interview-{sanitizedTitle}-{uniqueId}";
                var meetingLink = $"https://meet.jit.si/{roomName}";

                _logger.LogInformation("Generated Jitsi Meet link for interview: {Title}", request.Title);

                return Task.FromResult(new MeetingCredentialsDto
                {
                    MeetingId = roomName,
                    MeetingLink = meetingLink,
                    Title = request.Title,
                    StartDateTime = request.StartDateTime,
                    DurationMinutes = request.DurationMinutes,
                    Description = request.Description ?? $"Interview meeting via Jitsi Meet - No account required, just click the link to join",
                    AttendeeEmails = request.AttendeeEmails,
                    CreatedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Jitsi Meet link for title: {Title}", request.Title);

                return Task.FromResult(new MeetingCredentialsDto
                {
                    MeetingId = Guid.NewGuid().ToString(),
                    MeetingLink = GenerateFallbackMeetingLink(),
                    Title = request.Title,
                    StartDateTime = request.StartDateTime,
                    DurationMinutes = request.DurationMinutes,
                    Description = "Meeting link will be provided via email",
                    AttendeeEmails = request.AttendeeEmails,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }


        public Task<bool> CancelMeetingAsync(string meetingId)
        {
            _logger.LogInformation("Meeting cancellation requested for ID: {MeetingId}. Link remains valid.", meetingId);
            return Task.FromResult(true);
        }

        public Task<MeetingCredentialsDto?> GetMeetingAsync(string meetingId)
        {
            _logger.LogInformation("Meeting retrieval not supported for generated links. MeetingId: {MeetingId}", meetingId);
            return Task.FromResult<MeetingCredentialsDto?>(null);
        }

        public Task<bool> IsServiceAvailableAsync()
        {
            return Task.FromResult(true);
        }

        public string GetServiceType()
        {
            return "JitsiMeet";
        }

        private string GenerateFallbackMeetingLink()
        {
            var meetingCode = Guid.NewGuid().ToString("N")[..12];
            return $"https://meet.jit.si/ROIMA-Interview-{meetingCode}";
        }

        private string SanitizeForUrl(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "Meeting";

            // Remove special characters and replace spaces with hyphens
            var sanitized = new string(input
                .Take(30) // Limit length
                .Select(c => char.IsLetterOrDigit(c) ? c : '-')
                .ToArray())
                .Trim('-');

            return string.IsNullOrWhiteSpace(sanitized) ? "Meeting" : sanitized;
        }
    }
}