import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { interviewService } from "../../services/interviewService";
import {
  staffKeys,
  staffCacheConfig,
  type Schemas,
  type InterviewConflictParams,
} from "./types";

export const useInterviewsByApplication = (
  jobApplicationId: string,
  params?: { pageNumber?: number; pageSize?: number }
) => {
  return useQuery({
    queryKey: [...staffKeys.interviewsByApplication(jobApplicationId), params],
    queryFn: async () => {
      const response = await interviewService.getInterviewsByApplication(
        jobApplicationId,
        params
      );
      if (!response.success) {
        throw new Error(
          response.errors?.join(", ") || "Failed to fetch interviews"
        );
      }
      return response.data as Schemas["InterviewSummaryDtoPagedResult"];
    },
    enabled: !!jobApplicationId,
    ...staffCacheConfig,
  });
};

export const useUpcomingInterviews = (
  params?: { days?: number; pageNumber?: number; pageSize?: number },
  options?: { enabled?: boolean }
) => {
  const enabled = options?.enabled ?? true;
  return useQuery({
    queryKey: [...staffKeys.all, "interviews", "upcoming", params],
    queryFn: async () => {
      const response = await interviewService.getUpcomingInterviews(params);
      if (!response.success) {
        throw new Error(
          response.errors?.join(", ") || "Failed to fetch upcoming interviews"
        );
      }
      return response.data as Schemas["InterviewPublicSummaryDtoPagedResult"];
    },
    enabled,
    ...staffCacheConfig,
  });
};

export const useInterviewsRequiringAction = (params?: {
  pageNumber?: number;
  pageSize?: number;
}) => {
  return useQuery({
    queryKey: [...staffKeys.all, "interviews", "requiring-action", params],
    queryFn: async () => {
      const response = await interviewService.getInterviewsRequiringAction(
        params
      );
      if (!response.success) {
        throw new Error(
          response.errors?.join(", ") ||
            "Failed to fetch interviews requiring action"
        );
      }
      return response.data as Schemas["InterviewSummaryDtoPagedResult"];
    },
    ...staffCacheConfig,
  });
};

export const useInterviewById = (interviewId: string) => {
  return useQuery({
    queryKey: [...staffKeys.all, "interview", interviewId],
    queryFn: async () => {
      const response = await interviewService.getInterviewDetail(interviewId);
      if (!response.success) {
        throw new Error(
          response.errors?.join(", ") || "Failed to fetch interview details"
        );
      }
      return response.data as Schemas["InterviewDetailDto"];
    },
    enabled: !!interviewId,
    ...staffCacheConfig,
  });
};

export const useLatestInterviewForApplication = (jobApplicationId: string) => {
  return useQuery({
    queryKey: [
      ...staffKeys.interviewsByApplication(jobApplicationId),
      "latest",
    ],
    queryFn: async () => {
      const response = await interviewService.getLatestInterviewForApplication(
        jobApplicationId
      );
      if (!response.success) {
        throw new Error(
          response.errors?.join(", ") || "Failed to fetch latest interview"
        );
      }
      return response.data as Schemas["InterviewResponseDto"];
    },
    enabled: !!jobApplicationId,
    ...staffCacheConfig,
  });
};

export const useInterviewParticipants = (interviewId: string) => {
  return useQuery({
    queryKey: [...staffKeys.all, "interview", interviewId, "participants"],
    queryFn: async () => {
      const response = await interviewService.getInterviewParticipants(
        interviewId
      );
      if (!response.success) {
        throw new Error(
          response.errors?.join(", ") ||
            "Failed to fetch interview participants"
        );
      }
      return response.data as Schemas["InterviewParticipantResponseDto"][];
    },
    enabled: !!interviewId,
    ...staffCacheConfig,
  });
};

export const useCanScheduleInterview = (jobApplicationId: string) => {
  return useQuery({
    queryKey: [
      ...staffKeys.interviewsByApplication(jobApplicationId),
      "can-schedule",
    ],
    queryFn: async () => {
      const response = await interviewService.canScheduleInterview(
        jobApplicationId
      );
      if (!response.success) {
        throw new Error(
          response.errors?.join(", ") || "Failed to check scheduling permission"
        );
      }
      return response.data as boolean;
    },
    enabled: !!jobApplicationId,
    ...staffCacheConfig,
  });
};

