import { useQuery, useQueryClient } from "@tanstack/react-query";
import { candidateService } from "../../services/candidateService";
import { useAuth } from "../../store";
import { adminKeys, adminCacheConfig, type Schemas } from "./types";

// ============================================================================
// CANDIDATE PROFILE QUERIES (Admin Perspective)
// ============================================================================

/**
 * Get candidate profile by ID (Admin/HR/Recruiter)
 *
 * WHEN TO USE:
 * - Viewing candidate details from job application
 * - Admin candidate profile review
 * - On-demand candidate data loading
 *
 * NOTE: This is for admin access to ANY candidate's profile
 * Use enabled condition to load only when candidate is selected
 */
export const useCandidateProfileById = (
  candidateId: string,
  options?: { enabled?: boolean }
) => {
  const isAuthenticated = useAuth((state) => state.auth.isAuthenticated);

  return useQuery({
    queryKey: adminKeys.candidateProfile(candidateId),
    queryFn: async () => {
      const data = await candidateService.getCandidateProfile(candidateId);
      if (!data.success || !data.data) {
        throw new Error(
          data.errors?.join(", ") || "Failed to fetch candidate profile"
        );
      }
      return data.data;
    },
    enabled: !!isAuthenticated && !!candidateId && options?.enabled !== false,
    ...adminCacheConfig,
  });
};

/**
 * Get candidate profile by user ID (Admin/HR/Recruiter)
 *
 * WHEN TO USE:
 * - When you have user ID instead of candidate profile ID
 * - Cross-referencing between user and candidate data
 * - Admin user management workflows
 */
export const useCandidateProfileByUserId = (
  userId: string,
  options?: { enabled?: boolean }
) => {
  const isAuthenticated = useAuth((state) => state.auth.isAuthenticated);

  return useQuery({
    queryKey: [...adminKeys.candidates(), "by-user", userId],
    queryFn: async () => {
      const data = await candidateService.getCandidateProfileByUserId(userId);
      if (!data.success || !data.data) {
        throw new Error(
          data.errors?.join(", ") ||
            "Failed to fetch candidate profile by user ID"
        );
      }
      return data.data;
    },
    enabled: !!isAuthenticated && !!userId && options?.enabled !== false,
    ...adminCacheConfig,
  });
};

/**
 * Get candidate resume URL by candidate ID (Admin/HR/Recruiter)
 *
 * WHEN TO USE:
 * - Downloading/viewing candidate resume
 * - Admin resume review workflows
 * - Document management
 */
export const useCandidateResumeById = (
  candidateId: string,
  options?: { enabled?: boolean }
) => {
  const isAuthenticated = useAuth((state) => state.auth.isAuthenticated);

  return useQuery({
    queryKey: adminKeys.candidateResume(candidateId),
    queryFn: async () => {
      const data = await candidateService.getCandidateResume(candidateId);
      if (!data.success || !data.data) {
        throw new Error(
          data.errors?.join(", ") || "Failed to fetch candidate resume"
        );
      }
      return data.data;
    },
    enabled: !!isAuthenticated && !!candidateId && options?.enabled !== false,
    ...adminCacheConfig,
  });
};

// ============================================================================
// CANDIDATE SEARCH & DISCOVERY (Admin)
// ============================================================================

/**
 * Search candidates through job applications
 *
 * This is a convenience hook that leverages job application search
 * to find candidates based on their application data
 *
 * WHEN TO USE:
 * - Admin candidate search functionality
 * - Finding candidates with specific skills/experience
 * - Talent pool exploration
 */
