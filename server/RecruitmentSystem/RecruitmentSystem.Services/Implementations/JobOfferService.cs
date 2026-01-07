using AutoMapper;
using Microsoft.Extensions.Logging;
using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Core.Entities.Projections;
using RecruitmentSystem.Core.Enums;
using RecruitmentSystem.Core.Interfaces;
using RecruitmentSystem.Services.Interfaces;
using RecruitmentSystem.Shared.DTOs;

namespace RecruitmentSystem.Services.Implementations
{
    public class JobOfferService : IJobOfferService
    {
        private readonly IJobOfferRepository _jobOfferRepository;
        private readonly IJobApplicationRepository _jobApplicationRepository;
        private readonly IEmailService _emailService;
        private readonly IMapper _mapper;
        private readonly ILogger<JobOfferService> _logger;

        public JobOfferService(
            IJobOfferRepository jobOfferRepository,
            IJobApplicationRepository jobApplicationRepository,
            IEmailService emailService,
            IMapper mapper,
            ILogger<JobOfferService> logger)
        {
            _jobOfferRepository = jobOfferRepository;
            _jobApplicationRepository = jobApplicationRepository;
            _emailService = emailService;
            _mapper = mapper;
            _logger = logger;
        }

        #region Core CRUD Operations

        public Task<JobOffer> CreateOfferAsync(JobOffer jobOffer)
            => _jobOfferRepository.CreateAsync(jobOffer);

        public Task<JobOffer?> GetOfferByIdAsync(Guid id)
            => _jobOfferRepository.GetByIdAsync(id);

        public Task<JobOffer?> GetOfferByApplicationIdAsync(Guid jobApplicationId)
            => _jobOfferRepository.GetByApplicationIdAsync(jobApplicationId);

        public Task<JobOffer> UpdateOfferAsync(JobOffer jobOffer)
            => _jobOfferRepository.UpdateAsync(jobOffer);

        public Task<bool> DeleteOfferAsync(Guid id)
            => _jobOfferRepository.DeleteAsync(id);

        #endregion

        #region HR-Facing Offer Management

        public async Task<JobOffer> ExtendOfferAsync(
            Guid jobApplicationId,
            decimal offeredSalary,
            string? benefits,
            string? jobTitle,
            DateTime expiryDate,
            DateTime? joiningDate,
            Guid extendedByUserId,
            string? notes = null)
        {
            var application = await _jobApplicationRepository.GetByIdAsync(jobApplicationId);
            if (application == null)
                throw new ArgumentException("Job application not found");

            if (!await CanExtendOfferAsync(jobApplicationId))
                throw new InvalidOperationException("Cannot extend offer for this application");

            var offer = new JobOffer
            {
                JobApplicationId = jobApplicationId,
                OfferedSalary = offeredSalary,
                Benefits = benefits,
                JobTitle = jobTitle,
                ExpiryDate = expiryDate,
                JoiningDate = joiningDate,
                ExtendedByUserId = extendedByUserId,
                Notes = notes,
                Status = OfferStatus.Pending,
                JobApplication = null!,
                ExtendedByUser = null!
            };

            return await _jobOfferRepository.CreateAsync(offer);
        }

        public async Task<JobOffer> WithdrawOfferAsync(Guid offerId, Guid withdrawnByUserId, string? reason = null)
        {
            var offer = await _jobOfferRepository.GetByIdAsync(offerId);
            if (offer == null)
                throw new ArgumentException("Job offer not found");

            if (offer.Status != OfferStatus.Pending && offer.Status != OfferStatus.Countered)
                throw new InvalidOperationException("Offer cannot be withdrawn in its current status");

            offer.Status = OfferStatus.Withdrawn;
            if (!string.IsNullOrEmpty(reason))
                offer.Notes = string.IsNullOrEmpty(offer.Notes) ? reason : $"{offer.Notes}\n{reason}";

            return await _jobOfferRepository.UpdateAsync(offer);
        }

        #endregion

        #region Candidate-Facing Offer Actions

