using AutoMapper;
using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Shared.DTOs;

namespace RecruitmentSystem.Services.Mappings
{
    public class AuthenticationMappingProfile : Profile
    {
        public AuthenticationMappingProfile()
        {
            CreateMap<User, UserProfileDto>()
                .ForMember(dest => dest.Roles, opt => opt.Ignore());
        }
    }
}