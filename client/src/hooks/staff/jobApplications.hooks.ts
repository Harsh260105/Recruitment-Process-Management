import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { jobApplicationService } from "../../services/jobApplicationService";
import { useAuth } from "../../store";
import {
  staffKeys,
  staffCacheConfig,
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
  searchTerm?: string;
  pageNumber?: number;
  pageSize?: number;
}) => {
  const isAuthenticated = useAuth((state) => state.auth.isAuthenticated);

  return useQuery({
    queryKey: staffKeys.jobApplicationSearch(params || {}),
    queryFn: async () => {
      const data = await jobApplicationService.searchApplications(params);
      if (!data.success || !data.data) {
        throw new Error(
          data.message ||
            data.errors?.join(", ") ||
            "Failed to search applications"
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
    queryKey: staffKeys.jobApplicationsByJob(jobPositionId),
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
    ...staffCacheConfig,
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
    queryKey: staffKeys.jobApplicationsByStatus(String(status)),
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
    ...staffCacheConfig,
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
    queryKey: staffKeys.jobApplicationsByRecruiter("me"),
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
    ...staffCacheConfig,
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
    queryKey: staffKeys.jobApplicationsByCandidate(candidateProfileId),
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
    ...staffCacheConfig,
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
    queryKey: [...staffKeys.jobApplications(), "recent", params || {}],
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
    ...staffCacheConfig,
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
      ...staffKeys.jobApplications(),
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
    ...staffCacheConfig,
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
    queryKey: [...staffKeys.jobApplications(), "details", applicationId],
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
    ...staffCacheConfig,
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
    queryKey: [...staffKeys.analytics(), "application-count", jobPositionId],
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
    ...staffCacheConfig,
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
    queryKey: [...staffKeys.analytics(), "application-count-by-status", status],
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
    ...staffCacheConfig,
  });
};

/**
 * Get application status distribution
 */
export const useApplicationStatusDistribution = (jobPositionId?: string) => {
  const isAuthenticated = useAuth((state) => state.auth.isAuthenticated);

  return useQuery({
    queryKey: [
      ...staffKeys.analytics(),
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
    ...staffCacheConfig,
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
      return response;
    },
    onSuccess: () => {
      // Invalidate all application-related queries to ensure fresh data
      queryClient.invalidateQueries({ queryKey: staffKeys.jobApplications() });
      queryClient.invalidateQueries({ queryKey: staffKeys.analytics() });
    },
    onError: (error) => {
      console.error("Failed to update application status:", error);
    },
  });
};

export const useUpdateInternalNotes = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (params: { applicationId: string; notes: string }) => {
      const response = await jobApplicationService.updateInternalNotes(
        params.applicationId,
        params.notes
      );
      if (!response.success || !response.data) {
        throw new Error(
          response.errors?.join(", ") || "Failed to update internal notes"
        );
      }
      return response;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: staffKeys.jobApplications() });
    },
    onError: (error) => {
      console.error("Failed to update internal notes:", error);
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
      return response;
    },
    onSuccess: () => {
      // Invalidate all application-related queries
      queryClient.invalidateQueries({ queryKey: staffKeys.jobApplications() });
      queryClient.invalidateQueries({ queryKey: staffKeys.analytics() });
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
      return response;
    },
    onSuccess: () => {
      // Invalidate all application-related queries
      queryClient.invalidateQueries({ queryKey: staffKeys.jobApplications() });
      queryClient.invalidateQueries({ queryKey: staffKeys.analytics() });
    },
    onError: (error) => {
      console.error("Failed to reject application:", error);
    },
  });
};

/**
 * Put application on hold (Admin/HR/Recruiter)
 */
export const useHoldApplication = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (params: { applicationId: string; reason?: string }) => {
      const response = await jobApplicationService.holdApplication(
        params.applicationId,
        params.reason
      );
      if (!response.success || !response.data) {
        throw new Error(
          response.errors?.join(", ") || "Failed to put application on hold"
        );
      }
      return response;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: staffKeys.jobApplications() });
      queryClient.invalidateQueries({ queryKey: staffKeys.analytics() });
    },
    onError: (error) => {
      console.error("Failed to put application on hold:", error);
    },
  });
};

/**
 * Send assessment/test invitation (Recruiter/HR/Admin)
 */
export const useSendTestInvitation = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (params: { applicationId: string }) => {
      const response = await jobApplicationService.sendTest(
        params.applicationId
      );
      if (!response.success || !response.data) {
        throw new Error(
          response.errors?.join(", ") || "Failed to send test invitation"
        );
      }
      return response;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: staffKeys.jobApplications() });
      queryClient.invalidateQueries({ queryKey: staffKeys.analytics() });
    },
    onError: (error) => {
      console.error("Failed to send test invitation:", error);
    },
  });
};

/**
 * Mark candidate test as completed (Recruiter/HR/Admin)
 */
export const useCompleteTest = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (params: { applicationId: string; score: number }) => {
      const response = await jobApplicationService.completeTest(
        params.applicationId,
        params.score
      );
      if (!response.success || !response.data) {
        throw new Error(
          response.errors?.join(", ") || "Failed to complete test"
        );
      }
      return response;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: staffKeys.jobApplications() });
      queryClient.invalidateQueries({ queryKey: staffKeys.analytics() });
    },
    onError: (error) => {
      console.error("Failed to complete test:", error);
    },
  });
};

/**
 * Move application to review (Recruiter/HR/Admin)
 */
export const useMoveApplicationToReview = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (params: { applicationId: string }) => {
      const response = await jobApplicationService.moveToReview(
        params.applicationId
      );
      if (!response.success || !response.data) {
        throw new Error(
          response.errors?.join(", ") || "Failed to move application to review"
        );
      }
      return response;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: staffKeys.jobApplications() });
      queryClient.invalidateQueries({ queryKey: staffKeys.analytics() });
    },
    onError: (error) => {
      console.error("Failed to move application to review:", error);
    },
  });
};