export const useAdminCandidateSearch = (searchParams?: {
  candidateProfileId?: string;
  jobPositionId?: string;
  status?: Schemas["ApplicationStatus"];
  minTestScore?: number;
  maxTestScore?: number;
  pageNumber?: number;
  pageSize?: number;
}) => {
  const isAuthenticated = useAuth((state) => state.auth.isAuthenticated);

  // This uses the job application service to find candidates
  // since candidates are primarily discovered through their applications
  return useQuery({
    queryKey: [...adminKeys.candidates(), "search", searchParams || {}],
    queryFn: async () => {
      // Import here to avoid circular dependencies
      const { jobApplicationService } = await import(
        "../../services/jobApplicationService"
      );

      const data = await jobApplicationService.searchApplications(searchParams);
      if (!data.success || !data.data) {
        throw new Error(
          data.errors?.join(", ") || "Failed to search candidates"
        );
      }

      // Extract unique candidates from applications
      const candidates =
        data.data.items?.map((app: any) => ({
          candidateProfile: app.candidateProfile,
          applicationInfo: {
            id: app.id,
            status: app.status,
            appliedDate: app.appliedDate,
            jobPosition: app.jobPosition,
          },
        })) || [];

      return {
        candidates,
        totalCount: data.data.totalCount,
        pageNumber: data.data.pageNumber,
        pageSize: data.data.pageSize,
        totalPages: data.data.totalPages,
      };
    },
    enabled: !!isAuthenticated,
    ...adminCacheConfig,
  });
};

// ============================================================================
// UTILITY HOOKS (Admin)
// ============================================================================

/**
 * Prefetch candidate profile data
 *
 * WHEN TO USE:
 * - Hovering over candidate name in applications list
 * - Optimistic data loading for better UX
 * - Preloading frequently accessed candidates
 */
export const usePrefetchCandidateProfile = () => {
  const queryClient = useQueryClient();

  return {
    prefetchProfile: (candidateId: string) => {
      if (!candidateId) return;

      queryClient.prefetchQuery({
        queryKey: adminKeys.candidateProfile(candidateId),
        queryFn: async () => {
          const data = await candidateService.getCandidateProfile(candidateId);
          if (!data.success || !data.data) {
            throw new Error(
              data.errors?.join(", ") || "Failed to fetch candidate profile"
            );
          }
          return data.data;
        },
        staleTime: adminCacheConfig.staleTime,
      });
    },

    prefetchResume: (candidateId: string) => {
      if (!candidateId) return;

      queryClient.prefetchQuery({
        queryKey: adminKeys.candidateResume(candidateId),
        queryFn: async () => {
          const data = await candidateService.getCandidateResume(candidateId);
          if (!data.success || !data.data) {
            throw new Error(
              data.errors?.join(", ") || "Failed to fetch candidate resume"
            );
          }
          return data.data;
        },
        staleTime: adminCacheConfig.staleTime,
      });
    },
  };
};

// ============================================================================
// BULK CANDIDATE OPERATIONS (Admin)
// ============================================================================

/**
 * Get multiple candidate profiles efficiently
 *
 * WHEN TO USE:
 * - Loading candidate data for application lists
 * - Bulk candidate operations
 * - Dashboard candidate summaries
 */
export const useMultipleCandidateProfiles = (candidateIds: string[]) => {
  const isAuthenticated = useAuth((state) => state.auth.isAuthenticated);

  // Create individual queries for each candidate
  const profileQueries = candidateIds.map((candidateId) =>
    useQuery({
      queryKey: adminKeys.candidateProfile(candidateId),
      queryFn: async () => {
        const data = await candidateService.getCandidateProfile(candidateId);
        if (!data.success || !data.data) {
          throw new Error(
            data.errors?.join(", ") || "Failed to fetch candidate profile"
          );
        }
        return data.data;
      },
      enabled: !!isAuthenticated && !!candidateId,
      ...adminCacheConfig,
    })
  );

  return {
    profiles: profileQueries.map((query) => query.data).filter(Boolean),
    isLoading: profileQueries.some((query) => query.isLoading),
    isError: profileQueries.some((query) => query.isError),
    errors: profileQueries.map((query) => query.error).filter(Boolean),
    refetchAll: () => profileQueries.forEach((query) => query.refetch()),
  };
};