export const useAvailableTimeSlots = (
  params: Schemas["GetAvailableTimeSlotsRequestDto"] | null
) => {
  return useQuery({
    queryKey: [...staffKeys.all, "available-slots", params],
    queryFn: async () => {
      if (!params) throw new Error("Parameters are required");
      const response = await interviewService.getAvailableTimeSlots(params);
      if (!response.success) {
        throw new Error(
          response.errors?.join(", ") || "Failed to fetch available time slots"
        );
      }
      return response.data as Schemas["AvailableTimeSlotDto"][];
    },
    enabled: !!params && !!params.startDate && !!params.endDate,
    ...staffCacheConfig,
  });
};

export const useMyAvailableTimeSlots = (
  params: {
    startDate: string;
    endDate: string;
    durationMinutes: number;
  } | null
) => {
  return useQuery({
    queryKey: [...staffKeys.all, "my-available-slots", params],
    queryFn: async () => {
      if (!params) throw new Error("Parameters are required");
      const response = await interviewService.getMyAvailableTimeSlots(params);
      if (!response.success) {
        throw new Error(
          response.errors?.join(", ") ||
            "Failed to fetch your available time slots"
        );
      }
      return response.data as Schemas["AvailableTimeSlotDto"][];
    },
    enabled: !!params && !!params.startDate && !!params.endDate,
    ...staffCacheConfig,
  });
};

export const useScheduledInterviews = (
  params: Schemas["GetScheduledInterviewsRequestDto"] | null
) => {
  return useQuery({
    queryKey: [...staffKeys.all, "scheduled-interviews", params],
    queryFn: async () => {
      if (!params) throw new Error("Parameters are required");
      const response = await interviewService.getScheduledInterviews(params);
      if (!response.success) {
        throw new Error(
          response.errors?.join(", ") || "Failed to fetch scheduled interviews"
        );
      }
      return response.data as Schemas["ScheduledInterviewSlotDto"][];
    },
    enabled: !!params && !!params.startDate && !!params.endDate,
    ...staffCacheConfig,
  });
};

export const useCheckInterviewConflicts = () => {
  return useMutation({
    mutationFn: async (params: InterviewConflictParams) => {
      const response = await interviewService.checkConflicts(params);
      if (!response.success) {
        throw new Error(
          response.errors?.join(", ") || "Failed to check conflicts"
        );
      }
      return response.data as boolean;
    },
  });
};

export const useValidateTimeSlot = () => {
  return useMutation({
    mutationFn: async (data: Schemas["ValidateTimeSlotDto"]) => {
      const response = await interviewService.validateTimeSlot(data);
      if (!response.success) {
        throw new Error(
          response.errors?.join(", ") || "Failed to validate time slot"
        );
      }
      return response;
    },
  });
};

// ============================================================================
// MUTATION HOOKS
// ============================================================================

export const useScheduleInterview = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (data: Schemas["ScheduleInterviewDto"]) => {
      const response = await interviewService.scheduleInterview(data);
      if (!response.success) {
        throw new Error(
          response.errors?.join(", ") || "Failed to schedule interview"
        );
      }
      return response;
    },
    onSuccess: (response) => {
      // Invalidate interviews for the application
      if (response.data?.jobApplicationId) {
        queryClient.invalidateQueries({
          queryKey: staffKeys.interviewsByApplication(
            response.data.jobApplicationId
          ),
        });
      }
      // Invalidate job application to update status
      queryClient.invalidateQueries({
        queryKey: ["jobApplication", response.data?.jobApplicationId],
      });
    },
  });
};

