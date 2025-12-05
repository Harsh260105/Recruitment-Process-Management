using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Core.Enums;
using RecruitmentSystem.Core.Interfaces;
using RecruitmentSystem.Infrastructure.Data;

namespace RecruitmentSystem.Infrastructure.Repositories
{
    public class InterviewEvaluationRepository : IInterviewEvaluationRepository
    {

        private readonly ApplicationDbContext _context;

        public InterviewEvaluationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<InterviewEvaluation> CreateAsync(InterviewEvaluation evaluation)
        {
            evaluation.CreatedAt = DateTime.UtcNow;
            evaluation.UpdatedAt = DateTime.UtcNow;

            _context.InterviewEvaluations.Add(evaluation);
            await _context.SaveChangesAsync();
            return evaluation;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var evaluation = await _context.InterviewEvaluations.FindAsync(id);
            if (evaluation == null) return false;

            _context.InterviewEvaluations.Remove(evaluation);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.InterviewEvaluations.AnyAsync(e => e.Id == id);
        }

        public async Task<double> GetAverageScoreForInterviewAsync(Guid interviewId)
        {
            var ratings = await _context.InterviewEvaluations
                .Where(e => e.InterviewId == interviewId && e.OverallRating.HasValue)
                .Select(e => e.OverallRating!.Value)
                .ToListAsync();

            return ratings.Any() ? ratings.Average() : 0.0;
        }

        public async Task<IEnumerable<int?>> GetOverallRatingsByInterviewAsync(Guid interviewId)
        {
            return await _context.InterviewEvaluations
                .Where(e => e.InterviewId == interviewId)
                .Select(e => e.OverallRating)
                .ToListAsync();
        }

        public async Task<IEnumerable<InterviewEvaluation>> GetByEvaluatorAsync(Guid evaluatorUserId)
        {
            return await _context.InterviewEvaluations
                .Include(e => e.Interview)
                .Include(e => e.EvaluatorUser)
                .Where(e => e.EvaluatorUserId == evaluatorUserId)
                .ToListAsync();
        }

        public async Task<InterviewEvaluation?> GetByIdAsync(Guid id)
        {
            return await _context.InterviewEvaluations
                .Include(e => e.Interview)
                .Include(e => e.EvaluatorUser)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<IEnumerable<InterviewEvaluation>> GetByInterviewAsync(Guid interviewId)
        {
            return await _context.InterviewEvaluations
                .Include(e => e.Interview)
                .Include(e => e.EvaluatorUser)
                .Where(e => e.InterviewId == interviewId)
                .ToListAsync();
        }

        public async Task<IEnumerable<InterviewEvaluation>> GetEvaluationsForApplicationAsync(Guid applicationId)
        {
            return await _context.InterviewEvaluations
                .Include(e => e.Interview)
                    .ThenInclude(i => i.JobApplication)
                .Include(e => e.EvaluatorUser)
                .Where(e => e.Interview.JobApplicationId == applicationId)
                .ToListAsync();
        }

        public async Task<InterviewEvaluation> UpdateAsync(InterviewEvaluation evaluation)
        {
            evaluation.UpdatedAt = DateTime.UtcNow;

            _context.InterviewEvaluations.Update(evaluation);
            await _context.SaveChangesAsync();
            return evaluation;
        }

        public async Task<InterviewEvaluation?> GetByInterviewAndEvaluatorAsync(Guid interviewId, Guid evaluatorUserId)
        {
            return await _context.InterviewEvaluations
                .Include(e => e.Interview)
                .Include(e => e.EvaluatorUser)
                .FirstOrDefaultAsync(e => e.InterviewId == interviewId && e.EvaluatorUserId == evaluatorUserId);
        }

        public async Task<IEnumerable<EvaluationRecommendation>> GetRecommendationsByInterviewAsync(Guid interviewId)
        {
            return await _context.InterviewEvaluations
                .Where(e => e.InterviewId == interviewId)
                .Select(e => e.Recommendation)
                .ToListAsync();
        }

        public async Task<bool> HasEvaluatorSubmittedAsync(Guid interviewId, Guid evaluatorUserId)
        {
            return await _context.InterviewEvaluations
                .AsNoTracking()
                .AnyAsync(e => e.InterviewId == interviewId && e.EvaluatorUserId == evaluatorUserId);
        }
    }
}
