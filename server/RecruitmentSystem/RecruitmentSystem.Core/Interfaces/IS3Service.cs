using Microsoft.AspNetCore.Http;

namespace RecruitmentSystem.Core.Interfaces
{
    public interface IS3Service
    {
        Task<string> UploadResumeAsync(IFormFile file, string userId);
        Task<string?> GetResumeUrlAsync(string fileKey);
        Task<bool> DeleteResumeAsync(string fileKey);
    }
}