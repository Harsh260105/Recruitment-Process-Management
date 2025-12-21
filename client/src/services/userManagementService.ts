import { apiClient } from "./apiClient";
import type { components, paths } from "../types/api";
import type { ApiResponse } from "../types/http";

type Schemas = components["schemas"];
type ApiResult<T> = Promise<ApiResponse<T>>;
type UserSearchParams = paths["/api/Users"]["get"]["parameters"]["query"];

class UserManagementService {
  /**
   * Search and filter users across roles (Admin/HR)
   */
  searchUsers(
    params: UserSearchParams = {}
  ): ApiResult<Schemas["UserSummaryDtoPagedResult"]> {
    const queryParams = new URLSearchParams();
    if (params.Search) queryParams.append("Search", params.Search);
    if (params.Roles) {
      params.Roles.forEach((role) => queryParams.append("Roles", role));
    }
    if (params.IsActive !== undefined)
      queryParams.append("IsActive", params.IsActive.toString());
    if (params.HasProfile !== undefined)
      queryParams.append("HasProfile", params.HasProfile.toString());
    if (params.PageNumber !== undefined)
      queryParams.append("PageNumber", params.PageNumber.toString());
    if (params.PageSize !== undefined)
      queryParams.append("PageSize", params.PageSize.toString());

    return apiClient.get<Schemas["UserSummaryDtoPagedResult"]>(
      `/api/Users?${queryParams.toString()}`
    );
  }

  /**
   * Get detailed information about a specific user
   */
  getUserDetails(userId: string): ApiResult<Schemas["UserDetailsDto"]> {
    return apiClient.get<Schemas["UserDetailsDto"]>(`/api/Users/${userId}`);
  }

  /**
   * Update user basic information
   */
  updateUserInfo(
    userId: string,
    data: Schemas["UpdateUserInfoRequest"]
  ): ApiResult<Schemas["UserDetailsDto"]> {
    return apiClient.patch<Schemas["UserDetailsDto"]>(
      `/api/Users/${userId}`,
      data
    );
  }

  /**
   * Bulk activate or deactivate user accounts
   */
  updateUserStatus(
    data: Schemas["UpdateUserStatusRequest"]
  ): ApiResult<Schemas["UpdateUserStatusResult"]> {
    return apiClient.patch<Schemas["UpdateUserStatusResult"]>(
      "/api/Users/status",
      data
    );
  }

  /**
   * End lockout for locked out user accounts
   */
  endUserLockout(
    data: Schemas["EndUserLockoutRequest"]
  ): ApiResult<Schemas["EndUserLockoutResult"]> {
    return apiClient.post<Schemas["EndUserLockoutResult"]>(
      "/api/Users/end-lockout",
      data
    );
  }

  /**
   * Admin force password reset for users (bulk operation)
   */
  adminResetPassword(
    data: Schemas["AdminResetPasswordRequest"]
  ): ApiResult<Schemas["AdminResetPasswordResult"]> {
    return apiClient.post<Schemas["AdminResetPasswordResult"]>(
      "/api/Users/reset-password",
      data
    );
  }

  /**
   * Bulk add or remove roles for users
   */
  manageUserRoles(
    data: Schemas["ManageUserRolesRequest"]
  ): ApiResult<Schemas["ManageUserRolesResult"]> {
    return apiClient.post<Schemas["ManageUserRolesResult"]>(
      "/api/Users/roles",
      data
    );
  }
}

export const userManagementService = new UserManagementService();
