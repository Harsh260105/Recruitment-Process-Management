import { apiClient } from "./apiClient";
import type { components } from "../types/api";
import type { ApiResponse } from "../types/http";

type Schemas = components["schemas"];
type ApiResult<T> = Promise<ApiResponse<T>>;

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
  getJobPositions(params?: {
    pageNumber?: number;
    pageSize?: number;
    Status?: string;
    Department?: string;
    Location?: string;
    ExperienceLevel?: string;
    SkillIds?: number[];
    CreatedFromDate?: string;
    CreatedToDate?: string;
    DeadlineFromDate?: string;
    DeadlineToDate?: string;
  }): ApiResult<Schemas["JobPositionResponseDtoPagedResult"]> {
    return apiClient.get<Schemas["JobPositionResponseDtoPagedResult"]>(
      `/api/JobPosition${buildQueryString(params)}`
    );
  }

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

  getActiveJobPositions(params?: {
    pageNumber?: number;
    pageSize?: number;
  }): ApiResult<Schemas["JobPositionResponseDtoPagedResult"]> {
    return apiClient.get<Schemas["JobPositionResponseDtoPagedResult"]>(
      `/api/JobPosition/active${buildQueryString(params)}`
    );
  }

  searchJobPositions(params: {
    searchTerm?: string;
    pageNumber?: number;
    pageSize?: number;
    department?: string;
    status?: string;
  }): ApiResult<Schemas["JobPositionResponseDtoPagedResult"]> {
    return apiClient.get<Schemas["JobPositionResponseDtoPagedResult"]>(
      `/api/JobPosition/search${buildQueryString(params)}`
    );
  }

  getJobPositionsByDepartment(
    department: string,
    params?: { pageNumber?: number; pageSize?: number }
  ): ApiResult<Schemas["JobPositionResponseDtoPagedResult"]> {
    return apiClient.get<Schemas["JobPositionResponseDtoPagedResult"]>(
      `/api/JobPosition/by-department/${department}${buildQueryString(params)}`
    );
  }

  getJobPositionsByStatus(
    status: string,
    params?: { pageNumber?: number; pageSize?: number }
  ): ApiResult<Schemas["JobPositionResponseDtoPagedResult"]> {
    return apiClient.get<Schemas["JobPositionResponseDtoPagedResult"]>(
      `/api/JobPosition/by-status/${status}${buildQueryString(params)}`
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
