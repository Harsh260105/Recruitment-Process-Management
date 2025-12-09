import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { jobApplicationService } from "../../services/jobApplicationService";
import { useAuth } from "../../store";
import {
  adminKeys,
  adminCacheConfig,
  adminSearchCacheConfig,
  type Schemas,
} from "./types";

// ============================================================================
// JOB APPLICATION QUERIES (Admin Perspective)
// ============================================================================

/**
 * Search job applications with filters (Admin/HR/Recruiter)
 *
 * Primary way admins discover and manage candidates through their applications
 *
 * WHEN TO USE:
 * - Admin dashboard for browsing applications
 * - Candidate search and filtering workflows
 * - Application management lists
 */
export const useJobApplicationSearch = (params?: {
  status?: Schemas["ApplicationStatus"];
  jobPositionId?: string;
  candidateProfileId?: string;
  assignedRecruiterId?: string;
  appliedFromDate?: string;
  appliedToDate?: string;
  minTestScore?: number;
  maxTestScore?: number;
  pageNumber?: number;
  pageSize?: number;
}) => {
  const isAuthenticated = useAuth((state) => state.auth.isAuthenticated);

  return useQuery({
    queryKey: adminKeys.jobApplicationSearch(params || {}),
    queryFn: async () => {
      const data = await jobApplicationService.searchApplications(params);
      if (!data.success || !data.data) {
        throw new Error(
          data.errors?.join(", ") || "Failed to search applications"
        );
      }
      return data.data;
    },
    enabled: !!isAuthenticated,
    ...adminSearchCacheConfig,
  });
};

/**
 * Get job applications for a specific job position (Admin/HR/Recruiter)
 *
 * WHEN TO USE:
 * - Job position details page showing applicants
 * - Reviewing all candidates for a specific role
 * - Job-specific candidate management
 */
export const useJobApplicationsByJob = (
  jobPositionId: string,
  params?: { pageNumber?: number; pageSize?: number }
) => {
  const isAuthenticated = useAuth((state) => state.auth.isAuthenticated);

  return useQuery({
    queryKey: adminKeys.jobApplicationsByJob(jobPositionId),
    queryFn: async () => {
      const data = await jobApplicationService.getApplicationsByJob(
        jobPositionId,
        params
      );
      if (!data.success || !data.data) {
        throw new Error(
          data.errors?.join(", ") || "Failed to fetch job applications"
        );
      }
      return data.data;
    },
    enabled: !!isAuthenticated && !!jobPositionId,
    ...adminCacheConfig,
  });
};

/**
 * Get job applications by status (Admin/HR/Recruiter)
 *
 * WHEN TO USE:
 * - Status-based workflow management
 * - Review pending applications
 * - Track application progress across jobs
 */
export const useJobApplicationsByStatus = (
  status: Schemas["ApplicationStatus"],
  params?: { pageNumber?: number; pageSize?: number }
) => {
  const isAuthenticated = useAuth((state) => state.auth.isAuthenticated);

  return useQuery({
    queryKey: adminKeys.jobApplicationsByStatus(String(status)),
    queryFn: async () => {
      const data = await jobApplicationService.getApplicationsByStatus(
        status,
        params
      );
      if (!data.success || !data.data) {
        throw new Error(
          data.errors?.join(", ") || "Failed to fetch applications by status"
        );
      }
      return data.data;
    },
    enabled: !!isAuthenticated && !!status,
    ...adminCacheConfig,
  });
};

/**
 * Get applications assigned to current recruiter
 *
 * WHEN TO USE:
 * - Recruiter dashboard showing their assigned applications
 * - Personal workload management
 * - Recruiter-specific workflows
 */
export const useMyAssignedApplications = () => {
  const isAuthenticated = useAuth((state) => state.auth.isAuthenticated);

  return useQuery({
    queryKey: adminKeys.jobApplicationsByRecruiter("me"),
    queryFn: async () => {
      const data = await jobApplicationService.getMyAssignedApplications();
      if (!data.success || !data.data) {
        throw new Error(
          data.errors?.join(", ") || "Failed to fetch assigned applications"
        );
      }
      return data.data;
    },
    enabled: !!isAuthenticated,
    ...adminCacheConfig,
  });
};

