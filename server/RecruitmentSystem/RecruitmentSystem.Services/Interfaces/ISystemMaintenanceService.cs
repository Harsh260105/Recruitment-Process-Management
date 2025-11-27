using System.Threading;
using System.Threading.Tasks;

namespace RecruitmentSystem.Services.Interfaces
{
    public interface ISystemMaintenanceService
    {
        Task<int> DisableExpiredCandidateOverridesAsync(CancellationToken cancellationToken = default);
        Task<int> CloseExpiredJobPostingsAsync(CancellationToken cancellationToken = default);
        Task<int> PurgeExpiredRefreshTokensAsync(int retentionDays = 7, CancellationToken cancellationToken = default);
    }
}
