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

        private string GenerateEmailVerificationTemplate(string userName, string verificationUrl)
        {
            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>Email Verification</title>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #007bff; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
                        .content {{ background-color: #f9f9f9; padding: 30px; border-radius: 0 0 5px 5px; }}
                        .button {{ display: inline-block; padding: 12px 25px; background-color: #28a745; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
                        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #666; }}
                    </style>
                </head>
                <body>
                    <div class='header'>
                        <h1>Recruitment System</h1>
                    </div>
                    <div class='content'>
                        <h2>Verify Your Email Address</h2>
                        <p>Hello {userName},</p>
                        <p>Thank you for creating an account with our Recruitment System. To complete your registration, please verify your email address by clicking the button below:</p>
                        <div style='text-align: center;'>
                            <a href='{verificationUrl}' class='button'>Verify Email Address</a>
                        </div>
                        <p>If the button doesn't work, you can copy and paste the following link into your browser:</p>
                        <p style='word-break: break-all; background-color: #f0f0f0; padding: 10px; border-radius: 3px;'>{verificationUrl}</p>
                        <p>If you didn't create an account with us, please ignore this email.</p>
                        <p>Thank you!</p>
                    </div>
                    <div class='footer'>
                        <p>&copy; 2024 Recruitment System. All rights reserved.</p>
                    </div>
                </body>
                </html>";
        }

        private string GeneratePasswordResetTemplate(string userName, string resetUrl)
        {
            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>Password Reset</title>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #dc3545; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
                        .content {{ background-color: #f9f9f9; padding: 30px; border-radius: 0 0 5px 5px; }}
                        .button {{ display: inline-block; padding: 12px 25px; background-color: #dc3545; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
                        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #666; }}
                        .warning {{ background-color: #fff3cd; border: 1px solid #ffeaa7; padding: 10px; border-radius: 5px; margin: 15px 0; }}
                    </style>
                </head>
                <body>
                    <div class='header'>
                        <h1>Recruitment System</h1>
                    </div>
                    <div class='content'>
                        <h2>Reset Your Password</h2>
                        <p>Hello {userName},</p>
                        <p>You requested to reset your password for your Recruitment System account. Click the button below to set a new password:</p>
                        <div style='text-align: center;'>
                            <a href='{resetUrl}' class='button'>Reset Password</a>
                        </div>
                        <p>If the button doesn't work, you can copy and paste the following link into your browser:</p>
                        <p style='word-break: break-all; background-color: #f0f0f0; padding: 10px; border-radius: 3px;'>{resetUrl}</p>
                        <div class='warning'>
                            <strong>Security Notice:</strong> This link will expire in 24 hours for security reasons. If you didn't request this password reset, please ignore this email and your password will remain unchanged.
                        </div>
                        <p>Thank you!</p>
                    </div>
                    <div class='footer'>
                        <p>&copy; 2024 Recruitment System. All rights reserved.</p>
                    </div>
                </body>
                </html>";
        }

        private string GenerateWelcomeTemplate(string userName)
        {
            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>Welcome</title>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #28a745; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
                        .content {{ background-color: #f9f9f9; padding: 30px; border-radius: 0 0 5px 5px; }}
                        .footer {{ text-align: center; margin-top: 20px; font-size: 12px; color: #666; }}
                        .features {{ background-color: white; padding: 20px; border-radius: 5px; margin: 20px 0; }}
                    </style>
                </head>
                <body>
                    <div class='header'>
                        <h1>Welcome to Recruitment System!</h1>
                    </div>
                    <div class='content'>
                        <h2>Hello {userName}!</h2>
                        <p>Welcome to our Recruitment System! We're excited to have you join our platform.</p>
                        <div class='features'>
                            <h3>What you can do:</h3>
                            <ul>
                                <li>Browse and apply for job opportunities</li>
                                <li>Manage your profile and resume</li>
                                <li>Track your application status</li>
                                <li>Connect with potential employers</li>
                            </ul>
                        </div>
                        <p>You can now log in to your account and start exploring all the features we have to offer.</p>
                        <p>If you have any questions or need assistance, feel free to contact our support team.</p>
                        <p>Thank you for joining us!</p>
                    </div>
                    <div class='footer'>
                        <p>&copy; 2024 Recruitment System. All rights reserved.</p>
                    </div>
                </body>
                </html>";
        }

        private string GenerateBulkWelcomeTemplate(string userName, string password, bool isDefaultPassword)
        {
            var passwordSection = isDefaultPassword
                ? $@"<div style='background-color: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <h3 style='color: #856404; margin-top: 0;'>🔐 Your Temporary Password</h3>
                    <p style='font-family: monospace; font-size: 16px; background-color: #f8f9fa; padding: 10px; border-radius: 3px; margin: 10px 0;'>{password}</p>
                    <p style='color: #856404; font-weight: bold;'>⚠️ IMPORTANT: This is a system-generated password. Please change it immediately after your first login for security reasons.</p>
                </div>"
                : $@"<div style='background-color: #d4edda; border: 1px solid #c3e6cb; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <h3 style='color: #155724; margin-top: 0;'>🔑 Your Password</h3>
                    <p>The password you were assigned has been set successfully.</p>
                    <p style='color: #155724;'>💡 Tip: You can change your password anytime from your profile settings.</p>
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
                            <h3>🚀 Getting Started:</h3>
                            <ul>
                                <li><strong>Login to your account</strong> using your email and password</li>
                                <li><strong>Complete your profile</strong> to stand out to recruiters</li>
                                <li><strong>Browse and search</strong> for jobs that match your skills</li>
                                <li><strong>Track your applications</strong> all in one place</li>
                            </ul>
                        </div>

                        <div style='text-align: center;'>
                            <a href='#' class='button'>🚀 Login to Your Account</a>
                        </div>

                        <div class='security'>
                            <h3 style='color: #721c24; margin-top: 0;'>🔒 Security Reminder</h3>
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
                ? $"\n\n🔐 Your Temporary Password: {password}\n⚠️ IMPORTANT: This is a system-generated password. Please change it immediately after your first login for security reasons."
                : "\n\n🔑 Your Password: The password assigned to you has been set successfully.\n💡 Tip: You can change your password anytime from your profile settings.";

            return $@"Hello {userName},

Welcome to ROIMA Intelligence! Your account has been created by our recruitment team.

{passwordInfo}

🚀 Getting Started:
• Login to your account using your email and password
• Complete your profile to stand out to recruiters
• Browse and search for jobs that match your skills
• Track your applications all in one place

🔒 Security Reminder: Keep your login credentials secure and do not share them with anyone. If you suspect any unauthorized access to your account, please contact our support team immediately.

We're thrilled to have you join the ROIMA Intelligence community!

Best regards,
The ROIMA Intelligence Recruitment Team";
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
    }
}