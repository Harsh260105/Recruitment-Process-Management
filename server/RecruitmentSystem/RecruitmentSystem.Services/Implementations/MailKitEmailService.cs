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
            string primaryColor = "#6366f1"; // Modern indigo
            string secondaryColor = "#f8fafc"; // Light slate
            string accentColor = "#10b981"; // Emerald green
            string textColor = "#1e293b"; // Dark slate
            string lightTextColor = "#64748b"; // Medium slate
            string companyYear = DateTime.Now.Year.ToString();
            // string logoUrl = "https://cdn.brandfetch.io/idSaKF6uh4/w/250/h/94/theme/dark/logo.png?c=1bxid64Mup7aczewSAYMX&t=1753093438518";

            return $@"
                <!DOCTYPE html>
                <html lang='en'>
                <head>
                    <meta charset='UTF-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>{title}</title>
                    <style>
                        @import url('https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700&display=swap');

                        * {{
                            margin: 0;
                            padding: 0;
                            box-sizing: border-box;
                        }}

                        body {{
                            font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
                            background: linear-gradient(135deg, #f8fafc 0%, #f1f5f9 100%);
                            color: {textColor};
                            line-height: 1.6;
                            min-height: 100vh;
                        }}

                        .email-wrapper {{
                            width: 100%;
                            max-width: 600px;
                            margin: 0 auto;
                            padding: 20px;
                        }}

                        .email-container {{
                            background: #ffffff;
                            border-radius: 20px;
                            overflow: hidden;
                            box-shadow: 0 20px 40px rgba(0, 0, 0, 0.1);
                            border: 1px solid rgba(255, 255, 255, 0.8);
                        }}

                        .header {{
                            background: linear-gradient(135deg, {primaryColor} 0%, #4f46e5 100%);
                            padding: 40px 30px 30px;
                            text-align: center;
                            position: relative;
                            overflow: hidden;
                        }}

                        .header::before {{
                            content: '';
                            position: absolute;
                            top: 0;
                            left: 0;
                            right: 0;
                            bottom: 0;
                            background: linear-gradient(45deg, rgba(255,255,255,0.1) 0%, rgba(255,255,255,0.05) 100%);
                            opacity: 0.8;
                        }}

                        .logo-section {{
                            margin-bottom: 20px;
                            width: 100%;
                            display: flex;
                            justify-content: center;
                            align-items: center;
                            position: relative;
                        }}

                        .logo {{
                            width: 60px;
                            height: 60px;
                            background: rgba(255, 255, 255, 0.2);
                            border-radius: 16px;
                            display: flex;
                            align-items: center;
                            justify-content: center;
                            backdrop-filter: blur(10px);
                            border: 1px solid rgba(255, 255, 255, 0.3);
                            flex-shrink: 0;
                            position: relative;
                        }}

                        .logo img {{
                            width: 40px;
                            height: 40px;
                            object-fit: contain;
                        }}

                        .header h1 {{
                            color: #ffffff;
                            font-size: 28px;
                            font-weight: 700;
                            margin: 0;
                            text-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
                            position: relative;
                            z-index: 1;
                        }}

                        .content {{
                            padding: 40px 30px;
                            background: #ffffff;
                        }}

                        .content h2 {{
                            color: {textColor};
                            font-size: 24px;
                            font-weight: 600;
                            margin-bottom: 20px;
                            line-height: 1.3;
                        }}

                        .content p {{
                            color: {lightTextColor};
                            font-size: 16px;
                            margin-bottom: 20px;
                            line-height: 1.7;
                        }}

                        .highlight-box {{
                            background: linear-gradient(135deg, #f0f9ff 0%, #e0f2fe 100%);
                            border: 1px solid #0ea5e9;
                            border-radius: 12px;
                            padding: 20px;
                            margin: 25px 0;
                            position: relative;
                        }}

                        .highlight-box::before {{
                            content: '💡';
                            position: absolute;
                            top: -10px;
                            left: 20px;
                            background: #ffffff;
                            width: 24px;
                            height: 24px;
                            border-radius: 50%;
                            display: flex;
                            align-items: center;
                            justify-content: center;
                            font-size: 12px;
                            box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
                        }}

                        .security-note {{
                            background: linear-gradient(135deg, #fef3c7 0%, #fde68a 50%, #fcd34d 100%);
                            border: 1px solid #f59e0b;
                            border-radius: 12px;
                            padding: 20px;
                            margin: 25px 0;
                            position: relative;
                        }}

                        .security-note::before {{
                            content: '🔒';
                            position: absolute;
                            top: -10px;
                            left: 20px;
                            background: #ffffff;
                            width: 24px;
                            height: 24px;
                            border-radius: 50%;
                            display: flex;
                            align-items: center;
                            justify-content: center;
                            font-size: 12px;
                            box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
                        }}

                        .button {{
                            display: inline-block;
                            padding: 16px 32px;
                            background: linear-gradient(135deg, {primaryColor} 0%, #4f46e5 100%);
                            color: #ffffff !important;
                            text-decoration: none;
                            border-radius: 12px;
                            font-weight: 600;
                            font-size: 16px;
                            text-align: center;
                            margin: 30px 0;
                            box-shadow: 0 4px 16px rgba(99, 102, 241, 0.3);
                            transition: all 0.3s ease;
                            border: none;
                            cursor: pointer;
                        }}

                        .button:hover {{
                            transform: translateY(-2px);
                            box-shadow: 0 8px 24px rgba(99, 102, 241, 0.4);
                        }}

                        .link-fallback {{
                            background: {secondaryColor};
                            border: 1px solid #e2e8f0;
                            border-radius: 8px;
                            padding: 16px;
                            margin: 20px 0;
                            font-family: 'Monaco', 'Menlo', monospace;
                            font-size: 14px;
                            color: {textColor};
                            word-break: break-all;
                            line-height: 1.4;
                        }}

                        .features-list {{
                            background: {secondaryColor};
                            border-radius: 12px;
                            padding: 25px;
                            margin: 25px 0;
                        }}

                        .features-list ul {{
                            list-style: none;
                            padding: 0;
                            margin: 0;
                        }}

                        .features-list li {{
                            padding: 8px 0;
                            position: relative;
                            padding-left: 30px;
                            color: {textColor};
                            font-weight: 500;
                        }}

                        .features-list li::before {{
                            content: '✓';
                            position: absolute;
                            left: 0;
                            top: 8px;
                            color: {accentColor};
                            font-weight: bold;
                            font-size: 16px;
                        }}

                        .footer {{
                            background: linear-gradient(135deg, #1e293b 0%, #334155 100%);
                            color: #cbd5e1;
                            padding: 30px;
                            text-align: center;
                            border-top: 1px solid #475569;
                        }}

                        .footer p {{
                            margin: 5px 0;
                            font-size: 14px;
                            color: #94a3b8;
                        }}

                        .footer .brand {{
                            color: #ffffff;
                            font-weight: 600;
                            font-size: 16px;
                        }}

                        .divider {{
                            height: 1px;
                            background: linear-gradient(90deg, transparent 0%, #e2e8f0 50%, transparent 100%);
                            margin: 30px 0;
                        }}

                        @media (max-width: 600px) {{
                            .email-wrapper {{
                                padding: 10px;
                            }}

                            .header {{
                                padding: 30px 20px 20px;
                            }}

                            .header h1 {{
                                font-size: 24px;
                            }}

                            .content {{
                                padding: 30px 20px;
                            }}

                            .button {{
                                display: block;
                                width: 100%;
                                text-align: center;
                            }}
                        }}
                    </style>
                </head>
                <body>
                    <div class='email-wrapper'>
                        <div class='email-container'>
                            <div class='header'>
                                <h1>{headerText}</h1>
                            </div>

                            <div class='content'>
                                {bodyHtml}
                            </div>

                            <div class='footer'>
                                <p class='brand'>© {companyYear} {brandName}</p>
                                <p>Leading innovation through intelligent solutions</p>
                                <div class='divider'></div>
                                <p>Questions? Contact our HR team anytime</p>
                            </div>
                        </div>
                    </div>
                </body>
                </html>";
        }

        private string GenerateEmailVerificationTemplate(string userName, string verificationUrl)
        {
            string body = $@"
                <h2>Confirm Your Email</h2>
                <p>Hello {userName},</p>
                <p>Welcome to ROIMA Intelligence! We're excited to have you join our innovative recruitment platform. Please click the button below to verify your email address and activate your account.</p>
                <div style='text-align: center;'>
                    <a href='{verificationUrl}' class='button'>✉️ Verify Email Address</a>
                </div>
                <div class='highlight-box'>
                    <p><strong>What happens next?</strong></p>
                    <ul>
                        <li>Your account will be activated immediately</li>
                        <li>You'll receive a welcome email with next steps</li>
                        <li>You can start building your profile right away</li>
                    </ul>
                </div>
                <p>If the button doesn't work, copy and paste this link into your browser:</p>
                <p class='link-fallback'>{verificationUrl}</p>
                <p>If you did not create this account, you can safely ignore this email.</p>
                <p>Best regards,<br>The ROIMA Intelligence Team</p>";

            return GenerateBaseEmailTemplate("Verify Your Email Address", "Welcome to ROIMA Intelligence!", body);
        }

        private string GeneratePasswordResetTemplate(string userName, string resetUrl)
        {
            string body = $@"
                <h2>Password Reset Request</h2>
                <p>Hello {userName},</p>
                <p>We received a request to reset the password for your account. If you made this request, click the button below to set a new password.</p>
                <div style='text-align: center;'>
                    <a href='{resetUrl}' class='button'>🔐 Reset Your Password</a>
                </div>
                <div class='security-note'>
                    <strong>Security Notice:</strong> For your protection, this link will expire in 1 hour. If you did not request a password reset, please disregard this email. Your account is still secure.
                </div>
                <p>If the button doesn't work, copy and paste this link into your browser:</p>
                <p class='link-fallback'>{resetUrl}</p>
                <p>Thank you,<br>The ROIMA Intelligence Team</p>";

            return GenerateBaseEmailTemplate("Reset Your Password", "Password Reset", body);
        }

        private string GenerateWelcomeTemplate(string userName)
        {
            string dashboardUrl = _configuration["AppSettings:DashboardUrl"] ?? "#";

            string body = $@"
                <h2>Your Account is Ready! 🎉</h2>
                <p>Hi {userName},</p>
                <p>Your email has been verified, and your account is now active. Welcome to ROIMA Intelligence's Recruitment System, where we connect exceptional talent with groundbreaking opportunities.</p>

                <div class='highlight-box'>
                    <h3 style='margin-top: 0; color: #0c4a6e;'>What's next?</h3>
                    <div class='features-list'>
                        <ul>
                            <li><strong>Complete your profile</strong> to stand out to recruiters</li>
                            <li><strong>Browse and search</strong> for jobs that match your skills</li>
                            <li><strong>Track your applications</strong> all in one place</li>
                            <li><strong>Connect with industry professionals</strong></li>
                        </ul>
                    </div>
                </div>

                <p>Click the button below to log in and get started!</p>
                <div style='text-align: center;'>
                    <a href='{dashboardUrl}' class='button'>🚀 Go to Your Dashboard</a>
                </div>
                <p>We're thrilled to have you join the ROIMA Intelligence community!</p>
                <p>Best regards,<br>The ROIMA Intelligence Team</p>";

            return GenerateBaseEmailTemplate("Welcome to ROIMA Intelligence!", "Welcome Aboard!", body);
        }

        private string GenerateBulkWelcomeTemplate(string userName, string password, bool isDefaultPassword)
        {
            string dashboardUrl = _configuration["AppSettings:DashboardUrl"] ?? "#";

            string passwordSection = isDefaultPassword
                ? $@"<div class='security-note'>
                    <strong>🔐 Your Temporary Password:</strong> <code style='background: #f1f5f9; padding: 4px 8px; border-radius: 4px; font-family: monospace;'>{password}</code>
                    <br><br>
                    <strong>⚠️ Important:</strong> This is a system-generated password. Please change it immediately after your first login for security reasons.
                </div>"
                : $@"<div class='highlight-box'>
                    <strong>🔑 Your Password:</strong> The password you were assigned has been set successfully.
                    <br><br>
                    <strong>💡 Tip:</strong> You can change your password anytime from your profile settings.
                </div>";

            string body = $@"
                <h2>Welcome to ROIMA Intelligence! 🎉</h2>
                <p>Hi {userName},</p>
                <p>Your account has been created by our recruitment team! Welcome to ROIMA Intelligence's Recruitment System, where we connect exceptional talent with groundbreaking opportunities.</p>

                {passwordSection}

                <div class='highlight-box'>
                    <h3 style='margin-top: 0; color: #0c4a6e;'>🚀 Getting Started:</h3>
                    <div class='features-list'>
                        <ul>
                            <li><strong>Login to your account</strong> using your email and password</li>
                            <li><strong>Complete your profile</strong> to stand out to recruiters</li>
                            <li><strong>Browse and search</strong> for jobs that match your skills</li>
                            <li><strong>Track your applications</strong> all in one place</li>
                        </ul>
                    </div>
                </div>

                <p>Click the button below to log in and get started!</p>
                <div style='text-align: center;'>
                    <a href='{dashboardUrl}' class='button'>🚀 Login to Your Account</a>
                </div>

                <div class='security-note'>
                    <strong>🔒 Security Reminder:</strong> Keep your login credentials secure and do not share them with anyone. If you suspect any unauthorized access to your account, please contact our support team immediately.
                </div>

                <p>We're thrilled to have you join the ROIMA Intelligence community!</p>
                <p>Best regards,<br>The ROIMA Intelligence Recruitment Team</p>";

            return GenerateBaseEmailTemplate("Welcome to ROIMA Intelligence!", "Your Account is Ready!", body);
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