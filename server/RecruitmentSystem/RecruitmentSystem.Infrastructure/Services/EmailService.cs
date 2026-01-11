using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Resend;
using RecruitmentSystem.Services.Interfaces;
using System.Text;

namespace RecruitmentSystem.Services.Implementations
{
    public class EmailService : IEmailService
    {
        private readonly IResend _resend;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public EmailService(IResend resend, IConfiguration configuration, ILogger<EmailService> logger)
        {
            _resend = resend;
            _configuration = configuration;
            _logger = logger;
            _fromEmail = _configuration["Resend:FromEmail"] ?? "onboarding@resend.dev";
            _fromName = _configuration["Resend:FromName"] ?? "ROIMA Recruitment System";

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

        #region Interview Email Methods

        public async Task<bool> SendInterviewInvitationAsync(string toEmail, string participantName, string candidateName,
            string jobTitle, DateTime scheduledDateTime, int durationMinutes, string interviewType,
            int roundNumber, string mode, string? meetingDetails, string participantRole, bool isLead, string? instructions = null)
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
            string jobTitle, DateTime originalDateTime, DateTime newDateTime, int durationMinutes,
            string mode, string? meetingDetails, bool isCandidate = false)
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
            string jobTitle, DateTime scheduledDateTime, int durationMinutes, int roundNumber,
            string? reason, bool isCandidate = false)
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
            string jobTitle, DateTime scheduledDateTime, int durationMinutes, int roundNumber,
            string interviewType, string? meetingDetails)
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

        #endregion

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

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlContent, string? textContent = null)
        {
            try
            {
                _logger.LogInformation("Attempting to send email to {Email} from {FromEmail}", toEmail, _fromEmail);

                var message = new EmailMessage
                {
                    From = $"{_fromName} <{_fromEmail}>",
                    To = toEmail,
                    Subject = subject,
                    HtmlBody = htmlContent,
                    TextBody = textContent ?? StripHtml(htmlContent)
                };

                var response = await _resend.EmailSendAsync(message);

                if (response != null)
                {
                    _logger.LogInformation("Email sent successfully to {Email} with subject: {Subject}. Response: {Response}",
                        toEmail, subject, response.ToString());
                    return true;
                }
                else
                {
                    _logger.LogError("Failed to send email to {Email}. Null response received", toEmail);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while sending email to {Email}. Exception details: {ExceptionMessage}", toEmail, ex.Message);
                return false;
            }
        }

        private string GenerateBaseEmailTemplate(string title, string bodyHtml)
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
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333333; margin: 0; padding: 0; background-color: #f4f4f4;'>
    <table width='100%' cellpadding='0' cellspacing='0' style='background-color: #f4f4f4; padding: 20px 0;'>
        <tr>
            <td align='center'>
                <table width='600' cellpadding='0' cellspacing='0' style='background-color: #ffffff; max-width: 600px;'>
                    <!-- Header -->
                    <tr>
                        <td style='padding: 30px 40px; border-bottom: 3px solid #000000;'>
                            <h1 style='margin: 0; font-size: 24px; color: #000000;'>{brandName}</h1>
                        </td>
                    </tr>
                    
                    <!-- Content -->
                    <tr>
                        <td style='padding: 40px;'>
                            {bodyHtml}
                        </td>
                    </tr>
                    
                    <!-- Footer -->
                    <tr>
                        <td style='padding: 20px 40px; background-color: #f8f8f8; border-top: 1px solid #dddddd;'>
                            <p style='margin: 0; font-size: 12px; color: #666666; text-align: center;'>
                                © {companyYear} {brandName}. All rights reserved.
                            </p>
                            <p style='margin: 5px 0 0 0; font-size: 12px; color: #666666; text-align: center;'>
                                Questions? Contact our HR team.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
        }

        private string GenerateEmailVerificationTemplate(string userName, string verificationUrl)
        {
            var body = $@"
                <h2 style='margin: 0 0 20px 0; font-size: 20px; color: #333333;'>Verify Your Email Address</h2>
                <p style='margin: 0 0 15px 0;'>Hello <strong>{userName}</strong>,</p>
                <p style='margin: 0 0 15px 0;'>Thank you for creating an account. To complete your registration, please verify your email address by clicking the button below:</p>
                <table width='100%' cellpadding='0' cellspacing='0'>
                    <tr>
                        <td align='center' style='padding: 20px 0;'>
                            <a href='{verificationUrl}' style='display: inline-block; padding: 12px 30px; background-color: #007bff; color: #ffffff; text-decoration: none; border-radius: 4px; font-weight: bold;'>Verify Email Address</a>
                        </td>
                    </tr>
                </table>
                <p style='margin: 0 0 15px 0; font-size: 14px; color: #666666;'>If the button doesn't work, copy and paste this link into your browser:</p>
                <p style='margin: 0 0 15px 0; padding: 10px; background-color: #f8f8f8; border-radius: 4px; word-break: break-all; font-size: 12px;'>{verificationUrl}</p>
                <p style='margin: 20px 0 0 0; font-size: 14px; color: #999999;'>If you didn't create an account, please ignore this email.</p>
            ";

            return GenerateBaseEmailTemplate("Verify Your Email Address", body);
        }

        private string GeneratePasswordResetTemplate(string userName, string resetUrl)
        {
            var body = $@"
                <h2 style='margin: 0 0 20px 0; font-size: 20px; color: #333333;'>Reset Your Password</h2>
                <p style='margin: 0 0 15px 0;'>Hello <strong>{userName}</strong>,</p>
                <p style='margin: 0 0 15px 0;'>You requested to reset your password. Click the button below to set a new password:</p>
                <table width='100%' cellpadding='0' cellspacing='0'>
                    <tr>
                        <td align='center' style='padding: 20px 0;'>
                            <a href='{resetUrl}' style='display: inline-block; padding: 12px 30px; background-color: #dc3545; color: #ffffff; text-decoration: none; border-radius: 4px; font-weight: bold;'>Reset Password</a>
                        </td>
                    </tr>
                </table>
                <p style='margin: 0 0 15px 0; font-size: 14px; color: #666666;'>If the button doesn't work, copy and paste this link into your browser:</p>
                <p style='margin: 0 0 15px 0; padding: 10px; background-color: #f8f8f8; border-radius: 4px; word-break: break-all; font-size: 12px;'>{resetUrl}</p>
                <div style='margin: 20px 0; padding: 15px; background-color: #fff3cd; border-left: 4px solid: #f59e0b; border-radius: 4px;'>
                    <p style='margin: 0; font-size: 14px; color: #856404;'><strong>Security Notice:</strong> This link will expire in 24 hours. If you didn't request this, please ignore this email.</p>
                </div>
            ";

            return GenerateBaseEmailTemplate("Reset Your Password", body);
        }

        private string GenerateWelcomeTemplate(string userName)
        {
            var body = $@"
                <h2 style='margin: 0 0 20px 0; font-size: 20px; color: #333333;'>Welcome!</h2>
                <p style='margin: 0 0 15px 0;'>Hello <strong>{userName}</strong>,</p>
                <p style='margin: 0 0 15px 0;'>Welcome to ROIMA Intelligence! We're excited to have you join our platform.</p>
                <div style='margin: 25px 0; padding: 20px; background-color: #f8f9fa; border-radius: 4px;'>
                    <p style='margin: 0 0 10px 0; font-size: 16px; font-weight: bold; color: #333333;'>What you can do:</p>
                    <ul style='margin: 0; padding-left: 20px;'>
                        <li style='margin-bottom: 8px;'>Browse and apply for job opportunities</li>
                        <li style='margin-bottom: 8px;'>Manage your profile and resume</li>
                        <li style='margin-bottom: 8px;'>Track your application status</li>
                        <li style='margin-bottom: 8px;'>Connect with potential employers</li>
                    </ul>
                </div>
                <p style='margin: 0 0 15px 0;'>You can now log in to your account and start exploring.</p>
                <p style='margin: 0;'>Thank you for joining us!</p>
            ";

            return GenerateBaseEmailTemplate("Welcome to ROIMA Intelligence", body);
        }

        private string GenerateBulkWelcomeTemplate(string userName, string password, bool isDefaultPassword)
        {
            var passwordSection = isDefaultPassword
                ? $@"<div style='background-color: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <h3 style='color: #856404; margin-top: 0;'>Temporary Password</h3>
                    <p style='font-family: monospace; font-size: 16px; background-color: #f8f9fa; padding: 10px; border-radius: 3px; margin: 10px 0;'>{password}</p>
                    <p style='color: #856404; font-weight: bold;'>IMPORTANT: This is a system-generated password. Please change it immediately after your first login for security reasons.</p>
                </div>"
                : $@"<div style='background-color: #d4edda; border: 1px solid #c3e6cb; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <h3 style='color: #155724; margin-top: 0;'>Your Password</h3>
                    <p>The password you were assigned has been set successfully.</p>
                    <p style='color: #155724;'>Tip: You can change your password anytime from your profile settings.</p>
                </div>";

            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>Welcome - Account Details</title>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #007bff; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
                        .content {{ background-color: #f9f9f9; padding: 30px; border-radius: 0 0 5px 5px; }}
                        .button {{ display: inline-block; padding: 12px 25px; background-color: #007bff; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
                        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #666; }}
                        .features {{ background-color: white; padding: 20px; border-radius: 5px; margin: 20px 0; }}
                        .security {{ background-color: #f8d7da; border: 1px solid #f5c6cb; padding: 15px; border-radius: 5px; margin: 20px 0; }}
                    </style>
                </head>
                <body>
                    <div class='header'>
                        <h1>Welcome to Recruitment System!</h1>
                    </div>
                    <div class='content'>
                        <h2>Hello {userName}!</h2>
                        <p>Your account has been created by our recruitment team! Welcome to ROIMA Intelligence's Recruitment System.</p>

                        {passwordSection}

                        <div class='features'>
                            <h3>Getting Started</h3>
                            <ul>
                                <li><strong>Login to your account</strong> using your email and password</li>
                                <li><strong>Complete your profile</strong> to stand out to recruiters</li>
                                <li><strong>Browse and search</strong> for jobs that match your skills</li>
                                <li><strong>Track your applications</strong> all in one place</li>
                            </ul>
                        </div>

                        <div style='text-align: center;'>
                            <a href='{(_configuration["AppSettings:CandidateDashboardUrl"] ?? "#")}' class='button'>Login to Your Account</a>
                        </div>

                        <div class='security'>
                            <h3 style='color: #721c24; margin-top: 0;'>Security Reminder</h3>
                            <p>Keep your login credentials secure and do not share them with anyone. If you suspect any unauthorized access to your account, please contact our support team immediately.</p>
                        </div>

                        <p>We're thrilled to have you join the ROIMA Intelligence community!</p>
                        <p>Best regards,<br>The ROIMA Intelligence Recruitment Team</p>
                    </div>
                    <div class='footer'>
                        <p>&copy; 2024 Recruitment System. All rights reserved.</p>
                    </div>
                </body>
                </html>";
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
            var staffDashboardUrl = _configuration["AppSettings:StaffDashboardUrl"] ?? "#";

            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>Welcome - Staff Account</title>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #007bff; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
                        .content {{ background-color: #f9f9f9; padding: 30px; border-radius: 0 0 5px 5px; }}
                        .button {{ display: inline-block; padding: 12px 25px; background-color: #007bff; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
                        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #666; }}
                        .role-badge {{ background-color: #e9ecef; color: #495057; padding: 5px 10px; border-radius: 20px; font-weight: bold; display: inline-block; margin: 10px 0; }}
                        .features {{ background-color: white; padding: 20px; border-radius: 5px; margin: 20px 0; }}
                    </style>
                </head>
                <body>
                    <div class='header'>
                        <h1>Welcome to ROIMA Intelligence!</h1>
                    </div>
                    <div class='content'>
                        <h2>Hello {userName}!</h2>
                        <p>Your staff account has been successfully created by our HR team. Welcome to ROIMA Intelligence's Recruitment System.</p>
                        <div style='text-align: center;'>
                            <span class='role-badge'>Role: {role}</span>
                        </div>
                        <div style='background-color: #f8f9fa; padding: 15px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #007bff;'>
                            <h3 style='margin-top: 0; color: #007bff;'>Login Credentials</h3>
                            <p><strong>Email:</strong> {toEmail}</p>
                            <p><strong>Password:</strong> <code style='background-color: #e9ecef; padding: 2px 6px; border-radius: 4px; font-family: monospace;'>{password}</code></p>
                            <p style='color: #dc3545; font-size: 14px;'><strong>Important:</strong> Please change your password after first login for security.</p>
                        </div>
                        <div class='features'>
                            <h3>Your Responsibilities</h3>
                            <ul>
                                <li><strong>Access the system</strong> using your email and password above</li>
                                <li><strong>Complete your staff profile</strong> with your details</li>
                                <li><strong>Manage recruitment processes</strong> based on your role permissions</li>
                                <li><strong>Collaborate with team members</strong> on hiring decisions</li>
                            </ul>
                        </div>
                        <div style='text-align: center;'>
                            <a href='{staffDashboardUrl}' class='button'>Login to Your Account</a>
                        </div>
                        <p>Please check your profile and update any necessary information. If you have any questions about your role or system access, contact the HR department.</p>
                        <p>Welcome to the team!</p>
                        <p>Best regards,<br>The ROIMA Intelligence HR Team</p>
                    </div>
                    <div class='footer'>
                        <p>&copy; 2024 Recruitment System. All rights reserved.</p>
                    </div>
                </body>
                </html>";
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

        #region Interview Email Templates

        private string GenerateInterviewInvitationTemplate(string participantName, string candidateName, string jobTitle,
            DateTime scheduledDateTime, int durationMinutes, string interviewType, int roundNumber, string mode,
            string? meetingDetails, string participantRole, bool isLead, string? instructions = null)
        {
            var leadBadge = isLead ? "<span style='background-color: #ffc107; color: #212529; padding: 2px 8px; border-radius: 10px; font-size: 12px; font-weight: bold; margin-left: 10px;'>Lead</span>" : "";

            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>Interview Invitation</title>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #007bff; color: white; padding: 25px; text-align: center; border-radius: 8px 8px 0 0; }}
                        .content {{ background-color: #f8f9fa; padding: 30px; border-radius: 0 0 8px 8px; }}
                        .interview-card {{ background-color: white; padding: 25px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #007bff; }}
                        .detail-row {{ display: flex; justify-content: space-between; padding: 8px 0; border-bottom: 1px solid #e9ecef; }}
                        .detail-label {{ font-weight: bold; color: #495057; }}
                        .detail-value {{ color: #6c757d; }}
                        .button {{ display: inline-block; padding: 12px 30px; background-color: #28a745; color: white; text-decoration: none; border-radius: 6px; margin: 20px 0; font-weight: bold; }}
                        .footer {{ text-align: center; margin-top: 30px; font-size: 14px; color: #6c757d; }}
                        .role-badge {{ background-color: #e9ecef; color: #495057; padding: 4px 12px; border-radius: 15px; font-size: 12px; font-weight: bold; }}
                        .important {{ background-color: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 6px; margin: 20px 0; }}
                    </style>
                </head>
                <body>
                    <div class='header'>
                        <h1>Interview Invitation</h1>
                        <p style='margin: 0; font-size: 16px; opacity: 0.9;'>ROIMA Intelligence Recruitment System</p>
                    </div>
                    <div class='content'>
                        <h2>Hello {participantName}!</h2>
                        <p>You have been invited to participate in an interview. Here are the details:</p>
                        
                        <div class='interview-card'>
                            <h3 style='color: #007bff; margin-top: 0;'>📋 Interview Details</h3>
                            <div class='detail-row'>
                                <span class='detail-label'>Position:</span>
                                <span class='detail-value'>{jobTitle}</span>
                            </div>
                            <div class='detail-row'>
                                <span class='detail-label'>Candidate:</span>
                                <span class='detail-value'>{candidateName}</span>
                            </div>
                            <div class='detail-row'>
                                <span class='detail-label'>Date & Time:</span>
                                <span class='detail-value'>{scheduledDateTime:dddd, MMMM dd, yyyy 'at' h:mm tt}</span>
                            </div>
                            <div class='detail-row'>
                                <span class='detail-label'>Duration:</span>
                                <span class='detail-value'>{durationMinutes} minutes</span>
                            </div>
                            <div class='detail-row'>
                                <span class='detail-label'>Interview Type:</span>
                                <span class='detail-value'>{interviewType}</span>
                            </div>
                            <div class='detail-row'>
                                <span class='detail-label'>Round:</span>
                                <span class='detail-value'>Round {roundNumber}</span>
                            </div>
                            <div class='detail-row'>
                                <span class='detail-label'>Mode:</span>
                                <span class='detail-value'>{mode}</span>
                            </div>
                            <div class='detail-row'>
                                <span class='detail-label'>Your Role:</span>
                                <span class='detail-value'>
                                    <span class='role-badge'>{participantRole}</span>
                                    {leadBadge}
                                </span>
                            </div>
                            {(string.IsNullOrEmpty(meetingDetails) ? "" : $@"
                            <div class='detail-row'>
                                <span class='detail-label'>Meeting Details:</span>
                                <span class='detail-value'>{meetingDetails}</span>
                            </div>")}
                        </div>

                        {(string.IsNullOrEmpty(instructions) ? "" : $@"
                        <div class='important'>
                            <h4 style='color: #856404; margin-top: 0;'>Instructions</h4>
                            <p>{instructions}</p>
                        </div>")}

                        <div style='text-align: center;'>
                            <a href='#' class='button'>📅 Add to Calendar</a>
                        </div>

                        <p><strong>Please confirm your attendance</strong> and prepare accordingly. If you have any questions or need to make changes, please contact the HR team.</p>
                        
                        <p>Thank you for your participation!</p>
                        <p>Best regards,<br>ROIMA Intelligence Recruitment Team</p>
                    </div>
                    <div class='footer'>
                        <p>&copy; 2024 ROIMA Intelligence. All rights reserved.</p>
                        <p>This is an automated message from the Recruitment System.</p>
                    </div>
                </body>
                </html>";
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
• Date & Time: {scheduledDateTime:dddd, MMMM dd, yyyy 'at' h:mm tt}
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
            var apologyMessage = isCandidate ?
                "We apologize for any inconvenience caused by this change. If you have any concerns or conflicts with the new time, please contact us immediately." :
                "Please ensure all participants are notified of this change and update any relevant systems or calendars.";

            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>Interview Rescheduled</title>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #ffc107; color: #212529; padding: 25px; text-align: center; border-radius: 8px 8px 0 0; }}
                        .content {{ background-color: #f8f9fa; padding: 30px; border-radius: 0 0 8px 8px; }}
                        .schedule-comparison {{ background-color: white; padding: 25px; border-radius: 8px; margin: 20px 0; }}
                        .old-schedule {{ background-color: #f8d7da; border-left: 4px solid #dc3545; padding: 15px; margin: 10px 0; border-radius: 4px; }}
                        .new-schedule {{ background-color: #d4edda; border-left: 4px solid #28a745; padding: 15px; margin: 10px 0; border-radius: 4px; }}
                        .button {{ display: inline-block; padding: 12px 30px; background-color: #007bff; color: white; text-decoration: none; border-radius: 6px; margin: 20px 0; font-weight: bold; }}
                        .footer {{ text-align: center; margin-top: 30px; font-size: 14px; color: #6c757d; }}
                        .important {{ background-color: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 6px; margin: 20px 0; }}
                    </style>
                </head>
                <body>
                    <div class='header'>
                        <h1>📅 Interview Rescheduled</h1>
                        <p style='margin: 0; font-size: 16px; opacity: 0.8;'>ROIMA Intelligence Recruitment System</p>
                    </div>
                    <div class='content'>
                        <h2>{greeting}</h2>
                        <p>Your interview has been rescheduled. Please note the updated details below:</p>

                        <div class='schedule-comparison'>
                            <h3 style='color: #007bff; margin-top: 0;'>📋 Schedule Update</h3>
                            <p><strong>Position:</strong> {jobTitle}</p>
                            {(isCandidate ? "" : $"<p><strong>Candidate:</strong> {candidateName}</p>")}
                            
                            <div class='old-schedule'>
                                <h4 style='color: #721c24; margin-top: 0;'>❌ Previous Schedule</h4>
                                <p><strong>Date & Time:</strong> {originalDateTime:dddd, MMMM dd, yyyy 'at' h:mm tt}</p>
                            </div>
                            
                            <div class='new-schedule'>
                                <h4 style='color: #155724; margin-top: 0;'>✅ New Schedule</h4>
                                <p><strong>Date & Time:</strong> {newDateTime:dddd, MMMM dd, yyyy 'at' h:mm tt}</p>
                                <p><strong>Duration:</strong> {durationMinutes} minutes</p>
                                <p><strong>Mode:</strong> {mode}</p>
                                {(string.IsNullOrEmpty(meetingDetails) ? "" : $"<p><strong>Meeting Details:</strong> {meetingDetails}</p>")}
                            </div>
                        </div>

                        <div class='important'>
                            <h4 style='color: #856404; margin-top: 0;'>Action Required</h4>
                            <p>Please update your calendar with the new interview time and confirm your availability.</p>
                        </div>

                        <p>{apologyMessage}</p>
                        
                        <p>Thank you for your understanding!</p>
                        <p>Best regards,<br>ROIMA Intelligence Recruitment Team</p>
                    </div>
                    <div class='footer'>
                        <p>&copy; 2024 ROIMA Intelligence. All rights reserved.</p>
                        <p>This is an automated message from the Recruitment System.</p>
                    </div>
                </body>
                </html>";
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
            var apologyMessage = isCandidate ?
                "We sincerely apologize for any inconvenience this may cause." :
                "Please update your calendar and notify any other stakeholders as necessary.";

            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>Interview Cancelled</title>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #dc3545; color: white; padding: 25px; text-align: center; border-radius: 8px 8px 0 0; }}
                        .content {{ background-color: #f8f9fa; padding: 30px; border-radius: 0 0 8px 8px; }}
                        .cancelled-interview {{ background-color: white; padding: 25px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #dc3545; }}
                        .detail-row {{ display: flex; justify-content: space-between; padding: 8px 0; border-bottom: 1px solid #e9ecef; }}
                        .detail-label {{ font-weight: bold; color: #495057; }}
                        .detail-value {{ color: #6c757d; }}
                        .footer {{ text-align: center; margin-top: 30px; font-size: 14px; color: #6c757d; }}
                        .reason-box {{ background-color: #f8d7da; border: 1px solid #f5c6cb; padding: 15px; border-radius: 6px; margin: 20px 0; }}
                        .contact-info {{ background-color: #d1ecf1; border: 1px solid #bee5eb; padding: 15px; border-radius: 6px; margin: 20px 0; }}
                    </style>
                </head>
                <body>
                    <div class='header'>
                        <h1>❌ Interview Cancelled</h1>
                        <p style='margin: 0; font-size: 16px; opacity: 0.9;'>ROIMA Intelligence Recruitment System</p>
                    </div>
                    <div class='content'>
                        <h2>{greeting}</h2>
                        <p>We regret to inform you that the following interview has been cancelled:</p>

                        <div class='cancelled-interview'>
                            <h3 style='color: #dc3545; margin-top: 0;'>📋 Cancelled Interview Details</h3>
                            <div class='detail-row'>
                                <span class='detail-label'>Position:</span>
                                <span class='detail-value'>{jobTitle}</span>
                            </div>
                            {(isCandidate ? "" : $@"
                            <div class='detail-row'>
                                <span class='detail-label'>Candidate:</span>
                                <span class='detail-value'>{candidateName}</span>
                            </div>")}
                            <div class='detail-row'>
                                <span class='detail-label'>Originally Scheduled:</span>
                                <span class='detail-value'>{scheduledDateTime:dddd, MMMM dd, yyyy 'at' h:mm tt}</span>
                            </div>
                            <div class='detail-row'>
                                <span class='detail-label'>Duration:</span>
                                <span class='detail-value'>{durationMinutes} minutes</span>
                            </div>
                            <div class='detail-row'>
                                <span class='detail-label'>Round:</span>
                                <span class='detail-value'>Round {roundNumber}</span>
                            </div>
                        </div>

                        {(string.IsNullOrEmpty(reason) ? "" : $@"
                        <div class='reason-box'>
                            <h4 style='color: #721c24; margin-top: 0;'>Cancellation Reason</h4>
                            <p>{reason}</p>
                        </div>")}

                        <p>{apologyMessage}</p>

                        {(isCandidate ? @"
                        <div class='contact-info'>
                            <h4 style='color: #0c5460; margin-top: 0;'>📞 Next Steps</h4>
                            <p>Our recruitment team will contact you shortly to discuss rescheduling options or provide further information about your application status.</p>
                        </div>" : "")}

                        <p>Thank you for your understanding.</p>
                        <p>Best regards,<br>ROIMA Intelligence Recruitment Team</p>
                    </div>
                    <div class='footer'>
                        <p>&copy; 2024 ROIMA Intelligence. All rights reserved.</p>
                        <p>This is an automated message from the Recruitment System.</p>
                    </div>
                </body>
                </html>";
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
            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>Evaluation Required</title>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #17a2b8; color: white; padding: 25px; text-align: center; border-radius: 8px 8px 0 0; }}
                        .content {{ background-color: #f8f9fa; padding: 30px; border-radius: 0 0 8px 8px; }}
                        .interview-summary {{ background-color: white; padding: 25px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #17a2b8; }}
                        .button {{ display: inline-block; padding: 12px 30px; background-color: #28a745; color: white; text-decoration: none; border-radius: 6px; margin: 20px 0; font-weight: bold; }}
                        .footer {{ text-align: center; margin-top: 30px; font-size: 14px; color: #6c757d; }}
                        .urgent {{ background-color: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 6px; margin: 20px 0; }}
                        .deadline {{ background-color: #f8d7da; border: 1px solid #f5c6cb; padding: 15px; border-radius: 6px; margin: 20px 0; }}
                    </style>
                </head>
                <body>
                    <div class='header'>
                        <h1>Evaluation Required</h1>
                        <p style='margin: 0; font-size: 16px; opacity: 0.9;'>ROIMA Intelligence Recruitment System</p>
                    </div>
                    <div class='content'>
                        <h2>Hello {participantName}!</h2>
                        <p>The interview you participated in has been completed. Your evaluation is now required to proceed with the recruitment process.</p>

                        <div class='interview-summary'>
                            <h3 style='color: #17a2b8; margin-top: 0;'>📋 Interview Summary</h3>
                            <p><strong>Position:</strong> {jobTitle}</p>
                            <p><strong>Candidate:</strong> {candidateName}</p>
                            <p><strong>Interview Date:</strong> {interviewDateTime:dddd, MMMM dd, yyyy 'at' h:mm tt}</p>
                            <p><strong>Round:</strong> Round {roundNumber}</p>
                            <p><strong>Type:</strong> {interviewType}</p>
                            <p><strong>Duration:</strong> {durationMinutes} minutes</p>
                        </div>

                        <div class='urgent'>
                            <h4 style='color: #856404; margin-top: 0;'>Action Required</h4>
                            <p>Please submit your evaluation as soon as possible to keep the recruitment process moving smoothly.</p>
                        </div>

                        <div class='deadline'>
                            <h4 style='color: #721c24; margin-top: 0;'>📅 Evaluation Deadline</h4>
                            <p>Please complete your evaluation within <strong>24 hours</strong> of the interview completion to ensure timely processing.</p>
                        </div>

                        <div style='text-align: center;'>
                            <a href='#' class='button'>Submit Evaluation</a>
                        </div>

                        <p>Your evaluation is crucial for making informed hiring decisions. Please provide detailed feedback about the candidate's performance, skills, and suitability for the role.</p>
                        
                        <p>If you have any questions about the evaluation process, please contact the HR team.</p>
                        
                        <p>Thank you for your participation!</p>
                        <p>Best regards,<br>ROIMA Intelligence Recruitment Team</p>
                    </div>
                    <div class='footer'>
                        <p>&copy; 2024 ROIMA Intelligence. All rights reserved.</p>
                        <p>This is an automated message from the Recruitment System.</p>
                    </div>
                </body>
                </html>";
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
            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>Lead Interviewer Assignment</title>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #6f42c1; color: white; padding: 25px; text-align: center; border-radius: 8px 8px 0 0; }}
                        .content {{ background-color: #f8f9fa; padding: 30px; border-radius: 0 0 8px 8px; }}
                        .assignment-card {{ background-color: white; padding: 25px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #6f42c1; }}
                        .responsibilities {{ background-color: #e7f3ff; border: 1px solid #b8daff; padding: 15px; border-radius: 6px; margin: 20px 0; }}
                        .button {{ display: inline-block; padding: 12px 30px; background-color: #007bff; color: white; text-decoration: none; border-radius: 6px; margin: 20px 0; font-weight: bold; }}
                        .footer {{ text-align: center; margin-top: 30px; font-size: 14px; color: #6c757d; }}
                        .lead-badge {{ background-color: #ffc107; color: #212529; padding: 5px 15px; border-radius: 15px; font-weight: bold; display: inline-block; margin: 10px 0; }}
                    </style>
                </head>
                <body>
                    <div class='header'>
                        <h1>👑 Lead Interviewer Assignment</h1>
                        <p style='margin: 0; font-size: 16px; opacity: 0.9;'>ROIMA Intelligence Recruitment System</p>
                    </div>
                    <div class='content'>
                        <h2>Hello {newLeadName}!</h2>
                        <p>You have been assigned as the <span class='lead-badge'>Lead Interviewer</span> for the following interview:</p>

                        <div class='assignment-card'>
                            <h3 style='color: #6f42c1; margin-top: 0;'>📋 Interview Assignment</h3>
                            <p><strong>Position:</strong> {jobTitle}</p>
                            <p><strong>Candidate:</strong> {candidateName}</p>
                            <p><strong>Date & Time:</strong> {scheduledDateTime:dddd, MMMM dd, yyyy 'at' h:mm tt}</p>
                            <p><strong>Duration:</strong> {durationMinutes} minutes</p>
                            <p><strong>Round:</strong> Round {roundNumber}</p>
                            <p><strong>Type:</strong> {interviewType}</p>
                            {(string.IsNullOrEmpty(meetingDetails) ? "" : $"<p><strong>Meeting Details:</strong> {meetingDetails}</p>")}
                        </div>

                        <div class='responsibilities'>
                            <h4 style='color: #004085; margin-top: 0;'>👨‍💼 Lead Interviewer Responsibilities</h4>
                            <ul>
                                <li><strong>Coordinate the interview</strong> - Guide the flow and ensure all topics are covered</li>
                                <li><strong>Manage participants</strong> - Ensure all interviewers have their questions answered</li>
                                <li><strong>Lead the evaluation</strong> - Submit the primary evaluation and coordinate with other participants</li>
                                <li><strong>Provide feedback</strong> - Give comprehensive feedback to HR and hiring managers</li>
                                <li><strong>Follow-up actions</strong> - Ensure next steps are communicated to relevant stakeholders</li>
                            </ul>
                        </div>

                        <div style='text-align: center;'>
                            <a href='#' class='button'>📅 View Interview Details</a>
                        </div>

                        <p>As the lead interviewer, you play a crucial role in the recruitment process. Please prepare thoroughly and coordinate with other participants as needed.</p>
                        
                        <p>If you have any questions or need additional information, please contact the HR team.</p>
                        
                        <p>Thank you for taking on this important responsibility!</p>
                        <p>Best regards,<br>ROIMA Intelligence Recruitment Team</p>
                    </div>
                    <div class='footer'>
                        <p>&copy; 2025 ROIMA Intelligence. All rights reserved.</p>
                        <p>This is an automated message from the Recruitment System.</p>
                    </div>
                </body>
                </html>";
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
                ? $"<div class='benefits'><h3>📋 Benefits & Perks</h3><p>{benefits}</p></div>"
                : "";

            var joiningSection = joiningDate.HasValue
                ? $"<p><strong>Expected Joining Date:</strong> {joiningDate.Value:dddd, MMMM dd, yyyy}</p>"
                : "";

            var notesSection = !string.IsNullOrEmpty(notes)
                ? $"<div class='notes'><h3>Additional notes</h3><p>{notes}</p></div>"
                : "";

            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>Job Offer</title>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #28a745; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
                        .content {{ background-color: #f9f9f9; padding: 30px; border-radius: 0 0 5px 5px; }}
                        .offer-details {{ background-color: white; padding: 20px; border-radius: 5px; margin: 20px 0; border-left: 5px solid #28a745; }}
                        .salary {{ background-color: #e8f5e8; padding: 15px; border-radius: 5px; margin: 15px 0; text-align: center; }}
                        .expiry {{ background-color: #fff3cd; border: 1px solid #ffeaa7; padding: 10px; border-radius: 5px; margin: 15px 0; }}
                        .button {{ display: inline-block; padding: 12px 25px; background-color: #28a745; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
                        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #666; }}
                        .benefits, .notes {{ background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 15px 0; }}
                    </style>
                </head>
                <body>
                    <div class='header'>
                        <h1>Congratulations</h1>
                        <p>You have received a job offer</p>
                    </div>
                    <div class='content'>
                        <h2>Dear {candidateName},</h2>
                        <p>We are pleased to extend you an offer for the position of <strong>{jobTitle}</strong> at ROIMA Intelligence.</p>
                        
                        <div class='offer-details'>
                            <h3>💼 Offer Details</h3>
                            <div class='salary'>
                                <h3 style='margin: 0; color: #28a745;'>Offered Salary</h3>
                                <p style='font-size: 24px; font-weight: bold; margin: 10px 0; color: #28a745;'>${offeredSalary:N0}</p>
                            </div>
                            
                            {joiningSection}
                            
                            <div class='expiry'>
                                <p style='margin: 0; font-weight: bold; color: #856404;'>This offer expires on: {expiryDate:dddd, MMMM dd, yyyy}</p>
                            </div>
                        </div>

                        {benefitsSection}
                        {notesSection}

                        <p>We believe you would be a valuable addition to our team and look forward to your positive response.</p>
                        
                        <p>Please review the offer carefully and let us know your decision before the expiry date.</p>
                        
                        <p>If you have any questions about this offer, please don't hesitate to contact our HR team.</p>
                        
                        <p>Welcome to the ROIMA Intelligence family!</p>
                        
                        <p>Best regards,<br>The ROIMA Intelligence Recruitment Team</p>
                    </div>
                    <div class='footer'>
                        <p>&copy; 2025 ROIMA Intelligence. All rights reserved.</p>
                    </div>
                </body>
                </html>";
        }

        private string GenerateJobOfferNotificationText(string candidateName, string jobTitle, decimal offeredSalary,
            string? benefits, DateTime expiryDate, DateTime? joiningDate, string? notes = null)
        {
            var benefitsText = !string.IsNullOrEmpty(benefits) ? $"\n\nBenefits & Perks:\n{benefits}" : "";
            var joiningText = joiningDate.HasValue ? $"\nExpected Joining Date: {joiningDate.Value:dddd, MMMM dd, yyyy}" : "";
            var notesText = !string.IsNullOrEmpty(notes) ? $"\n\nAdditional Notes:\n{notes}" : "";

            return $@"Congratulations! You have received a job offer

Dear {candidateName},

We are pleased to extend you an offer for the position of {jobTitle} at ROIMA Intelligence.

Offer Details:
Offered Salary: ${offeredSalary:N0}{joiningText}
This offer expires on: {expiryDate:dddd, MMMM dd, yyyy}{benefitsText}{notesText}

We believe you would be a valuable addition to our team and look forward to your positive response.

Please review the offer carefully and let us know your decision before the expiry date.

If you have any questions about this offer, please don't hesitate to contact our HR team.

Welcome to the ROIMA Intelligence family!

Best regards,
The ROIMA Intelligence Recruitment Team

© 2025 ROIMA Intelligence. All rights reserved.";
        }

        private string GenerateOfferExpiryReminderTemplate(string candidateName, string jobTitle, DateTime expiryDate, int daysRemaining)
        {
            var urgencyColor = daysRemaining <= 1 ? "#dc3545" : "#ffc107";
            var urgencyText = daysRemaining <= 1 ? "URGENT" : "REMINDER";

            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>Job Offer Expiry Reminder</title>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: {urgencyColor}; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
                        .content {{ background-color: #f9f9f9; padding: 30px; border-radius: 0 0 5px 5px; }}
                        .reminder {{ background-color: white; padding: 20px; border-radius: 5px; margin: 20px 0; border-left: 5px solid {urgencyColor}; }}
                        .countdown {{ background-color: #fff3cd; border: 2px solid {urgencyColor}; padding: 15px; border-radius: 5px; margin: 15px 0; text-align: center; }}
                        .button {{ display: inline-block; padding: 12px 25px; background-color: {urgencyColor}; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
                        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #666; }}
                    </style>
                </head>
                <body>
                    <div class='header'>
                        <h1>{urgencyText}</h1>
                        <p>Job Offer Expiry Reminder</p>
                    </div>
                    <div class='content'>
                        <h2>Dear {candidateName},</h2>
                        <p>This is a friendly reminder that your job offer for the <strong>{jobTitle}</strong> position is expiring soon.</p>
                        
                        <div class='reminder'>
                            <h3>📋 Offer Summary</h3>
                            <p><strong>Position:</strong> {jobTitle}</p>
                            <p><strong>Company:</strong> ROIMA Intelligence</p>
                            
                            <div class='countdown'>
                                <h3 style='margin: 0; color: {urgencyColor};'>Time Remaining</h3>
                                <p style='font-size: 20px; font-weight: bold; margin: 10px 0; color: {urgencyColor};'>{daysRemaining} day{(daysRemaining != 1 ? "s" : "")} remaining</p>
                                <p style='margin: 0; font-weight: bold;'>Expires: {expiryDate:dddd, MMMM dd, yyyy}</p>
                            </div>
                        </div>

                        <p><strong>Action Required:</strong> Please review your offer and provide your decision before the expiry date to secure your position.</p>
                        
                        <p>If you need more time to consider the offer or have any questions, please contact our HR team immediately.</p>
                        
                        <p>We're excited about the possibility of having you join our team!</p>
                        
                        <p>Best regards,<br>The ROIMA Intelligence Recruitment Team</p>
                    </div>
                    <div class='footer'>
                        <p>&copy; 2025 ROIMA Intelligence. All rights reserved.</p>
                    </div>
                </body>
                </html>";
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

© 2025 ROIMA Intelligence. All rights reserved.";
        }

        #endregion
    }
}