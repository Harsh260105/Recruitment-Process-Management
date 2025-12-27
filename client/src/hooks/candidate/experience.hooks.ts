import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { candidateService } from "../../services/candidateService";
import { useAuth } from "../../store";
import { candidateKeys, type Schemas } from "./types";

// ============================================================================
// WORK EXPERIENCE QUERIES & CRUD OPERATIONS
// ============================================================================

/**
 * Fetch current user's work experience separately
 *
 * Use case: Same as skills/education - granular control vs. profile-based fetching
 */
export const useCandidateWorkExperience = () => {
  const isAuthenticated = useAuth((state) => state.auth.isAuthenticated);
  const queryClient = useQueryClient();

  return useQuery({
    queryKey: candidateKeys.workExperience(),
    queryFn: async () => {
      const data = await candidateService.getMyWorkExperience();
      if (!data.success || !data.data) {
        throw new Error(
          data.errors?.join(", ") || "Failed to fetch work experience"
        );
      }
      return data.data;
    },
    enabled: !!isAuthenticated,
    // Smart caching: Use profile data if available
    initialData: () => {
      const profile = queryClient.getQueryData<
        Schemas["CandidateProfileResponseDto"]
      >(candidateKeys.profile());
      return profile?.workExperience;
    },
  });
};

/**
 * Add work experience entry
 */
export const useAddCandidateWorkExperience = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (data: Schemas["CreateCandidateWorkExperienceDto"]) => {
      const response = await candidateService.addWorkExperience(data);

      if (!response.success || !response.data) {
        throw new Error(
          response.errors?.join(", ") || "Failed to add work experience"
        );
      }

      return response;
    },
    onSuccess: (response) => {
      // Update work experience cache directly for immediate UI feedback
      queryClient.setQueryData<Schemas["CandidateWorkExperienceDto"][]>(
        candidateKeys.workExperience(),
        (oldWorkExperience) => {
          if (!oldWorkExperience) return [response.data!];
          return [...oldWorkExperience, response.data!];
        }
      );

      // Also invalidate profile cache to keep it in sync
      queryClient.invalidateQueries({ queryKey: candidateKeys.profile() });
    },
    onError: (error) => {
      console.error("Failed to add candidate work experience:", error);
    },
  });
};

/**
 * Update work experience entry
 */
export const useUpdateCandidateWorkExperience = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (params: {
      workExperienceId: string;
      data: Schemas["UpdateCandidateWorkExperienceDto"];
    }) => {
      const response = await candidateService.updateWorkExperience(
        params.workExperienceId,
        params.data
      );
      if (!response.success || !response.data) {
        throw new Error(
          response.errors?.join(", ") || "Failed to update work experience"
        );
      }
      return response;
    },
    onSuccess: (response) => {
      // Update work experience cache directly for immediate UI feedback
      queryClient.setQueryData<Schemas["CandidateWorkExperienceDto"][]>(
        candidateKeys.workExperience(),
        (oldWorkExperience) => {
          if (!oldWorkExperience) return [response.data!];
          return oldWorkExperience.map((workExp) =>
            workExp.id === response.data!.id ? response.data! : workExp
          );
        }
      );

      // Also invalidate profile cache to keep it in sync
      queryClient.invalidateQueries({ queryKey: candidateKeys.profile() });
    },
    onError: (error) => {
      console.error("Failed to update candidate work experience:", error);
    },
  });
};

/**
 * Delete work experience entry
 */
export const useDeleteCandidateWorkExperience = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (workExperienceId: string) => {
      const response = await candidateService.deleteWorkExperience(
        workExperienceId
      );

      if (!response.success) {
        throw new Error(
          response.errors?.join(", ") || "Failed to delete work experience"
        );
      }

      return response;
    },
    onSuccess: (_, workExperienceId) => {
      // Update work experience cache directly for immediate UI feedback
      queryClient.setQueryData<Schemas["CandidateWorkExperienceDto"][]>(
        candidateKeys.workExperience(),
        (oldWorkExperience) => {
          if (!oldWorkExperience) return [];
          return oldWorkExperience.filter(
            (workExp) => workExp.id !== workExperienceId
          );
        }
      );

      // Also invalidate profile cache to keep it in sync
      queryClient.invalidateQueries({ queryKey: candidateKeys.profile() });
    },
    onError: (error) => {
      console.error("Failed to delete candidate work experience:", error);
    },
  });
};
