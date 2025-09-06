using AutoMapper;
using Microsoft.AspNet.Identity;
using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Shared.DTOs;

namespace RecruitmentSystem.Services.Mappings
{
    public class AuthenticationProfile : Profile
    {
        public AuthenticationProfile()
        {
            CreateMap<User, UserProfileDto>()
                .ForMember(dest => dest.Roles, opt => opt.Ignore()); 
        }
    }
}