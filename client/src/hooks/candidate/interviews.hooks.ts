import { useQuery } from "@tanstack/react-query";
import { interviewService } from "../../services/interviewService";
import { useAuth } from "../../store";
import { candidateKeys, type Schemas } from "./types";

export const useCandidateInterviews = (params?: {
  pageNumber?: number;
  pageSize?: number;
}) => {
  const isAuthenticated = useAuth((state) => state.auth.isAuthenticated);

  return useQuery({
    queryKey: [...candidateKeys.interviews(), params],
    enabled: !!isAuthenticated,
    queryFn: async () => {
      const response = await interviewService.getUpcomingInterviews(params);

      if (!response.success || !response.data) {
        throw new Error(
          response.errors?.join(", ") || "Failed to fetch interviews"
        );
      }

      return response.data as Schemas["InterviewPublicSummaryDtoPagedResult"];
    },
    staleTime: 30 * 1000,
  });
};
