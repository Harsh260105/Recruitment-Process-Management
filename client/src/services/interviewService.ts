import { apiClient } from "./apiClient";
import type { components, paths } from "../types/api";
import type { ApiResponse } from "../types/http";

type Schemas = components["schemas"];
type ApiResult<T> = Promise<ApiResponse<T>>;
type InterviewConflictParams =
  paths["/api/interviews/conflicts"]["get"]["parameters"]["query"];
type InterviewPublicPaged = Schemas["InterviewPublicSummaryDtoPagedResult"];
type InterviewSummaryPaged = Schemas["InterviewSummaryDtoPagedResult"];

const buildQueryString = (params?: Record<string, unknown>): string => {
  if (!params) return "";

  const query = new URLSearchParams();

  Object.entries(params).forEach(([key, value]) => {
    if (value === undefined || value === null) return;

    if (Array.isArray(value)) {
      value.forEach((item) => {
        if (item === undefined || item === null) return;
        if (item instanceof Date) {
          query.append(key, item.toISOString());
          return;
        }
        query.append(key, String(item));
      });

      return;
    }

    if (value instanceof Date) {
      query.append(key, value.toISOString());
      return;
    }

    query.append(key, String(value));
  });

  const qs = query.toString();

  return qs ? `?${qs}` : "";
};

class InterviewService {
  scheduleInterview(
    data: Schemas["ScheduleInterviewDto"]
  ): ApiResult<Schemas["InterviewResponseDto"]> {
    return apiClient.post<Schemas["InterviewResponseDto"]>(
      "/api/interviews",
      data
    );
  }

  getUpcomingInterviews(params?: {
    days?: number;
    pageNumber?: number;
    pageSize?: number;
  }): ApiResult<InterviewPublicPaged> {
    return apiClient.get<InterviewPublicPaged>(
      `/api/interviews/upcoming${buildQueryString(params)}`
    );
  }

  getUpcomingInterviewsForUser(
    userId: string,
    params?: { days?: number; pageNumber?: number; pageSize?: number }
  ): ApiResult<InterviewSummaryPaged> {
    return apiClient.get<InterviewSummaryPaged>(
      `/api/interviews/users/${userId}/upcoming${buildQueryString(params)}`
    );
  }

  getTodaysInterviews(params?: {
    participantUserId?: string;
    pageNumber?: number;
    pageSize?: number;
  }): ApiResult<InterviewSummaryPaged> {
    return apiClient.get<InterviewSummaryPaged>(
      `/api/interviews/today${buildQueryString(params)}`
    );
  }

  getInterviewsRequiringAction(params?: {
    pageNumber?: number;
    pageSize?: number;
  }): ApiResult<InterviewSummaryPaged> {
    return apiClient.get<InterviewSummaryPaged>(
      `/api/interviews/requiring-action${buildQueryString(params)}`
    );
  }

  getInterviewsByApplication(
    jobApplicationId: string,
    params?: { pageNumber?: number; pageSize?: number }
  ): ApiResult<InterviewSummaryPaged> {
    return apiClient.get<InterviewSummaryPaged>(
      `/api/interviews/applications/${jobApplicationId}${buildQueryString(
        params
      )}`
    );
  }

  getMyParticipations(params?: {
    pageNumber?: number;
    pageSize?: number;
  }): ApiResult<InterviewPublicPaged> {
    return apiClient.get<InterviewPublicPaged>(
      `/api/interviews/my-participations${buildQueryString(params)}`
    );
  }

  rescheduleInterview(
    interviewId: string,
    data: Schemas["RescheduleInterviewDto"]
  ): ApiResult<Schemas["InterviewResponseDto"]> {
    return apiClient.put<Schemas["InterviewResponseDto"]>(
      `/api/interviews/${interviewId}/reschedule`,
      data
    );
  }

  cancelInterview(
    interviewId: string,
    data: Schemas["CancelInterviewDto"]
  ): ApiResult<Schemas["InterviewResponseDto"]> {
    return apiClient.put<Schemas["InterviewResponseDto"]>(
      `/api/interviews/${interviewId}/cancel`,
      data
    );
  }