/**
 * Get applications by specific candidate (Admin/HR/Recruiter)
 *
 * WHEN TO USE:
 * - Candidate profile page showing all their applications
 * - Reviewing a candidate's application history
 * - Cross-job candidate analysis
 */
export const useJobApplicationsByCandidate = (candidateProfileId: string) => {
  const isAuthenticated = useAuth((state) => state.auth.isAuthenticated);

  return useQuery({
    queryKey: adminKeys.jobApplicationsByCandidate(candidateProfileId),
    queryFn: async () => {
      const data = await jobApplicationService.getApplicationsByCandidate(
        candidateProfileId
      );
      if (!data.success || !data.data) {
        throw new Error(
          data.errors?.join(", ") || "Failed to fetch candidate applications"
        );
      }
      return data.data;
    },
    enabled: !!isAuthenticated && !!candidateProfileId,
    ...adminCacheConfig,
  });
};

/**
 * Get recent applications (Admin/HR/Recruiter)
 *
 * WHEN TO USE:
 * - Dashboard recent activity feed
 * - Quick overview of new applications
 * - Activity monitoring
 */
export const useRecentApplications = (params?: {
  pageNumber?: number;
  pageSize?: number;
}) => {
  const isAuthenticated = useAuth((state) => state.auth.isAuthenticated);

  return useQuery({
    queryKey: [...adminKeys.jobApplications(), "recent", params || {}],
    queryFn: async () => {
      const data = await jobApplicationService.getRecentApplications(params);
      if (!data.success || !data.data) {
        throw new Error(
          data.errors?.join(", ") || "Failed to fetch recent applications"
        );
      }
      return data.data;
    },
    enabled: !!isAuthenticated,
    ...adminCacheConfig,
  });
};

/**
 * Get applications requiring action (Admin/HR/Recruiter)
 *
 * WHEN TO USE:
 * - Admin dashboard action items
 * - Workflow management
 * - Priority task lists
 */
export const useApplicationsRequiringAction = (params?: {
  pageNumber?: number;
  pageSize?: number;
}) => {
  const isAuthenticated = useAuth((state) => state.auth.isAuthenticated);

  return useQuery({
    queryKey: [
      ...adminKeys.jobApplications(),
      "requiring-action",
      params || {},
    ],
    queryFn: async () => {
      const data = await jobApplicationService.getApplicationsRequiringAction(
        params
      );
      if (!data.success || !data.data) {
        throw new Error(
          data.errors?.join(", ") ||
            "Failed to fetch applications requiring action"
        );
      }
      return data.data;
    },
    enabled: !!isAuthenticated,
    ...adminCacheConfig,
  });
};

/**
 * Get specific application details (Admin/HR/Recruiter)
 *
 * WHEN TO USE:
 * - Application review page
 * - Detailed candidate assessment
 * - Application status management
 */
export const useJobApplicationById = (applicationId: string) => {
  const isAuthenticated = useAuth((state) => state.auth.isAuthenticated);

  return useQuery({
    queryKey: [...adminKeys.jobApplications(), "details", applicationId],
    queryFn: async () => {
      const data = await jobApplicationService.getApplication(applicationId);
      if (!data.success || !data.data) {
        throw new Error(
          data.errors?.join(", ") || "Failed to fetch application details"
        );
      }
      return data.data;
    },
    enabled: !!isAuthenticated && !!applicationId,
    ...adminCacheConfig,
  });
};

// ============================================================================
// APPLICATION ANALYTICS (Admin)
// ============================================================================

/**
 * Get application count for a job position
 */
export const useApplicationCount = (jobPositionId: string) => {
  const isAuthenticated = useAuth((state) => state.auth.isAuthenticated);

  return useQuery({
    queryKey: [...adminKeys.analytics(), "application-count", jobPositionId],
    queryFn: async () => {
      const data = await jobApplicationService.getApplicationCount(
        jobPositionId
      );
      if (!data.success) {
        throw new Error(
          data.errors?.join(", ") || "Failed to fetch application count"
        );
      }
      return data.data;
    },
    enabled: !!isAuthenticated && !!jobPositionId,
    ...adminCacheConfig,
  });
};