        public async Task<JobOffer> AcceptOfferAsync(Guid offerId, Guid acceptedByUserId)
        {
            // Check ownership first
            if (!await CanCandidateAccessOfferAsync(offerId, acceptedByUserId))
                throw new UnauthorizedAccessException("You can only accept your own offers");

            var offer = await _jobOfferRepository.GetByIdAsync(offerId);
            if (offer == null)
                throw new ArgumentException("Job offer not found");

            if (offer.Status != OfferStatus.Pending && offer.Status != OfferStatus.Countered)
                throw new InvalidOperationException("Offer cannot be accepted in its current status");

            offer.Status = OfferStatus.Accepted;
            offer.ResponseDate = DateTime.UtcNow;

            await _jobOfferRepository.UpdateAsync(offer);

            // Update application to Hired
            var application = await _jobApplicationRepository.GetByIdAsync(offer.JobApplicationId);
            if (application != null)
            {
                application.Status = ApplicationStatus.Hired;
                application.UpdatedAt = DateTime.UtcNow;
                await _jobApplicationRepository.UpdateAsync(application);
            }

            return offer;
        }

        public async Task<JobOffer> RejectOfferAsync(Guid offerId, Guid rejectedByUserId, string? rejectionReason = null)
        {
            // Check ownership first
            if (!await CanCandidateAccessOfferAsync(offerId, rejectedByUserId))
                throw new UnauthorizedAccessException("You can only reject your own offers");

            var offer = await _jobOfferRepository.GetByIdAsync(offerId);
            if (offer == null)
                throw new ArgumentException("Job offer not found");

            if (offer.Status != OfferStatus.Pending && offer.Status != OfferStatus.Countered)
                throw new InvalidOperationException("Offer cannot be rejected in its current status");

            offer.Status = OfferStatus.Rejected;
            offer.ResponseDate = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(rejectionReason))
                offer.Notes = string.IsNullOrEmpty(offer.Notes) ? rejectionReason : $"{offer.Notes}\n{rejectionReason}";

            await _jobOfferRepository.UpdateAsync(offer);

            // Update application to Rejected if it was Selected
            var application = await _jobApplicationRepository.GetByIdAsync(offer.JobApplicationId);
            if (application != null && application.Status == ApplicationStatus.Selected)
            {
                application.Status = ApplicationStatus.Rejected;
                application.UpdatedAt = DateTime.UtcNow;
                await _jobApplicationRepository.UpdateAsync(application);
            }

            return offer;
        }

        public async Task<JobOffer> CounterOfferAsync(Guid offerId, decimal counterAmount, string? counterNotes, Guid counteredByUserId)
        {
            // Check ownership first
            if (!await CanCandidateAccessOfferAsync(offerId, counteredByUserId))
                throw new UnauthorizedAccessException("You can only counter your own offers");

            var offer = await _jobOfferRepository.GetByIdAsync(offerId);
            if (offer == null)
                throw new ArgumentException("Job offer not found");

            if (offer.Status != OfferStatus.Pending)
                throw new InvalidOperationException("Can only counter pending offers");

            offer.Status = OfferStatus.Countered;
            offer.CounterOfferAmount = counterAmount;
            offer.CounterOfferNotes = counterNotes;
            offer.ResponseDate = DateTime.UtcNow;

            return await _jobOfferRepository.UpdateAsync(offer);
        }

        #endregion

        #region HR-Facing Offer Negotiation Management

        public async Task<JobOffer> RespondToCounterOfferAsync(Guid offerId, bool accepted, Guid respondedByUserId, decimal? revisedSalary = null, string? response = null)
        {
            var offer = await _jobOfferRepository.GetByIdAsync(offerId);
            if (offer == null)
                throw new ArgumentException("Job offer not found");

            if (offer.Status != OfferStatus.Countered)
                throw new InvalidOperationException("Offer is not in countered status");

            if (accepted)
            {
                offer.Status = OfferStatus.Accepted;
                if (revisedSalary.HasValue)
                    offer.OfferedSalary = (decimal)revisedSalary;
                else
                    offer.OfferedSalary = (decimal)offer.CounterOfferAmount!;
            }
            else
            {
                offer.Status = OfferStatus.Pending;

                if (string.IsNullOrEmpty(response))
                    response = "Counter offer rejected. Original offer terms are available for acceptance.";
            }

            if (!string.IsNullOrEmpty(response))
                offer.Notes = string.IsNullOrEmpty(offer.Notes) ? response : $"{offer.Notes}\n{response}";

            offer.ResponseDate = DateTime.UtcNow;

            var updatedOffer = await _jobOfferRepository.UpdateAsync(offer);

            // Send notification to candidate about the response
            await SendOfferNotificationAsync(offerId);

            return updatedOffer;
        }

