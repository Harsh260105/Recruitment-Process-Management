using AutoMapper;
using RecruitmentSystem.Shared.DTOs;
using RecruitmentSystem.Core.Entities;

namespace RecruitmentSystem.Services.Mappings
{
    public class JobApplicationMappingProfile : Profile
    {
        public JobApplicationMappingProfile()
        {
            // JobApplication to JobApplicationDto
            CreateMap<JobApplication, JobApplicationDto>()
                .ForMember(dest => dest.CandidateName, opt => opt.MapFrom(src => $"{src.CandidateProfile.User.FirstName} {src.CandidateProfile.User.LastName}"))
                .ForMember(dest => dest.JobTitle, opt => opt.MapFrom(src => src.JobPosition.Title))
                .ForMember(dest => dest.AssignedRecruiterName, opt => opt.MapFrom(src => src.AssignedRecruiter != null ? $"{src.AssignedRecruiter.FirstName} {src.AssignedRecruiter.LastName}" : null));

            // JobApplicationCreateDto to JobApplication
            CreateMap<JobApplicationCreateDto, JobApplication>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.AppliedDate, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.InternalNotes, opt => opt.Ignore())
                .ForMember(dest => dest.AssignedRecruiterId, opt => opt.Ignore())
                .ForMember(dest => dest.TestScore, opt => opt.Ignore())
                .ForMember(dest => dest.TestCompletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.RejectionReason, opt => opt.Ignore())
                .ForMember(dest => dest.CandidateProfile, opt => opt.Ignore())
                .ForMember(dest => dest.JobPosition, opt => opt.Ignore())
                .ForMember(dest => dest.AssignedRecruiter, opt => opt.Ignore())
                .ForMember(dest => dest.Interviews, opt => opt.Ignore())
                .ForMember(dest => dest.StatusHistory, opt => opt.Ignore())
                .ForMember(dest => dest.JobOffer, opt => opt.Ignore());

            // JobApplicationUpdateDto to JobApplication
            CreateMap<JobApplicationUpdateDto, JobApplication>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CandidateProfileId, opt => opt.Ignore())
                .ForMember(dest => dest.JobPositionId, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.AppliedDate, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.AssignedRecruiterId, opt => opt.Condition((src, dest, srcMember) => srcMember != null))
                .ForMember(dest => dest.TestScore, opt => opt.Ignore())
                .ForMember(dest => dest.TestCompletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.RejectionReason, opt => opt.Ignore())
                .ForMember(dest => dest.CandidateProfile, opt => opt.Ignore())
                .ForMember(dest => dest.JobPosition, opt => opt.Ignore())
                .ForMember(dest => dest.AssignedRecruiter, opt => opt.Ignore())
                .ForMember(dest => dest.Interviews, opt => opt.Ignore())
                .ForMember(dest => dest.StatusHistory, opt => opt.Ignore())
                .ForMember(dest => dest.JobOffer, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // JobApplicationStatusUpdateDto to parameters (not directly to entity, but can be used in service)
            // If needed, add custom mapping or handle in service

            // JobApplication to JobApplicationSummaryDto
            CreateMap<JobApplication, JobApplicationSummaryDto>()
                .ForMember(dest => dest.CandidateName, opt => opt.MapFrom(src => $"{src.CandidateProfile.User.FirstName} {src.CandidateProfile.User.LastName}"))
                .ForMember(dest => dest.JobTitle, opt => opt.MapFrom(src => src.JobPosition.Title))
                .ForMember(dest => dest.AssignedRecruiterName, opt => opt.MapFrom(src => src.AssignedRecruiter != null ? $"{src.AssignedRecruiter.FirstName} {src.AssignedRecruiter.LastName}" : null));

            // JobApplication to JobApplicationDetailedDto - for detailed views with full navigation properties
            CreateMap<JobApplication, JobApplicationDetailedDto>()
                .ForMember(dest => dest.Candidate, opt => opt.MapFrom(src => src.CandidateProfile))
                .ForMember(dest => dest.JobPosition, opt => opt.MapFrom(src => src.JobPosition))
                .ForMember(dest => dest.AssignedRecruiter, opt => opt.MapFrom(src => src.AssignedRecruiter))
                .ForMember(dest => dest.StatusHistory, opt => opt.MapFrom(src => src.StatusHistory))
                .ForMember(dest => dest.JobOffer, opt => opt.MapFrom(src => src.JobOffer));

            // CandidateProfile to JobApplicationCandidateDto
            CreateMap<CandidateProfile, JobApplicationCandidateDto>()
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.User.FirstName))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.User.LastName))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User.Email))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.User.PhoneNumber));

            // JobPosition to JobApplicationJobPositionDto  
            CreateMap<JobPosition, JobApplicationJobPositionDto>();

            // User to JobApplicationRecruiterDto
            CreateMap<User, JobApplicationRecruiterDto>();

            // ApplicationStatusHistory to JobApplicationStatusHistoryDto
            CreateMap<ApplicationStatusHistory, JobApplicationStatusHistoryDto>()
                .ForMember(dest => dest.ChangedByName, opt => opt.MapFrom(src => $"{src.ChangedByUser.FirstName} {src.ChangedByUser.LastName}"));

            // JobOffer to JobApplicationOfferDto
            CreateMap<JobOffer, JobApplicationOfferDto>()
                .ForMember(dest => dest.OfferStatus, opt => opt.MapFrom(src => src.Status.ToString()));

            // JobApplication to JobApplicationCandidateViewDto (candidate-facing)
            CreateMap<JobApplication, JobApplicationCandidateViewDto>()
                .ForMember(dest => dest.JobPosition, opt => opt.MapFrom(src => src.JobPosition))
                .ForMember(dest => dest.AssignedRecruiterName,
                    opt => opt.MapFrom(src => src.AssignedRecruiter != null
                        ? src.AssignedRecruiter.FirstName + " " + src.AssignedRecruiter.LastName
                        : null))
                .ForMember(dest => dest.StatusHistory, opt => opt.MapFrom(src => src.StatusHistory))
                .ForMember(dest => dest.JobOffer, opt => opt.MapFrom(src => src.JobOffer));

            // JobApplication to JobApplicationStaffViewDto (staff-facing)
            CreateMap<JobApplication, JobApplicationStaffViewDto>()
                .ForMember(dest => dest.Candidate, opt => opt.MapFrom(src => src.CandidateProfile))
                .ForMember(dest => dest.JobPosition, opt => opt.MapFrom(src => src.JobPosition))
                .ForMember(dest => dest.AssignedRecruiter, opt => opt.MapFrom(src => src.AssignedRecruiter))
                .ForMember(dest => dest.StatusHistory, opt => opt.MapFrom(src => src.StatusHistory))
                .ForMember(dest => dest.JobOffer, opt => opt.MapFrom(src => src.JobOffer));

            // JobApplicationDetailedDto to JobApplicationCandidateViewDto
            CreateMap<JobApplicationDetailedDto, JobApplicationCandidateViewDto>();

            // JobApplicationDetailedDto to JobApplicationStaffViewDto  
            CreateMap<JobApplicationDetailedDto, JobApplicationStaffViewDto>();
        }
    }
}