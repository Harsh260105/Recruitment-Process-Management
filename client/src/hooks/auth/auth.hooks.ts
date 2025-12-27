import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { authService } from "../../services/authService";
import { useAuth as useAuthStore } from "../../store";
import type { components } from "../../types/api";

type Schemas = components["schemas"];

// ============================================================================
// AUTHENTICATION QUERY KEYS
// ============================================================================

export const authKeys = {
  all: ["auth"] as const,
  profile: () => [...authKeys.all, "profile"] as const,
  roles: () => [...authKeys.all, "roles"] as const,
  setup: () => [...authKeys.all, "setup"] as const,
} as const;

// ============================================================================
// PROFILE QUERIES
// ============================================================================

/**
 * Get current user profile
 *
 * WHEN TO USE:
 * - User profile page
 * - Navigation header (display user info)
 * - Profile completion checks
 */
export const useUserProfile = () => {
  const isAuthenticated = useAuthStore((state) => state.auth.isAuthenticated);

  return useQuery({
    queryKey: authKeys.profile(),
    queryFn: async () => {
      const data = await authService.getProfile();
      if (!data.success || !data.data) {
        throw new Error(
          data.errors?.join(", ") || "Failed to fetch user profile"
        );
      }
      return data.data;
    },
    enabled: !!isAuthenticated,
    staleTime: 5 * 60 * 1000, // 5 minutes
    gcTime: 10 * 60 * 1000, // 10 minutes
  });
};

/**
 * Get current user roles
 *
 * WHEN TO USE:
 * - Role-based UI rendering
 * - Permission checks
 * - Navigation menu customization
 */
export const useUserRoles = () => {
  const isAuthenticated = useAuthStore((state) => state.auth.isAuthenticated);

  return useQuery({
    queryKey: authKeys.roles(),
    queryFn: async () => {
      const data = await authService.getRoles();
      if (!data.success || !data.data) {
        throw new Error(
          data.errors?.join(", ") || "Failed to fetch user roles"
        );
      }
      return data.data;
    },
    enabled: !!isAuthenticated,
    staleTime: 5 * 60 * 1000, // 5 minutes
    gcTime: 10 * 60 * 1000, // 10 minutes
  });
};

/**
 * Check if initial system setup is needed
 *
 * WHEN TO USE:
 * - App initialization
 * - Setup wizard routing
 * - First-run detection
 */
export const useNeedsSetup = () => {
  return useQuery({
    queryKey: authKeys.setup(),
    queryFn: async () => {
      const data = await authService.needsSetup();
      if (!data.success || data.data === undefined) {
        throw new Error(
          data.errors?.join(", ") || "Failed to check setup status"
        );
      }
      return data.data;
    },
    staleTime: Infinity, // Only check once per session
    gcTime: Infinity,
  });
};

// ============================================================================
// PROFILE MUTATIONS
// ============================================================================

/**
 * Update user basic information (first name, last name, phone)
 *
 * WHEN TO USE:
 * - User profile editing
 * - Settings page
 * - Account information updates
 */
export const useUpdateBasicInfo = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (data: Schemas["UpdateBasicProfileDto"]) => {
      const result = await authService.updateBasicInfo(data);
      if (!result.success || !result.data) {
        throw new Error(
          result.errors?.join(", ") || "Failed to update basic information"
        );
      }
      return result;
    },
    onSuccess: (response) => {
      // Update the profile query cache
      if (response.data) {
        queryClient.setQueryData(authKeys.profile(), response.data);
      }

      // Invalidate to ensure fresh data
      queryClient.invalidateQueries({ queryKey: authKeys.profile() });
    },
  });
};

/**
 * Change user password
 *
 * WHEN TO USE:
 * - Password change form
 * - Security settings
 * - Account settings page
 */
export const useChangePassword = () => {
  return useMutation({
    mutationFn: async (data: Schemas["ChangePasswordDto"]) => {
      const result = await authService.changePassword(data);
      if (!result.success) {
        throw new Error(
          result.errors?.join(", ") || "Failed to change password"
        );
      }
      return result;
    },
  });
};

/**
 * Request password reset email
 *
 * WHEN TO USE:
 * - Forgot password flow
 * - Login page
 * - Password recovery
 */
export const useForgotPassword = () => {
  return useMutation({
    mutationFn: async (data: Schemas["ForgotPasswordDto"]) => {
      const result = await authService.forgotPassword(data);
      if (!result.success) {
        throw new Error(
          result.errors?.join(", ") || "Failed to request password reset"
        );
      }
      return result;
    },
  });
};

/**
 * Reset password with token
 *
 * WHEN TO USE:
 * - Password reset confirmation page
 * - After clicking email reset link
 */
export const useResetPassword = () => {
  return useMutation({
    mutationFn: async (data: Schemas["ResetPasswordDto"]) => {
      const result = await authService.resetPassword(data);
      if (!result.success) {
        throw new Error(
          result.errors?.join(", ") || "Failed to reset password"
        );
      }
      return result;
    },
  });
};

// ============================================================================
// EMAIL VERIFICATION MUTATIONS
// ============================================================================

/**
 * Confirm email with verification token
 *
 * WHEN TO USE:
 * - Email confirmation page
 * - After clicking verification link
 */
export const useConfirmEmail = () => {
  return useMutation({
    mutationFn: async (data: Schemas["ConfirmEmailDto"]) => {
      const result = await authService.confirmEmail(data);
      if (!result.success) {
        throw new Error(result.errors?.join(", ") || "Failed to confirm email");
      }
      return result;
    },
  });
};

/**
 * Resend email verification link
 *
 * WHEN TO USE:
 * - Verification reminder page
 * - Account activation flow
 */
export const useResendVerification = () => {
  return useMutation({
    mutationFn: async (data: Schemas["ResendVerificationDto"]) => {
      const result = await authService.resendVerification(data);
      if (!result.success) {
        throw new Error(
          result.errors?.join(", ") || "Failed to resend verification email"
        );
      }
      return result;
    },
  });
};

// ============================================================================
// ACCOUNT MANAGEMENT MUTATIONS (Admin)
// ============================================================================

/**
 * Delete user account (Admin or self-deletion)
 *
 * WHEN TO USE:
 * - User management page
 * - Account deletion
 * - Self-service account removal
 */
export const useDeleteUser = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (userId: string) => {
      const result = await authService.deleteUser(userId);
      if (!result.success) {
        throw new Error(
          result.errors?.join(", ") || "Failed to delete user account"
        );
      }
      return result;
    },
    onSuccess: () => {
      // Invalidate user-related queries
      queryClient.invalidateQueries({ queryKey: ["staff", "users"] });
      queryClient.invalidateQueries({ queryKey: authKeys.all });
    },
  });
};

/**
 * Unlock locked user account (Admin)
 *
 * WHEN TO USE:
 * - User management page
 * - Account recovery
 * - Admin security management
 */
export const useUnlockAccount = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (userId: string) => {
      const result = await authService.unlockAccount(userId);
      if (!result.success) {
        throw new Error(
          result.errors?.join(", ") || "Failed to unlock user account"
        );
      }
      return result;
    },
    onSuccess: () => {
      // Invalidate user-related queries
      queryClient.invalidateQueries({ queryKey: ["staff", "users"] });
    },
  });
};
