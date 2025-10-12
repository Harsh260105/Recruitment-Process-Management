using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Core.Enums;

namespace RecruitmentSystem.Core.Interfaces
{
    public interface IJobOfferRepository
    {
        Task<JobOffer> CreateAsync(JobOffer jobOffer);
        Task<JobOffer?> GetByIdAsync(Guid id);
        Task<JobOffer?> GetByApplicationIdAsync(Guid jobApplicationId);
        Task<IEnumerable<JobOffer>> GetByStatusAsync(OfferStatus status);
        Task<IEnumerable<JobOffer>> GetByExtendedByUserAsync(Guid extendedByUserId);
        Task<IEnumerable<JobOffer>> GetExpiringOffersAsync(DateTime beforeDate);
        Task<JobOffer> UpdateAsync(JobOffer jobOffer);
        Task<JobOffer> UpdateStatusAsync(Guid offerId, OfferStatus newStatus);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
        Task<bool> HasActiveOfferAsync(Guid jobApplicationId);
        Task<IEnumerable<JobOffer>> GetOffersWithFiltersAsync(
            OfferStatus? status = null,
            Guid? extendedByUserId = null,
            DateTime? offerFromDate = null,
            DateTime? offerToDate = null,
            DateTime? expiryFromDate = null,
            DateTime? expiryToDate = null);
    }
}