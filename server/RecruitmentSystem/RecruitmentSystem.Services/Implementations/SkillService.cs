using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using RecruitmentSystem.Core.Interfaces;
using RecruitmentSystem.Services.Interfaces;
using RecruitmentSystem.Shared.DTOs;

namespace RecruitmentSystem.Services.Implementations;

public class SkillService : ISkillService
{
    private readonly ISkillRepository _skillRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<SkillService> _logger;

    public SkillService(
        ISkillRepository skillRepository,
        IMapper mapper,
        ILogger<SkillService> logger)
    {
        _skillRepository = skillRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<List<SkillDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var skills = await _skillRepository.GetAllAsync(cancellationToken);
            return _mapper.Map<List<SkillDto>>(skills);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving skill catalog");
            throw;
        }
    }
}
