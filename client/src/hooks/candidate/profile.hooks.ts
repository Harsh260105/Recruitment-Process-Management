import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { candidateService } from "../../services/candidateService";
import { useAuth } from "../../store";
import { candidateKeys, type Schemas } from "./types";

// ============================================================================
// PROFILE QUERIES
// ============================================================================

/**
 * Fetch current user's candidate profile
 */
export const useCandidateProfile = () => {
  const isAuthenticated = useAuth((state) => state.auth.isAuthenticated);

  return useQuery({
    queryKey: candidateKeys.profile(),
    queryFn: async () => {
      const data = await candidateService.getMyProfile();
      if (!data.success || !data.data) {
        throw new Error(data.errors?.join(", ") || "Failed to fetch profile");
      }
      return data.data;
    },
    enabled: !!isAuthenticated,
    staleTime: 2 * 60 * 1000, // 2 minutes
  });
};

// ============================================================================
// PROFILE MUTATIONS
// ============================================================================

/**
 * Create a new candidate profile
 */
export const useCreateCandidateProfile = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (data: Schemas["CandidateProfileDto"]) => {
      const response = await candidateService.createProfile(data);

      if (!response.success || !response.data) {
        throw new Error(
          response.errors?.join(", ") || "Failed to create profile"
        );
      }

      return response.data;
    },
    onSuccess: (newProfile) => {
      queryClient.setQueryData(candidateKeys.profile(), newProfile);

      // Invalidate the profile cache to ensure fresh data
      queryClient.invalidateQueries({ queryKey: candidateKeys.profile() });

      // NOTE: We don't need to prefetch skills/education/experience separately
      // because the profile endpoint already includes all this data in the response.
      // The separate skill/education hooks should only be used for CRUD operations,
      // not for data fetching - profile data should be the single source of truth.
    },
    onError: (error) => {
      console.error("Failed to create candidate profile:", error);
    },
  });
};

/**
 * Update candidate profile
 */
export const useUpdateCandidateProfile = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (data: Schemas["UpdateCandidateProfileDto"]) => {
      const response = await candidateService.updateProfile(data);

      if (!response.success || !response.data) {
        throw new Error(
          response.errors?.join(", ") || "Failed to update profile"
        );
      }
      return response.data;
    },
    onSuccess: (updatedProfile) => {
      queryClient.setQueryData(candidateKeys.profile(), updatedProfile);

      // Invalidate related queries if necessary
      queryClient.invalidateQueries({ queryKey: candidateKeys.profile() });
    },
    onError: (error) => {
      console.error("Failed to update candidate profile:", error);
    },
  });
};

/**
 * Delete candidate profile
 */
export const useDeleteCandidateProfile = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (id: string) => {
      const response = await candidateService.deleteProfile(id);

      if (!response.success) {
        throw new Error(
          response.errors?.join(", ") || "Failed to delete profile"
        );
      }

      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: candidateKeys.all });
    },
    onError: (error) => {
      console.error("Failed to delete candidate profile:", error);
    },
  });
};

/**
 * Optimistic profile updates
 */
export const useOptimisticProfileUpdate = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (data: Schemas["UpdateCandidateProfileDto"]) => {
      const response = await candidateService.updateProfile(data);
      if (!response.success || !response.data) {
        throw new Error(
          response.errors?.join(", ") || "Failed to update profile"
        );
      }
      return response.data;
    },
    onMutate: async (updateData) => {
      // Cancel outgoing refetches
      await queryClient.cancelQueries({ queryKey: candidateKeys.profile() });

      // Snapshot the previous value
      const previousProfile = queryClient.getQueryData(candidateKeys.profile());

      // Optimistically update to the new value
      queryClient.setQueryData(candidateKeys.profile(), (old: any) => {
        if (!old) return old;
        return { ...old, ...updateData };
      });

      // Return a context object with the snapshotted value
      return { previousProfile };
    },
    onError: (err, _updateData, context) => {
      // If the mutation fails, use the context returned from onMutate to roll back
      if (context?.previousProfile) {
        queryClient.setQueryData(
          candidateKeys.profile(),
          context.previousProfile
        );
      }
      console.error("Optimistic update failed:", err);
    },
    onSettled: () => {
      // Always refetch after error or success to ensure we have the correct data
      queryClient.invalidateQueries({ queryKey: candidateKeys.profile() });
    },
  });
};
