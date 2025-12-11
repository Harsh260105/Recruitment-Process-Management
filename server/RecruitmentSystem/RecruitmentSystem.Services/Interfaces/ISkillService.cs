using System.Threading;
using System.Threading.Tasks;
using RecruitmentSystem.Shared.DTOs;

namespace RecruitmentSystem.Services.Interfaces;

public interface ISkillService
{
    Task<List<SkillDto>> GetAllAsync(CancellationToken cancellationToken = default);
}
