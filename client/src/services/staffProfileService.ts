import { apiClient } from "./apiClient";
import type { components } from "../types/api";
import type { ApiResponse } from "../types/http";

type Schemas = components["schemas"];
type ApiResult<T> = Promise<ApiResponse<T>>;

type StaffProfileDto = Schemas["StaffProfileResponseDto"];

type CreateStaffProfileDto = Schemas["CreateStaffProfileDto"];
type UpdateStaffProfileDto = Schemas["UpdateStaffProfileDto"];

const buildQueryString = (params?: Record<string, unknown>) => {
  if (!params) return "";

  const query = new URLSearchParams();

  Object.entries(params).forEach(([key, value]) => {
    if (value === undefined || value === null) {
      return;
    }

    if (Array.isArray(value)) {
      value.forEach((item) => {
        if (item === undefined || item === null) {
          return;
        }
        query.append(key, String(item));
      });
      return;
    }

    query.append(key, String(value));
  });

  const qs = query.toString();
  return qs ? `?${qs}` : "";
};

type PagedResult<T> = Schemas["StaffProfileResponseDtoPagedResult"] & {
  items?: T[];
};

class StaffProfileService {
  searchStaff(params?: {
    query?: string;
    department?: string;
    location?: string;
    roles?: string[];
    status?: string;
    pageNumber?: number;
    pageSize?: number;
  }): ApiResult<PagedResult<StaffProfileDto>> {
    const queryString = buildQueryString(params);
    return apiClient.get<PagedResult<StaffProfileDto>>(
      `/api/StaffProfile/search${queryString}`
    );
  }

  getProfile(id: string): ApiResult<StaffProfileDto> {
    return apiClient.get<StaffProfileDto>(`/api/StaffProfile/${id}`);
  }

  getProfileByUserId(userId: string): ApiResult<StaffProfileDto> {
    return apiClient.get<StaffProfileDto>(`/api/StaffProfile/user/${userId}`);
  }

  getMyProfile(): ApiResult<StaffProfileDto> {
    return apiClient.get<StaffProfileDto>("/api/StaffProfile/my-profile");
  }

  createProfile(data: CreateStaffProfileDto): ApiResult<StaffProfileDto> {
    return apiClient.post<StaffProfileDto>("/api/StaffProfile", data);
  }

  updateProfile(
    id: string,
    data: UpdateStaffProfileDto
  ): ApiResult<StaffProfileDto> {
    return apiClient.patch<StaffProfileDto>(`/api/StaffProfile/${id}`, data);
  }

  deleteProfile(id: string): ApiResult<void> {
    return apiClient.delete<void>(`/api/StaffProfile/${id}`);
  }
}

export const staffProfileService = new StaffProfileService();