        public async Task<JobOffer> ExtendOfferExpiryAsync(Guid offerId, DateTime newExpiryDate, Guid extendedByUserId, string? reason = null)
        {
            var offer = await _jobOfferRepository.GetByIdAsync(offerId);
            if (offer == null)
                throw new ArgumentException("Job offer not found");

            if (offer.Status != OfferStatus.Pending && offer.Status != OfferStatus.Countered)
                throw new InvalidOperationException("Can only extend expiry for pending or countered offers");

            if (newExpiryDate <= offer.ExpiryDate)
                throw new ArgumentException("New expiry date must be after current expiry date");

            offer.ExpiryDate = newExpiryDate;
            if (!string.IsNullOrEmpty(reason))
                offer.Notes = string.IsNullOrEmpty(offer.Notes) ? reason : $"{offer.Notes}\n{reason}";

            return await _jobOfferRepository.UpdateAsync(offer);
        }

        public async Task<JobOffer> ReviseOfferAsync(Guid offerId, decimal? newSalary, string? newBenefits, DateTime? newJoiningDate, Guid revisedByUserId)
        {
            var offer = await _jobOfferRepository.GetByIdAsync(offerId);
            if (offer == null)
                throw new ArgumentException("Job offer not found");

            if (offer.Status != OfferStatus.Pending && offer.Status != OfferStatus.Countered)
                throw new InvalidOperationException("Can only revise pending or countered offers");

            if (newSalary.HasValue)
                offer.OfferedSalary = newSalary.Value;

            if (!string.IsNullOrEmpty(newBenefits))
                offer.Benefits = newBenefits;

            if (newJoiningDate.HasValue)
                offer.JoiningDate = newJoiningDate.Value;

            return await _jobOfferRepository.UpdateAsync(offer);
        }

        #endregion

        #region Expiration Management

        public async Task<PagedResult<JobOfferSummaryDto>> GetExpiringOffersAsync(int daysAhead = 3, int pageNumber = 1, int pageSize = 20)
        {
            if (pageNumber < 1)
                throw new ArgumentException("Page number must be greater than 0.", nameof(pageNumber));
            if (pageSize < 1 || pageSize > 100)
                throw new ArgumentException("Page size must be between 1 and 100.", nameof(pageSize));

            var expiryDate = DateTime.UtcNow.AddDays(daysAhead);
            var fetchTask = _jobOfferRepository.GetExpiringOffersPagedAsync(expiryDate, pageNumber, pageSize);

            return await MapOfferSummaryResultAsync(fetchTask, pageNumber, pageSize);
        }

        public async Task<PagedResult<JobOfferSummaryDto>> GetExpiredOffersAsync(int pageNumber = 1, int pageSize = 20)
        {
            if (pageNumber < 1)
                throw new ArgumentException("Page number must be greater than 0.", nameof(pageNumber));
            if (pageSize < 1 || pageSize > 100)
                throw new ArgumentException("Page size must be between 1 and 100.", nameof(pageSize));

            var fetchTask = _jobOfferRepository.GetByStatusPagedAsync(OfferStatus.Expired, pageNumber, pageSize);

            return await MapOfferSummaryResultAsync(fetchTask, pageNumber, pageSize);
        }

        public async Task<JobOffer> MarkOfferExpiredAsync(Guid offerId, Guid markedByUserId)
        {
            var offer = await _jobOfferRepository.GetByIdAsync(offerId);
            if (offer == null)
                throw new ArgumentException("Job offer not found");

            if (offer.Status != OfferStatus.Pending)
                throw new InvalidOperationException("Can only mark pending offers as expired");

            if (offer.ExpiryDate > DateTime.UtcNow)
                throw new InvalidOperationException("Offer has not yet expired");

            return await _jobOfferRepository.UpdateStatusAsync(offerId, OfferStatus.Expired);
        }