/**
 * Get application count by status
 */
export const useApplicationCountByStatus = (
  status: Schemas["ApplicationStatus"]
) => {
  const isAuthenticated = useAuth((state) => state.auth.isAuthenticated);

  return useQuery({
    queryKey: [...adminKeys.analytics(), "application-count-by-status", status],
    queryFn: async () => {
      const data = await jobApplicationService.getApplicationCountByStatus(
        status
      );
      if (!data.success) {
        throw new Error(
          data.errors?.join(", ") ||
            "Failed to fetch application count by status"
        );
      }
      return data.data;
    },
    enabled: !!isAuthenticated && !!status,
    ...adminCacheConfig,
  });
};

/**
 * Get application status distribution
 */
export const useApplicationStatusDistribution = (jobPositionId?: string) => {
  const isAuthenticated = useAuth((state) => state.auth.isAuthenticated);

  return useQuery({
    queryKey: [
      ...adminKeys.analytics(),
      "status-distribution",
      jobPositionId || "all",
    ],
    queryFn: async () => {
      const data = await jobApplicationService.getStatusDistribution(
        jobPositionId
      );
      if (!data.success) {
        throw new Error(
          data.errors?.join(", ") || "Failed to fetch status distribution"
        );
      }
      return data.data;
    },
    enabled: !!isAuthenticated,
    ...adminCacheConfig,
  });
};

// ============================================================================
// APPLICATION MUTATIONS (Admin)
// ============================================================================

/**
 * Update application status (Admin/HR/Recruiter)
 */
export const useUpdateApplicationStatus = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (params: {
      applicationId: string;
      data: Schemas["JobApplicationStatusUpdateDto"];
    }) => {
      const response = await jobApplicationService.updateApplicationStatus(
        params.applicationId,
        params.data
      );
      if (!response.success || !response.data) {
        throw new Error(
          response.errors?.join(", ") || "Failed to update application status"
        );
      }
      return response.data;
    },
    onSuccess: () => {
      // Invalidate all application-related queries to ensure fresh data
      queryClient.invalidateQueries({ queryKey: adminKeys.jobApplications() });
      queryClient.invalidateQueries({ queryKey: adminKeys.analytics() });
    },
    onError: (error) => {
      console.error("Failed to update application status:", error);
    },
  });
};

/**
 * Shortlist application (Admin/HR/Recruiter)
 */
export const useShortlistApplication = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (params: { applicationId: string; notes?: string }) => {
      const response = await jobApplicationService.shortlistApplication(
        params.applicationId,
        params.notes
      );
      if (!response.success || !response.data) {
        throw new Error(
          response.errors?.join(", ") || "Failed to shortlist application"
        );
      }
      return response.data;
    },
    onSuccess: () => {
      // Invalidate all application-related queries
      queryClient.invalidateQueries({ queryKey: adminKeys.jobApplications() });
      queryClient.invalidateQueries({ queryKey: adminKeys.analytics() });
    },
    onError: (error) => {
      console.error("Failed to shortlist application:", error);
    },
  });
};

/**
 * Reject application (Admin/HR/Recruiter)
 */
export const useRejectApplication = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (params: { applicationId: string; reason?: string }) => {
      const response = await jobApplicationService.rejectApplication(
        params.applicationId,
        params.reason
      );
      if (!response.success || !response.data) {
        throw new Error(
          response.errors?.join(", ") || "Failed to reject application"
        );
      }
      return response.data;
    },
    onSuccess: () => {
      // Invalidate all application-related queries
      queryClient.invalidateQueries({ queryKey: adminKeys.jobApplications() });
      queryClient.invalidateQueries({ queryKey: adminKeys.analytics() });
    },
    onError: (error) => {
      console.error("Failed to reject application:", error);
    },
  });
};