  completeInterview(
    interviewId: string,
    data: Schemas["MarkInterviewCompletedDto"]
  ): ApiResult<Schemas["InterviewResponseDto"]> {
    return apiClient.put<Schemas["InterviewResponseDto"]>(
      `/api/interviews/${interviewId}/complete`,
      data
    );
  }

  markNoShow(
    interviewId: string,
    data: Schemas["MarkInterviewNoShowDto"]
  ): ApiResult<Schemas["InterviewResponseDto"]> {
    return apiClient.put<Schemas["InterviewResponseDto"]>(
      `/api/interviews/${interviewId}/no-show`,
      data
    );
  }

  getInterviewParticipants(
    interviewId: string
  ): ApiResult<Schemas["InterviewParticipantResponseDto"][]> {
    return apiClient.get<Schemas["InterviewParticipantResponseDto"][]>(
      `/api/interviews/${interviewId}/participants`
    );
  }

  getLatestInterviewForApplication(
    jobApplicationId: string
  ): ApiResult<Schemas["InterviewResponseDto"]> {
    return apiClient.get<Schemas["InterviewResponseDto"]>(
      `/api/interviews/applications/${jobApplicationId}/latest`
    );
  }

  canScheduleInterview(jobApplicationId: string): ApiResult<boolean> {
    return apiClient.get<boolean>(
      `/api/interviews/applications/${jobApplicationId}/can-schedule`
    );
  }

  checkConflicts(params: InterviewConflictParams = {}): ApiResult<boolean> {
    return apiClient.get<boolean>(
      `/api/interviews/conflicts${buildQueryString(params)}`
    );
  }

  validateTimeSlot(data: Schemas["ValidateTimeSlotDto"]): ApiResult<void> {
    return apiClient.post<void>("/api/interviews/validate-time-slot", data);
  }

  getAvailableTimeSlots(
    data: Schemas["GetAvailableTimeSlotsRequestDto"]
  ): ApiResult<Schemas["AvailableTimeSlotDto"][]> {
    return apiClient.post<Schemas["AvailableTimeSlotDto"][]>(
      "/api/interviews/available-slots",
      data
    );
  }

  getMyAvailableTimeSlots(params?: {
    startDate: string;
    endDate: string;
    durationMinutes: number;
  }): ApiResult<Schemas["AvailableTimeSlotDto"][]> {
    return apiClient.post<Schemas["AvailableTimeSlotDto"][]>(
      "/api/interviews/available-slots",
      {
        startDate: params?.startDate,
        endDate: params?.endDate,
        durationMinutes: params?.durationMinutes || 60,
        participantUserIds: [], // Empty array means check for current user
      }
    );
  }

  getScheduledInterviews(
    data: Schemas["GetScheduledInterviewsRequestDto"]
  ): ApiResult<Schemas["ScheduledInterviewSlotDtoIEnumerableApiResponse"]> {
    return apiClient.post<
      Schemas["ScheduledInterviewSlotDtoIEnumerableApiResponse"]
    >("/api/interviews/scheduled-interviews", data);
  }

  // region: evaluations ----------------------------------------------------

  submitEvaluation(
    interviewId: string,
    data: Schemas["SubmitEvaluationDto"]
  ): ApiResult<Schemas["InterviewEvaluationResponseDtoApiResponse"]> {
    return apiClient.post<Schemas["InterviewEvaluationResponseDtoApiResponse"]>(
      `/api/interview-evaluations/interviews/${interviewId}/evaluations`,
      data
    );
  }

  getMyEvaluation(
    interviewId: string
  ): ApiResult<Schemas["InterviewEvaluationResponseDtoApiResponse"]> {
    return apiClient.get<Schemas["InterviewEvaluationResponseDtoApiResponse"]>(
      `/api/interview-evaluations/interviews/${interviewId}/my-evaluation`
    );
  }

  getAllEvaluations(
    interviewId: string
  ): ApiResult<Schemas["InterviewEvaluationResponseDtoListApiResponse"]> {
    return apiClient.get<
      Schemas["InterviewEvaluationResponseDtoListApiResponse"]
    >(`/api/interview-evaluations/interviews/${interviewId}/all`);
  }

