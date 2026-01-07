using Microsoft.EntityFrameworkCore;
using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Core.Entities.Projections;
using RecruitmentSystem.Core.Enums;
using RecruitmentSystem.Core.Interfaces;
using RecruitmentSystem.Infrastructure.Data;

namespace RecruitmentSystem.Infrastructure.Repositories
{
    public class JobOfferRepository : IJobOfferRepository
    {
        private readonly ApplicationDbContext _context;

        public JobOfferRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<JobOffer> CreateAsync(JobOffer jobOffer)
        {
            jobOffer.CreatedAt = DateTime.UtcNow;
            jobOffer.UpdatedAt = DateTime.UtcNow;

            _context.JobOffers.Add(jobOffer);
            await _context.SaveChangesAsync();
            return jobOffer;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var jobOffer = await _context.JobOffers.FindAsync(id);
            if (jobOffer == null) return false;

            _context.JobOffers.Remove(jobOffer);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<JobOffer?> GetByApplicationIdAsync(Guid jobApplicationId)
        {
            return await _context.JobOffers
                .AsNoTracking()
                .Include(jo => jo.JobApplication)
                .Include(jo => jo.ExtendedByUser)
                .SingleOrDefaultAsync(jo => jo.JobApplicationId == jobApplicationId);
        }

        public async Task<(List<JobOfferSummaryProjection> Items, int TotalCount)> GetByExtendedByUserPagedAsync(Guid extendedByUserId, int pageNumber, int pageSize)
        {
            var query = _context.JobOffers
                .AsNoTracking()
                .Where(jo => jo.ExtendedByUserId == extendedByUserId)
                .OrderByDescending(jo => jo.OfferDate);

            var totalCount = await query.CountAsync();
            var items = await ProjectToSummary(query)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<JobOffer?> GetByIdAsync(Guid id)
        {
            return await _context.JobOffers
                .AsNoTracking()
                .Include(jo => jo.JobApplication)
                    .ThenInclude(ja => ja.JobPosition)
                .Include(jo => jo.ExtendedByUser)
                .FirstOrDefaultAsync(jo => jo.Id == id);
        }

        public async Task<(List<JobOfferSummaryProjection> Items, int TotalCount)> GetByStatusPagedAsync(OfferStatus status, int pageNumber, int pageSize)
        {
            var query = _context.JobOffers
                .AsNoTracking()
                .Where(jo => jo.Status == status)
                .OrderByDescending(jo => jo.OfferDate);

            var totalCount = await query.CountAsync();
            var items = await ProjectToSummary(query)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(List<JobOfferSummaryProjection> Items, int TotalCount)> GetExpiringOffersPagedAsync(DateTime beforeDate, int pageNumber, int pageSize)
        {
            var query = _context.JobOffers
                .AsNoTracking()
                .Where(jo => jo.Status == OfferStatus.Pending && jo.ExpiryDate <= beforeDate)
                .OrderBy(jo => jo.ExpiryDate);

            var totalCount = await query.CountAsync();
            var items = await ProjectToSummary(query)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(IEnumerable<JobOffer> Items, int TotalCount)> GetExpiringOfferEntitiesPagedAsync(DateTime beforeDate, int pageNumber, int pageSize)
        {
            var query = _context.JobOffers
                .AsNoTracking()
                .Where(jo => jo.Status == OfferStatus.Pending && jo.ExpiryDate <= beforeDate)
                .OrderBy(jo => jo.ExpiryDate);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(List<JobOfferSummaryProjection> Items, int TotalCount)> GetOffersWithFiltersPagedAsync(
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
            IQueryable<JobOffer> query = _context.JobOffers
                .AsNoTracking();

            if (status.HasValue)
                query = query.Where(jo => jo.Status == status.Value);

            if (extendedByUserId.HasValue)
                query = query.Where(jo => jo.ExtendedByUserId == extendedByUserId.Value);

            if (offerFromDate.HasValue)
                query = query.Where(jo => jo.OfferDate >= offerFromDate.Value);

            if (offerToDate.HasValue)
                query = query.Where(jo => jo.OfferDate <= offerToDate.Value);

            if (expiryFromDate.HasValue)
                query = query.Where(jo => jo.ExpiryDate >= expiryFromDate.Value);

            if (expiryToDate.HasValue)
                query = query.Where(jo => jo.ExpiryDate <= expiryToDate.Value);

            if (minSalary.HasValue)
                query = query.Where(jo => jo.OfferedSalary >= minSalary.Value);

            if (maxSalary.HasValue)
                query = query.Where(jo => jo.OfferedSalary <= maxSalary.Value);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var searchTermLower = searchTerm.ToLower();
                query = query.Where(jo =>
                    (jo.JobApplication.CandidateProfile.User != null &&
                     (jo.JobApplication.CandidateProfile.User.FirstName + " " + jo.JobApplication.CandidateProfile.User.LastName).ToLower().Contains(searchTermLower)) ||
                    (jo.JobApplication.JobPosition != null && jo.JobApplication.JobPosition.Title.ToLower().Contains(searchTermLower)) ||
                    (jo.ExtendedByUser != null &&
                     (jo.ExtendedByUser.FirstName + " " + jo.ExtendedByUser.LastName).ToLower().Contains(searchTermLower)) ||
                    (jo.Benefits != null && jo.Benefits.ToLower().Contains(searchTermLower)) ||
                    (jo.Notes != null && jo.Notes.ToLower().Contains(searchTermLower)));
            }

            query = query.OrderByDescending(jo => jo.OfferDate);

            var totalCount = await query.CountAsync();
            var items = await ProjectToSummary(query)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(List<JobOfferSummaryProjection> Items, int TotalCount)> GetOffersRequiringActionPagedAsync(Guid? userId, int pageNumber, int pageSize)
        {
            var query = _context.JobOffers
                .AsNoTracking()
                .Where(jo => jo.Status == OfferStatus.Pending || jo.Status == OfferStatus.Countered);

            if (userId.HasValue)
            {
                query = query.Where(jo => jo.ExtendedByUserId == userId.Value);
            }

            query = query.OrderBy(jo => jo.ExpiryDate);

            var totalCount = await query.CountAsync();
            var items = await ProjectToSummary(query)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(List<JobOfferSummaryProjection> Items, int TotalCount)> GetByCandidateUserIdPagedAsync(Guid candidateUserId, int pageNumber, int pageSize)
        {
            var query = _context.JobOffers
                .AsNoTracking()
                .Where(jo => jo.JobApplication.CandidateProfile.UserId == candidateUserId)
                .OrderByDescending(jo => jo.OfferDate);

            var totalCount = await query.CountAsync();
            var items = await ProjectToSummary(query)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<Dictionary<OfferStatus, int>> GetOfferStatusDistributionAsync()
        {
            var statusCounts = await _context.JobOffers
                .AsNoTracking()
                .GroupBy(jo => jo.Status)
                .Select(group => new
                {
                    Status = group.Key,
                    Count = group.Count()
                })
                .ToListAsync();

            var distribution = Enum.GetValues(typeof(OfferStatus))
                .Cast<OfferStatus>()
                .ToDictionary(status => status, _ => 0);

            foreach (var item in statusCounts)
            {
                distribution[item.Status] = item.Count;
            }

            return distribution;
        }

        public async Task<bool> HasActiveOfferAsync(Guid jobApplicationId)
        {
            return await _context.JobOffers
                .AnyAsync(jo => jo.JobApplicationId == jobApplicationId &&
                               (jo.Status == OfferStatus.Pending || jo.Status == OfferStatus.Accepted));
        }

        public async Task<JobOffer> UpdateAsync(JobOffer jobOffer)
        {
            jobOffer.UpdatedAt = DateTime.UtcNow;

            _context.JobOffers.Update(jobOffer);
            await _context.SaveChangesAsync();
            return jobOffer;
        }

        public async Task<JobOffer> UpdateStatusAsync(Guid offerId, OfferStatus newStatus)
        {
            var jobOffer = await _context.JobOffers.FindAsync(offerId);
            if (jobOffer == null) throw new ArgumentException("Job offer not found");

            jobOffer.Status = newStatus;
            jobOffer.UpdatedAt = DateTime.UtcNow;

            if (newStatus == OfferStatus.Accepted ||
                newStatus == OfferStatus.Rejected ||
                newStatus == OfferStatus.Countered)
            {
                jobOffer.ResponseDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return jobOffer;
        }

        public async Task<decimal> GetAverageOfferAmountAsync(Guid? jobPositionId = null)
        {
            var query = _context.JobOffers
                .AsNoTracking()
                .Where(jo => jo.Status == OfferStatus.Accepted);

            if (jobPositionId.HasValue)
            {
                query = query.Where(jo => jo.JobApplication.JobPositionId == jobPositionId.Value);
            }

            var averageSalary = await query.AverageAsync(jo => (decimal?)jo.OfferedSalary);
            return averageSalary ?? 0;
        }

        public async Task<double> GetOfferAcceptanceRateAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.JobOffers.AsNoTracking();

            if (fromDate.HasValue)
                query = query.Where(jo => jo.OfferDate >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(jo => jo.OfferDate <= toDate.Value);

            var totalOffers = await query.CountAsync();
            if (totalOffers == 0) return 0;

            var acceptedOffers = await query.CountAsync(jo => jo.Status == OfferStatus.Accepted);
            return (double)acceptedOffers / totalOffers * 100;
        }

        public async Task<TimeSpan> GetAverageOfferResponseTimeAsync()
        {
            var averageTicksQuery = await _context.JobOffers
                .AsNoTracking()
                .Where(jo => jo.Status == OfferStatus.Accepted && jo.ResponseDate.HasValue && jo.OfferDate != default)
                .Select(jo => (jo.ResponseDate!.Value - jo.OfferDate).Ticks)
                .ToListAsync();

            if (!averageTicksQuery.Any())
                return TimeSpan.Zero;

            var averageTicks = (long)averageTicksQuery.Average();
            return TimeSpan.FromTicks(averageTicks);
        }

        private static IQueryable<JobOfferSummaryProjection> ProjectToSummary(IQueryable<JobOffer> query)
        {
            return query.Select(jo => new JobOfferSummaryProjection
            {
                Id = jo.Id,
                JobApplicationId = jo.JobApplicationId,
                CandidateName = jo.JobApplication.CandidateProfile.User != null
                    ? jo.JobApplication.CandidateProfile.User.FirstName + " " + jo.JobApplication.CandidateProfile.User.LastName
                    : null,
                JobTitle = jo.JobApplication.JobPosition != null ? jo.JobApplication.JobPosition.Title : null,
                OfferedSalary = jo.OfferedSalary,
                Status = jo.Status,
                OfferDate = jo.OfferDate,
                ExpiryDate = jo.ExpiryDate,
                ExtendedByUserId = jo.ExtendedByUserId,
                ExtendedByUserName = jo.ExtendedByUser != null
                    ? jo.ExtendedByUser.FirstName + " " + jo.ExtendedByUser.LastName
                    : null
            });
        }
    }
}