        public async Task<int> ProcessExpiredOffersAsync(Guid systemUserId)
        {
            var expiryDate = DateTime.UtcNow;
            var processedCount = 0;
            var pageNumber = 1;
            var pageSize = 50;
            bool hasMorePages;

            do
            {
                var (expiredOffers, totalCount) = await _jobOfferRepository.GetExpiringOffersPagedAsync(expiryDate, pageNumber, pageSize);
                hasMorePages = pageNumber * pageSize < totalCount;

                foreach (var offer in expiredOffers.Where(o => o.Status == OfferStatus.Pending))
                {
                    await _jobOfferRepository.UpdateStatusAsync(offer.Id, OfferStatus.Expired);
                    processedCount++;
                }

                pageNumber++;
            } while (hasMorePages);

            return processedCount;
        }

        #endregion

        #region Search and Filtering

        public async Task<PagedResult<JobOfferSummaryDto>> SearchOffersAsync(
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
            int pageSize = 20)
        {
            if (pageNumber < 1)
                throw new ArgumentException("Page number must be greater than 0.", nameof(pageNumber));
            if (pageSize < 1 || pageSize > 100)
                throw new ArgumentException("Page size must be between 1 and 100.", nameof(pageSize));

            var fetchTask = _jobOfferRepository.GetOffersWithFiltersPagedAsync(
                status, extendedByUserId, offerFromDate, offerToDate, expiryFromDate, expiryToDate,
                minSalary, maxSalary, searchTerm, pageNumber, pageSize);

            return await MapOfferSummaryResultAsync(fetchTask, pageNumber, pageSize);
        }

        public async Task<PagedResult<JobOfferSummaryDto>> GetOffersByStatusPagedAsync(OfferStatus status, int pageNumber = 1, int pageSize = 20)
        {
            if (pageNumber < 1)
                throw new ArgumentException("Page number must be greater than 0.", nameof(pageNumber));
            if (pageSize < 1 || pageSize > 100)
                throw new ArgumentException("Page size must be between 1 and 100.", nameof(pageSize));

            var fetchTask = _jobOfferRepository.GetByStatusPagedAsync(status, pageNumber, pageSize);

            return await MapOfferSummaryResultAsync(fetchTask, pageNumber, pageSize);
        }

        public async Task<PagedResult<JobOfferSummaryDto>> GetOffersByExtendedByUserPagedAsync(Guid extendedByUserId, int pageNumber = 1, int pageSize = 20)
        {
            if (extendedByUserId == Guid.Empty)
                throw new ArgumentException("ExtendedByUserId cannot be empty.", nameof(extendedByUserId));
            if (pageNumber < 1)
                throw new ArgumentException("Page number must be greater than 0.", nameof(pageNumber));
            if (pageSize < 1 || pageSize > 100)
                throw new ArgumentException("Page size must be between 1 and 100.", nameof(pageSize));

            var fetchTask = _jobOfferRepository.GetByExtendedByUserPagedAsync(extendedByUserId, pageNumber, pageSize);

            return await MapOfferSummaryResultAsync(fetchTask, pageNumber, pageSize);
        }

        public async Task<PagedResult<JobOfferSummaryDto>> GetOffersRequiringActionAsync(Guid? userId = null, int pageNumber = 1, int pageSize = 20)
        {
            if (pageNumber < 1)
                throw new ArgumentException("Page number must be greater than 0.", nameof(pageNumber));
            if (pageSize < 1 || pageSize > 100)
                throw new ArgumentException("Page size must be between 1 and 100.", nameof(pageSize));

            var fetchTask = _jobOfferRepository.GetOffersRequiringActionPagedAsync(userId, pageNumber, pageSize);

            return await MapOfferSummaryResultAsync(fetchTask, pageNumber, pageSize);
        }

        public async Task<PagedResult<JobOfferSummaryDto>> GetOffersByCandidateUserIdAsync(Guid candidateUserId, int pageNumber = 1, int pageSize = 20)
        {
            if (candidateUserId == Guid.Empty)
                throw new ArgumentException("Candidate user ID cannot be empty.", nameof(candidateUserId));
            if (pageNumber < 1)
                throw new ArgumentException("Page number must be greater than 0.", nameof(pageNumber));
            if (pageSize < 1 || pageSize > 100)
                throw new ArgumentException("Page size must be between 1 and 100.", nameof(pageSize));

            var fetchTask = _jobOfferRepository.GetByCandidateUserIdPagedAsync(candidateUserId, pageNumber, pageSize);

            return await MapOfferSummaryResultAsync(fetchTask, pageNumber, pageSize);
        }

