import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { userManagementService } from "../../services/userManagementService";
import { useAuth } from "../../store";
import {
  staffKeys,
  staffCacheConfig,
  type Schemas,
  type UserSearchParams,
} from "./types";

// ============================================================================
// USER SEARCH & LIST QUERIES
// ============================================================================

/**
 * Search and filter users (Admin/HR)
 *
 * WHEN TO USE:
 * - User management page
 * - Admin dashboard
 * - User search and filtering
 * - Role-based user management
 */
export const useUserSearch = (params?: UserSearchParams) => {
  const isAuthenticated = useAuth((state) => state.auth.isAuthenticated);

  return useQuery({
    queryKey: [...staffKeys.users(), "search", params || {}],
    queryFn: async () => {
      const data = await userManagementService.searchUsers(params || {});
      if (!data.success || !data.data) {
        throw new Error(
          data.message || data.errors?.join(", ") || "Failed to search users"
        );
      }
      return data.data;
    },
    enabled: !!isAuthenticated,
    ...staffCacheConfig,
  });
};

// ============================================================================
// USER DETAILS QUERY
// ============================================================================

/**
 * Get detailed information about a specific user (Admin/HR)
 *
 * WHEN TO USE:
 * - User details page
 * - User profile modal
 * - Admin user review
 */
export const useUserDetails = (
  userId: string,
  options?: { enabled?: boolean }
) => {
  const isAuthenticated = useAuth((state) => state.auth.isAuthenticated);

  return useQuery({
    queryKey: [...staffKeys.users(), "details", userId],
    queryFn: async () => {
      const data = await userManagementService.getUserDetails(userId);
      if (!data.success || !data.data) {
        throw new Error(
          data.errors?.join(", ") || "Failed to fetch user details"
        );
      }
      return data.data;
    },
    enabled: !!isAuthenticated && !!userId && options?.enabled !== false,
    ...staffCacheConfig,
  });
};

// ============================================================================
// USER MANAGEMENT MUTATIONS
// ============================================================================

/**
 * Update user basic information (Admin/HR)
 *
 * WHEN TO USE:
 * - User profile editing
 * - Admin user information updates
 * - Contact information management
 */
export const useUpdateUserInfo = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      userId,
      data,
    }: {
      userId: string;
      data: Schemas["UpdateUserInfoRequest"];
    }) => {
      const result = await userManagementService.updateUserInfo(userId, data);
      if (!result.success || !result.data) {
        throw new Error(
          result.errors?.join(", ") || "Failed to update user information"
        );
      }
      return result;
    },
    onSuccess: (_response, variables) => {
      // Invalidate user details and search queries
      queryClient.invalidateQueries({
        queryKey: [...staffKeys.users(), "details", variables.userId],
      });
      queryClient.invalidateQueries({ queryKey: staffKeys.users() });
    },
  });
};

/**
 * Bulk activate or deactivate user accounts (Admin/HR)
 *
 * WHEN TO USE:
 * - Bulk user status management
 * - User activation/deactivation
 * - Administrative user control
 */
export const useUpdateUserStatus = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (data: Schemas["UpdateUserStatusRequest"]) => {
      const result = await userManagementService.updateUserStatus(data);
      if (!result.success || !result.data) {
        throw new Error(
          result.errors?.join(", ") || "Failed to update user status"
        );
      }
      return result;
    },
    onSuccess: () => {
      // Invalidate all user queries
      queryClient.invalidateQueries({ queryKey: staffKeys.users() });
    },
  });
};

/**
 * End lockout for locked out user accounts (Admin)
 *
 * WHEN TO USE:
 * - Unlock locked user accounts
 * - Security management
 * - Admin user access control
 */
export const useEndUserLockout = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (data: Schemas["EndUserLockoutRequest"]) => {
      const result = await userManagementService.endUserLockout(data);
      if (!result.success || !result.data) {
        throw new Error(
          result.errors?.join(", ") || "Failed to end user lockout"
        );
      }
      return result;
    },
    onSuccess: () => {
      // Invalidate all user queries
      queryClient.invalidateQueries({ queryKey: staffKeys.users() });
    },
  });
};

/**
 * Admin force password reset for users (Admin)
 *
 * WHEN TO USE:
 * - Force password reset for users
 * - Security incident response
 * - Administrative password management
 */
export const useAdminResetPassword = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (data: Schemas["AdminResetPasswordRequest"]) => {
      const result = await userManagementService.adminResetPassword(data);
      if (!result.success || !result.data) {
        throw new Error(
          result.errors?.join(", ") || "Failed to reset user passwords"
        );
      }
      return result;
    },
    onSuccess: () => {
      // Invalidate all user queries
      queryClient.invalidateQueries({ queryKey: staffKeys.users() });
    },
  });
};

/**
 * Bulk add or remove roles for users (Admin)
 *
 * WHEN TO USE:
 * - Bulk role management
 * - Administrative role assignment
 * - Permission management
 */
export const useManageUserRoles = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (data: Schemas["ManageUserRolesRequest"]) => {
      const result = await userManagementService.manageUserRoles(data);
      if (!result.success || !result.data) {
        throw new Error(
          result.errors?.join(", ") || "Failed to manage user roles"
        );
      }
      return result;
    },
    onSuccess: () => {
      // Invalidate all user queries
      queryClient.invalidateQueries({ queryKey: staffKeys.users() });
    },
  });
};
