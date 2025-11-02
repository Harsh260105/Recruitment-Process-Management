using RecruitmentSystem.Core.Entities;
using RecruitmentSystem.Core.Enums;

namespace RecruitmentSystem.Services.Interfaces
{
    public interface IJobApplicationWorkflowService
    {
        // Business Logic Operations
        Task<JobApplication> UpdateApplicationStatusAsync(Guid applicationId, ApplicationStatus newStatus, Guid changedByUserId, string? comments = null);
        Task<JobApplication> AssignRecruiterAsync(Guid applicationId, Guid recruiterId);
        Task<JobApplication> AddInternalNotesAsync(Guid applicationId, string notes);

        // Workflow Management
        Task<JobApplication> SendTestInvitationAsync(Guid applicationId, Guid sentByUserId);
        Task<JobApplication> MarkTestCompletedAsync(Guid applicationId, int score, Guid updatedByUserId);
        Task<JobApplication> MoveToReviewAsync(Guid applicationId, Guid reviewedByUserId);
        Task<JobApplication> ShortlistApplicationAsync(Guid applicationId, Guid shortlistedByUserId, string? comments = null);
        Task<JobApplication> RejectApplicationAsync(Guid applicationId, string rejectionReason, Guid rejectedByUserId);
        Task<JobApplication> WithdrawApplicationAsync(Guid applicationId, Guid withdrawnByUserId);
        Task<JobApplication> PutOnHoldAsync(Guid applicationId, string reason, Guid putOnHoldByUserId);

        // Status History and Audit
        Task<(List<ApplicationStatusHistory> Items, int TotalCount)> GetApplicationStatusHistoryPagedAsync(Guid applicationId, int pageNumber = 1, int pageSize = 10);
        Task<ApplicationStatusHistory?> GetLatestStatusChangeAsync(Guid applicationId);

        // Validation Methods
        Task<bool> ValidateStatusTransitionAsync(ApplicationStatus fromStatus, ApplicationStatus toStatus);
    }
}