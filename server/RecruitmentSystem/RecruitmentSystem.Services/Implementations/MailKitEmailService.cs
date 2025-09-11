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
            _fromName = _configuration["MailKit:FromName"] ?? "ROIMA Recruitment System";
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
            string brandName = "ROIMA Recruitment System";
            string brandColor = "#28A745"; 
            string headerTextColor = "#FFFFFF"; 
            string companyYear = DateTime.Now.Year.ToString();
            string logoUrl = "https://cdn.brandfetch.io/idSaKF6uh4/w/250/h/94/theme/dark/logo.png?c=1bxid64Mup7aczewSAYMX&t=1753093438518";

            return $@"
                <!DOCTYPE html>
                <html lang='en'>
                <head>
                    <meta charset='UTF-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>{title}</title>
                    <style>
                        body {{
                            margin: 0;
                            padding: 0;
                            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif, 'Apple Color Emoji', 'Segoe UI Emoji', 'Segoe UI Symbol';
                            background-color: #f4f4f7; /* Light grey background for the entire email */
                            color: #333; /* Dark grey for main text */
                        }}
                        .container {{
                            max-width: 600px;
                            margin: 20px auto;
                            background-color: #ffffff; /* White background for the main content area */
                            border: 1px solid #e2e8f0; /* Light border around the container */
                            border-radius: 8px;
                            overflow: hidden;
                            box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06); /* Subtle shadow for depth */
                        }}
                        .header {{
                            background-color: {brandColor}; /* Green header */
                            padding: 20px;
                            text-align: center;
                            color: {headerTextColor}; /* White text for header */
                        }}
                        .header h1 {{
                            margin: 0;
                            font-size: 24px;
                        }}
                        .content {{
                            padding: 30px;
                            line-height: 1.6;
                        }}
                        .content h2 {{
                            color: #1a202c; /* Darker heading color for content */
                            margin-top: 0;
                        }}
                        .button {{
                            display: inline-block;
                            padding: 12px 25px;
                            background-color: {brandColor}; /* Green button */
                            color: #ffffff;
                            text-decoration: none;
                            border-radius: 5px;
                            font-weight: bold;
                            margin: 20px 0;
                            transition: background-color 0.2s ease-in-out; /* Smooth hover effect */
                        }}
                        .button:hover {{
                            background-color: #218838; /* Slightly darker green on hover */
                        }}
                        .link-fallback {{
                            word-break: break-all;
                            background-color: #f7fafc; /* Very light grey background for link */
                            padding: 10px;
                            border-radius: 4px;
                            font-size: 12px;
                            color: #718096; /* Muted text color for fallback link */
                        }}
                        .footer {{
                            text-align: center;
                            padding: 20px;
                            font-size: 12px;
                            color: #a0aec0; /* Lighter grey for footer text */
                            background-color: #f9f9f9; /* Light grey background for footer */
                            border-top: 1px solid #edf2f7; /* Subtle border above footer */
                        }}
                        .security-note {{
                            background-color: #fffbeb; /* Light yellow background */
                            border: 1px solid #fde68a; /* Yellow border */
                            padding: 15px;
                            border-radius: 5px;
                            margin: 15px 0;
                            font-size: 14px;
                            color: #744210; /* Darker text for warning */
                        }}
                        ul {{
                            list-style-type: none; /* Remove default bullet points */
                            padding: 0;
                            margin: 15px 0;
                        }}
                        ul li {{
                            padding-left: 1.5em;
                            position: relative;
                            margin-bottom: 8px;
                        }}
                        ul li:before {{
                            content: '•'; /* Custom bullet point */
                            position: absolute;
                            left: 0;
                            color: {brandColor}; /* Green bullet point */
                            font-weight: bold;
                        }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>{headerText}</h1>
                        </div>
                        <div class='content'>
                            {bodyHtml}
                        </div>
                        <div class='footer'>
                            <p>&copy; {companyYear} {brandName}. All rights reserved.</p>
                            <p>If you have any questions, please contact our support team.</p>
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
                <p>Welcome to ROIMA! We're excited to have you on board. Please click the button below to verify your email address and activate your account.</p>
                <div style='text-align: center;'>
                    <a href='{verificationUrl}' class='button'>Verify Email Address</a>
                </div>
                <p>If the button doesn't work, copy and paste this link into your browser:</p>
                <p class='link-fallback'>{verificationUrl}</p>
                <p>If you did not create this account, you can safely ignore this email.</p>
                <p>Best regards,<br>The ROIMA Team</p>";

            return GenerateBaseEmailTemplate("Verify Your Email Address", "Welcome to ROIMA!", body);
        }

        private string GeneratePasswordResetTemplate(string userName, string resetUrl)
        {
            string body = $@"
                <h2>Password Reset Request</h2>
                <p>Hello {userName},</p>
                <p>We received a request to reset the password for your account. If you made this request, click the button below to set a new password.</p>
                <div style='text-align: center;'>
                    <a href='{resetUrl}' class='button'>Reset Your Password</a>
                </div>
                <div class='security-note'>
                    <strong>Security Notice:</strong> For your protection, this link will expire in 1 hour. If you did not request a password reset, please disregard this email. Your account is still secure.
                </div>
                <p>If the button doesn't work, copy and paste this link into your browser:</p>
                <p class='link-fallback'>{resetUrl}</p>
                <p>Thank you,<br>The ROIMA Team</p>";

            return GenerateBaseEmailTemplate("Reset Your Password", "Password Reset", body);
        }

        private string GenerateWelcomeTemplate(string userName)
        {

            string dashboardUrl = "#";

            string body = $@"
                <h2>Your Account is Ready!</h2>
                <p>Hi {userName},</p>
                <p>Your email has been verified, and your account is now active. Welcome to the ROIMA Recruitment System, where you can connect with your next great opportunity.</p>
        
                <h3 style='margin-top: 30px;'>What's next?</h3>
                <ul>
                    <li><strong>Complete your profile</strong> to stand out to recruiters.</li>
                    <li><strong>Browse and search</strong> for jobs that match your skills.</li>
                    <li><strong>Track your applications</strong> all in one place.</li>
                </ul>

                <p>Click the button below to log in and get started!</p>
                <div style='text-align: center;'>
                    <a href='{dashboardUrl}' class='button'>Go to Your Dashboard</a>
                </div>
                <p>We're thrilled to have you with us!</p>
                <p>Best regards,<br>The ROIMA Team</p>";

            return GenerateBaseEmailTemplate("Welcome to ROIMA!", "Welcome Aboard!", body);
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