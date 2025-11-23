using AutoMapper;
using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Shared.DTOs;

namespace RecruitmentSystem.Services.Mappings
{
    public class JobOfferMappingProfile : Profile
    {
        public JobOfferMappingProfile()
        {
            // JobOffer to JobOfferDto
            CreateMap<JobOffer, JobOfferDto>()
                .ForMember(dest => dest.ExtendedByUserName,
                    opt => opt.MapFrom(src => $"{src.ExtendedByUser.FirstName} {src.ExtendedByUser.LastName}"));

            // JobOfferCreateDto to JobOffer
            CreateMap<JobOfferCreateDto, JobOffer>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.OfferDate, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.ExtendedByUserId, opt => opt.Ignore())
                .ForMember(dest => dest.CounterOfferAmount, opt => opt.Ignore())
                .ForMember(dest => dest.CounterOfferNotes, opt => opt.Ignore())
                .ForMember(dest => dest.ResponseDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.JobApplication, opt => opt.Ignore())
                .ForMember(dest => dest.ExtendedByUser, opt => opt.Ignore());

            // JobOfferUpdateDto to JobOffer
            CreateMap<JobOfferUpdateDto, JobOffer>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.JobApplicationId, opt => opt.Ignore())
                .ForMember(dest => dest.OfferDate, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.ExtendedByUserId, opt => opt.Ignore())
                .ForMember(dest => dest.CounterOfferAmount, opt => opt.Ignore())
                .ForMember(dest => dest.CounterOfferNotes, opt => opt.Ignore())
                .ForMember(dest => dest.ResponseDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.JobApplication, opt => opt.Ignore())
                .ForMember(dest => dest.ExtendedByUser, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // JobOfferExtendDto to JobOffer
            CreateMap<JobOfferExtendDto, JobOffer>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.OfferDate, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.ExtendedByUserId, opt => opt.Ignore())
                .ForMember(dest => dest.CounterOfferAmount, opt => opt.Ignore())
                .ForMember(dest => dest.CounterOfferNotes, opt => opt.Ignore())
                .ForMember(dest => dest.ResponseDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.JobApplication, opt => opt.Ignore())
                .ForMember(dest => dest.ExtendedByUser, opt => opt.Ignore());

            // JobOffer to JobOfferDetailedDto
            CreateMap<JobOffer, JobOfferDetailedDto>()
                .ForMember(dest => dest.Application, opt => opt.MapFrom(src => src.JobApplication))
                .ForMember(dest => dest.ExtendedByUser, opt => opt.MapFrom(src => src.ExtendedByUser));

            // JobOffer to JobOfferSummaryDto
            CreateMap<JobOffer, JobOfferSummaryDto>()
                .ForMember(dest => dest.CandidateName,
                    opt => opt.MapFrom(src => $"{src.JobApplication.CandidateProfile.User.FirstName} {src.JobApplication.CandidateProfile.User.LastName}"))
                .ForMember(dest => dest.JobTitle, opt => opt.MapFrom(src => src.JobApplication.JobPosition.Title))
                .ForMember(dest => dest.ExtendedByUserName,
                    opt => opt.MapFrom(src => $"{src.ExtendedByUser.FirstName} {src.ExtendedByUser.LastName}"));

            // JobApplication to JobOfferApplicationDto
            CreateMap<JobApplication, JobOfferApplicationDto>()
                .ForMember(dest => dest.CandidateName,
                    opt => opt.MapFrom(src => $"{src.CandidateProfile.User.FirstName} {src.CandidateProfile.User.LastName}"))
                .ForMember(dest => dest.JobTitle, opt => opt.MapFrom(src => src.JobPosition.Title))
                .ForMember(dest => dest.ApplicationStatus, opt => opt.MapFrom(src => src.Status));

            // User to JobOfferUserDto
            CreateMap<User, JobOfferUserDto>();

            // JobOffer to JobApplicationOfferDto (existing mapping from JobApplicationMappingProfile)
            CreateMap<JobOffer, JobApplicationOfferDto>()
                .ForMember(dest => dest.OfferStatus, opt => opt.MapFrom(src => src.Status.ToString()));
        }
    }
}