        #endregion

        #region Analytics and Reporting

        public Task<Dictionary<OfferStatus, int>> GetOfferStatusDistributionAsync()
            => _jobOfferRepository.GetOfferStatusDistributionAsync();

        public Task<decimal> GetAverageOfferAmountAsync(Guid? jobPositionId = null)
            => _jobOfferRepository.GetAverageOfferAmountAsync(jobPositionId);

        public Task<double> GetOfferAcceptanceRateAsync(DateTime? fromDate = null, DateTime? toDate = null)
            => _jobOfferRepository.GetOfferAcceptanceRateAsync(fromDate, toDate);

        public Task<TimeSpan> GetAverageOfferResponseTimeAsync()
            => _jobOfferRepository.GetAverageOfferResponseTimeAsync();

        public async Task<PagedResult<JobOfferSummaryDto>> GetOfferTrendsAsync(DateTime fromDate, DateTime toDate, int pageNumber = 1, int pageSize = 20)
        {
            if (pageNumber < 1)
                throw new ArgumentException("Page number must be greater than 0.", nameof(pageNumber));
            if (pageSize < 1 || pageSize > 100)
                throw new ArgumentException("Page size must be between 1 and 100.", nameof(pageSize));

            var (items, totalCount) = await _jobOfferRepository.GetOffersWithFiltersPagedAsync(
                status: null,
                extendedByUserId: null,
                offerFromDate: fromDate,
                offerToDate: toDate,
                expiryFromDate: null,
                expiryToDate: null,
                minSalary: null,
                maxSalary: null,
                pageNumber: pageNumber,
                pageSize: pageSize);

            var summaryItems = _mapper.Map<List<JobOfferSummaryDto>>(items);
            return PagedResult<JobOfferSummaryDto>.Create(summaryItems, totalCount, pageNumber, pageSize);
        }

        #endregion

        #region Validation and Business Rules

        public async Task<bool> CanExtendOfferAsync(Guid jobApplicationId)
        {
            var application = await _jobApplicationRepository.GetByIdAsync(jobApplicationId);
            if (application == null || application.Status != ApplicationStatus.Selected)
                return false;

            return !await _jobOfferRepository.HasActiveOfferAsync(jobApplicationId);
        }

        public Task<bool> HasActiveOfferAsync(Guid jobApplicationId)
            => _jobOfferRepository.HasActiveOfferAsync(jobApplicationId);

        public async Task<bool> IsOfferExpiredAsync(Guid offerId)
        {
            var offer = await _jobOfferRepository.GetByIdAsync(offerId);
            return offer != null && offer.ExpiryDate < DateTime.UtcNow && offer.Status == OfferStatus.Pending;
        }

        #endregion

        #region Authorization and Access Control

        public async Task<bool> CanCandidateAccessOfferAsync(Guid offerId, Guid candidateUserId)
        {
            var offer = await _jobOfferRepository.GetByIdAsync(offerId);
            return offer?.JobApplication?.CandidateProfile?.UserId == candidateUserId;
        }

        #endregion

        #region Notification and Communication

