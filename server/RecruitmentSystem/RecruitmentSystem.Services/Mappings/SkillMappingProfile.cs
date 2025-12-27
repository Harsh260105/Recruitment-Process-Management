using AutoMapper;
using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Shared.DTOs;

namespace RecruitmentSystem.Services.Mappings;

public class SkillMappingProfile : Profile
{
    public SkillMappingProfile()
    {
        CreateMap<Skill, SkillDto>();
    }
}
