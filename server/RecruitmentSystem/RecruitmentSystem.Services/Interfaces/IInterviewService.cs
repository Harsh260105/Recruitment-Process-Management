using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Core.Enums;
using RecruitmentSystem.Shared.DTOs;

namespace RecruitmentSystem.Services.Interfaces
{
    /// <summary>
    /// Core interview management service - handles CRUD operations and basic business validation
    /// ENTERPRISE SEPARATION: This service focuses ONLY on core data operations
    /// - Scheduling logic -> IInterviewSchedulingService (USER-FACING)
    /// - Reporting/Analytics -> IInterviewReportingService  
    /// - Evaluation/Outcomes -> IInterviewEvaluationService
    /// 
    /// USAGE: This service is primarily for INTERNAL operations between services,
    /// not for direct controller usage. Controllers should use InterviewSchedulingService.
    /// </summary>
    public interface IInterviewService
    {
        // Core CRUD Operations - Enterprise Standard with DTOs
        Task<Interview> CreateInterviewAsync(CreateInterviewDto dto);
        Task<Interview?> GetInterviewByIdAsync(Guid id, bool includeDetails = false);
        Task<Interview> UpdateInterviewAsync(Guid interviewId, UpdateInterviewDto dto);
        Task<bool> DeleteInterviewAsync(Guid id);
    }
}