using AutoMapper;
using RecruitmentSystem.Shared.DTOs.CandidateProfile;
using RecruitmentSystem.Core.Entities;

namespace RecruitmentSystem.Services.Mappings
{
    public class CandidateProfileMappingProfile : Profile
    {
        public CandidateProfileMappingProfile()
        {
            CreateMap<CandidateProfile, CandidateProfileResponseDto>()
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.User.FirstName))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.User.LastName))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User.Email))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.User.PhoneNumber))
                .ForMember(dest => dest.Skills, opt => opt.MapFrom(src => src.CandidateSkills))
                .ForMember(dest => dest.Education, opt => opt.MapFrom(src => src.CandidateEducations))
                .ForMember(dest => dest.WorkExperience, opt => opt.MapFrom(src => src.CandidateWorkExperiences));

            CreateMap<CandidateProfileDto, CandidateProfile>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.LinkedInProfile, opt => opt.MapFrom(src => src.LinkedInProfile ?? string.Empty))
                .ForMember(dest => dest.GitHubProfile, opt => opt.MapFrom(src => src.GitHubProfile ?? string.Empty))
                .ForMember(dest => dest.PortfolioUrl, opt => opt.MapFrom(src => src.PortfolioUrl ?? string.Empty))
                .ForMember(dest => dest.College, opt => opt.MapFrom(src => src.College ?? string.Empty))
                .ForMember(dest => dest.Degree, opt => opt.MapFrom(src => src.Degree ?? string.Empty))
                .ForMember(dest => dest.ResumeFileName, opt => opt.MapFrom(src => string.Empty))
                .ForMember(dest => dest.ResumeFilePath, opt => opt.MapFrom(src => string.Empty))
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedByUser, opt => opt.Ignore())
                .ForMember(dest => dest.CandidateSkills, opt => opt.Ignore())
                .ForMember(dest => dest.CandidateEducations, opt => opt.Ignore())
                .ForMember(dest => dest.CandidateWorkExperiences, opt => opt.Ignore());

            CreateMap<UpdateCandidateProfileDto, CandidateProfile>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.ResumeFileName, opt => opt.Ignore())
                .ForMember(dest => dest.ResumeFilePath, opt => opt.Ignore())
                .ForMember(dest => dest.Source, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedByUser, opt => opt.Ignore())
                .ForMember(dest => dest.CandidateSkills, opt => opt.Ignore())
                .ForMember(dest => dest.CandidateEducations, opt => opt.Ignore())
                .ForMember(dest => dest.CandidateWorkExperiences, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<CandidateSkill, CandidateSkillDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.SkillName, opt => opt.MapFrom(src => src.Skill.Name))
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Skill.Category));

            CreateMap<CandidateEducation, CandidateEducationDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.InstitutionName, opt => opt.MapFrom(src => src.InstitutionName))
                .ForMember(dest => dest.Degree, opt => opt.MapFrom(src => src.Degree))
                .ForMember(dest => dest.FieldOfStudy, opt => opt.MapFrom(src => src.FieldOfStudy))
                .ForMember(dest => dest.StartYear, opt => opt.MapFrom(src => src.StartYear))
                .ForMember(dest => dest.EndYear, opt => opt.MapFrom(src => src.EndYear))
                .ForMember(dest => dest.GPAScale, opt => opt.MapFrom(src => src.GPAScale))
                .ForMember(dest => dest.GPA, opt => opt.MapFrom(src => src.GPA))
                .ForMember(dest => dest.EducationType, opt => opt.MapFrom(src => src.EducationType));

            CreateMap<CandidateWorkExperience, CandidateWorkExperienceDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.CompanyName, opt => opt.MapFrom(src => src.CompanyName))
                .ForMember(dest => dest.JobTitle, opt => opt.MapFrom(src => src.JobTitle))
                .ForMember(dest => dest.EmploymentType, opt => opt.MapFrom(src => src.EmploymentType))
                .ForMember(dest => dest.StartDate, opt => opt.MapFrom(src => src.StartDate))
                .ForMember(dest => dest.EndDate, opt => opt.MapFrom(src => src.EndDate))
                .ForMember(dest => dest.IsCurrentJob, opt => opt.MapFrom(src => src.IsCurrentJob))
                .ForMember(dest => dest.Location, opt => opt.MapFrom(src => src.Location))
                .ForMember(dest => dest.JobDescription, opt => opt.MapFrom(src => src.JobDescription));

            // Explicitly ignoring all non-matching fields for clarity and future safety
            CreateMap<CreateCandidateSkillDto, CandidateSkill>()
                .ForMember(dest => dest.Id, opt => opt.Ignore()) 
                .ForMember(dest => dest.CandidateProfileId, opt => opt.Ignore()) 
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore()) 
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore()) 
                .ForMember(dest => dest.CandidateProfile, opt => opt.Ignore()) 
                .ForMember(dest => dest.Skill, opt => opt.Ignore()); 

            CreateMap<CreateCandidateEducationDto, CandidateEducation>()
                .ForMember(dest => dest.Id, opt => opt.Ignore()) 
                .ForMember(dest => dest.CandidateProfileId, opt => opt.Ignore()) 
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore()) 
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore()) 
                .ForMember(dest => dest.CandidateProfile, opt => opt.Ignore()); 

            CreateMap<CreateCandidateWorkExperienceDto, CandidateWorkExperience>()
                .ForMember(dest => dest.Id, opt => opt.Ignore()) 
                .ForMember(dest => dest.CandidateProfileId, opt => opt.Ignore()) 
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore()) 
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore()) 
                .ForMember(dest => dest.CandidateProfile, opt => opt.Ignore());

            // For PATCH/PUT - only mapping non-null values
            CreateMap<UpdateCandidateSkillDto, CandidateSkill>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CandidateProfileId, opt => opt.Ignore())
                .ForMember(dest => dest.SkillId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CandidateProfile, opt => opt.Ignore())
                .ForMember(dest => dest.Skill, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<UpdateCandidateEducationDto, CandidateEducation>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CandidateProfileId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CandidateProfile, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<UpdateCandidateWorkExperienceDto, CandidateWorkExperience>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CandidateProfileId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CandidateProfile, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        }
    }
}