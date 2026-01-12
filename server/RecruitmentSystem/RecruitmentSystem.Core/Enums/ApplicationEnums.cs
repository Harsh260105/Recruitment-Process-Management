namespace RecruitmentSystem.Core.Enums
{
    public enum ApplicationStatus
    {
        Applied = 1,
        TestInvited = 2,    // Test link sent to candidate
        TestCompleted = 3,  // Candidate completed the test
        UnderReview = 4,    // Recruiter reviewing application + test results
        Shortlisted = 5,    // Passed initial screening, ready for interviews
        Interview = 6,      // In interview process
        Selected = 7,       // Passed all rounds, ready for offer
        Hired = 8,          // Offer accepted, candidate hired
        Rejected = 9,       // Rejected at any stage
        Withdrawn = 10,     // Candidate withdrew
        OnHold = 11         // Process paused
    }

    public enum InterviewStatus
    {
        Scheduled = 1,
        Completed = 2,
        Cancelled = 3,
        NoShow = 4
    }

    public enum InterviewType
    {
        Screening = 1,
        Technical = 2,
        Cultural = 3,
        Final = 4
    }

    public enum InterviewMode
    {
        InPerson = 1,
        Online = 2,
        Phone = 3
    }

    public enum InterviewOutcome
    {
        Pass = 1,
        Fail = 2,
        Pending = 3
    }

    public enum EvaluationRecommendation
    {
        Pass = 1,
        Fail = 2,
        Maybe = 3
    }

    public enum OfferStatus
    {
        Pending = 1,
        Accepted = 2,
        Rejected = 3,
        Countered = 4,
        Expired = 5,
        Withdrawn = 6
    }

    public enum ParticipantRole
    {
        PrimaryInterviewer = 1,
        Interviewer = 2,
        Observer = 3,
        Shadow = 4
    }
}