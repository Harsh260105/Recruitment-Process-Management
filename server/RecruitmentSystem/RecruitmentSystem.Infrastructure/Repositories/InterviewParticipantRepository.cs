using Microsoft.EntityFrameworkCore;
using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Core.Interfaces;
using RecruitmentSystem.Infrastructure.Data;

namespace RecruitmentSystem.Infrastructure.Repositories
{
    public class InterviewParticipantRepository : IInterviewParticipantRepository
    {

        private readonly ApplicationDbContext _context;

        public InterviewParticipantRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<InterviewParticipant> CreateAsync(InterviewParticipant participant)
        {
            participant.CreatedAt = DateTime.UtcNow;
            participant.UpdatedAt = DateTime.UtcNow;

            _context.InterviewParticipants.Add(participant);
            await _context.SaveChangesAsync();
            return participant;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var participant = await _context.InterviewParticipants.FindAsync(id);
            if (participant == null) return false;

            _context.InterviewParticipants.Remove(participant);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.InterviewParticipants.AnyAsync(p => p.Id == id);
        }

        public async Task<InterviewParticipant?> GetByIdAsync(Guid id)
        {
            return await _context.InterviewParticipants.FindAsync(id);
        }

        public async Task<InterviewParticipant?> GetByInterviewAndUserAsync(Guid interviewId, Guid userId)
        {
            return await _context.InterviewParticipants
                .FirstOrDefaultAsync(p => p.InterviewId == interviewId && p.ParticipantUserId == userId);
        }

        public async Task<IEnumerable<InterviewParticipant>> GetByInterviewAsync(Guid interviewId)
        {
            return await _context.InterviewParticipants
                .Where(p => p.InterviewId == interviewId)
                .ToListAsync();
        }

        public async Task<IEnumerable<InterviewParticipant>> GetByUserAsync(Guid userId)
        {
            return await _context.InterviewParticipants
                .Where(p => p.ParticipantUserId == userId)
                .ToListAsync();
        }

        public async Task<int> GetParticipantCountForInterviewAsync(Guid interviewId)
        {
            return await _context.InterviewParticipants
                .CountAsync(p => p.InterviewId == interviewId);
        }

        public async Task<bool> IsUserParticipantInInterviewAsync(Guid interviewId, Guid userId)
        {
            return await _context.InterviewParticipants
                .AnyAsync(p => p.InterviewId == interviewId && p.ParticipantUserId == userId);
        }

        public async Task<InterviewParticipant> UpdateAsync(InterviewParticipant participant)
        {
            participant.UpdatedAt = DateTime.UtcNow;
            
            _context.InterviewParticipants.Update(participant);
            await _context.SaveChangesAsync();
            return participant;
        }
    }
}