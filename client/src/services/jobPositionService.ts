import { apiClient } from "./apiClient";
import type { components, paths } from "../types/api";
import type { ApiResponse } from "../types/http";

type Schemas = components["schemas"];
type Paths = paths;
type ApiResult<T> = Promise<ApiResponse<T>>;
type PublicSummaryPaged = Schemas["JobPositionPublicSummaryDtoPagedResult"];
type StaffSummaryPaged = Schemas["JobPositionStaffSummaryDtoPagedResult"];
export type PublicSummaryQuery =
  Paths["/api/JobPosition/summaries/public"]["get"]["parameters"]["query"];
export type StaffSummaryQuery =
  Paths["/api/JobPosition/summaries/staff"]["get"]["parameters"]["query"];

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

class JobPositionService {
  getJobPosition(id: string): ApiResult<Schemas["JobPositionResponseDto"]> {
    return apiClient.get<Schemas["JobPositionResponseDto"]>(
      `/api/JobPosition/${id}`
    );
  }

  createJobPosition(
    data: Schemas["CreateJobPositionDto"]
  ): ApiResult<Schemas["JobPositionResponseDto"]> {
    return apiClient.post<Schemas["JobPositionResponseDto"]>(
      "/api/JobPosition",
      data
    );
  }

  updateJobPosition(
    id: string,
    data: Schemas["UpdateJobPositionDto"]
  ): ApiResult<Schemas["JobPositionResponseDto"]> {
    return apiClient.patch<Schemas["JobPositionResponseDto"]>(
      `/api/JobPosition/${id}`,
      data
    );
  }

  deleteJobPosition(id: string): ApiResult<void> {
    return apiClient.delete<void>(`/api/JobPosition/${id}`);
  }

  getPublicJobSummaries(
    params?: PublicSummaryQuery
  ): ApiResult<PublicSummaryPaged> {
    return apiClient.get<PublicSummaryPaged>(
      `/api/JobPosition/summaries/public${buildQueryString(params)}`
    );
  }

  getStaffJobSummaries(
    params?: StaffSummaryQuery
  ): ApiResult<StaffSummaryPaged> {
    return apiClient.get<StaffSummaryPaged>(
      `/api/JobPosition/summaries/staff${buildQueryString(params)}`
    );
  }

  closeJobPosition(id: string): ApiResult<void> {
    return apiClient.put<void>(`/api/JobPosition/${id}/close`);
  }

  async jobPositionExists(id: string): Promise<boolean> {
    try {
      const response = await apiClient.request<void>({
        url: `/api/JobPosition/${id}/exists`,
        method: "HEAD",
      });
      return response.success;
    } catch {
      return false;
    }
  }
}

export const jobPositionService = new JobPositionService();
