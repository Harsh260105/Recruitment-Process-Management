using System.ComponentModel.DataAnnotations;
using RecruitmentSystem.Core.Enums;

namespace RecruitmentSystem.Shared.DTOs
{
    public class JobOfferDto
    {
        public Guid Id { get; set; }
        public Guid JobApplicationId { get; set; }
        public decimal OfferedSalary { get; set; }
        public string? Benefits { get; set; }
        public string? JobTitle { get; set; }
        public DateTime OfferDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public OfferStatus Status { get; set; }
        public Guid ExtendedByUserId { get; set; }
        public string? ExtendedByUserName { get; set; }
        public string? Notes { get; set; }
        public DateTime? JoiningDate { get; set; }
        public decimal? CounterOfferAmount { get; set; }
        public string? CounterOfferNotes { get; set; }
        public DateTime? ResponseDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class JobOfferCreateDto
    {
        [Required]
        public Guid JobApplicationId { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal OfferedSalary { get; set; }

        [StringLength(1000)]
        public string? Benefits { get; set; }

        [StringLength(100)]
        public string? JobTitle { get; set; }

        [Required]
        public DateTime ExpiryDate { get; set; }

        public DateTime? JoiningDate { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }
    }

    public class JobOfferUpdateDto
    {
        [Range(0, double.MaxValue)]
        public decimal? OfferedSalary { get; set; }

        [StringLength(1000)]
        public string? Benefits { get; set; }

        [StringLength(100)]
        public string? JobTitle { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public DateTime? JoiningDate { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }
    }

    public class JobOfferExtendDto
    {
        [Required]
        public Guid JobApplicationId { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal OfferedSalary { get; set; }

        [StringLength(1000)]
        public string? Benefits { get; set; }

        [StringLength(100)]
        public string? JobTitle { get; set; }

        [Required]
        public DateTime ExpiryDate { get; set; }

        public DateTime? JoiningDate { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }
    }

    public class JobOfferAcceptDto
    {
        public DateTime? JoiningDate { get; set; }

        [StringLength(500)]
        public string? AcceptanceNotes { get; set; }
    }

    public class JobOfferRejectDto
    {
        [StringLength(500)]
        public string? RejectionReason { get; set; }
    }

    public class JobOfferCounterDto
    {
        [Required]
        [Range(0, double.MaxValue)]
        public decimal CounterAmount { get; set; }

        [StringLength(500)]
        public string? CounterNotes { get; set; }
    }

    public class JobOfferReviseDto
    {
        [Range(0, double.MaxValue)]
        public decimal? NewSalary { get; set; }

        [StringLength(1000)]
        public string? NewBenefits { get; set; }

        public DateTime? NewJoiningDate { get; set; }
    }

    public class JobOfferRespondToCounterDto
    {
        [Required]
        public bool Accepted { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? RevisedSalary { get; set; }

        [StringLength(500)]
        public string? Response { get; set; }
    }

    public class JobOfferExtendExpiryDto
    {
        [Required]
        public DateTime NewExpiryDate { get; set; }

        [StringLength(500)]
        public string? Reason { get; set; }
    }

    public class JobOfferWithdrawDto
    {
        [StringLength(500)]
        public string? Reason { get; set; }
    }

    public class JobOfferSearchDto
    {
        public OfferStatus? Status { get; set; }
        public Guid? ExtendedByUserId { get; set; }
        public DateTime? OfferFromDate { get; set; }
        public DateTime? OfferToDate { get; set; }
        public DateTime? ExpiryFromDate { get; set; }
        public DateTime? ExpiryToDate { get; set; }
        public decimal? MinSalary { get; set; }
        public decimal? MaxSalary { get; set; }
    }

    public class JobOfferDetailedDto
    {
        public Guid Id { get; set; }
        public Guid JobApplicationId { get; set; }
        public decimal OfferedSalary { get; set; }
        public string? Benefits { get; set; }
        public string? JobTitle { get; set; }
        public DateTime OfferDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public OfferStatus Status { get; set; }
        public Guid ExtendedByUserId { get; set; }
        public string? Notes { get; set; }
        public DateTime? JoiningDate { get; set; }
        public decimal? CounterOfferAmount { get; set; }
        public string? CounterOfferNotes { get; set; }
        public DateTime? ResponseDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation Properties
        public JobOfferApplicationDto Application { get; set; } = new();
        public JobOfferUserDto ExtendedByUser { get; set; } = new();
    }

    public class JobOfferApplicationDto
    {
        public Guid Id { get; set; }
        public string? CandidateName { get; set; }
        public string? JobTitle { get; set; }
        public ApplicationStatus ApplicationStatus { get; set; }
    }

    public class JobOfferUserDto
    {
        public Guid Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
    }

    public class JobOfferSummaryDto
    {
        public Guid Id { get; set; }
        public Guid JobApplicationId { get; set; }
        public string? CandidateName { get; set; }
        public string? JobTitle { get; set; }
        public decimal OfferedSalary { get; set; }
        public OfferStatus Status { get; set; }
        public DateTime OfferDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string? ExtendedByUserName { get; set; }
    }

    public class JobOfferAnalyticsDto
    {
        public Dictionary<OfferStatus, int> StatusDistribution { get; set; } = new();
        public decimal AverageOfferAmount { get; set; }
        public double AcceptanceRate { get; set; }
        public TimeSpan AverageResponseTime { get; set; }
        public int TotalOffers { get; set; }
        public int PendingOffers { get; set; }
        public int AcceptedOffers { get; set; }
        public int RejectedOffers { get; set; }
        public int ExpiredOffers { get; set; }
    }

    public class JobOfferTrendDto
    {
        public DateTime Date { get; set; }
        public int OffersExtended { get; set; }
        public int OffersAccepted { get; set; }
        public int OffersRejected { get; set; }
        public decimal AverageSalary { get; set; }
    }

    public class JobOfferValidationDto
    {
        public bool CanExtendOffer { get; set; }
        public bool HasActiveOffer { get; set; }
        public bool CanModifyOffer { get; set; }
        public bool IsOfferExpired { get; set; }
        public bool IsValidOfferAmount { get; set; }
        public string? ValidationMessage { get; set; }
    }
}