export const useRescheduleInterview = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({
      interviewId,
      data,
    }: {
      interviewId: string;
      data: Schemas["RescheduleInterviewDto"];
    }) => {
      const response = await interviewService.rescheduleInterview(
        interviewId,
        data
      );
      if (!response.success) {
        throw new Error(
          response.errors?.join(", ") || "Failed to reschedule interview"
        );
      }
      return response;
    },
    onSuccess: (response) => {
      // Invalidate interviews for the application
      if (response.data?.jobApplicationId) {
        queryClient.invalidateQueries({
          queryKey: staffKeys.interviewsByApplication(
            response.data.jobApplicationId
          ),
        });
      }
      // Invalidate the specific interview
      queryClient.invalidateQueries({
        queryKey: [...staffKeys.all, "interview", response.data?.id],
      });
    },
  });
};

export const useCancelInterview = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({
      interviewId,
      data,
    }: {
      interviewId: string;
      data: Schemas["CancelInterviewDto"];
    }) => {
      const response = await interviewService.cancelInterview(
        interviewId,
        data
      );
      if (!response.success) {
        throw new Error(
          response.errors?.join(", ") || "Failed to cancel interview"
        );
      }
      return response;
    },
    onSuccess: (response) => {
      // Invalidate interviews for the application
      if (response.data?.jobApplicationId) {
        queryClient.invalidateQueries({
          queryKey: staffKeys.interviewsByApplication(
            response.data.jobApplicationId
          ),
        });
      }
      // Invalidate the specific interview
      queryClient.invalidateQueries({
        queryKey: [...staffKeys.all, "interview", response.data?.id],
      });
    },
  });
};

export const useCompleteInterview = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({
      interviewId,
      data,
    }: {
      interviewId: string;
      data: Schemas["MarkInterviewCompletedDto"];
    }) => {
      const response = await interviewService.completeInterview(
        interviewId,
        data
      );
      if (!response.success) {
        throw new Error(
          response.errors?.join(", ") || "Failed to complete interview"
        );
      }
      return response;
    },
    onSuccess: (response) => {
      // Invalidate interviews for the application
      if (response.data?.jobApplicationId) {
        queryClient.invalidateQueries({
          queryKey: staffKeys.interviewsByApplication(
            response.data.jobApplicationId
          ),
        });
      }
      // Invalidate the specific interview
      queryClient.invalidateQueries({
        queryKey: [...staffKeys.all, "interview", response.data?.id],
      });
    },
  });
};

export const useMarkInterviewNoShow = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({
      interviewId,
      data,
    }: {
      interviewId: string;
      data: Schemas["MarkInterviewNoShowDto"];
    }) => {
      const response = await interviewService.markNoShow(interviewId, data);
      if (!response.success) {
        throw new Error(
          response.errors?.join(", ") || "Failed to mark interview no-show"
        );
      }
      return response;
    },
    onSuccess: (response) => {
      // Invalidate interviews for the application
      if (response.data?.jobApplicationId) {
        queryClient.invalidateQueries({
          queryKey: staffKeys.interviewsByApplication(
            response.data.jobApplicationId
          ),
        });
      }
      // Invalidate the specific interview
      queryClient.invalidateQueries({
        queryKey: [...staffKeys.all, "interview", response.data?.id],
      });
    },
  });
};

// ============================================================================
// EVALUATION HOOKS
// ============================================================================

export const useSubmitInterviewEvaluation = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({
      interviewId,
      data,
    }: {
      interviewId: string;
      data: Schemas["SubmitEvaluationDto"];
    }) => {
      const response = await interviewService.submitEvaluation(
        interviewId,
        data
      );
      if (!response.success) {
        throw new Error(
          response.errors?.join(", ") || "Failed to submit evaluation"
        );
      }
      return response;
    },
    onSuccess: (_, { interviewId }) => {
      // Invalidate evaluation-related queries
      queryClient.invalidateQueries({
        queryKey: [...staffKeys.all, "interview", interviewId, "evaluation"],
      });
      queryClient.invalidateQueries({
        queryKey: [...staffKeys.all, "interview", interviewId, "evaluations"],
      });
    },
  });
};

