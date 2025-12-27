import { apiClient } from "./apiClient";
import type { components } from "../types/api";
import type { ApiResponse } from "../types/http";

type Schemas = components["schemas"];
type ApiResult<T> = Promise<ApiResponse<T>>;
type JobApplicationDetailDto =
  | Schemas["JobApplicationStaffViewDto"]
  | Schemas["JobApplicationCandidateViewDto"];

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

class JobApplicationService {
  searchApplications(params?: {
    status?: Schemas["ApplicationStatus"];
    jobPositionId?: string;
    candidateProfileId?: string;
    assignedRecruiterId?: string;
    appliedFromDate?: string;
    appliedToDate?: string;
    minTestScore?: number;
    maxTestScore?: number;
    pageNumber?: number;
    pageSize?: number;
  }): ApiResult<Schemas["JobApplicationSummaryDtoPagedResult"]> {
    return apiClient.get<Schemas["JobApplicationSummaryDtoPagedResult"]>(
      `/api/job-applications/search${buildQueryString(params)}`
    );
  }

  getApplicationCount(jobPositionId: string): ApiResult<number> {
    return apiClient.get<number>(
      `/api/job-applications/stats/job/${jobPositionId}/count`
    );
  }

  getApplicationCountByStatus(
    status: Schemas["ApplicationStatus"]
  ): ApiResult<number> {
    return apiClient.get<number>(
      `/api/job-applications/stats/status/${status}/count`
    );
  }

  getRecentApplications(params?: {
    pageNumber?: number;
    pageSize?: number;
  }): ApiResult<Schemas["JobApplicationSummaryDtoPagedResult"]> {
    return apiClient.get<Schemas["JobApplicationSummaryDtoPagedResult"]>(
      `/api/job-applications/recent${buildQueryString(params)}`
    );
  }

  getApplicationsRequiringAction(params?: {
    pageNumber?: number;
    pageSize?: number;
  }): ApiResult<Schemas["JobApplicationSummaryDtoPagedResult"]> {
    return apiClient.get<Schemas["JobApplicationSummaryDtoPagedResult"]>(
      `/api/job-applications/requiring-action${buildQueryString(params)}`
    );
  }

  getStatusDistribution(
    jobPositionId?: string
  ): ApiResult<Record<string, number>> {
    const query = jobPositionId ? `?jobPositionId=${jobPositionId}` : "";
    return apiClient.get<Record<string, number>>(
      `/api/job-applications/stats/status-distribution${query}`
    );
  }

  getApplicationHistory(
    id: string,
    params?: { pageNumber?: number; pageSize?: number }
  ): ApiResult<Schemas["JobApplicationStatusHistoryDtoPagedResult"]> {
    return apiClient.get<Schemas["JobApplicationStatusHistoryDtoPagedResult"]>(
      `/api/job-applications/${id}/history${buildQueryString(params)}`
    );
  }

  getApplication(id: string): ApiResult<JobApplicationDetailDto> {
    return apiClient.get<JobApplicationDetailDto>(
      `/api/job-applications/${id}`
    );
  }

  updateApplication(
    id: string,
    data: Schemas["JobApplicationUpdateDto"]
  ): ApiResult<Schemas["JobApplicationDto"]> {
    return apiClient.patch<Schemas["JobApplicationDto"]>(
      `/api/job-applications/${id}`,
      data
    );
  }

  deleteApplication(id: string): ApiResult<void> {
    return apiClient.delete<void>(`/api/job-applications/${id}`);
  }

  getApplicationsByJob(
    jobPositionId: string,
    params?: { pageNumber?: number; pageSize?: number }
  ): ApiResult<Schemas["JobApplicationSummaryDtoPagedResult"]> {
    return apiClient.get<Schemas["JobApplicationSummaryDtoPagedResult"]>(
      `/api/job-applications/job/${jobPositionId}${buildQueryString(params)}`
    );
  }

  getApplicationsByCandidate(
    candidateProfileId: string
  ): ApiResult<Schemas["JobApplicationSummaryDto"][]> {
    return apiClient.get<Schemas["JobApplicationSummaryDto"][]>(
      `/api/job-applications/candidate/${candidateProfileId}`
    );
  }

