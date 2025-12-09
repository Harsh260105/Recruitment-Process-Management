using AutoMapper;
using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Core.Entities.Projections;
using RecruitmentSystem.Shared.DTOs;

namespace RecruitmentSystem.Services.Mappings
{
    public class JobPositionMappingProfile : Profile
    {
        public JobPositionMappingProfile()
        {
            // JobPosition to JobPositionResponseDto
            CreateMap<JobPosition, JobPositionResponseDto>()
                .ForMember(dest => dest.CreatedByUserId, opt => opt.MapFrom(src => src.CreatedByUserId))
                .ForMember(dest => dest.CreatorFirstName, opt => opt.MapFrom(src => src.CreatedByUser != null ? src.CreatedByUser.FirstName : null))
                .ForMember(dest => dest.CreatorLastName, opt => opt.MapFrom(src => src.CreatedByUser != null ? src.CreatedByUser.LastName : null))
                .ForMember(dest => dest.CreatorEmail, opt => opt.MapFrom(src => src.CreatedByUser != null ? src.CreatedByUser.Email : null))
                .ForMember(dest => dest.Skills, opt => opt.MapFrom(src => src.JobPositionSkills));

            CreateMap<JobPositionSummaryProjection, JobPositionPublicSummaryDto>()
                .ForMember(dest => dest.Skills, opt => opt.MapFrom(src => src.Skills));

            CreateMap<JobPositionSummaryProjection, JobPositionStaffSummaryDto>()
                .IncludeBase<JobPositionSummaryProjection, JobPositionPublicSummaryDto>();

            CreateMap<JobPositionSummarySkillProjection, JobPositionSummarySkillDto>();

            // JobPositionSkill to JobPositionSkillResponseDto
            CreateMap<JobPositionSkill, JobPositionSkillResponseDto>()
                .ForMember(dest => dest.SkillName, opt => opt.MapFrom(src => src.Skill != null ? src.Skill.Name : null))
                .ForMember(dest => dest.SkillCategory, opt => opt.MapFrom(src => src.Skill != null ? src.Skill.Category : null));

            // CreateJobPositionDto to JobPosition
            CreateMap<CreateJobPositionDto, JobPosition>()
                .ForMember(dest => dest.CreatedByUserId, opt => opt.Ignore())
                .ForMember(dest => dest.JobPositionSkills, opt => opt.Ignore()); // skilles will be added separately

            // CreateJobPositionSkillDto to JobPositionSkill
            CreateMap<CreateJobPositionSkillDto, JobPositionSkill>()
                .ForMember(dest => dest.JobPositionId, opt => opt.Ignore());

            // UpdateJobPositionDto to JobPosition (for partial updates)
            CreateMap<UpdateJobPositionDto, JobPosition>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        }
    }
}