  getAverageScore(
    interviewId: string
  ): ApiResult<Schemas["DoubleApiResponse"]> {
    return apiClient.get<Schemas["DoubleApiResponse"]>(
      `/api/interview-evaluations/interviews/${interviewId}/average-score`
    );
  }

  isEvaluationComplete(
    interviewId: string
  ): ApiResult<Schemas["BooleanApiResponse"]> {
    return apiClient.get<Schemas["BooleanApiResponse"]>(
      `/api/interview-evaluations/interviews/${interviewId}/completion-status`
    );
  }

  getRecommendation(
    interviewId: string
  ): ApiResult<Schemas["EvaluationRecommendationNullableApiResponse"]> {
    return apiClient.get<
      Schemas["EvaluationRecommendationNullableApiResponse"]
    >(`/api/interview-evaluations/interviews/${interviewId}/recommendation`);
  }

  setInterviewOutcome(
    interviewId: string,
    data: Schemas["SetInterviewOutcomeDto"]
  ): ApiResult<Schemas["InterviewResponseDtoApiResponse"]> {
    return apiClient.put<Schemas["InterviewResponseDtoApiResponse"]>(
      `/api/interview-evaluations/interviews/${interviewId}/outcome`,
      data
    );
  }

  getInterviewOutcome(
    jobApplicationId: string
  ): ApiResult<Schemas["InterviewOutcomeNullableApiResponse"]> {
    return apiClient.get<Schemas["InterviewOutcomeNullableApiResponse"]>(
      `/api/interview-evaluations/applications/${jobApplicationId}/outcome`
    );
  }

  canEvaluate(interviewId: string): ApiResult<Schemas["BooleanApiResponse"]> {
    return apiClient.get<Schemas["BooleanApiResponse"]>(
      `/api/interview-evaluations/interviews/${interviewId}/can-evaluate`
    );
  }

  isProcessComplete(
    jobApplicationId: string
  ): ApiResult<Schemas["BooleanApiResponse"]> {
    return apiClient.get<Schemas["BooleanApiResponse"]>(
      `/api/interview-evaluations/applications/${jobApplicationId}/process-complete`
    );
  }

  getPendingEvaluations(): ApiResult<
    Schemas["InterviewResponseDtoListApiResponse"]
  > {
    return apiClient.get<Schemas["InterviewResponseDtoListApiResponse"]>(
      "/api/interview-evaluations/pending"
    );
  }

  // endregion --------------------------------------------------------------

  // region: analytics ------------------------------------------------------

  getStatusDistribution(params?: {
    fromDate?: string;
    toDate?: string;
  }): ApiResult<Record<string, number>> {
    return apiClient.get<Record<string, number>>(
      `/api/interviews/analytics/status-distribution${buildQueryString(params)}`
    );
  }

  getTypeDistribution(params?: {
    fromDate?: string;
    toDate?: string;
  }): ApiResult<Record<string, number>> {
    return apiClient.get<Record<string, number>>(
      `/api/interviews/analytics/type-distribution${buildQueryString(params)}`
    );
  }

  getInterviewAnalytics(params?: {
    fromDate?: string;
    toDate?: string;
  }): ApiResult<Schemas["InterviewAnalyticsDto"]> {
    return apiClient.get<Schemas["InterviewAnalyticsDto"]>(
      `/api/interviews/analytics${buildQueryString(params)}`
    );
  }

  searchInterviews(
    data: Schemas["InterviewSearchDto"]
  ): ApiResult<Schemas["InterviewSummaryDtoPagedResult"]> {
    return apiClient.post<Schemas["InterviewSummaryDtoPagedResult"]>(
      "/api/interviews/search",
      data
    );
  }

  getInterviewDetail(
    interviewId: string
  ): ApiResult<Schemas["InterviewDetailDto"]> {
    return apiClient.get<Schemas["InterviewDetailDto"]>(
      `/api/interviews/${interviewId}`
    );
  }

  // endregion --------------------------------------------------------------
}

export const interviewService = new InterviewService();
