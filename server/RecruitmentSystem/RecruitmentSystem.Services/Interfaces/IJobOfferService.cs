using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Core.Enums;
using RecruitmentSystem.Shared.DTOs;

namespace RecruitmentSystem.Services.Interfaces
{
    public interface IJobOfferService
    {
        // Core CRUD Operations
        Task<JobOffer> CreateOfferAsync(JobOffer jobOffer);
        Task<JobOffer?> GetOfferByIdAsync(Guid id);
        Task<JobOffer?> GetOfferByApplicationIdAsync(Guid jobApplicationId);
        Task<JobOffer> UpdateOfferAsync(JobOffer jobOffer);
        Task<bool> DeleteOfferAsync(Guid id);

        // Offer Lifecycle Management
        Task<JobOffer> ExtendOfferAsync(
            Guid jobApplicationId,
            decimal offeredSalary,
            string? benefits,
            string? jobTitle,
            DateTime expiryDate,
            DateTime? joiningDate,
            Guid extendedByUserId,
            string? notes = null);

        Task<JobOffer> AcceptOfferAsync(Guid offerId, Guid acceptedByUserId);
        Task<JobOffer> RejectOfferAsync(Guid offerId, Guid rejectedByUserId, string? rejectionReason = null);
        Task<JobOffer> CounterOfferAsync(Guid offerId, decimal counterAmount, string? counterNotes, Guid counteredByUserId);
        Task<JobOffer> WithdrawOfferAsync(Guid offerId, Guid withdrawnByUserId, string? reason = null);

        // Offer Negotiation Management
        Task<JobOffer> RespondToCounterOfferAsync(Guid offerId, bool accepted, Guid respondedByUserId, decimal? revisedSalary = null, string? response = null);
        Task<JobOffer> ExtendOfferExpiryAsync(Guid offerId, DateTime newExpiryDate, Guid extendedByUserId, string? reason = null);
        Task<JobOffer> ReviseOfferAsync(Guid offerId, decimal? newSalary, string? newBenefits, DateTime? newJoiningDate, Guid revisedByUserId);

        // Expiration Management
        Task<PagedResult<JobOfferSummaryDto>> GetExpiringOffersAsync(int daysAhead = 3, int pageNumber = 1, int pageSize = 20);
        Task<PagedResult<JobOfferSummaryDto>> GetExpiredOffersAsync(int pageNumber = 1, int pageSize = 20);
        Task<JobOffer> MarkOfferExpiredAsync(Guid offerId, Guid markedByUserId);
        Task<int> ProcessExpiredOffersAsync(Guid systemUserId);

        // Search and Filtering
        Task<PagedResult<JobOfferSummaryDto>> SearchOffersAsync(
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

        Task<PagedResult<JobOfferSummaryDto>> GetOffersByStatusPagedAsync(OfferStatus status, int pageNumber = 1, int pageSize = 20);
        Task<PagedResult<JobOfferSummaryDto>> GetOffersByExtendedByUserPagedAsync(Guid extendedByUserId, int pageNumber = 1, int pageSize = 20);
        Task<PagedResult<JobOfferSummaryDto>> GetOffersRequiringActionAsync(Guid? userId = null, int pageNumber = 1, int pageSize = 20);
        Task<PagedResult<JobOfferSummaryDto>> GetOffersByCandidateUserIdAsync(Guid candidateUserId, int pageNumber = 1, int pageSize = 20);

        // Analytics and Reporting
        Task<Dictionary<OfferStatus, int>> GetOfferStatusDistributionAsync();
        Task<decimal> GetAverageOfferAmountAsync(Guid? jobPositionId = null);
        Task<double> GetOfferAcceptanceRateAsync(DateTime? fromDate = null, DateTime? toDate = null);
        Task<TimeSpan> GetAverageOfferResponseTimeAsync();
        Task<PagedResult<JobOfferSummaryDto>> GetOfferTrendsAsync(DateTime fromDate, DateTime toDate, int pageNumber = 1, int pageSize = 20);

        // Validation and Business Rules
        Task<bool> CanExtendOfferAsync(Guid jobApplicationId);
        Task<bool> HasActiveOfferAsync(Guid jobApplicationId);
        Task<bool> IsOfferExpiredAsync(Guid offerId);

        // Authorization and Access Control
        Task<bool> CanCandidateAccessOfferAsync(Guid offerId, Guid candidateUserId);

        // Notification and Communication
        Task<bool> SendOfferNotificationAsync(Guid offerId);
        Task<bool> SendExpiryReminderAsync(Guid offerId, int daysBefore = 1);
        Task<int> SendBulkExpiryRemindersAsync(int daysBefore = 1);
    }
}