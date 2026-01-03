import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { jobPositionService } from "../../services/jobPositionService";
import { useAuth } from "../../store";
import {
  staffKeys,
  adminStableCacheConfig,
  staffCacheConfig,
  type Schemas,
} from "./types";
import type {
  PublicSummaryQuery,
  StaffSummaryQuery,
} from "../../services/jobPositionService";

export const useJobPositionById = (id: string) => {
  const isAuthenticated = useAuth((state) => state.auth.isAuthenticated);

  return useQuery({
    queryKey: [...staffKeys.jobPositionById(id)],
    queryFn: async () => {
      const response = await jobPositionService.getJobPosition(id);
      if (!response.success) {
        throw new Error(
          response.errors?.join(", ") || "Failed to fetch job position"
        );
      }
      return response.data as Schemas["JobPositionResponseDto"];
    },
    enabled: isAuthenticated && !!id,
    ...adminStableCacheConfig,
  });
};

export const usePublicJobSummaries = (params?: {
  pageNumber?: number;
  pageSize?: number;
  query?: PublicSummaryQuery;
}) => {
  return useQuery({
    queryKey: [...staffKeys.jobPositions(), "public-summaries", params || {}],
    queryFn: async () => {
      // Flatten the params structure to match the API endpoint
      const flatParams: PublicSummaryQuery = {
        pageNumber: params?.pageNumber,
        pageSize: params?.pageSize,
        ...params?.query,
      };
      const response = await jobPositionService.getPublicJobSummaries(
        flatParams
      );
      if (!response.success || !response.data) {
        throw new Error(
          response.errors?.join(", ") || "Failed to fetch public job summaries"
        );
      }
      return response.data;
    },
    ...adminStableCacheConfig,
  });
};

export const useStaffJobSummaries = (params?: {
  pageNumber?: number;
  pageSize?: number;
  query?: StaffSummaryQuery;
}) => {
  const isAuthenticated = useAuth((state) => state.auth.isAuthenticated);

  return useQuery({
    queryKey: [...staffKeys.jobPositions(), "staff-summaries", params || {}],
    queryFn: async () => {
      // Flatten the params structure to match the API endpoint
      const flatParams: StaffSummaryQuery = {
        pageNumber: params?.pageNumber,
        pageSize: params?.pageSize,
        ...params?.query,
      };
      const response = await jobPositionService.getStaffJobSummaries(
        flatParams
      );
      if (!response.success || !response.data) {
        throw new Error(
          response.errors?.join(", ") || "Failed to fetch staff job summaries"
        );
      }
      return response.data;
    },
    enabled: !!isAuthenticated,
    ...adminStableCacheConfig,
  });
};

export const useJobPositionExists = (id: string) => {
  const isAuthenticated = useAuth((state) => state.auth.isAuthenticated);

  return useQuery({
    queryKey: [...staffKeys.jobPositions(), "exists", id],
    queryFn: async () => {
      return await jobPositionService.jobPositionExists(id);
    },
    enabled: isAuthenticated && !!id,
    ...adminStableCacheConfig,
  });
};

export const useCreateJobPosition = () => {
  const queryClient = useQueryClient();
  const isAuthenticated = useAuth((state) => state.auth.isAuthenticated);

  return useMutation({
    mutationFn: async (data: Schemas["CreateJobPositionDto"]) => {
      if (!isAuthenticated) {
        throw new Error("Authentication required");
      }
      const response = await jobPositionService.createJobPosition(data);
      if (!response.success) {
        throw new Error(
          response.errors?.join(", ") || "Failed to create job position"
        );
      }
      return response;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: staffKeys.jobPositions() });
    },
    ...staffCacheConfig,
  });
};

export const useUpdateJobPosition = () => {
  const queryClient = useQueryClient();
  const isAuthenticated = useAuth((state) => state.auth.isAuthenticated);

  return useMutation({
    mutationFn: async ({
      id,
      data,
    }: {
      id: string;
      data: Schemas["UpdateJobPositionDto"];
    }) => {
      if (!isAuthenticated) {
        throw new Error("Authentication required");
      }
      const response = await jobPositionService.updateJobPosition(id, data);
      if (!response.success) {
        throw new Error(
          response.errors?.join(", ") || "Failed to update job position"
        );
      }
      return response;
    },
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: staffKeys.jobPositions() });
      queryClient.invalidateQueries({
        queryKey: staffKeys.jobPositionById(variables.id),
      });
    },
    ...staffCacheConfig,
  });
};

export const useDeleteJobPosition = () => {
  const queryClient = useQueryClient();
  const isAuthenticated = useAuth((state) => state.auth.isAuthenticated);

  return useMutation({
    mutationFn: async (id: string) => {
      if (!isAuthenticated) {
        throw new Error("Authentication required");
      }
      const response = await jobPositionService.deleteJobPosition(id);
      if (!response.success) {
        throw new Error(
          response.errors?.join(", ") || "Failed to delete job position"
        );
      }
      return response;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: staffKeys.jobPositions() });
    },
    ...staffCacheConfig,
  });
};

export const useCloseJobPosition = () => {
  const queryClient = useQueryClient();
  const isAuthenticated = useAuth((state) => state.auth.isAuthenticated);

  return useMutation({
    mutationFn: async (id: string) => {
      if (!isAuthenticated) {
        throw new Error("Authentication required");
      }
      const response = await jobPositionService.closeJobPosition(id);
      if (!response.success) {
        throw new Error(
          response.errors?.join(", ") || "Failed to close job position"
        );
      }
      return response;
    },
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: staffKeys.jobPositions() });
      queryClient.invalidateQueries({
        queryKey: staffKeys.jobPositionById(variables),
      });
    },
    ...staffCacheConfig,
  });
};