export const useMyInterviewEvaluation = (interviewId: string) => {
  return useQuery({
    queryKey: [...staffKeys.all, "interview", interviewId, "evaluation", "my"],
    queryFn: async () => {
      const response = await interviewService.getMyEvaluation(interviewId);
      if (!response.success) {
        throw new Error(
          response.errors?.join(", ") || "Failed to fetch my evaluation"
        );
      }
      return response.data?.data as Schemas["InterviewEvaluationResponseDto"];
    },
    enabled: !!interviewId,
    ...staffCacheConfig,
  });
};

export const useAllInterviewEvaluations = (interviewId: string) => {
  return useQuery({
    queryKey: [...staffKeys.all, "interview", interviewId, "evaluations"],
    queryFn: async () => {
      const response = await interviewService.getAllEvaluations(interviewId);
      if (!response.success) {
        throw new Error(
          response.errors?.join(", ") || "Failed to fetch evaluations"
        );
      }
      return response.data?.data as Schemas["InterviewEvaluationResponseDto"][];
    },
    enabled: !!interviewId,
    ...staffCacheConfig,
  });
};

export const useInterviewAverageScore = (interviewId: string) => {
  return useQuery({
    queryKey: [...staffKeys.all, "interview", interviewId, "average-score"],
    queryFn: async () => {
      const response = await interviewService.getAverageScore(interviewId);
      if (!response.success) {
        throw new Error(
          response.errors?.join(", ") || "Failed to fetch average score"
        );
      }
      return response.data?.data as number;
    },
    enabled: !!interviewId,
    ...staffCacheConfig,
  });
};

export const useIsEvaluationComplete = (interviewId: string) => {
  return useQuery({
    queryKey: [
      ...staffKeys.all,
      "interview",
      interviewId,
      "evaluation-complete",
    ],
    queryFn: async () => {
      const response = await interviewService.isEvaluationComplete(interviewId);
      if (!response.success) {
        throw new Error(
          response.errors?.join(", ") || "Failed to check evaluation status"
        );
      }
      return response.data?.data as boolean;
    },
    enabled: !!interviewId,
    ...staffCacheConfig,
  });
};

export const useCanEvaluateInterview = (interviewId: string) => {
  return useQuery({
    queryKey: [...staffKeys.all, "interview", interviewId, "can-evaluate"],
    queryFn: async () => {
      const response = await interviewService.canEvaluate(interviewId);
      if (!response.success) {
        throw new Error(
          response.errors?.join(", ") || "Failed to check evaluation permission"
        );
      }
      return response.data?.data as boolean;
    },
    enabled: !!interviewId,
    ...staffCacheConfig,
  });
};

export const useInterviewRecommendation = (interviewId: string) => {
  return useQuery({
    queryKey: [...staffKeys.all, "interview", interviewId, "recommendation"],
    queryFn: async () => {
      const response = await interviewService.getRecommendation(interviewId);
      if (!response.success) {
        throw new Error(
          response.errors?.join(", ") || "Failed to fetch recommendation"
        );
      }
      return response.data?.data as Schemas["EvaluationRecommendation"];
    },
    enabled: !!interviewId,
    ...staffCacheConfig,
  });
};

export const useSetInterviewOutcome = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({
      interviewId,
      data,
    }: {
      interviewId: string;
      data: Schemas["SetInterviewOutcomeDto"];
    }) => {
      const response = await interviewService.setInterviewOutcome(
        interviewId,
        data
      );
      if (!response.success) {
        throw new Error(
          response.errors?.join(", ") || "Failed to set interview outcome"
        );
      }
      return response.data as Schemas["InterviewResponseDto"];
    },
    onSuccess: (data) => {
      // Invalidate interviews for the application
      if (data.jobApplicationId) {
        queryClient.invalidateQueries({
          queryKey: staffKeys.interviewsByApplication(data.jobApplicationId),
        });
      }
      // Invalidate the specific interview
      queryClient.invalidateQueries({
        queryKey: [...staffKeys.all, "interview", data.id],
      });
      // Invalidate job application to update status
      queryClient.invalidateQueries({
        queryKey: ["jobApplication", data.jobApplicationId],
      });
    },
  });
};

