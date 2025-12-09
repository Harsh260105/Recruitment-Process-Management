using System;

namespace RecruitmentSystem.Shared.DTOs
{
    public class JobApplicationEligibilityResult
    {
        public bool CanApply { get; init; }
        public string? Reason { get; init; }
        public DateTime? CooldownEndsAt { get; init; }
        public bool OverrideUsed { get; init; }

        public static JobApplicationEligibilityResult Allowed(bool overrideUsed = false)
            => new JobApplicationEligibilityResult { CanApply = true, OverrideUsed = overrideUsed };

        public static JobApplicationEligibilityResult Forbidden(string reason)
            => new JobApplicationEligibilityResult { CanApply = false, Reason = reason };

        public static JobApplicationEligibilityResult Cooldown(string reason, DateTime cooldownEndsAt)
            => new JobApplicationEligibilityResult
            {
                CanApply = false,
                Reason = reason,
                CooldownEndsAt = cooldownEndsAt,
                OverrideUsed = false
            };
    }
}
