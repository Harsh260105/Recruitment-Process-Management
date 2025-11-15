using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RecruitmentSystem.Core.Interfaces;

namespace RecruitmentSystem.Services.Implementations
{
    public class S3Service : IS3Service
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;
        private readonly ILogger<S3Service> _logger;

        public S3Service(IConfiguration configuration, ILogger<S3Service> logger)
        {
            _logger = logger;

            var awsOptions = configuration.GetSection("AWS:S3");
            _bucketName = awsOptions["BucketName"] ?? throw new InvalidOperationException("AWS S3 BucketName is not configured");

            _s3Client = new AmazonS3Client(
                awsOptions["AccessKey"],
                awsOptions["SecretKey"],
                Amazon.RegionEndpoint.GetBySystemName(awsOptions["Region"])
            );
        }

        public async Task<string> UploadResumeAsync(IFormFile file, string userId)
        {
            try
            {
                if (file == null || file.Length == 0)
                    throw new ArgumentException("File is empty or null");

                // unique file key
                var fileExtension = Path.GetExtension(file.FileName);
                var fileKey = $"resumes/{userId}/{Guid.NewGuid()}{fileExtension}";

                using var stream = file.OpenReadStream();

                var uploadRequest = new TransferUtilityUploadRequest
                {
                    InputStream = stream,
                    Key = fileKey,
                    BucketName = _bucketName,
                    ContentType = file.ContentType,
                    CannedACL = S3CannedACL.Private
                };

                var transferUtility = new TransferUtility(_s3Client);
                await transferUtility.UploadAsync(uploadRequest);

                _logger.LogInformation("Successfully uploaded resume for user {UserId} with key {FileKey}", userId, fileKey);

                return fileKey;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading resume for user {UserId}", userId);
                throw;
            }
        }

        public async Task<string?> GetResumeUrlAsync(string fileKey)
        {
            try
            {
                if (string.IsNullOrEmpty(fileKey))
                    return null;

                var request = new GetObjectMetadataRequest
                {
                    BucketName = _bucketName,
                    Key = fileKey
                };

                await _s3Client.GetObjectMetadataAsync(request);

                // presigned URL (valid for 1 hour)
                var presignedUrl = _s3Client.GetPreSignedURL(new GetPreSignedUrlRequest
                {
                    BucketName = _bucketName,
                    Key = fileKey,
                    Expires = DateTime.UtcNow.AddHours(1),
                    Protocol = Protocol.HTTPS
                });

                return presignedUrl;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Resume file not found: {FileKey}", fileKey);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating resume URL for key {FileKey}", fileKey);
                throw;
            }
        }

        public async Task<bool> DeleteResumeAsync(string fileKey)
        {
            try
            {
                if (string.IsNullOrEmpty(fileKey))
                    return false;

                var deleteRequest = new DeleteObjectRequest
                {
                    BucketName = _bucketName,
                    Key = fileKey
                };

                await _s3Client.DeleteObjectAsync(deleteRequest);

                _logger.LogInformation("Successfully deleted resume with key {FileKey}", fileKey);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting resume with key {FileKey}", fileKey);
                return false;
            }
        }
    }
}