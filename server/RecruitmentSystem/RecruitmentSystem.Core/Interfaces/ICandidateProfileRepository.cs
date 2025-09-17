using RecruitmentSystem.Core.Entities;

namespace RecruitmentSystem.Core.Interfaces
{
    public interface ICandidateProfileRepository
    {
        Task<CandidateProfile?> GetByIdAsync(Guid id);
        Task<CandidateProfile?> GetByUserIdAsync(Guid userId);
        Task<CandidateProfile> CreateAsync(CandidateProfile profile);
        Task<CandidateProfile> UpdateAsync(CandidateProfile profile);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
        Task<bool> ExistsByUserIdAsync(Guid userId);

        // Skills
        Task<List<CandidateSkill>> GetSkillsAsync(Guid candidateProfileId);
        Task<CandidateSkill?> AddSkillAsync(CandidateSkill skill);
        Task<CandidateSkill?> UpdateSkillAsync(CandidateSkill skill);
        Task<bool> RemoveSkillAsync(Guid candidateProfileId, int skillId);
        Task<CandidateSkill?> GetSkillAsync(Guid candidateProfileId, int skillId);

        // Education
        Task<List<CandidateEducation>> GetEducationAsync(Guid candidateProfileId);
        Task<CandidateEducation> AddEducationAsync(CandidateEducation education);
        Task<CandidateEducation> UpdateEducationAsync(CandidateEducation education);
        Task<bool> RemoveEducationAsync(Guid educationId);
        Task<CandidateEducation?> GetEducationByIdAsync(Guid educationId);

        // Work Experience
        Task<List<CandidateWorkExperience>> GetWorkExperienceAsync(Guid candidateProfileId);
        Task<CandidateWorkExperience> AddWorkExperienceAsync(CandidateWorkExperience workExperience);
        Task<CandidateWorkExperience> UpdateWorkExperienceAsync(CandidateWorkExperience workExperience);
        Task<bool> RemoveWorkExperienceAsync(Guid workExperienceId);
        Task<CandidateWorkExperience?> GetWorkExperienceByIdAsync(Guid workExperienceId);
    }
}