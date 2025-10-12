using Microsoft.EntityFrameworkCore;
using RecruitmentSystem.Core.Entities;
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

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.JobOffers.AnyAsync(j => j.Id == id);
        }

        public async Task<JobOffer?> GetByApplicationIdAsync(Guid jobApplicationId)
        {
            return await _context.JobOffers
                .Include(jo => jo.JobApplication)
                .Include(jo => jo.ExtendedByUser)
                .SingleOrDefaultAsync(jo => jo.JobApplicationId == jobApplicationId);
        }

        public async Task<IEnumerable<JobOffer>> GetByExtendedByUserAsync(Guid extendedByUserId)
        {
            return await _context.JobOffers
                .Include(jo => jo.JobApplication)
                    .ThenInclude(ja => ja.CandidateProfile)
                        .ThenInclude(cp => cp.User)
                .Include(jo => jo.JobApplication)
                    .ThenInclude(ja => ja.JobPosition)
                .Where(jo => jo.ExtendedByUserId == extendedByUserId)
                .OrderByDescending(jo => jo.OfferDate)
                .ToListAsync();
        }

        public async Task<JobOffer?> GetByIdAsync(Guid id)
        {
            return await _context.JobOffers
                .Include(jo => jo.JobApplication)
                    .ThenInclude(ja => ja.CandidateProfile)
                        .ThenInclude(cp => cp.User)
                .Include(jo => jo.JobApplication)
                    .ThenInclude(ja => ja.JobPosition)
                .Include(jo => jo.ExtendedByUser)
                .FirstOrDefaultAsync(jo => jo.Id == id);
        }

        public async Task<IEnumerable<JobOffer>> GetByStatusAsync(OfferStatus status)
        {
            return await _context.JobOffers
                .Include(jo => jo.JobApplication)
                    .ThenInclude(ja => ja.CandidateProfile)
                        .ThenInclude(cp => cp.User)
                .Include(jo => jo.JobApplication)
                    .ThenInclude(ja => ja.JobPosition)
                .Include(jo => jo.ExtendedByUser)
                .Where(jo => jo.Status == status)
                .OrderByDescending(jo => jo.OfferDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<JobOffer>> GetExpiringOffersAsync(DateTime beforeDate)
        {
            return await _context.JobOffers
                .Include(jo => jo.JobApplication)
                    .ThenInclude(ja => ja.CandidateProfile)
                        .ThenInclude(cp => cp.User)
                .Include(jo => jo.JobApplication)
                    .ThenInclude(ja => ja.JobPosition)
                .Include(jo => jo.ExtendedByUser)
                .Where(jo => jo.Status == OfferStatus.Pending && jo.ExpiryDate <= beforeDate)
                .OrderBy(jo => jo.ExpiryDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<JobOffer>> GetOffersWithFiltersAsync(OfferStatus? status = null, Guid? extendedByUserId = null, DateTime? offerFromDate = null, DateTime? offerToDate = null, DateTime? expiryFromDate = null, DateTime? expiryToDate = null)
        {
            var query = _context.JobOffers
                .Include(jo => jo.JobApplication)
                    .ThenInclude(ja => ja.CandidateProfile)
                        .ThenInclude(cp => cp.User)
                .Include(jo => jo.JobApplication)
                    .ThenInclude(ja => ja.JobPosition)
                .Include(jo => jo.ExtendedByUser)
                .AsQueryable();

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

            return await query
                .OrderByDescending(jo => jo.OfferDate)
                .ToListAsync();
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
            var jobOffer = await _context.JobOffers
                .Include(jo => jo.JobApplication)
                .SingleAsync(jo => jo.Id == offerId);

            jobOffer.Status = newStatus;
            jobOffer.UpdatedAt = DateTime.UtcNow;

            // Set ResponseDate for candidate responses (Accepted, Rejected, Countered)
            if (newStatus == OfferStatus.Accepted ||
                newStatus == OfferStatus.Rejected ||
                newStatus == OfferStatus.Countered)
            {
                jobOffer.ResponseDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return jobOffer;
        }
    }
}