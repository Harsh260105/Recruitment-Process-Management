using System.Threading;
using System.Threading.Tasks;
using RecruitmentSystem.Core.Entities;

namespace RecruitmentSystem.Core.Interfaces;

public interface ISkillRepository
{
    Task<List<Skill>> GetAllAsync(CancellationToken cancellationToken = default);
}
