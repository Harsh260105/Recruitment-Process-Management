using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Core.Interfaces;
using RecruitmentSystem.Infrastructure.Data;

namespace RecruitmentSystem.Infrastructure.Repositories;

public class SkillRepository : ISkillRepository
{
    private readonly ApplicationDbContext _context;

    public SkillRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Skill>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Skills
            .AsNoTracking()
            .OrderBy(skill => skill.Name)
            .ToListAsync(cancellationToken);
    }
}
