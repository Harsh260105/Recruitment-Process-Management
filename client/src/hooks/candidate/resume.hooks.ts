import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { candidateService } from "../../services/candidateService";
import { useAuth } from "../../store";
import { candidateKeys } from "./types";

// ============================================================================
// RESUME QUERIES & MUTATIONS
// ============================================================================

/**
 * Fetch resume URL
 */
export const useCandidateResumeUrl = (candidateId?: string) => {
  const isAuthenticated = useAuth((state) => state.auth.isAuthenticated);

  return useQuery({
    queryKey: [...candidateKeys.resume(), candidateId || "my"],
    queryFn: async () => {
      const data = candidateId
        ? await candidateService.getCandidateResume(candidateId)
        : await candidateService.getMyResume();

      if (!data.success) {
        // Don't throw error if resume doesn't exist, return null
        if (
          data.errors?.some(
            (err: string) =>
              err.includes("not found") || err.includes("No resume")
          )
        ) {
          return null;
        }
        throw new Error(
          data.errors?.join(", ") || "Failed to fetch resume URL"
        );
      }
      return data.data;
    },
    enabled: !!isAuthenticated,
  });
};

/**
 * Upload resume file
 */
export const useUploadCandidateResume = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (file: File) => {
      // Basic file validation
      const maxSize = 5 * 1024 * 1024; // 5MB
      const allowedTypes = [
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
      ];

      if (file.size > maxSize) {
        throw new Error("File size must be less than 5MB");
      }

      if (!allowedTypes.includes(file.type)) {
        throw new Error("Only PDF and Word documents are allowed");
      }

      const response = await candidateService.uploadResume(file);

      if (!response.success || !response.data) {
        throw new Error(
          response.errors?.join(", ") || "Failed to upload resume"
        );
      }

      return response;
    },
    onSuccess: (response) => {
      // Update resume URL cache
      queryClient.setQueryData(
        [...candidateKeys.resume(), "my"],
        response.data
      );

      // Invalidate profile to update resume status
      queryClient.invalidateQueries({ queryKey: candidateKeys.profile() });
    },
    onError: (error) => {
      console.error("Failed to upload resume:", error);
    },
  });
};

/**
 * Delete resume
 */
export const useDeleteCandidateResume = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async () => {
      const response = await candidateService.deleteResume();

      if (!response.success) {
        throw new Error(
          response.errors?.join(", ") || "Failed to delete resume"
        );
      }

      return response;
    },
    onSuccess: () => {
      // Clear resume URL from cache
      queryClient.setQueryData([...candidateKeys.resume(), "my"], null);

      // Invalidate profile to update resume status
      queryClient.invalidateQueries({ queryKey: candidateKeys.profile() });
    },
    onError: (error) => {
      console.error("Failed to delete resume:", error);
    },
  });
};
