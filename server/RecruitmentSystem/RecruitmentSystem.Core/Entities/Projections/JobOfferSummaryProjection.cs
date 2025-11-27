using RecruitmentSystem.Core.Enums;

namespace RecruitmentSystem.Core.Entities.Projections
{
    public class JobOfferSummaryProjection
    {
        public Guid Id { get; set; }
        public Guid JobApplicationId { get; set; }
        public string? CandidateName { get; set; }
        public string? JobTitle { get; set; }
        public decimal OfferedSalary { get; set; }
        public OfferStatus Status { get; set; }
        public DateTime OfferDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public Guid ExtendedByUserId { get; set; }
        public string? ExtendedByUserName { get; set; }
    }
}
