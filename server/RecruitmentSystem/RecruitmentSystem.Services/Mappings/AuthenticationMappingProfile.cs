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

            CreateMap<User, UserSummaryDto>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Roles, opt => opt.MapFrom(src => src.UserRoles.Select(ur => ur.Role!.Name).ToList()))
                .ForMember(dest => dest.HasCandidateProfile, opt => opt.MapFrom(src => src.CandidateProfile != null))
                .ForMember(dest => dest.HasStaffProfile, opt => opt.MapFrom(src => src.StaffProfile != null))
                .ForMember(dest => dest.RegisteredAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.IsCurrentlyLockedOut, opt => opt.MapFrom(src =>
                    src.LockoutEnd.HasValue && src.LockoutEnd.Value > DateTimeOffset.UtcNow));

            CreateMap<User, UserDetailsDto>()
                .ForMember(dest => dest.Roles, opt => opt.MapFrom(src => src.UserRoles.Select(ur => ur.Role!.Name).ToList()))
                .ForMember(dest => dest.HasCandidateProfile, opt => opt.MapFrom(src => src.CandidateProfile != null))
                .ForMember(dest => dest.HasStaffProfile, opt => opt.MapFrom(src => src.StaffProfile != null))
                .ForMember(dest => dest.RegisteredAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt))
                .ForMember(dest => dest.AccessFailedCount, opt => opt.MapFrom(src => src.AccessFailedCount))
                .ForMember(dest => dest.LockoutEnd, opt => opt.MapFrom(src => src.LockoutEnd))
                .ForMember(dest => dest.IsCurrentlyLockedOut, opt => opt.MapFrom(src =>
                    src.LockoutEnd.HasValue && src.LockoutEnd.Value > DateTimeOffset.UtcNow));
        }
    }
}