export const useInterviewOutcome = (jobApplicationId: string) => {
  return useQuery({
    queryKey: [
      ...staffKeys.interviewsByApplication(jobApplicationId),
      "outcome",
    ],
    queryFn: async () => {
      const response = await interviewService.getInterviewOutcome(
        jobApplicationId
      );
      if (!response.success) {
        throw new Error(
          response.errors?.join(", ") || "Failed to fetch interview outcome"
        );
      }
      return response.data?.data as Schemas["InterviewOutcome"];
    },
    enabled: !!jobApplicationId,
    ...staffCacheConfig,
  });
};

export const useIsInterviewProcessComplete = (jobApplicationId: string) => {
  return useQuery({
    queryKey: [
      ...staffKeys.interviewsByApplication(jobApplicationId),
      "process-complete",
    ],
    queryFn: async () => {
      const response = await interviewService.isProcessComplete(
        jobApplicationId
      );
      if (!response.success) {
        throw new Error(
          response.errors?.join(", ") || "Failed to check process completion"
        );
      }
      return response.data?.data as boolean;
    },
    enabled: !!jobApplicationId,
    ...staffCacheConfig,
  });
};

export const usePendingEvaluations = () => {
  return useQuery({
    queryKey: [...staffKeys.all, "interviews", "pending-evaluations"],
    queryFn: async () => {
      const response = await interviewService.getPendingEvaluations();
      if (!response.success) {
        throw new Error(
          response.errors?.join(", ") || "Failed to fetch pending evaluations"
        );
      }
      return response.data?.data as Schemas["InterviewResponseDto"][];
    },
    ...staffCacheConfig,
  });
};

// ============================================================================
// ANALYTICS HOOKS
// ============================================================================

export const useInterviewStatusDistribution = (params?: {
  fromDate?: string;
  toDate?: string;
}) => {
  return useQuery({
    queryKey: [
      ...staffKeys.all,
      "interviews",
      "analytics",
      "status-distribution",
      params,
    ],
    queryFn: async () => {
      const response = await interviewService.getStatusDistribution(params);
      if (!response.success) {
        throw new Error(
          response.errors?.join(", ") || "Failed to fetch status distribution"
        );
      }
      return response.data as Record<string, number>;
    },
    ...staffCacheConfig,
  });
};

export const useInterviewTypeDistribution = (params?: {
  fromDate?: string;
  toDate?: string;
}) => {
  return useQuery({
    queryKey: [
      ...staffKeys.all,
      "interviews",
      "analytics",
      "type-distribution",
      params,
    ],
    queryFn: async () => {
      const response = await interviewService.getTypeDistribution(params);
      if (!response.success) {
        throw new Error(
          response.errors?.join(", ") || "Failed to fetch type distribution"
        );
      }
      return response.data as Record<string, number>;
    },
    ...staffCacheConfig,
  });
};

export const useInterviewAnalytics = (params?: {
  fromDate?: string;
  toDate?: string;
}) => {
  return useQuery({
    queryKey: [...staffKeys.all, "interviews", "analytics", params],
    queryFn: async () => {
      const response = await interviewService.getInterviewAnalytics(params);
      if (!response.success) {
        throw new Error(
          response.errors?.join(", ") || "Failed to fetch interview analytics"
        );
      }
      return response.data as Schemas["InterviewAnalyticsDto"];
    },
    ...staffCacheConfig,
  });
};

export const useSearchInterviews = (
  params: Schemas["InterviewSearchDto"],
  options?: { enabled?: boolean }
) => {
  const enabled = options?.enabled ?? true;
  return useQuery({
    queryKey: [...staffKeys.all, "interviews", "search", params],
    queryFn: async () => {
      const response = await interviewService.searchInterviews(params);
      if (!response.success) {
        throw new Error(
          response.errors?.join(", ") || "Failed to search interviews"
        );
      }
      return response.data as Schemas["InterviewSummaryDtoPagedResult"];
    },
    enabled,
    ...staffCacheConfig,
  });
};
