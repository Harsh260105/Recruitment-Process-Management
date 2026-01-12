namespace RecruitmentSystem.Core.Entities.Projections
{
    public class JobPositionSummaryProjection
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string EmploymentType { get; set; } = string.Empty;
        public string ExperienceLevel { get; set; } = string.Empty;
        public string? SalaryRange { get; set; }
        public string? Status { get; set; }
        public DateTime? ApplicationDeadline { get; set; }
        public decimal? MinExperience { get; set; }
        public int TotalApplicants { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid CreatedByUserId { get; set; }
        public string? CreatorFirstName { get; set; }
        public string? CreatorLastName { get; set; }
        public string? CreatorEmail { get; set; }
        public List<JobPositionSummarySkillProjection> Skills { get; set; } = new();
    }

    public class JobPositionSummarySkillProjection
    {
        public int SkillId { get; set; }
        public string? SkillName { get; set; }
        public bool IsRequired { get; set; }
        public int MinimumExperience { get; set; }
        public int ProficiencyLevel { get; set; }
    }
}
