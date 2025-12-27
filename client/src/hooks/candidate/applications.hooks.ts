import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { jobApplicationService } from "../../services/jobApplicationService";
import { useAuth } from "../../store";
import { candidateKeys, type Schemas } from "./types";

export const useCandidateApplications = (candidateProfileId?: string) => {
  const isAuthenticated = useAuth((state) => state.auth.isAuthenticated);

  return useQuery({
    queryKey: candidateKeys.applications(candidateProfileId),
    enabled: Boolean(isAuthenticated && candidateProfileId),
    queryFn: async () => {
      if (!candidateProfileId) {
        throw new Error("Candidate profile id missing");
      }

      const response = await jobApplicationService.getApplicationsByCandidate(
        candidateProfileId
      );

      if (!response.success || !response.data) {
        throw new Error(
          response.errors?.join(", ") || "Failed to fetch applications"
        );
      }

      return response.data;
    },
    staleTime: 60 * 1000,
  });
};

export const useCandidateApplicationById = (applicationId?: string) => {
  const isAuthenticated = useAuth((state) => state.auth.isAuthenticated);

  return useQuery({
    queryKey: [...candidateKeys.applications("details"), applicationId],
    enabled: Boolean(isAuthenticated && applicationId),
    queryFn: async () => {
      if (!applicationId) {
        throw new Error("Application id required");
      }

      const response = await jobApplicationService.getApplication(
        applicationId
      );

      if (!response.success || !response.data) {
        throw new Error(
          response.errors?.join(", ") || "Failed to fetch application"
        );
      }

      return response.data as Schemas["JobApplicationCandidateViewDto"];
    },
  });
};

export const useWithdrawApplication = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (applicationId: string) => {
      const response = await jobApplicationService.withdrawApplication(
        applicationId
      );
      if (!response.success) {
        throw new Error(
          response.errors?.join(", ") || "Failed to withdraw application"
        );
      }
      return response;
    },
    onSuccess: (_, applicationId) => {
      // Invalidate and refetch applications
      queryClient.invalidateQueries({ queryKey: candidateKeys.applications() });
      queryClient.invalidateQueries({
        queryKey: [...candidateKeys.applications("details"), applicationId],
      });
    },
  });
};

export const useUpdateApplication = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      applicationId,
      data,
    }: {
      applicationId: string;
      data: Schemas["JobApplicationUpdateDto"];
    }) => {
      const response = await jobApplicationService.updateApplication(
        applicationId,
        data
      );
      if (!response.success) {
        throw new Error(
          response.errors?.join(", ") || "Failed to update application"
        );
      }
      return response;
    },
    onSuccess: (_, { applicationId }) => {
      // Invalidate and refetch applications
      queryClient.invalidateQueries({ queryKey: candidateKeys.applications() });
      queryClient.invalidateQueries({
        queryKey: [...candidateKeys.applications("details"), applicationId],
      });
    },
  });
};
