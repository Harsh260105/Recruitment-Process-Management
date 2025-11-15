using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecruitmentSystem.Services.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendEmailVerificationAsync(string toEmail, string userName, string verificationToken, string verificationUrl);
        Task<bool> SendPasswordResetAsync(string toEmail, string userName, string resetToken, string resetUrl);
        Task<bool> SendWelcomeEmailAsync(string toEmail, string userName);
        Task<bool> SendStaffRegistrationEmailAsync(string toEmail, string userName, string role);
        Task<bool> SendBulkWelcomeEmailAsync(string toEmail, string userName, string password, bool isDefaultPassword);
        Task<bool> SendEmailAsync(string toEmail, string subject, string htmlContent, string? textContent = null);
    }
}
