import { useInfiniteQuery, useQuery } from "@tanstack/react-query";
import { interviewService } from "../../services/interviewService";
import { useAuth } from "../../store";
import { candidateKeys, type Schemas } from "./types";

type PaginationParams = {
  pageNumber?: number;
  pageSize?: number;
};

type UpcomingInterviewParams = PaginationParams & {
  days?: number;
};

const useIsAuthenticated = () => useAuth((state) => state.auth.isAuthenticated);

export const useCandidateUpcomingInterviews = (
  params?: UpcomingInterviewParams
) => {
  const isAuthenticated = useIsAuthenticated();

  return useQuery({
    queryKey: [...candidateKeys.interviews(), "upcoming", params],
    enabled: !!isAuthenticated,
    queryFn: async () => {
      const response = await interviewService.getUpcomingInterviews(params);
      if (!response.success || !response.data) {
        throw new Error(
          response.errors?.join(", ") || "Failed to fetch upcoming interviews"
        );
      }

      return response.data as Schemas["InterviewPublicSummaryDtoPagedResult"];
    },
    staleTime: 30 * 1000,
  });
};

export const useCandidateInterviewDetail = (interviewId: string) => {
  const isAuthenticated = useIsAuthenticated();

  return useQuery({
    queryKey: [...candidateKeys.interviews(), "detail", interviewId],
    enabled: !!isAuthenticated && !!interviewId,
    queryFn: async () => {
      const response = await interviewService.getInterviewDetail(interviewId);
      if (!response.success || !response.data) {
        throw new Error(
          response.errors?.join(", ") || "Failed to fetch interview details"
        );
      }

      return response.data as Schemas["InterviewDetailDto"];
    },
    staleTime: 5 * 60 * 1000,
  });
};

export const useCandidateUpcomingInterviewsInfinite = (
  params?: UpcomingInterviewParams
) => {
  const isAuthenticated = useIsAuthenticated();

  return useInfiniteQuery<
    Schemas["InterviewPublicSummaryDtoPagedResult"],
    Error,
    Schemas["InterviewPublicSummaryDtoPagedResult"],
    (string | UpcomingInterviewParams | undefined)[],
    number
  >({
    queryKey: [...candidateKeys.interviews(), "upcoming-infinite", params],
    enabled: !!isAuthenticated,
    initialPageParam: 1,
    queryFn: async ({ pageParam }) => {
      const response = await interviewService.getUpcomingInterviews({
        ...params,
        pageNumber: pageParam,
      });
      if (!response.success || !response.data) {
        throw new Error(
          response.errors?.join(", ") || "Failed to fetch upcoming interviews"
        );
      }

      return response.data as Schemas["InterviewPublicSummaryDtoPagedResult"];
    },
    getNextPageParam: (lastPage) =>
      lastPage.hasNextPage ? lastPage.pageNumber! + 1 : undefined,
    staleTime: 30 * 1000,
  });
};

export const useCandidateInterviewHistoryInfinite = (
  params?: PaginationParams
) => {
  const isAuthenticated = useIsAuthenticated();

  return useInfiniteQuery<
    Schemas["InterviewPublicSummaryDtoPagedResult"],
    Error,
    Schemas["InterviewPublicSummaryDtoPagedResult"],
    (string | PaginationParams | undefined)[],
    number
  >({
    queryKey: [...candidateKeys.interviews(), "history-infinite", params],
    enabled: !!isAuthenticated,
    initialPageParam: 1,
    queryFn: async ({ pageParam }) => {
      const response = await interviewService.getMyParticipations({
        ...params,
        pageNumber: pageParam,
      });
      if (!response.success || !response.data) {
        throw new Error(
          response.errors?.join(", ") || "Failed to fetch past interviews"
        );
      }

      return response.data as Schemas["InterviewPublicSummaryDtoPagedResult"];
    },
    getNextPageParam: (lastPage) =>
      lastPage.hasNextPage ? lastPage.pageNumber! + 1 : undefined,
    staleTime: 5 * 60 * 1000,
  });
};
