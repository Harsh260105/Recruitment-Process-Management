using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RecruitmentSystem.Services.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Text;

namespace RecruitmentSystem.Services.Implementations
{
    public class MailKitEmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<MailKitEmailService> _logger;
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public MailKitEmailService(IConfiguration configuration, ILogger<MailKitEmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            _smtpHost = _configuration["MailKit:SmtpHost"] ?? throw new InvalidOperationException("MailKit SmtpHost is not configured");
            _smtpPort = int.Parse(_configuration["MailKit:SmtpPort"] ?? "587");
            _smtpUsername = _configuration["MailKit:Username"] ?? throw new InvalidOperationException("MailKit Username is not configured");
            _smtpPassword = _configuration["MailKit:Password"] ?? throw new InvalidOperationException("MailKit Password is not configured");
            _fromEmail = _configuration["MailKit:FromEmail"] ?? _smtpUsername;
            _fromName = _configuration["MailKit:FromName"] ?? "ROIMA Intelligence";
        }

        private string FormatDateTimeForEmail(DateTime utcDateTime)
        {
            try
            {
                var businessTimeZone = _configuration["AppSettings:BusinessTimeZone"] ?? "UTC";
                var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(businessTimeZone);
                var localTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, timeZoneInfo);
                var timeZoneAbbr = timeZoneInfo.IsDaylightSavingTime(localTime)
                    ? timeZoneInfo.DaylightName
                    : timeZoneInfo.StandardName;

                // Get short timezone name (e.g., "IST" for India Standard Time)
                var tzShort = string.Concat(timeZoneAbbr.Split(' ').Select(w => w[0]));

                return $"{localTime:dddd, MMMM dd, yyyy 'at' h:mm tt} {tzShort}";
            }
            catch
            {
                return $"{utcDateTime:dddd, MMMM dd, yyyy 'at' h:mm tt} UTC";
            }
        }

        public async Task<bool> SendEmailVerificationAsync(string toEmail, string userName, string verificationToken, string verificationUrl)
        {
            try
            {
                var subject = "Verify Your Email Address";
                var htmlContent = GenerateEmailVerificationTemplate(userName, verificationUrl);
                var textContent = $"Hello {userName},\n\nPlease verify your email address by clicking the following link:\n{verificationUrl}\n\nIf you didn't create an account, please ignore this email.\n\nThank you!";

                return await SendEmailAsync(toEmail, subject, htmlContent, textContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email verification to {Email}", toEmail);
                return false;
            }
        }

        public async Task<bool> SendPasswordResetAsync(string toEmail, string userName, string resetToken, string resetUrl)
        {
            try
            {
                var subject = "Reset Your Password";
                var htmlContent = GeneratePasswordResetTemplate(userName, resetUrl);
                var textContent = $"Hello {userName},\n\nYou requested to reset your password. Click the following link to reset it:\n{resetUrl}\n\nIf you didn't request this, please ignore this email.\n\nThank you!";

                return await SendEmailAsync(toEmail, subject, htmlContent, textContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to {Email}", toEmail);
                return false;
            }
        }

        public async Task<bool> SendWelcomeEmailAsync(string toEmail, string userName)
        {
            try
            {
                var subject = "Welcome to Recruitment System";
                var htmlContent = GenerateWelcomeTemplate(userName);
                var textContent = $"Hello {userName},\n\nWelcome to our Recruitment System! We're excited to have you on board.\n\nYou can now log in to your account and start exploring our platform.\n\nThank you for joining us!";

                return await SendEmailAsync(toEmail, subject, htmlContent, textContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send welcome email to {Email}", toEmail);
                return false;
            }
        }

        public async Task<bool> SendStaffRegistrationEmailAsync(string toEmail, string userName, string role, string password)
        {
            try
            {
                var subject = "Welcome to ROIMA Intelligence - Staff Account Created";
                var htmlContent = GenerateStaffRegistrationTemplate(userName, role, password, toEmail);
                var textContent = GenerateStaffRegistrationText(userName, role, password, toEmail);

                return await SendEmailAsync(toEmail, subject, htmlContent, textContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send staff registration email to {Email}", toEmail);
                return false;
            }
        }

        public async Task<bool> SendBulkWelcomeEmailAsync(string toEmail, string userName, string password, bool isDefaultPassword)
        {
            try
            {
                var subject = "Welcome to Recruitment System - Your Account Details";
                var htmlContent = GenerateBulkWelcomeTemplate(userName, password, isDefaultPassword);
                var textContent = GenerateBulkWelcomeText(userName, password, isDefaultPassword);

                return await SendEmailAsync(toEmail, subject, htmlContent, textContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send bulk welcome email to {Email}", toEmail);
                return false;
            }
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlContent, string? textContent = null)
        {
            try
            {
                _logger.LogInformation("Attempting to send email via MailKit to {Email} from {FromEmail}", toEmail, _fromEmail);

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_fromName, _fromEmail));
                message.To.Add(new MailboxAddress("", toEmail));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder();
                bodyBuilder.HtmlBody = htmlContent;
                bodyBuilder.TextBody = textContent ?? StripHtml(htmlContent);
                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();

                await client.ConnectAsync(_smtpHost, _smtpPort, SecureSocketOptions.StartTls);

                if (!string.IsNullOrEmpty(_smtpUsername) && !string.IsNullOrEmpty(_smtpPassword))
                {
                    await client.AuthenticateAsync(_smtpUsername, _smtpPassword);
                }

                // Send the message
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email sent successfully via MailKit to {Email} with subject: {Subject}", toEmail, subject);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while sending email via MailKit to {Email}. Exception details: {ExceptionMessage}", toEmail, ex.Message);
                return false;
            }
        }

        // Base Template
        private string GenerateBaseEmailTemplate(string title, string headerText, string bodyHtml)
        {
            string brandName = "ROIMA Intelligence";
            string companyYear = DateTime.Now.Year.ToString();

            return $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>{title}</title>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333333; margin: 20px; padding: 0;'>
    <div style='max-width: 600px;'>
        <h2 style='color: #000000; border-bottom: 2px solid #000000; padding-bottom: 10px;'>{brandName}</h2>
        {bodyHtml}
        <hr style='border: none; border-top: 1px solid #cccccc; margin: 30px 0;' />
        <p style='font-size: 12px; color: #666666;'>
            © {companyYear} {brandName}. All rights reserved.<br>
            Questions? Contact our HR team.
        </p>
    </div>
</body>
</html>";
        }

        private string GenerateEmailVerificationTemplate(string userName, string verificationUrl)
        {
            string body = $@"
                <h3>Confirm Your Email</h3>
                <p>Hello {userName},</p>
                <p>Welcome to ROIMA Intelligence! Please verify your email address to activate your account.</p>
                <p><a href='{verificationUrl}' style='color: #0066cc;'>Click here to verify your email</a></p>
                <p>Or copy and paste this link into your browser:<br>{verificationUrl}</p>
                <p>If you did not create this account, you can safely ignore this email.</p>
                <p>Best regards,<br>The ROIMA Intelligence Team</p>";

            return GenerateBaseEmailTemplate("Verify Your Email Address", "Welcome to ROIMA Intelligence!", body);
        }

        private string GeneratePasswordResetTemplate(string userName, string resetUrl)
        {
            string body = $@"
                <h3>Password Reset Request</h3>
                <p>Hello {userName},</p>
                <p>We received a request to reset your password. Click the link below to set a new password:</p>
                <p><a href='{resetUrl}' style='color: #0066cc;'>Reset Your Password</a></p>
                <p>Or copy and paste this link into your browser:<br>{resetUrl}</p>
                <p><strong>Note:</strong> This link will expire in 1 hour. If you did not request a password reset, please ignore this email.</p>
                <p>Thank you,<br>The ROIMA Intelligence Team</p>";

            return GenerateBaseEmailTemplate("Reset Your Password", "Password Reset", body);
        }

        private string GenerateWelcomeTemplate(string userName)
        {
            string dashboardUrl = _configuration["AppSettings:CandidateDashboardUrl"] ?? "#";

            string body = $@"
                <h3>Your account is ready</h3>
                <p>Hi {userName},</p>
                <p>Your email has been verified, and your account is now active. Welcome to ROIMA Intelligence!</p>
                <p><strong>What's next?</strong></p>
                <ul>
                    <li>Complete your profile to stand out to recruiters</li>
                    <li>Browse and search for jobs that match your skills</li>
                    <li>Track your applications all in one place</li>
                </ul>
                <p><a href='{dashboardUrl}' style='color: #0066cc;'>Go to Dashboard</a></p>
                <p>Best regards,<br>The ROIMA Intelligence Team</p>";

            return GenerateBaseEmailTemplate("Welcome to ROIMA Intelligence!", "Welcome Aboard!", body);
        }

        private string GenerateBulkWelcomeTemplate(string userName, string password, bool isDefaultPassword)
        {
            string dashboardUrl = _configuration["AppSettings:CandidateDashboardUrl"] ?? "#";

            string passwordSection = isDefaultPassword
                ? $@"<p><strong>Temporary password:</strong> {password}</p>
                    <p><strong>Important:</strong> Please change this password after your first login.</p>"
                : $@"<p><strong>Your password has been set.</strong> You can change it anytime from your profile settings.</p>";

            string body = $@"
                <h3>Welcome to ROIMA Intelligence</h3>
                <p>Hi {userName},</p>
                <p>Your account has been created by our recruitment team.</p>
                {passwordSection}
                <p><strong>Getting Started:</strong></p>
                <ul>
                    <li>Login to your account using your email and password</li>
                    <li>Complete your profile</li>
                    <li>Browse and search for jobs</li>
                    <li>Track your applications</li>
                </ul>
                <p><a href='{dashboardUrl}' style='color: #0066cc;'>Login to Your Account</a></p>
                <p>Best regards,<br>The ROIMA Intelligence Recruitment Team</p>";

            return GenerateBaseEmailTemplate("Welcome to ROIMA Intelligence!", "Your Account is Ready!", body);
        }

        private string GenerateBulkWelcomeText(string userName, string password, bool isDefaultPassword)
        {
            var passwordInfo = isDefaultPassword
                ? $"\n\nTemporary Password: {password}\nIMPORTANT: This is a system-generated password. Please change it immediately after your first login for security reasons."
                : "\n\nYour Password: The password assigned to you has been set successfully.\nTip: You can change your password anytime from your profile settings.";

            return $@"Hello {userName},

Welcome to ROIMA Intelligence! Your account has been created by our recruitment team.

{passwordInfo}

Getting Started:
• Login to your account using your email and password
• Complete your profile to stand out to recruiters
• Browse and search for jobs that match your skills
• Track your applications all in one place

Security Reminder: Keep your login credentials secure and do not share them with anyone. If you suspect any unauthorized access to your account, please contact our support team immediately.

We're thrilled to have you join the ROIMA Intelligence community!

Best regards,
The ROIMA Intelligence Recruitment Team";
        }

        private string GenerateStaffRegistrationTemplate(string userName, string role, string password, string toEmail)
        {
            string dashboardUrl = _configuration["AppSettings:StaffDashboardUrl"] ?? "#";

            string body = $@"
                <h3>Welcome to ROIMA Intelligence</h3>
                <p>Hello {userName},</p>
                <p>Your staff account has been successfully created.</p>
                <p><strong>Role:</strong> {role}</p>
                <p><strong>Login Credentials:</strong></p>
                <ul>
                    <li>Email: {toEmail}</li>
                    <li>Password: {password}</li>
                </ul>
                <p><strong>Important:</strong> Please change your password after first login.</p>
                <p><strong>Your Responsibilities:</strong></p>
                <ul>
                    <li>Access the system using your credentials</li>
                    <li>Complete your staff profile</li>
                    <li>Manage recruitment processes based on your role</li>
                    <li>Collaborate with team members on hiring decisions</li>
                </ul>
                <p><a href='{dashboardUrl}' style='color: #0066cc;'>Login to Your Account</a></p>
                <p>If you have any questions, contact the HR department.</p>
                <p>Best regards,<br>The ROIMA Intelligence HR Team</p>";

            return GenerateBaseEmailTemplate("Welcome to ROIMA Intelligence - Staff Account", "Welcome to ROIMA Intelligence!", body);
        }

        private string GenerateStaffRegistrationText(string userName, string role, string password, string toEmail)
        {
            return $@"Hello {userName},

Your staff account has been successfully created by our HR team. Welcome to ROIMA Intelligence's Recruitment System.

Role: {role}

� Your Login Credentials:
Email: {toEmail}
Password: {password}

Important: Please change your password after first login for security.

Your Responsibilities:
• Access the system using your email and password above
• Complete your staff profile with your details
• Manage recruitment processes based on your role permissions
• Collaborate with team members on hiring decisions

Please check your profile and update any necessary information. If you have any questions about your role or system access, contact the HR department.

Welcome to the team!

Best regards,
The ROIMA Intelligence HR Team";
        }

        public async Task<bool> SendInterviewInvitationAsync(string toEmail, string participantName, string candidateName,
            string jobTitle, DateTime scheduledDateTime, int durationMinutes, string interviewType, int roundNumber,
            string mode, string? meetingDetails, string participantRole, bool isLead, string? instructions = null)
        {
            try
            {
                var subject = $"Interview Invitation - {jobTitle} with {candidateName}";
                var htmlContent = GenerateInterviewInvitationTemplate(participantName, candidateName, jobTitle,
                    scheduledDateTime, durationMinutes, interviewType, roundNumber, mode, meetingDetails,
                    participantRole, isLead, instructions);
                var textContent = GenerateInterviewInvitationText(participantName, candidateName, jobTitle,
                    scheduledDateTime, durationMinutes, interviewType, roundNumber, mode, meetingDetails,
                    participantRole, isLead, instructions);

                return await SendEmailAsync(toEmail, subject, htmlContent, textContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send interview invitation to {Email}", toEmail);
                return false;
            }
        }

        public async Task<bool> SendInterviewReschedulingAsync(string toEmail, string recipientName, string candidateName,
            string jobTitle, DateTime originalDateTime, DateTime newDateTime, int durationMinutes, string mode,
            string? meetingDetails, bool isCandidate = false)
        {
            try
            {
                var subject = isCandidate ? "Your Interview Has Been Rescheduled" : "Interview Rescheduled";
                var htmlContent = GenerateInterviewReschedulingTemplate(recipientName, candidateName, jobTitle,
                    originalDateTime, newDateTime, durationMinutes, mode, meetingDetails, isCandidate);
                var textContent = GenerateInterviewReschedulingText(recipientName, candidateName, jobTitle,
                    originalDateTime, newDateTime, durationMinutes, mode, meetingDetails, isCandidate);

                return await SendEmailAsync(toEmail, subject, htmlContent, textContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send interview rescheduling notification to {Email}", toEmail);
                return false;
            }
        }

        public async Task<bool> SendInterviewCancellationAsync(string toEmail, string recipientName, string candidateName,
            string jobTitle, DateTime scheduledDateTime, int durationMinutes, int roundNumber, string? reason, bool isCandidate = false)
        {
            try
            {
                var subject = isCandidate ? "Your Interview Has Been Cancelled" : "Interview Cancelled";
                var htmlContent = GenerateInterviewCancellationTemplate(recipientName, candidateName, jobTitle,
                    scheduledDateTime, durationMinutes, roundNumber, reason, isCandidate);
                var textContent = GenerateInterviewCancellationText(recipientName, candidateName, jobTitle,
                    scheduledDateTime, durationMinutes, roundNumber, reason, isCandidate);

                return await SendEmailAsync(toEmail, subject, htmlContent, textContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send interview cancellation notification to {Email}", toEmail);
                return false;
            }
        }

        public async Task<bool> SendEvaluationReminderAsync(string toEmail, string participantName, string candidateName,
            string jobTitle, DateTime interviewDateTime, int roundNumber, string interviewType, int durationMinutes)
        {
            try
            {
                var subject = "Evaluation Required - Interview Completed";
                var htmlContent = GenerateEvaluationReminderTemplate(participantName, candidateName, jobTitle,
                    interviewDateTime, roundNumber, interviewType, durationMinutes);
                var textContent = GenerateEvaluationReminderText(participantName, candidateName, jobTitle,
                    interviewDateTime, roundNumber, interviewType, durationMinutes);

                return await SendEmailAsync(toEmail, subject, htmlContent, textContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send evaluation reminder to {Email}", toEmail);
                return false;
            }
        }

        public async Task<bool> SendNewLeadInterviewerNotificationAsync(string toEmail, string newLeadName, string candidateName,
            string jobTitle, DateTime scheduledDateTime, int durationMinutes, int roundNumber, string interviewType, string? meetingDetails)
        {
            try
            {
                var subject = $"You are now the Lead Interviewer - {jobTitle}";
                var htmlContent = GenerateNewLeadInterviewerTemplate(newLeadName, candidateName, jobTitle,
                    scheduledDateTime, durationMinutes, roundNumber, interviewType, meetingDetails);
                var textContent = GenerateNewLeadInterviewerText(newLeadName, candidateName, jobTitle,
                    scheduledDateTime, durationMinutes, roundNumber, interviewType, meetingDetails);

                return await SendEmailAsync(toEmail, subject, htmlContent, textContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send new lead interviewer notification to {Email}", toEmail);
                return false;
            }
        }

        #region Job Offer Email Methods

        public async Task<bool> SendJobOfferNotificationAsync(string toEmail, string candidateName, string jobTitle,
            decimal offeredSalary, string? benefits, DateTime expiryDate, DateTime? joiningDate, string? notes = null)
        {
            try
            {
                var subject = $"Job Offer - {jobTitle} Position";
                var htmlContent = GenerateJobOfferNotificationTemplate(candidateName, jobTitle, offeredSalary, benefits, expiryDate, joiningDate, notes);
                var textContent = GenerateJobOfferNotificationText(candidateName, jobTitle, offeredSalary, benefits, expiryDate, joiningDate, notes);

                return await SendEmailAsync(toEmail, subject, htmlContent, textContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send job offer notification to {Email}", toEmail);
                return false;
            }
        }

        public async Task<bool> SendOfferExpiryReminderAsync(string toEmail, string candidateName, string jobTitle,
            DateTime expiryDate, int daysRemaining)
        {
            try
            {
                var subject = $"Reminder: Job Offer Expires Soon - {jobTitle}";
                var htmlContent = GenerateOfferExpiryReminderTemplate(candidateName, jobTitle, expiryDate, daysRemaining);
                var textContent = GenerateOfferExpiryReminderText(candidateName, jobTitle, expiryDate, daysRemaining);

                return await SendEmailAsync(toEmail, subject, htmlContent, textContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send offer expiry reminder to {Email}", toEmail);
                return false;
            }
        }

        #endregion

        #region Interview Email Templates

        private string GenerateInterviewInvitationTemplate(string participantName, string candidateName, string jobTitle,
            DateTime scheduledDateTime, int durationMinutes, string interviewType, int roundNumber, string mode,
            string? meetingDetails, string participantRole, bool isLead, string? instructions = null)
        {
            var leadBadge = isLead ? "<span style='background-color: #ffc107; color: #212529; padding: 2px 8px; border-radius: 10px; font-size: 12px; font-weight: bold; margin-left: 10px;'>Lead</span>" : "";
            var meetingDetailsSection = string.IsNullOrEmpty(meetingDetails) ? "" : $"<p><strong>Meeting Details:</strong> {meetingDetails}</p>";
            var instructionsSection = string.IsNullOrEmpty(instructions) ? "" : $@"
                <div class='highlight-box'>
                    <h3 style='margin-top: 0; color: #0c4a6e;'>Instructions</h3>
                    <p>{instructions}</p>
                </div>";

            string body = $@"
                <h2>Interview Invitation</h2>
                <p>Hello {participantName}!</p>
                <p>You have been invited to participate in an interview. Here are the details:</p>
                
                <div class='highlight-box'>
                    <h3 style='margin-top: 0; color: #0c4a6e;'>📋 Interview Details</h3>
                    <p><strong>Position:</strong> {jobTitle}</p>
                    <p><strong>Candidate:</strong> {candidateName}</p>
                    <p><strong>Date & Time:</strong> {FormatDateTimeForEmail(scheduledDateTime)}</p>
                    <p><strong>Duration:</strong> {durationMinutes} minutes</p>
                    <p><strong>Interview Type:</strong> {interviewType}</p>
                    <p><strong>Round:</strong> Round {roundNumber}</p>
                    <p><strong>Mode:</strong> {mode}</p>
                    <p><strong>Your Role:</strong> {participantRole} {leadBadge}</p>
                    {meetingDetailsSection}
                </div>

                {instructionsSection}

                <p><strong>Please confirm your attendance</strong> and prepare accordingly. If you have any questions or need to make changes, please contact the HR team.</p>
                
                <p>Thank you for your participation!</p>
                <p>Best regards,<br>ROIMA Intelligence Recruitment Team</p>";

            return GenerateBaseEmailTemplate("Interview Invitation", "Interview Invitation", body);
        }

        private string GenerateInterviewInvitationText(string participantName, string candidateName, string jobTitle,
            DateTime scheduledDateTime, int durationMinutes, string interviewType, int roundNumber, string mode,
            string? meetingDetails, string participantRole, bool isLead, string? instructions = null)
        {
            var leadText = isLead ? " (Lead)" : "";
            var meetingText = string.IsNullOrEmpty(meetingDetails) ? "" : $"\nMeeting Details: {meetingDetails}";
            var instructionsText = string.IsNullOrEmpty(instructions) ? "" : $"\n\nInstructions:\n{instructions}";

            return $@"Hello {participantName},

You have been invited to participate in an interview.

Interview Details:
• Position: {jobTitle}
• Candidate: {candidateName}
• Date & Time: {FormatDateTimeForEmail(scheduledDateTime)}
• Duration: {durationMinutes} minutes
• Interview Type: {interviewType}
• Round: Round {roundNumber}
• Mode: {mode}
• Your Role: {participantRole}{leadText}{meetingText}{instructionsText}

Please confirm your attendance and prepare accordingly. If you have any questions or need to make changes, please contact the HR team.

Thank you for your participation!

Best regards,
ROIMA Intelligence Recruitment Team";
        }

        private string GenerateInterviewReschedulingTemplate(string recipientName, string candidateName, string jobTitle,
            DateTime originalDateTime, DateTime newDateTime, int durationMinutes, string mode, string? meetingDetails, bool isCandidate = false)
        {
            var greeting = isCandidate ? $"Dear {recipientName}," : $"Hello {recipientName},";
            var candidateInfo = isCandidate ? "" : $"<p><strong>Candidate:</strong> {candidateName}</p>";
            var meetingDetailsSection = string.IsNullOrEmpty(meetingDetails) ? "" : $"<p><strong>Meeting Details:</strong> {meetingDetails}</p>";
            var apologyMessage = isCandidate ?
                "We apologize for any inconvenience caused by this change. If you have any concerns or conflicts with the new time, please contact us immediately." :
                "Please ensure all participants are notified of this change and update any relevant systems or calendars.";

            string body = $@"
                <h2>Interview Rescheduled 📅</h2>
                <p>{greeting}</p>
                <p>Your interview has been rescheduled. Please note the updated details below:</p>

                <div class='highlight-box'>
                    <h3 style='margin-top: 0; color: #0c4a6e;'>📋 Schedule Update</h3>
                    <p><strong>Position:</strong> {jobTitle}</p>
                    {candidateInfo}
                    
                    <div style='background-color: #f8d7da; border-left: 4px solid #dc3545; padding: 15px; margin: 10px 0; border-radius: 4px;'>
                        <h4 style='color: #721c24; margin-top: 0;'>❌ Previous Schedule</h4>
                        <p><strong>Date & Time:</strong> {FormatDateTimeForEmail(originalDateTime)}</p>
                    </div>
                    
                    <div style='background-color: #d4edda; border-left: 4px solid #28a745; padding: 15px; margin: 10px 0; border-radius: 4px;'>
                        <h4 style='color: #155724; margin-top: 0;'>✅ New Schedule</h4>
                        <p><strong>Date & Time:</strong> {FormatDateTimeForEmail(newDateTime)}</p>
                        <p><strong>Duration:</strong> {durationMinutes} minutes</p>
                        <p><strong>Mode:</strong> {mode}</p>
                        {meetingDetailsSection}
                    </div>
                </div>

                <div class='security-note'>
                    <strong>Action Required:</strong> Please update your calendar with the new interview time and confirm your availability.
                </div>

                <p>{apologyMessage}</p>
                
                <p>Thank you for your understanding!</p>
                <p>Best regards,<br>ROIMA Intelligence Recruitment Team</p>";

            return GenerateBaseEmailTemplate("Interview Rescheduled", "Interview Rescheduled 📅", body);
        }

        private string GenerateInterviewReschedulingText(string recipientName, string candidateName, string jobTitle,
            DateTime originalDateTime, DateTime newDateTime, int durationMinutes, string mode, string? meetingDetails, bool isCandidate = false)
        {
            var greeting = isCandidate ? $"Dear {recipientName}," : $"Hello {recipientName},";
            var candidateInfo = isCandidate ? "" : $"\nCandidate: {candidateName}";
            var meetingText = string.IsNullOrEmpty(meetingDetails) ? "" : $"\nMeeting Details: {meetingDetails}";
            var apologyMessage = isCandidate ?
                "We apologize for any inconvenience caused by this change. If you have any concerns or conflicts with the new time, please contact us immediately." :
                "Please ensure all participants are notified of this change and update any relevant systems or calendars.";

            return $@"{greeting}

Your interview has been rescheduled. Please note the updated details below:

Position: {jobTitle}{candidateInfo}

Previous Schedule:
Date & Time: {originalDateTime:dddd, MMMM dd, yyyy 'at' h:mm tt}

New Schedule:
Date & Time: {newDateTime:dddd, MMMM dd, yyyy 'at' h:mm tt}
Duration: {durationMinutes} minutes
Mode: {mode}{meetingText}

Please update your calendar with the new interview time and confirm your availability.

{apologyMessage}

Thank you for your understanding!

Best regards,
ROIMA Intelligence Recruitment Team";
        }

        private string GenerateInterviewCancellationTemplate(string recipientName, string candidateName, string jobTitle,
            DateTime scheduledDateTime, int durationMinutes, int roundNumber, string? reason, bool isCandidate = false)
        {
            var greeting = isCandidate ? $"Dear {recipientName}," : $"Hello {recipientName},";
            var candidateInfo = isCandidate ? "" : $"<p><strong>Candidate:</strong> {candidateName}</p>";
            var reasonSection = string.IsNullOrEmpty(reason) ? "" : $@"
                <div style='background-color: #f8d7da; border: 1px solid #f5c6cb; padding: 15px; border-radius: 6px; margin: 20px 0;'>
                    <h4 style='color: #721c24; margin-top: 0;'>Cancellation Reason</h4>
                    <p>{reason}</p>
                </div>";
            var apologyMessage = isCandidate ?
                "We sincerely apologize for any inconvenience this may cause." :
                "Please update your calendar and notify any other stakeholders as necessary.";
            var nextSteps = isCandidate ? @"
                <div class='highlight-box'>
                    <h3 style='margin-top: 0; color: #0c4a6e;'>📞 Next Steps</h3>
                    <p>Our recruitment team will contact you shortly to discuss rescheduling options or provide further information about your application status.</p>
                </div>" : "";

            string body = $@"
                <h2>Interview Cancelled ❌</h2>
                <p>{greeting}</p>
                <p>We regret to inform you that the following interview has been cancelled:</p>

                <div class='highlight-box'>
                    <h3 style='margin-top: 0; color: #dc3545;'>📋 Cancelled Interview Details</h3>
                    <p><strong>Position:</strong> {jobTitle}</p>
                    {candidateInfo}
                    <p><strong>Originally Scheduled:</strong> {scheduledDateTime:dddd, MMMM dd, yyyy 'at' h:mm tt}</p>
                    <p><strong>Duration:</strong> {durationMinutes} minutes</p>
                    <p><strong>Round:</strong> Round {roundNumber}</p>
                </div>

                {reasonSection}

                <p>{apologyMessage}</p>

                {nextSteps}

                <p>Thank you for your understanding.</p>
                <p>Best regards,<br>ROIMA Intelligence Recruitment Team</p>";

            return GenerateBaseEmailTemplate("Interview Cancelled", "Interview Cancelled ❌", body);
        }

        private string GenerateInterviewCancellationText(string recipientName, string candidateName, string jobTitle,
            DateTime scheduledDateTime, int durationMinutes, int roundNumber, string? reason, bool isCandidate = false)
        {
            var greeting = isCandidate ? $"Dear {recipientName}," : $"Hello {recipientName},";
            var candidateInfo = isCandidate ? "" : $"\nCandidate: {candidateName}";
            var reasonText = string.IsNullOrEmpty(reason) ? "" : $"\nReason: {reason}";
            var apologyMessage = isCandidate ?
                "We sincerely apologize for any inconvenience this may cause." :
                "Please update your calendar and notify any other stakeholders as necessary.";
            var nextSteps = isCandidate ?
                "\n\nNext Steps:\nOur recruitment team will contact you shortly to discuss rescheduling options or provide further information about your application status." : "";

            return $@"{greeting}

We regret to inform you that the following interview has been cancelled:

Position: {jobTitle}{candidateInfo}
Originally Scheduled: {scheduledDateTime:dddd, MMMM dd, yyyy 'at' h:mm tt}
Duration: {durationMinutes} minutes
Round: Round {roundNumber}{reasonText}

{apologyMessage}{nextSteps}

Thank you for your understanding.

Best regards,
ROIMA Intelligence Recruitment Team";
        }

        private string GenerateEvaluationReminderTemplate(string participantName, string candidateName, string jobTitle,
            DateTime interviewDateTime, int roundNumber, string interviewType, int durationMinutes)
        {
            string body = $@"
                <h2>Evaluation Required</h2>
                <p>Hello {participantName}!</p>
                <p>The interview you participated in has been completed. Your evaluation is now required to proceed with the recruitment process.</p>

                <div class='highlight-box'>
                    <h3 style='margin-top: 0; color: #17a2b8;'>📋 Interview Summary</h3>
                    <p><strong>Position:</strong> {jobTitle}</p>
                    <p><strong>Candidate:</strong> {candidateName}</p>
                    <p><strong>Interview Date:</strong> {interviewDateTime:dddd, MMMM dd, yyyy 'at' h:mm tt}</p>
                    <p><strong>Round:</strong> Round {roundNumber}</p>
                    <p><strong>Type:</strong> {interviewType}</p>
                    <p><strong>Duration:</strong> {durationMinutes} minutes</p>
                </div>

                <div class='security-note'>
                    <strong>Action Required:</strong> Please submit your evaluation as soon as possible to keep the recruitment process moving smoothly.
                </div>

                <div style='background-color: #f8d7da; border: 1px solid #f5c6cb; padding: 15px; border-radius: 6px; margin: 20px 0;'>
                    <h4 style='color: #721c24; margin-top: 0;'>📅 Evaluation Deadline</h4>
                    <p>Please complete your evaluation within <strong>24 hours</strong> of the interview completion to ensure timely processing.</p>
                </div>

                <p>Your evaluation is crucial for making informed hiring decisions. Please provide detailed feedback about the candidate's performance, skills, and suitability for the role.</p>
                
                <p>If you have any questions about the evaluation process, please contact the HR team.</p>
                
                <p>Thank you for your participation!</p>
                <p>Best regards,<br>ROIMA Intelligence Recruitment Team</p>";

            return GenerateBaseEmailTemplate("Evaluation Required", "Evaluation Required", body);
        }

        private string GenerateEvaluationReminderText(string participantName, string candidateName, string jobTitle,
            DateTime interviewDateTime, int roundNumber, string interviewType, int durationMinutes)
        {
            return $@"Hello {participantName}!

The interview you participated in has been completed. Your evaluation is now required to proceed with the recruitment process.

Interview Summary:
• Position: {jobTitle}
• Candidate: {candidateName}
• Interview Date: {interviewDateTime:dddd, MMMM dd, yyyy 'at' h:mm tt}
• Round: Round {roundNumber}
• Type: {interviewType}
• Duration: {durationMinutes} minutes

Action Required:
Please submit your evaluation as soon as possible to keep the recruitment process moving smoothly.

Evaluation Deadline:
Please complete your evaluation within 24 hours of the interview completion to ensure timely processing.

Your evaluation is crucial for making informed hiring decisions. Please provide detailed feedback about the candidate's performance, skills, and suitability for the role.

If you have any questions about the evaluation process, please contact the HR team.

Thank you for your participation!

Best regards,
ROIMA Intelligence Recruitment Team";
        }

        private string GenerateNewLeadInterviewerTemplate(string newLeadName, string candidateName, string jobTitle,
            DateTime scheduledDateTime, int durationMinutes, int roundNumber, string interviewType, string? meetingDetails)
        {
            var meetingDetailsSection = string.IsNullOrEmpty(meetingDetails) ? "" : $"<p><strong>Meeting Details:</strong> {meetingDetails}</p>";

            string body = $@"
                <h2>Lead Interviewer Assignment 👑</h2>
                <p>Hello {newLeadName}!</p>
                <p>You have been assigned as the <span style='background-color: #ffc107; color: #212529; padding: 5px 15px; border-radius: 15px; font-weight: bold; display: inline-block; margin: 10px 0;'>Lead Interviewer</span> for the following interview:</p>

                <div class='highlight-box'>
                    <h3 style='margin-top: 0; color: #6f42c1;'>📋 Interview Assignment</h3>
                    <p><strong>Position:</strong> {jobTitle}</p>
                    <p><strong>Candidate:</strong> {candidateName}</p>
                    <p><strong>Date & Time:</strong> {scheduledDateTime:dddd, MMMM dd, yyyy 'at' h:mm tt}</p>
                    <p><strong>Duration:</strong> {durationMinutes} minutes</p>
                    <p><strong>Round:</strong> Round {roundNumber}</p>
                    <p><strong>Type:</strong> {interviewType}</p>
                    {meetingDetailsSection}
                </div>

                <div class='features-list'>
                    <h3 style='margin-top: 0; color: #004085;'>👨‍💼 Lead Interviewer Responsibilities</h3>
                    <ul>
                        <li><strong>Coordinate the interview</strong> - Guide the flow and ensure all topics are covered</li>
                        <li><strong>Manage participants</strong> - Ensure all interviewers have their questions answered</li>
                        <li><strong>Lead the evaluation</strong> - Submit the primary evaluation and coordinate with other participants</li>
                        <li><strong>Provide feedback</strong> - Give comprehensive feedback to HR and hiring managers</li>
                        <li><strong>Follow-up actions</strong> - Ensure next steps are communicated to relevant stakeholders</li>
                    </ul>
                </div>

                <p>As the lead interviewer, you play a crucial role in the recruitment process. Please prepare thoroughly and coordinate with other participants as needed.</p>
                
                <p>If you have any questions or need additional information, please contact the HR team.</p>
                
                <p>Thank you for taking on this important responsibility!</p>
                <p>Best regards,<br>ROIMA Intelligence Recruitment Team</p>";

            return GenerateBaseEmailTemplate("Lead Interviewer Assignment", "Lead Interviewer Assignment 👑", body);
        }

        private string GenerateNewLeadInterviewerText(string newLeadName, string candidateName, string jobTitle,
            DateTime scheduledDateTime, int durationMinutes, int roundNumber, string interviewType, string? meetingDetails)
        {
            var meetingText = string.IsNullOrEmpty(meetingDetails) ? "" : $"\nMeeting Details: {meetingDetails}";

            return $@"Hello {newLeadName}!

You have been assigned as the Lead Interviewer for the following interview:

Interview Assignment:
• Position: {jobTitle}
• Candidate: {candidateName}
• Date & Time: {scheduledDateTime:dddd, MMMM dd, yyyy 'at' h:mm tt}
• Duration: {durationMinutes} minutes
• Round: Round {roundNumber}
• Type: {interviewType}{meetingText}

Lead Interviewer Responsibilities:
• Coordinate the interview - Guide the flow and ensure all topics are covered
• Manage participants - Ensure all interviewers have their questions answered
• Lead the evaluation - Submit the primary evaluation and coordinate with other participants
• Provide feedback - Give comprehensive feedback to HR and hiring managers
• Follow-up actions - Ensure next steps are communicated to relevant stakeholders

As the lead interviewer, you play a crucial role in the recruitment process. Please prepare thoroughly and coordinate with other participants as needed.

If you have any questions or need additional information, please contact the HR team.

Thank you for taking on this important responsibility!

Best regards,
ROIMA Intelligence Recruitment Team";
        }

        #endregion

        #region Job Offer Email Templates

        private string GenerateJobOfferNotificationTemplate(string candidateName, string jobTitle, decimal offeredSalary,
            string? benefits, DateTime expiryDate, DateTime? joiningDate, string? notes = null)
        {
            var benefitsSection = !string.IsNullOrEmpty(benefits)
                ? $"<div class='info-card'><div class='info-header'><strong>📋 Benefits & Perks</strong></div><div class='info-content'>{benefits}</div></div>"
                : "";

            var joiningSection = joiningDate.HasValue
                ? $"<div class='detail-item'><strong>Expected Joining Date:</strong> {joiningDate.Value:dddd, MMMM dd, yyyy}</div>"
                : "";

            var notesSection = !string.IsNullOrEmpty(notes)
                ? $"<div class='info-card'><div class='info-header'><strong>Additional notes</strong></div><div class='info-content'>{notes}</div></div>"
                : "";

            var bodyContent = $@"
                <div class='content-section'>
                    <h2 style='color: #1e293b; margin-bottom: 20px;'>Dear {candidateName},</h2>
                    <p style='margin-bottom: 20px;'>We are delighted to extend you an offer for the position of <strong>{jobTitle}</strong> at ROIMA Intelligence.</p>
                    
                    <div class='highlight-box'>
                        <div class='salary-display'>
                            <div class='salary-label'>Offered Salary</div>
                            <div class='salary-amount'>${offeredSalary:N0}</div>
                        </div>
                        {joiningSection}
                        <div class='expiry-notice'>
                            <strong>This offer expires on: {expiryDate:dddd, MMMM dd, yyyy}</strong>
                        </div>
                    </div>

                    {benefitsSection}
                    {notesSection}

                    <p>We believe you would be a valuable addition to our team and look forward to your positive response.</p>
                    <p>Please review the offer carefully and let us know your decision before the expiry date.</p>
                    <p>If you have any questions about this offer, please don't hesitate to contact our HR team.</p>
                    <p style='font-weight: 600; color: #10b981;'>Welcome to the ROIMA Intelligence family!</p>
                </div>";

            return GenerateBaseEmailTemplate("Job Offer", "Congratulations — You have received a job offer", bodyContent);
        }

        private string GenerateJobOfferNotificationText(string candidateName, string jobTitle, decimal offeredSalary,
            string? benefits, DateTime expiryDate, DateTime? joiningDate, string? notes = null)
        {
            var benefitsText = !string.IsNullOrEmpty(benefits) ? $"\n\nBenefits & Perks:\n{benefits}" : "";
            var joiningText = joiningDate.HasValue ? $"\nExpected Joining Date: {joiningDate.Value:dddd, MMMM dd, yyyy}" : "";
            var notesText = !string.IsNullOrEmpty(notes) ? $"\n\nAdditional Notes:\n{notes}" : "";

            return $@"Congratulations! You have received a job offer

Dear {candidateName},

We are delighted to extend you an offer for the position of {jobTitle} at ROIMA Intelligence.

Offer Details:
Offered Salary: ${offeredSalary:N0}{joiningText}
This offer expires on: {expiryDate:dddd, MMMM dd, yyyy}{benefitsText}{notesText}

We believe you would be a valuable addition to our team and look forward to your positive response.

Please review the offer carefully and let us know your decision before the expiry date.

If you have any questions about this offer, please don't hesitate to contact our HR team.

Welcome to the ROIMA Intelligence family!

Best regards,
The ROIMA Intelligence Recruitment Team

© 2024 ROIMA Intelligence. All rights reserved.";
        }

        private string GenerateOfferExpiryReminderTemplate(string candidateName, string jobTitle, DateTime expiryDate, int daysRemaining)
        {
            var urgencyColor = daysRemaining <= 1 ? "#ef4444" : "#f59e0b";
            var urgencyText = daysRemaining <= 1 ? "URGENT" : "REMINDER";

            var bodyContent = $@"
                <div class='content-section'>
                    <h2 style='color: #1e293b; margin-bottom: 20px;'>Dear {candidateName},</h2>
                    <p style='margin-bottom: 20px;'>This is a friendly reminder that your job offer for the <strong>{jobTitle}</strong> position is expiring soon.</p>
                    
                    <div class='highlight-box' style='border-left-color: {urgencyColor};'>
                        <div class='detail-item'><strong>Position:</strong> {jobTitle}</div>
                        <div class='detail-item'><strong>Company:</strong> ROIMA Intelligence</div>
                        
                        <div style='background: linear-gradient(135deg, #fef3c7 0%, #fde68a 100%); padding: 20px; border-radius: 8px; text-align: center; margin: 20px 0; border: 2px solid {urgencyColor};'>
                            <div style='color: {urgencyColor}; font-weight: 700; font-size: 18px; margin-bottom: 8px;'>Time Remaining</div>
                            <div style='color: {urgencyColor}; font-weight: 800; font-size: 24px; margin: 10px 0;'>{daysRemaining} day{(daysRemaining != 1 ? "s" : "")} remaining</div>
                            <div style='font-weight: 600;'>Expires: {expiryDate:dddd, MMMM dd, yyyy}</div>
                        </div>
                    </div>

                    <div class='info-card' style='background: #fef2f2; border-left-color: {urgencyColor};'>
                        <div class='info-header' style='color: #991b1b;'><strong>Action Required</strong></div>
                        <div class='info-content'>Please review your offer and provide your decision before the expiry date to secure your position.</div>
                    </div>

                    <p>If you need more time to consider the offer or have any questions, please contact our HR team immediately.</p>
                    <p style='font-weight: 600; color: #10b981;'>We're excited about the possibility of having you join our team!</p>
                </div>";

            return GenerateBaseEmailTemplate("Job Offer Expiry Reminder", $"{urgencyText}: Job Offer Expiry Reminder", bodyContent);
        }

        private string GenerateOfferExpiryReminderText(string candidateName, string jobTitle, DateTime expiryDate, int daysRemaining)
        {
            var urgencyText = daysRemaining <= 1 ? "URGENT" : "REMINDER";

            return $@"{urgencyText}: Job Offer Expiry Reminder

Dear {candidateName},

This is a friendly reminder that your job offer for the {jobTitle} position is expiring soon.

Offer Summary:
Position: {jobTitle}
Company: ROIMA Intelligence

Time Remaining: {daysRemaining} day{(daysRemaining != 1 ? "s" : "")} remaining
Expires: {expiryDate:dddd, MMMM dd, yyyy}

Action Required: Please review your offer and provide your decision before the expiry date to secure your position.

If you need more time to consider the offer or have any questions, please contact our HR team immediately.

We're excited about the possibility of having you join our team!

Best regards,
The ROIMA Intelligence Recruitment Team

© 2024 ROIMA Intelligence. All rights reserved.";
        }

        #endregion

        private static string StripHtml(string html)
        {
            if (string.IsNullOrEmpty(html))
                return string.Empty;

            var result = new StringBuilder();
            bool insideTag = false;

            for (int i = 0; i < html.Length; i++)
            {
                char c = html[i];
                if (c == '<')
                {
                    insideTag = true;
                }
                else if (c == '>')
                {
                    insideTag = false;
                }
                else if (!insideTag)
                {
                    result.Append(c);
                }
            }

            return result.ToString().Trim();
        }
    }
}