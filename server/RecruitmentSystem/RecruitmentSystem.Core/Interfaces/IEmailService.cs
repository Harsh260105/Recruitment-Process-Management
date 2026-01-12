namespace RecruitmentSystem.Services.Interfaces
{
    public interface IEmailService
    {
        // Authentication & User Management
        Task<bool> SendEmailVerificationAsync(string toEmail, string userName, string verificationToken, string verificationUrl);
        Task<bool> SendPasswordResetAsync(string toEmail, string userName, string resetToken, string resetUrl);
        Task<bool> SendWelcomeEmailAsync(string toEmail, string userName);
        Task<bool> SendStaffRegistrationEmailAsync(string toEmail, string userName, string role, string password);
        Task<bool> SendBulkWelcomeEmailAsync(string toEmail, string userName, string password, bool isDefaultPassword);

        // Interview Management
        Task<bool> SendInterviewInvitationAsync(string toEmail, string participantName, string candidateName,
            string jobTitle, DateTime scheduledDateTime, int durationMinutes, string interviewType,
            int roundNumber, string mode, string? meetingDetails, string participantRole, bool isLead, string? instructions = null);

        Task<bool> SendInterviewReschedulingAsync(string toEmail, string recipientName, string candidateName,
            string jobTitle, DateTime originalDateTime, DateTime newDateTime, int durationMinutes,
            string mode, string? meetingDetails, bool isCandidate = false);

        Task<bool> SendInterviewCancellationAsync(string toEmail, string recipientName, string candidateName,
            string jobTitle, DateTime scheduledDateTime, int durationMinutes, int roundNumber,
            string? reason, bool isCandidate = false);

        Task<bool> SendEvaluationReminderAsync(string toEmail, string participantName, string candidateName,
            string jobTitle, DateTime interviewDateTime, int roundNumber, string interviewType, int durationMinutes);

        Task<bool> SendNewLeadInterviewerNotificationAsync(string toEmail, string newLeadName, string candidateName,
            string jobTitle, DateTime scheduledDateTime, int durationMinutes, int roundNumber,
            string interviewType, string? meetingDetails);

        // Job Offer Management
        Task<bool> SendJobOfferNotificationAsync(string toEmail, string candidateName, string jobTitle,
            decimal offeredSalary, string? benefits, DateTime expiryDate, DateTime? joiningDate, string? notes = null);

        Task<bool> SendOfferExpiryReminderAsync(string toEmail, string candidateName, string jobTitle,
            DateTime expiryDate, int daysRemaining);

        // Generic Email
        Task<bool> SendEmailAsync(string toEmail, string subject, string htmlContent, string? textContent = null);
    }
}
