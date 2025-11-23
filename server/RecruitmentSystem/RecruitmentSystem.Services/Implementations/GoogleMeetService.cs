using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RecruitmentSystem.Services.Interfaces;
using RecruitmentSystem.Shared.DTOs;

namespace RecruitmentSystem.Services.Implementations
{

    internal class MeetingServiceConfiguration
    {
        public string ServiceType { get; set; } = "";
        public string ClientId { get; set; } = "";
        public string ClientSecret { get; set; } = "";
        public string RefreshToken { get; set; } = "";
        public bool IsEnabled { get; set; } = false;
    }

    public class GoogleMeetService : IMeetingService
    {
        private readonly ILogger<GoogleMeetService> _logger;
        private readonly IConfiguration _configuration;
        private readonly MeetingServiceConfiguration _config;

        public GoogleMeetService(
            ILogger<GoogleMeetService> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;

            _config = new MeetingServiceConfiguration
            {
                ServiceType = "GoogleMeet",
                ClientId = _configuration["GoogleMeet:ClientId"] ?? "",
                ClientSecret = _configuration["GoogleMeet:ClientSecret"] ?? "",
                RefreshToken = _configuration["GoogleMeet:RefreshToken"] ?? "",
                IsEnabled = bool.TryParse(_configuration["GoogleMeet:IsEnabled"], out var isEnabled) && isEnabled
            };
        }
        public Task<MeetingCredentialsDto> CreateMeetingAsync(CreateMeetingRequestDto request)
        {
            try
            {
                var meetingCode = Guid.NewGuid().ToString("N")[..10].ToUpper();
                var meetingLink = $"https://meet.google.com/{meetingCode}";

                return Task.FromResult(new MeetingCredentialsDto
                {
                    MeetingId = Guid.NewGuid().ToString(),
                    MeetingLink = meetingLink,
                    Title = request.Title,
                    StartDateTime = request.StartDateTime,
                    DurationMinutes = request.DurationMinutes,
                    Description = request.Description ?? $"Interview meeting scheduled via Recruitment System",
                    AttendeeEmails = request.AttendeeEmails,
                    CreatedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Google Meet for title: {Title}", request.Title);

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
            return "GoogleMeet";
        }

        private string GenerateFallbackMeetingLink()
        {
            var meetingCode = Guid.NewGuid().ToString("N")[..10].ToUpper();
            return $"https://meet.google.com/{meetingCode}";
        }
    }
}