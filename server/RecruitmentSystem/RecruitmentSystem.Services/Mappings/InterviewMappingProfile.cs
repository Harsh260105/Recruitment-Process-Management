using AutoMapper;
using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Core.Entities.Projections;
using RecruitmentSystem.Shared.DTOs;

namespace RecruitmentSystem.Services.Mappings
{
    public class InterviewMappingProfile : Profile
    {
        public InterviewMappingProfile()
        {
            #region Interview Entity Mappings

            // Interview to InterviewResponseDto
            CreateMap<Interview, InterviewResponseDto>()
                .ForMember(dest => dest.ScheduledByUserName, opt => opt.MapFrom(src => $"{src.ScheduledByUser.FirstName} {src.ScheduledByUser.LastName}"))
                .ForMember(dest => dest.JobApplication, opt => opt.MapFrom(src => src.JobApplication))
                .ForMember(dest => dest.Participants, opt => opt.MapFrom(src => src.Participants))
                .ForMember(dest => dest.Evaluations, opt => opt.MapFrom(src => src.Evaluations));

            // CreateInterviewDto to Interview - Core service mapping
            CreateMap<CreateInterviewDto, Interview>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => Core.Enums.InterviewStatus.Scheduled))
                .ForMember(dest => dest.Outcome, opt => opt.Ignore())
                .ForMember(dest => dest.SummaryNotes, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.JobApplication, opt => opt.Ignore())
                .ForMember(dest => dest.ScheduledByUser, opt => opt.Ignore())
                .ForMember(dest => dest.Participants, opt => opt.Ignore())
                .ForMember(dest => dest.Evaluations, opt => opt.Ignore());

