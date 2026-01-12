using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Core.Entities.Projections;
using RecruitmentSystem.Core.Enums;

namespace RecruitmentSystem.Core.Interfaces
{
    public interface IJobOfferRepository
    {
        Task<JobOffer> CreateAsync(JobOffer jobOffer);
        Task<JobOffer?> GetByIdAsync(Guid id);
        Task<JobOffer?> GetByApplicationIdAsync(Guid jobApplicationId);
        Task<(List<JobOfferSummaryProjection> Items, int TotalCount)> GetByStatusPagedAsync(OfferStatus status, int pageNumber, int pageSize);
        Task<(List<JobOfferSummaryProjection> Items, int TotalCount)> GetByExtendedByUserPagedAsync(Guid extendedByUserId, int pageNumber, int pageSize);
        Task<(List<JobOfferSummaryProjection> Items, int TotalCount)> GetByCandidateUserIdPagedAsync(Guid candidateUserId, int pageNumber, int pageSize);
        Task<(List<JobOfferSummaryProjection> Items, int TotalCount)> GetExpiringOffersPagedAsync(DateTime beforeDate, int pageNumber, int pageSize);
        Task<(IEnumerable<JobOffer> Items, int TotalCount)> GetExpiringOfferEntitiesPagedAsync(DateTime beforeDate, int pageNumber, int pageSize);
        Task<JobOffer> UpdateAsync(JobOffer jobOffer);
        Task<JobOffer> UpdateStatusAsync(Guid offerId, OfferStatus newStatus);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> HasActiveOfferAsync(Guid jobApplicationId);
        Task<(List<JobOfferSummaryProjection> Items, int TotalCount)> GetOffersWithFiltersPagedAsync(
            OfferStatus? status = null,
            Guid? extendedByUserId = null,
            DateTime? offerFromDate = null,
            DateTime? offerToDate = null,
            DateTime? expiryFromDate = null,
            DateTime? expiryToDate = null,
            decimal? minSalary = null,
            decimal? maxSalary = null,
            string? searchTerm = null,
            int pageNumber = 1,
            int pageSize = 20);
        Task<(List<JobOfferSummaryProjection> Items, int TotalCount)> GetOffersRequiringActionPagedAsync(Guid? userId, int pageNumber, int pageSize);
        Task<Dictionary<OfferStatus, int>> GetOfferStatusDistributionAsync();
        Task<decimal> GetAverageOfferAmountAsync(Guid? jobPositionId = null);
        Task<double> GetOfferAcceptanceRateAsync(DateTime? fromDate = null, DateTime? toDate = null);
        Task<TimeSpan> GetAverageOfferResponseTimeAsync();
    }
}