  getMyAssignedApplications(): ApiResult<
    Schemas["JobApplicationSummaryDto"][]
  > {
    return apiClient.get<Schemas["JobApplicationSummaryDto"][]>(
      "/api/job-applications/my-assigned"
    );
  }

  getApplicationsByStatus(
    status: Schemas["ApplicationStatus"],
    params?: { pageNumber?: number; pageSize?: number }
  ): ApiResult<Schemas["JobApplicationSummaryDtoPagedResult"]> {
    return apiClient.get<Schemas["JobApplicationSummaryDtoPagedResult"]>(
      `/api/job-applications/status/${status}${buildQueryString(params)}`
    );
  }

  applyForJob(
    data: Schemas["JobApplicationCreateDto"]
  ): ApiResult<Schemas["JobApplicationDto"]> {
    return apiClient.post<Schemas["JobApplicationDto"]>(
      "/api/job-applications",
      data
    );
  }

  updateApplicationStatus(
    id: string,
    data: Schemas["JobApplicationStatusUpdateDto"]
  ): ApiResult<Schemas["JobApplicationStaffViewDto"]> {
    return apiClient.patch<Schemas["JobApplicationStaffViewDto"]>(
      `/api/job-applications/${id}/status`,
      data
    );
  }

  shortlistApplication(
    id: string,
    notes?: string
  ): ApiResult<Schemas["JobApplicationStaffViewDto"]> {
    return apiClient.patch<Schemas["JobApplicationStaffViewDto"]>(
      `/api/job-applications/${id}/shortlist`,
      JSON.stringify(notes ?? "")
    );
  }

  rejectApplication(
    id: string,
    reason?: string
  ): ApiResult<Schemas["JobApplicationStaffViewDto"]> {
    return apiClient.patch<Schemas["JobApplicationStaffViewDto"]>(
      `/api/job-applications/${id}/reject`,
      JSON.stringify(reason ?? "")
    );
  }

  withdrawApplication(
    id: string
  ): ApiResult<Schemas["JobApplicationCandidateViewDto"]> {
    return apiClient.patch<Schemas["JobApplicationCandidateViewDto"]>(
      `/api/job-applications/${id}/withdraw`
    );
  }

  holdApplication(
    id: string,
    reason?: string
  ): ApiResult<Schemas["JobApplicationStaffViewDto"]> {
    return apiClient.patch<Schemas["JobApplicationStaffViewDto"]>(
      `/api/job-applications/${id}/hold`,
      JSON.stringify(reason ?? "")
    );
  }

  assignRecruiter(
    id: string,
    recruiterId: string
  ): ApiResult<Schemas["JobApplicationStaffViewDto"]> {
    return apiClient.patch<Schemas["JobApplicationStaffViewDto"]>(
      `/api/job-applications/${id}/assign-recruiter/${recruiterId}`
    );
  }

  sendTest(id: string): ApiResult<Schemas["JobApplicationStaffViewDto"]> {
    return apiClient.patch<Schemas["JobApplicationStaffViewDto"]>(
      `/api/job-applications/${id}/send-test`
    );
  }

  completeTest(
    id: string,
    score: number
  ): ApiResult<Schemas["JobApplicationStaffViewDto"]> {
    return apiClient.patch<Schemas["JobApplicationStaffViewDto"]>(
      `/api/job-applications/${id}/complete-test`,
      score
    );
  }

  moveToReview(id: string): ApiResult<Schemas["JobApplicationStaffViewDto"]> {
    return apiClient.patch<Schemas["JobApplicationStaffViewDto"]>(
      `/api/job-applications/${id}/move-to-review`
    );
  }

  updateInternalNotes(
    id: string,
    notes: string
  ): ApiResult<Schemas["JobApplicationStaffViewDto"]> {
    return apiClient.patch<Schemas["JobApplicationStaffViewDto"]>(
      `/api/job-applications/${id}/internal-notes`,
      JSON.stringify(notes)
    );
  }
}

export const jobApplicationService = new JobApplicationService();
