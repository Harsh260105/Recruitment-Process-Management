import { apiClient } from "./apiClient";
import type { components } from "../types/api";
import type { ApiResponse } from "../types/http";

type Schemas = components["schemas"];
type ApiResult<T> = Promise<ApiResponse<T>>;

type StaffProfileDto = Schemas["StaffProfileResponseDto"];

type CreateStaffProfileDto = Schemas["CreateStaffProfileDto"];
type UpdateStaffProfileDto = Schemas["UpdateStaffProfileDto"];

class StaffProfileService {
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
