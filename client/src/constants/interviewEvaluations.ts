// Interview Type mapping
export const interviewTypeLabels: Record<number, string> = {
  0: "Unknown",
  1: "Screening",
  2: "Technical",
  3: "Cultural",
  4: "Final",
};

// Interview Mode mapping
export const interviewModeLabels: Record<number, string> = {
  1: "In-Person",
  2: "Online",
  3: "Phone",
};

// Interview Status mapping
export const interviewStatusLabels: Record<number, string> = {
  1: "Scheduled",
  2: "Completed",
  3: "Cancelled",
  4: "No-Show",
};

// Interview Status with Badge Variants
export const interviewStatusMeta: Record<
  number,
  {
    label: string;
    variant:
      | "default"
      | "secondary"
      | "destructive"
      | "outline"
      | "success"
      | "warning"
      | "info"
      | "pending";
  }
> = {
  1: { label: "Scheduled", variant: "default" }, // Blue - upcoming
  2: { label: "Completed", variant: "success" }, // Green - done
  3: { label: "Cancelled", variant: "destructive" }, // Red - cancelled
  4: { label: "No-Show", variant: "warning" }, // Yellow - no-show
};

// Interview Outcome mapping
export const interviewOutcomeLabels: Record<number, string> = {
  1: "Passed",
  2: "Failed",
  3: "OnHold",
};

// Interview Outcome with Badge Variants
export const interviewOutcomeMeta: Record<
  number,
  {
    label: string;
    variant:
      | "default"
      | "secondary"
      | "destructive"
      | "outline"
      | "success"
      | "warning"
      | "info"
      | "pending";
  }
> = {
  1: { label: "Passed", variant: "success" }, // Green - passed
  2: { label: "Failed", variant: "destructive" }, // Red - failed
  3: { label: "On Hold", variant: "warning" }, // Yellow - on hold
};

// Evaluation Recommendation mapping
export const evaluationRecommendationLabels: Record<number, string> = {
  1: "StronglyRecommend",
  2: "Recommend",
  3: "DoNotRecommend",
};

// Evaluation Recommendation with Badge Variants
export const evaluationRecommendationMeta: Record<
  number,
  {
    label: string;
    variant:
      | "default"
      | "secondary"
      | "destructive"
      | "outline"
      | "success"
      | "warning"
      | "info"
      | "pending";
  }
> = {
  1: { label: "Strongly Recommend", variant: "success" }, // Green - strong positive
  2: { label: "Recommend", variant: "default" }, // Blue - positive
  3: { label: "Do Not Recommend", variant: "destructive" }, // Red - negative
};

// Helper functions
export const getInterviewStatusMeta = (status?: number) => {
  return (
    interviewStatusMeta[status ?? 0] ?? { label: "Unknown", variant: "outline" }
  );
};

export const getInterviewOutcomeMeta = (outcome?: number) => {
  return (
    interviewOutcomeMeta[outcome ?? 0] ?? {
      label: "Unknown",
      variant: "outline",
    }
  );
};

export const getEvaluationRecommendationMeta = (recommendation?: number) => {
  return (
    evaluationRecommendationMeta[recommendation ?? 0] ?? {
      label: "Unknown",
      variant: "outline",
    }
  );
};
