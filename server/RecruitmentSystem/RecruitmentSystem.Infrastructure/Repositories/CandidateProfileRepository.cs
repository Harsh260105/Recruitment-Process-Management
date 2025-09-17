using Microsoft.EntityFrameworkCore;
using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Core.Interfaces;
using RecruitmentSystem.Infrastructure.Data;

namespace RecruitmentSystem.Infrastructure.Repositories
{
    public class CandidateProfileRepository : ICandidateProfileRepository
    {
        private readonly ApplicationDbContext _context;

        public CandidateProfileRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<CandidateProfile?> GetByIdAsync(Guid id)
        {
            return await _context.CandidateProfiles
                .Include(cp => cp.User)
                .Include(cp => cp.CandidateSkills)
                    .ThenInclude(cs => cs.Skill)
                .Include(cp => cp.CandidateEducations)
                .Include(cp => cp.CandidateWorkExperiences)
                .FirstOrDefaultAsync(cp => cp.Id == id);
        }

        public async Task<CandidateProfile?> GetByUserIdAsync(Guid userId)
        {
            return await _context.CandidateProfiles
                .Include(cp => cp.User)
                .Include(cp => cp.CandidateSkills)
                    .ThenInclude(cs => cs.Skill)
                .Include(cp => cp.CandidateEducations)
                .Include(cp => cp.CandidateWorkExperiences)
                .FirstOrDefaultAsync(cp => cp.UserId == userId);
        }

        public async Task<CandidateProfile> CreateAsync(CandidateProfile profile)
        {
            profile.CreatedAt = DateTime.UtcNow;
            profile.UpdatedAt = DateTime.UtcNow;

            _context.CandidateProfiles.Add(profile);
            await _context.SaveChangesAsync();
            return profile;
        }

        public async Task<CandidateProfile> UpdateAsync(CandidateProfile profile)
        {
            profile.UpdatedAt = DateTime.UtcNow;

            _context.CandidateProfiles.Update(profile);
            await _context.SaveChangesAsync();
            return profile;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var profile = await _context.CandidateProfiles.FindAsync(id);
            if (profile == null) return false;

            _context.CandidateProfiles.Remove(profile);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.CandidateProfiles.AnyAsync(cp => cp.Id == id);
        }

        public async Task<bool> ExistsByUserIdAsync(Guid userId)
        {
            return await _context.CandidateProfiles.AnyAsync(cp => cp.UserId == userId);
        }

        // Skills
        public async Task<List<CandidateSkill>> GetSkillsAsync(Guid candidateProfileId)
        {
            return await _context.CandidateSkills
                .Include(cs => cs.Skill)
                .Where(cs => cs.CandidateProfileId == candidateProfileId)
                .ToListAsync();
        }

        public async Task<CandidateSkill?> AddSkillAsync(CandidateSkill skill)
        {
            skill.CreatedAt = DateTime.UtcNow;
            skill.UpdatedAt = DateTime.UtcNow;

            _context.CandidateSkills.Add(skill);
            await _context.SaveChangesAsync();

            return await _context.CandidateSkills
                .Include(cs => cs.Skill)
                .FirstOrDefaultAsync(cs => cs.Id == skill.Id);
        }

        public async Task<CandidateSkill?> UpdateSkillAsync(CandidateSkill skill)
        {
            skill.UpdatedAt = DateTime.UtcNow;

            _context.CandidateSkills.Update(skill);
            await _context.SaveChangesAsync();

            return await _context.CandidateSkills
                .Include(cs => cs.Skill)
                .FirstOrDefaultAsync(cs => cs.Id == skill.Id);
        }

        public async Task<bool> RemoveSkillAsync(Guid candidateProfileId, int skillId)
        {
            var skill = await _context.CandidateSkills
                .FirstOrDefaultAsync(cs => cs.CandidateProfileId == candidateProfileId && cs.SkillId == skillId);

            if (skill == null) return false;

            _context.CandidateSkills.Remove(skill);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<CandidateSkill?> GetSkillAsync(Guid candidateProfileId, int skillId)
        {
            return await _context.CandidateSkills
                .Include(cs => cs.Skill)
                .FirstOrDefaultAsync(cs => cs.CandidateProfileId == candidateProfileId && cs.SkillId == skillId);
        }

        // Education
        public async Task<List<CandidateEducation>> GetEducationAsync(Guid candidateProfileId)
        {
            return await _context.CandidateEducations
                .Where(ce => ce.CandidateProfileId == candidateProfileId)
                .ToListAsync();
        }

        public async Task<CandidateEducation> AddEducationAsync(CandidateEducation education)
        {
            education.CreatedAt = DateTime.UtcNow;
            education.UpdatedAt = DateTime.UtcNow;

            _context.CandidateEducations.Add(education);
            await _context.SaveChangesAsync();
            return education;
        }

        public async Task<CandidateEducation> UpdateEducationAsync(CandidateEducation education)
        {
            education.UpdatedAt = DateTime.UtcNow;

            _context.CandidateEducations.Update(education);
            await _context.SaveChangesAsync();
            return education;
        }

        public async Task<bool> RemoveEducationAsync(Guid educationId)
        {
            var education = await _context.CandidateEducations.FindAsync(educationId);

            if (education == null) return false;

            _context.CandidateEducations.Remove(education);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<CandidateEducation?> GetEducationByIdAsync(Guid educationId)
        {
            return await _context.CandidateEducations.FindAsync(educationId);
        }

        // Work Experience
        public async Task<List<CandidateWorkExperience>> GetWorkExperienceAsync(Guid candidateProfileId)
        {
            return await _context.CandidateWorkExperiences
                .Where(cwe => cwe.CandidateProfileId == candidateProfileId)
                .OrderByDescending(cwe => cwe.StartDate)
                .ToListAsync();
        }

        public async Task<CandidateWorkExperience> AddWorkExperienceAsync(CandidateWorkExperience workExperience)
        {
            workExperience.CreatedAt = DateTime.UtcNow;
            workExperience.UpdatedAt = DateTime.UtcNow;

            _context.CandidateWorkExperiences.Add(workExperience);
            await _context.SaveChangesAsync();
            return workExperience;
        }

        public async Task<CandidateWorkExperience> UpdateWorkExperienceAsync(CandidateWorkExperience workExperience)
        {
            workExperience.UpdatedAt = DateTime.UtcNow;

            _context.CandidateWorkExperiences.Update(workExperience);
            await _context.SaveChangesAsync();
            return workExperience;
        }

        public async Task<bool> RemoveWorkExperienceAsync(Guid workExperienceId)
        {
            var workExperience = await _context.CandidateWorkExperiences.FindAsync(workExperienceId);

            if (workExperience == null) return false;

            _context.CandidateWorkExperiences.Remove(workExperience);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<CandidateWorkExperience?> GetWorkExperienceByIdAsync(Guid workExperienceId)
        {
            return await _context.CandidateWorkExperiences.FindAsync(workExperienceId);
        }
    }
}