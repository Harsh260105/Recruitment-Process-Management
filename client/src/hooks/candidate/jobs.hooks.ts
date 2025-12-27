import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";

import { jobPositionService } from "@/services/jobPositionService";
import { jobApplicationService } from "@/services/jobApplicationService";
import { candidateKeys, type Schemas } from "./types";

export type JobListFilters = {
  pageNumber?: number;
  pageSize?: number;
  searchTerm?: string;
  department?: string;
  location?: string;
  experienceLevel?: string;
};

const sanitize = (value?: string) => value?.trim() || undefined;

export const usePublicJobSummaries = (filters?: JobListFilters) => {
  const processedFilters = {
    pageNumber: filters?.pageNumber ?? 1,
    pageSize: filters?.pageSize ?? 10,
    searchTerm: sanitize(filters?.searchTerm),
    department: sanitize(filters?.department),
    location: sanitize(filters?.location),
    experienceLevel: sanitize(filters?.experienceLevel),
  };

  const filterKey = JSON.stringify(processedFilters);

  return useQuery({
    queryKey: candidateKeys.jobSummaries(filterKey),
    queryFn: async () => {
      const response = await jobPositionService.getPublicJobSummaries({
        pageNumber: processedFilters.pageNumber,
        pageSize: processedFilters.pageSize,
        SearchTerm: processedFilters.searchTerm,
        Department: processedFilters.department,
        Location: processedFilters.location,
        ExperienceLevel: processedFilters.experienceLevel,
      });

      if (!response.success || !response.data) {
        throw new Error(
          response.errors?.join(", ") || "Failed to load job listings"
        );
      }

      return response.data;
    },
    staleTime: 60 * 1000,
  });
};

export const useJobPositionDetails = (jobId?: string) => {
  return useQuery({
    queryKey: candidateKeys.jobDetail(jobId),
    enabled: Boolean(jobId),
    queryFn: async () => {
      if (!jobId) {
        throw new Error("Job id is required");
      }

      const response = await jobPositionService.getJobPosition(jobId);

      if (!response.success || !response.data) {
        throw new Error(
          response.errors?.join(", ") || "Failed to load job details"
        );
      }

      return response.data as Schemas["JobPositionResponseDto"];
    },
  });
};

type ApplyPayload = Schemas["JobApplicationCreateDto"];

export const useApplyToJob = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (payload: ApplyPayload) => {
      const response = await jobApplicationService.applyForJob(payload);

      if (!response.success || !response.data) {
        throw new Error(
          response.errors?.join(", ") || "Failed to submit application"
        );
      }

      return response;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: candidateKeys.applications(),
      });
      queryClient.invalidateQueries({ queryKey: candidateKeys.jobs() });
    },
  });
};