        public async Task<bool> SendOfferNotificationAsync(Guid offerId)
        {
            try
            {
                var offer = await _jobOfferRepository.GetByIdAsync(offerId);
                if (offer?.JobApplication?.CandidateProfile?.User == null)
                {
                    _logger.LogWarning("Cannot send offer notification for offer {OfferId} - missing candidate information", offerId);
                    return false;
                }

                var candidateEmail = offer.JobApplication.CandidateProfile.User.Email;
                if (string.IsNullOrEmpty(candidateEmail))
                {
                    _logger.LogWarning("Cannot send offer notification for offer {OfferId} - candidate email is empty", offerId);
                    return false;
                }

                var candidateName = $"{offer.JobApplication.CandidateProfile.User.FirstName} {offer.JobApplication.CandidateProfile.User.LastName}";
                var jobTitle = offer.JobTitle ?? offer.JobApplication.JobPosition?.Title ?? "Unknown Position";

                var emailSent = await _emailService.SendJobOfferNotificationAsync(
                    candidateEmail,
                    candidateName,
                    jobTitle,
                    offer.OfferedSalary,
                    offer.Benefits,
                    offer.ExpiryDate,
                    offer.JoiningDate,
                    offer.Notes
                );

                if (emailSent)
                {
                    _logger.LogInformation("Job offer notification sent successfully for offer {OfferId} to {Email}", offerId, candidateEmail);
                }
                else
                {
                    _logger.LogWarning("Failed to send job offer notification for offer {OfferId} to {Email}", offerId, candidateEmail);
                }

                return emailSent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending offer notification for offer {OfferId}", offerId);
                return false;
            }
        }

        public async Task<bool> SendExpiryReminderAsync(Guid offerId, int daysBefore = 1)
        {
            try
            {
                var offer = await _jobOfferRepository.GetByIdAsync(offerId);
                if (offer?.JobApplication?.CandidateProfile?.User == null)
                {
                    _logger.LogWarning("Cannot send expiry reminder for offer {OfferId} - missing candidate information", offerId);
                    return false;
                }

                var candidateEmail = offer.JobApplication.CandidateProfile.User.Email;
                if (string.IsNullOrEmpty(candidateEmail))
                {
                    _logger.LogWarning("Cannot send expiry reminder for offer {OfferId} - candidate email is empty", offerId);
                    return false;
                }

                var candidateName = $"{offer.JobApplication.CandidateProfile.User.FirstName} {offer.JobApplication.CandidateProfile.User.LastName}";
                var jobTitle = offer.JobTitle ?? offer.JobApplication.JobPosition?.Title ?? "Unknown Position";
                var daysRemaining = (offer.ExpiryDate.Date - DateTime.UtcNow.Date).Days;

                // Only send if the offer is still pending and hasn't expired
                if (offer.Status != OfferStatus.Pending || daysRemaining < 0)
                {
                    _logger.LogInformation("Skipping expiry reminder for offer {OfferId} - offer status: {Status}, days remaining: {Days}",
                        offerId, offer.Status, daysRemaining);
                    return false;
                }

                var emailSent = await _emailService.SendOfferExpiryReminderAsync(
                    candidateEmail,
                    candidateName,
                    jobTitle,
                    offer.ExpiryDate,
                    daysRemaining
                );

                if (emailSent)
                {
                    _logger.LogInformation("Offer expiry reminder sent successfully for offer {OfferId} to {Email}, {Days} days remaining",
                        offerId, candidateEmail, daysRemaining);
                }
                else
                {
                    _logger.LogWarning("Failed to send offer expiry reminder for offer {OfferId} to {Email}", offerId, candidateEmail);
                }

                return emailSent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending expiry reminder for offer {OfferId}", offerId);
                return false;
            }
        }

        public async Task<int> SendBulkExpiryRemindersAsync(int daysBefore = 1)
        {
            var sentCount = 0;
            var pageNumber = 1;
            var pageSize = 50;
            bool hasMorePages;

            do
            {
                var expiringOffers = await GetExpiringOffersAsync(daysBefore, pageNumber, pageSize);
                hasMorePages = pageNumber < expiringOffers.TotalPages;

                foreach (var offer in expiringOffers.Items)
                {
                    if (await SendExpiryReminderAsync(offer.Id, daysBefore))
                        sentCount++;
                }

                pageNumber++;
            } while (hasMorePages);

            return sentCount;
        }

        #endregion

        #region Private Helpers

        private async Task<PagedResult<JobOfferSummaryDto>> MapOfferSummaryResultAsync(
            Task<(List<JobOfferSummaryProjection> Items, int TotalCount)> fetchTask,
            int pageNumber,
            int pageSize)
        {
            try
            {
                var (items, totalCount) = await fetchTask;
                var summaryItems = _mapper.Map<List<JobOfferSummaryDto>>(items);
                return PagedResult<JobOfferSummaryDto>.Create(summaryItems, totalCount, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving job offer summaries");
                throw;
            }
        }

        #endregion
    }
}