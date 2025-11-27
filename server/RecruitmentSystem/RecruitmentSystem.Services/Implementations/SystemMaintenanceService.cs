using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RecruitmentSystem.Infrastructure.Data;
using RecruitmentSystem.Services.Interfaces;
using System.Linq;

namespace RecruitmentSystem.Services.Implementations
{
    public class SystemMaintenanceService : ISystemMaintenanceService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SystemMaintenanceService> _logger;

        public SystemMaintenanceService(ApplicationDbContext context, ILogger<SystemMaintenanceService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<int> DisableExpiredCandidateOverridesAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            var expiredOverrides = await _context.CandidateProfiles
                .Where(cp => cp.CanBypassApplicationLimits &&
                             cp.OverrideExpiresAt.HasValue &&
                             cp.OverrideExpiresAt.Value < now)
                .ToListAsync(cancellationToken);

            foreach (var profile in expiredOverrides)
            {
                profile.CanBypassApplicationLimits = false;
                profile.OverrideExpiresAt = null;
                profile.UpdatedAt = now;
            }

            if (expiredOverrides.Count > 0)
            {
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Disabled application overrides for {Count} candidate profiles", expiredOverrides.Count);
            }

            return expiredOverrides.Count;
        }

        public async Task<int> CloseExpiredJobPostingsAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            var expiredJobs = await _context.JobPositions
                .Where(j => j.Status == "Active" &&
                            j.ApplicationDeadline.HasValue &&
                            j.ApplicationDeadline.Value < now)
                .ToListAsync(cancellationToken);

            foreach (var job in expiredJobs)
            {
                job.Status = "Closed";
                job.ClosedDate = now;
                job.UpdatedAt = now;
            }

            if (expiredJobs.Count > 0)
            {
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Closed {Count} job postings whose deadlines passed", expiredJobs.Count);
            }

            return expiredJobs.Count;
        }

        public async Task<int> PurgeExpiredRefreshTokensAsync(int retentionDays = 7, CancellationToken cancellationToken = default)
        {
            retentionDays = retentionDays <= 0 ? 7 : retentionDays;
            var now = DateTime.UtcNow;
            var cutoff = now.AddDays(-retentionDays);

            var tokensToDelete = await _context.RefreshTokens
                .Where(rt => rt.ExpiresAt < cutoff || (rt.RevokedAt != null && rt.RevokedAt < cutoff))
                .ToListAsync(cancellationToken);

            if (tokensToDelete.Count == 0)
            {
                return 0;
            }

            _context.RefreshTokens.RemoveRange(tokensToDelete);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Purged {Count} expired refresh tokens older than {RetentionDays} days", tokensToDelete.Count, retentionDays);
            return tokensToDelete.Count;
        }
    }
}
