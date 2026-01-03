import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { staffProfileService } from "../../services/staffProfileService";
import { useAuth } from "../../store";
import { staffKeys, staffCacheConfig, type Schemas } from "./types";

// ============================================================================
// STAFF PROFILE QUERIES
// ============================================================================

/**
 * Search staff profiles (HR/Admin)
 *
 * WHEN TO USE:
 * - Staff management page
 * - Assigning recruiters to applications
 * - Team directory
 */
export const useStaffSearch = (params?: {
  query?: string;
  department?: string;
  location?: string;
  roles?: string[];
  status?: string;
  pageNumber?: number;
  pageSize?: number;
}) => {
  const isAuthenticated = useAuth((state) => state.auth.isAuthenticated);

  return useQuery({
    queryKey: staffKeys.staffSearch(params || {}),
    queryFn: async () => {
      const data = await staffProfileService.searchStaff(params);
      if (!data.success || !data.data) {
        throw new Error(data.errors?.join(", ") || "Failed to search staff");
      }
      return data.data;
    },
    enabled: !!isAuthenticated,
    ...staffCacheConfig,
  });
};

/**
 * Get staff profile by ID
 */
export const useStaffProfile = (id: string | null) => {
  const isAuthenticated = useAuth((state) => state.auth.isAuthenticated);

  return useQuery({
    queryKey: staffKeys.staffProfile(id || ""),
    queryFn: async () => {
      if (!id) throw new Error("Staff ID is required");
      const data = await staffProfileService.getProfile(id);
      if (!data.success || !data.data) {
        throw new Error(
          data.errors?.join(", ") || "Failed to fetch staff profile"
        );
      }
      return data.data;
    },
    enabled: !!isAuthenticated && !!id,
    ...staffCacheConfig,
  });
};

/**
 * Get staff profile by User ID
 */
export const useStaffProfileByUserId = (userId: string | null) => {
  const isAuthenticated = useAuth((state) => state.auth.isAuthenticated);

  return useQuery({
    queryKey: staffKeys.staffProfileByUserId(userId || ""),
    queryFn: async () => {
      if (!userId) throw new Error("User ID is required");
      const data = await staffProfileService.getProfileByUserId(userId);
      if (!data.success || !data.data) {
        throw new Error(
          data.errors?.join(", ") || "Failed to fetch staff profile"
        );
      }
      return data.data;
    },
    enabled: !!isAuthenticated && !!userId,
    ...staffCacheConfig,
  });
};

/**
 * Get current user's staff profile
 */
export const useMyStaffProfile = () => {
  const isAuthenticated = useAuth((state) => state.auth.isAuthenticated);

  return useQuery({
    queryKey: staffKeys.myStaffProfile(),
    queryFn: async () => {
      const data = await staffProfileService.getMyProfile();
      if (!data.success || !data.data) {
        throw new Error(
          data.errors?.join(", ") || "Failed to fetch your profile"
        );
      }
      return data.data;
    },
    enabled: !!isAuthenticated,
    staleTime: 5 * 60 * 1000, // 5 minutes
    gcTime: 10 * 60 * 1000, // 10 minutes
  });
};

// ============================================================================
// STAFF PROFILE MUTATIONS
// ============================================================================

/**
 * Create staff profile
 */
export const useCreateStaffProfile = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (data: Schemas["CreateStaffProfileDto"]) => {
      const response = await staffProfileService.createProfile(data);
      if (!response.success || !response.data) {
        throw new Error(
          response.errors?.join(", ") || "Failed to create staff profile"
        );
      }
      return response;
    },
    onSuccess: () => {
      // Invalidate staff search queries
      queryClient.invalidateQueries({ queryKey: staffKeys.staffSearch({}) });
    },
  });
};

/**
 * Update staff profile
 */
export const useUpdateStaffProfile = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      id,
      data,
    }: {
      id: string;
      data: Schemas["UpdateStaffProfileDto"];
    }) => {
      const response = await staffProfileService.updateProfile(id, data);
      if (!response.success || !response.data) {
        throw new Error(
          response.errors?.join(", ") || "Failed to update staff profile"
        );
      }
      return response;
    },
    onSuccess: (_, variables) => {
      // Invalidate specific staff profile and search queries
      queryClient.invalidateQueries({
        queryKey: staffKeys.staffProfile(variables.id),
      });
      queryClient.invalidateQueries({ queryKey: staffKeys.staffSearch({}) });
      queryClient.invalidateQueries({ queryKey: staffKeys.myStaffProfile() });
    },
  });
};

/**
 * Delete staff profile
 */
export const useDeleteStaffProfile = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (id: string) => {
      const response = await staffProfileService.deleteProfile(id);
      if (!response.success) {
        throw new Error(
          response.errors?.join(", ") || "Failed to delete staff profile"
        );
      }
      return response;
    },
    onSuccess: () => {
      // Invalidate staff search queries
      queryClient.invalidateQueries({ queryKey: staffKeys.staffSearch({}) });
    },
  });
};
