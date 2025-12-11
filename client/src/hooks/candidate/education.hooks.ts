import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { candidateService } from "../../services/candidateService";
import { useAuth } from "../../store";
import { candidateKeys, type Schemas } from "./types";

// ============================================================================
// EDUCATION QUERIES & CRUD OPERATIONS
// ============================================================================

/**
 * Fetch current user's education separately
 *
 * Use case: Same as skills - granular control vs. profile-based fetching
 */
export const useCandidateEducation = () => {
  const isAuthenticated = useAuth((state) => state.auth.isAuthenticated);
  const queryClient = useQueryClient();

  return useQuery({
    queryKey: candidateKeys.education(),
    queryFn: async () => {
      const data = await candidateService.getMyEducation();
      if (!data.success || !data.data) {
        throw new Error(data.errors?.join(", ") || "Failed to fetch education");
      }
      return data.data;
    },
    enabled: !!isAuthenticated,
    // Smart caching: Use profile data if available
    initialData: () => {
      const profile = queryClient.getQueryData<
        Schemas["CandidateProfileResponseDto"]
      >(candidateKeys.profile());
      return profile?.education;
    },
  });
};

/**
 * Add education entry
 */
export const useAddCandidateEducation = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (data: Schemas["CreateCandidateEducationDto"]) => {
      const response = await candidateService.addEducation(data);

      if (!response.success || !response.data) {
        throw new Error(
          response.errors?.join(", ") || "Failed to add education"
        );
      }

      return response.data;
    },
    onSuccess: (newEducation) => {
      // Update education cache directly for immediate UI feedback
      queryClient.setQueryData<Schemas["CandidateEducationDto"][]>(
        candidateKeys.education(),
        (oldEducation) => {
          if (!oldEducation) return [newEducation];
          return [...oldEducation, newEducation];
        }
      );

      // Also invalidate profile cache to keep it in sync
      queryClient.invalidateQueries({ queryKey: candidateKeys.profile() });
    },
    onError: (error) => {
      console.error("Failed to add candidate education:", error);
    },
  });
};

/**
 * Update education entry
 */
export const useUpdateCandidateEducation = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (params: {
      educationId: string;
      data: Schemas["UpdateCandidateEducationDto"];
    }) => {
      const response = await candidateService.updateEducation(
        params.educationId,
        params.data
      );
      if (!response.success || !response.data) {
        throw new Error(
          response.errors?.join(", ") || "Failed to update education"
        );
      }
      return response.data;
    },
    onSuccess: (updatedEducation) => {
      // Update education cache directly for immediate UI feedback
      queryClient.setQueryData<Schemas["CandidateEducationDto"][]>(
        candidateKeys.education(),
        (oldEducation) => {
          if (!oldEducation) return [updatedEducation];
          return oldEducation.map((education) =>
            education.id === updatedEducation.id ? updatedEducation : education
          );
        }
      );

      // Also invalidate profile cache to keep it in sync
      queryClient.invalidateQueries({ queryKey: candidateKeys.profile() });
    },
    onError: (error) => {
      console.error("Failed to update candidate education:", error);
    },
  });
};

/**
 * Delete education entry
 */
export const useDeleteCandidateEducation = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (educationId: string) => {
      const response = await candidateService.deleteEducation(educationId);

      if (!response.success) {
        throw new Error(
          response.errors?.join(", ") || "Failed to delete education"
        );
      }

      return response.data;
    },
    onSuccess: (_, educationId) => {
      // Update education cache directly for immediate UI feedback
      queryClient.setQueryData<Schemas["CandidateEducationDto"][]>(
        candidateKeys.education(),
        (oldEducation) => {
          if (!oldEducation) return [];
          return oldEducation.filter(
            (education) => education.id !== educationId
          );
        }
      );

      // Also invalidate profile cache to keep it in sync
      queryClient.invalidateQueries({ queryKey: candidateKeys.profile() });
    },
    onError: (error) => {
      console.error("Failed to delete candidate education:", error);
    },
  });
};