            // UpdateInterviewDto to Interview - Partial update mapping
            CreateMap<UpdateInterviewDto, Interview>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.JobApplicationId, opt => opt.Ignore())
                .ForMember(dest => dest.RoundNumber, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.ScheduledByUserId, opt => opt.Ignore())
                .ForMember(dest => dest.Outcome, opt => opt.Ignore())
                .ForMember(dest => dest.SummaryNotes, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.JobApplication, opt => opt.Ignore())
                .ForMember(dest => dest.ScheduledByUser, opt => opt.Ignore())
                .ForMember(dest => dest.Participants, opt => opt.Ignore())
                .ForMember(dest => dest.Evaluations, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // Interview to InterviewSummaryDto
            CreateMap<Interview, InterviewSummaryDto>()
                .ForMember(dest => dest.ParticipantCount, opt => opt.MapFrom(src => src.Participants.Count))
                .ForMember(dest => dest.EvaluationCount, opt => opt.MapFrom(src => src.Evaluations.Count))
                .ForMember(dest => dest.AverageRating, opt => opt.MapFrom(src => src.Evaluations.Any(e => e.OverallRating.HasValue)
                    ? src.Evaluations.Where(e => e.OverallRating.HasValue).Average(e => e.OverallRating!.Value)
                    : (double?)null));

            // InterviewSummaryProjection to InterviewSummaryDto
            CreateMap<InterviewSummaryProjection, InterviewSummaryDto>();

            // Interview to InterviewPublicSummaryDto
            CreateMap<Interview, InterviewPublicSummaryDto>();

            // InterviewSummaryProjection to InterviewPublicSummaryDto
            CreateMap<InterviewSummaryProjection, InterviewPublicSummaryDto>();

            // InterviewNeedingActionProjection to InterviewSummaryDto
            CreateMap<InterviewNeedingActionProjection, InterviewSummaryDto>();

            // InterviewDetailProjection to InterviewDetailDto
            CreateMap<InterviewDetailProjection, InterviewDetailDto>()
                .ForMember(dest => dest.Job, opt => opt.MapFrom(src => src.JobApplication));

            // InterviewDetailJobApplicationProjection to InterviewDetailJobInfoDto
            CreateMap<InterviewDetailJobApplicationProjection, InterviewDetailJobInfoDto>()
                .ForMember(dest => dest.CandidateFullName, opt => opt.MapFrom(src =>
                    string.Join(" ", new[] { src.CandidateFirstName, src.CandidateLastName }
                        .Where(name => !string.IsNullOrWhiteSpace(name)))))
                .ForMember(dest => dest.JobTitle, opt => opt.MapFrom(src => src.JobPositionTitle))
                .ForMember(dest => dest.Department, opt => opt.MapFrom(src => src.JobPositionDepartment))
                .ForMember(dest => dest.Location, opt => opt.MapFrom(src => src.JobPositionLocation));

            // InterviewDetailParticipantProjection to InterviewParticipantResponseDto
            CreateMap<InterviewDetailParticipantProjection, InterviewParticipantResponseDto>()
                .ForMember(dest => dest.ParticipantUserName, opt => opt.MapFrom(src => src.ParticipantName))
                .ForMember(dest => dest.ParticipantUserEmail, opt => opt.MapFrom(src => src.ParticipantEmail));

            // InterviewDetailEvaluationProjection to InterviewEvaluationResponseDto
            CreateMap<InterviewDetailEvaluationProjection, InterviewEvaluationResponseDto>()
                .ForMember(dest => dest.EvaluatorUserName, opt => opt.MapFrom(src => src.EvaluatorName))
                .ForMember(dest => dest.EvaluatorUserEmail, opt => opt.MapFrom(src => src.EvaluatorEmail));

            // ScheduleInterviewDto to CreateInterviewDto
            CreateMap<ScheduleInterviewDto, CreateInterviewDto>()
                .ForMember(dest => dest.RoundNumber, opt => opt.Ignore()) // Set in service
                .ForMember(dest => dest.ScheduledByUserId, opt => opt.Ignore()); // Set in service

            // ScheduleInterviewDto to Interview
            CreateMap<ScheduleInterviewDto, Interview>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.RoundNumber, opt => opt.MapFrom(src => 1)) // Default to round 1
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => Core.Enums.InterviewStatus.Scheduled))
                .ForMember(dest => dest.ScheduledByUserId, opt => opt.Ignore()) // Set in service
                .ForMember(dest => dest.Outcome, opt => opt.Ignore())
                .ForMember(dest => dest.SummaryNotes, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.JobApplication, opt => opt.Ignore())
                .ForMember(dest => dest.ScheduledByUser, opt => opt.Ignore())
                .ForMember(dest => dest.Participants, opt => opt.Ignore())
                .ForMember(dest => dest.Evaluations, opt => opt.Ignore());

            // ScheduleInterviewWithParticipantsDto to Interview
            CreateMap<ScheduleInterviewWithParticipantsDto, Interview>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.RoundNumber, opt => opt.MapFrom(src => 1)) // Default to round 1
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => Core.Enums.InterviewStatus.Scheduled))
                .ForMember(dest => dest.ScheduledByUserId, opt => opt.Ignore()) // Set in service
                .ForMember(dest => dest.Outcome, opt => opt.Ignore())
                .ForMember(dest => dest.SummaryNotes, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.JobApplication, opt => opt.Ignore())
                .ForMember(dest => dest.ScheduledByUser, opt => opt.Ignore())
                .ForMember(dest => dest.Participants, opt => opt.Ignore())
                .ForMember(dest => dest.Evaluations, opt => opt.Ignore());

            // RescheduleInterviewDto to Interview (partial update mapping)
            CreateMap<RescheduleInterviewDto, Interview>()
                .ForMember(dest => dest.ScheduledDateTime, opt => opt.MapFrom(src => src.NewDateTime))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.JobApplication, opt => opt.Ignore())
                .ForMember(dest => dest.ScheduledByUser, opt => opt.Ignore())
                .ForMember(dest => dest.Participants, opt => opt.Ignore())
                .ForMember(dest => dest.Evaluations, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            #endregion

            #region InterviewParticipant Mappings

            // InterviewParticipant to InterviewParticipantResponseDto
            CreateMap<InterviewParticipant, InterviewParticipantResponseDto>()
                .ForMember(dest => dest.ParticipantUserName, opt => opt.MapFrom(src => $"{src.ParticipantUser.FirstName} {src.ParticipantUser.LastName}"))
                .ForMember(dest => dest.ParticipantUserEmail, opt => opt.MapFrom(src => src.ParticipantUser.Email));

            // InterviewParticipantDto to InterviewParticipant
            CreateMap<InterviewParticipantDto, InterviewParticipant>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.InterviewId, opt => opt.Ignore()) // Set in service
                .ForMember(dest => dest.ParticipantUserId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role))
                .ForMember(dest => dest.IsLead, opt => opt.MapFrom(src => src.IsLead))
                .ForMember(dest => dest.Notes, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Interview, opt => opt.Ignore())
                .ForMember(dest => dest.ParticipantUser, opt => opt.Ignore());

            #endregion

            #region InterviewEvaluation Mappings

            // InterviewEvaluation to InterviewEvaluationResponseDto
            CreateMap<InterviewEvaluation, InterviewEvaluationResponseDto>()
                .ForMember(dest => dest.EvaluatorUserName, opt => opt.MapFrom(src => $"{src.EvaluatorUser.FirstName} {src.EvaluatorUser.LastName}"))
                .ForMember(dest => dest.EvaluatorUserEmail, opt => opt.MapFrom(src => src.EvaluatorUser.Email));

            // SubmitEvaluationDto to InterviewEvaluation
            CreateMap<SubmitEvaluationDto, InterviewEvaluation>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.EvaluatorUserId, opt => opt.Ignore()) // Set in service
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Interview, opt => opt.Ignore())
                .ForMember(dest => dest.EvaluatorUser, opt => opt.Ignore());

            // UpdateEvaluationDto to InterviewEvaluation (partial update)
            CreateMap<UpdateEvaluationDto, InterviewEvaluation>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.InterviewId, opt => opt.Ignore())
                .ForMember(dest => dest.EvaluatorUserId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Interview, opt => opt.Ignore())
                .ForMember(dest => dest.EvaluatorUser, opt => opt.Ignore())
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

            // InterviewEvaluation to InterviewEvaluation (patch update for service layer)
            CreateMap<InterviewEvaluation, InterviewEvaluation>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.InterviewId, opt => opt.Ignore())
                .ForMember(dest => dest.EvaluatorUserId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Interview, opt => opt.Ignore())
                .ForMember(dest => dest.EvaluatorUser, opt => opt.Ignore())
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

            #endregion

            #region Workflow and Analytics DTOs

            // Collection of interviews to InterviewWorkflowDto
            CreateMap<IEnumerable<Interview>, InterviewWorkflowDto>()
                .ForMember(dest => dest.JobApplicationId, opt => opt.MapFrom(src => src.FirstOrDefault() != null ? src.First().JobApplicationId : Guid.Empty))
                .ForMember(dest => dest.Interviews, opt => opt.MapFrom(src => src.OrderBy(i => i.RoundNumber)))
                .ForMember(dest => dest.IsProcessComplete, opt => opt.Ignore()) // Set in service based on business logic
                .ForMember(dest => dest.FinalOutcome, opt => opt.Ignore()) // Set in service based on business logic
                .ForMember(dest => dest.CurrentRound, opt => opt.MapFrom(src => src.Any() ? src.Max(i => i.RoundNumber) : 0))
                .ForMember(dest => dest.TotalRounds, opt => opt.MapFrom(src => src.Count()));

            // InterviewSearchDto validation mapping (if needed for internal processing)
            CreateMap<InterviewSearchDto, InterviewSearchDto>(); // Identity mapping for validation

            #endregion

            #region Status and Completion DTOs

            // MarkInterviewCompletedDto partial update
            CreateMap<MarkInterviewCompletedDto, Interview>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => Core.Enums.InterviewStatus.Completed))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.JobApplication, opt => opt.Ignore())
                .ForMember(dest => dest.ScheduledByUser, opt => opt.Ignore())
                .ForMember(dest => dest.Participants, opt => opt.Ignore())
                .ForMember(dest => dest.Evaluations, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // MarkInterviewNoShowDto partial update
            CreateMap<MarkInterviewNoShowDto, Interview>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => Core.Enums.InterviewStatus.NoShow))
                .ForMember(dest => dest.SummaryNotes, opt => opt.MapFrom(src => src.Notes))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.JobApplication, opt => opt.Ignore())
                .ForMember(dest => dest.ScheduledByUser, opt => opt.Ignore())
                .ForMember(dest => dest.Participants, opt => opt.Ignore())
                .ForMember(dest => dest.Evaluations, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // CancelInterviewDto partial update
            CreateMap<CancelInterviewDto, Interview>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => Core.Enums.InterviewStatus.Cancelled))
                .ForMember(dest => dest.SummaryNotes, opt => opt.MapFrom(src => src.Reason))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.JobApplication, opt => opt.Ignore())
                .ForMember(dest => dest.ScheduledByUser, opt => opt.Ignore())
                .ForMember(dest => dest.Participants, opt => opt.Ignore())
                .ForMember(dest => dest.Evaluations, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // SetInterviewOutcomeDto partial update
            CreateMap<SetInterviewOutcomeDto, Interview>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.JobApplication, opt => opt.Ignore())
                .ForMember(dest => dest.ScheduledByUser, opt => opt.Ignore())
                .ForMember(dest => dest.Participants, opt => opt.Ignore())
                .ForMember(dest => dest.Evaluations, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // CompleteInterviewWithEvaluationDto partial update
            CreateMap<CompleteInterviewWithEvaluationDto, Interview>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => Core.Enums.InterviewStatus.Completed))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.JobApplication, opt => opt.Ignore())
                .ForMember(dest => dest.ScheduledByUser, opt => opt.Ignore())
                .ForMember(dest => dest.Participants, opt => opt.Ignore())
                .ForMember(dest => dest.Evaluations, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            #endregion

            #region Reverse Mappings (if needed)

            // These reverse mappings can be useful for some scenarios
            CreateMap<InterviewResponseDto, Interview>()
                .ForMember(dest => dest.JobApplication, opt => opt.Ignore())
                .ForMember(dest => dest.ScheduledByUser, opt => opt.Ignore())
                .ForMember(dest => dest.Participants, opt => opt.Ignore())
                .ForMember(dest => dest.Evaluations, opt => opt.Ignore());

            CreateMap<InterviewParticipantResponseDto, InterviewParticipant>()
                .ForMember(dest => dest.Interview, opt => opt.Ignore())
                .ForMember(dest => dest.ParticipantUser, opt => opt.Ignore());

            CreateMap<InterviewEvaluationResponseDto, InterviewEvaluation>()
                .ForMember(dest => dest.Interview, opt => opt.Ignore())
                .ForMember(dest => dest.EvaluatorUser, opt => opt.Ignore());

            #endregion
        }
    }
}