namespace RecruitmentSystem.Shared.DTOs.CandidateProfile
{
    public class CandidateSearchFilters
    {
        public string? Query { get; set; }
        public string? Skills { get; set; }
        public string? Location { get; set; }
        public decimal? MinExperience { get; set; }
        public decimal? MaxExperience { get; set; }
        public decimal? MinExpectedCTC { get; set; }
        public decimal? MaxExpectedCTC { get; set; }
        public int? MaxNoticePeriod { get; set; }
        public bool? IsOpenToRelocation { get; set; }
        public string? Degree { get; set; }
        public int? MinGraduationYear { get; set; }
        public int? MaxGraduationYear { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 25;
    }

    public class CandidateSearchResultDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? CurrentLocation { get; set; }
        public decimal? TotalExperience { get; set; }
        public decimal? ExpectedCTC { get; set; }
        public int? NoticePeriod { get; set; }
        public string? College { get; set; }
        public string? Degree { get; set; }
        public int? GraduationYear { get; set; }
        public bool IsOpenToRelocation { get; set; }
        public List<string> Skills { get; set; } = new List<string>();
        public string? LinkedInProfile { get; set; }
        public string? GitHubProfile { get; set; }
        public string? PortfolioUrl { get; set; }
        public string? Source { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
