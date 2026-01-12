import type { components, paths } from "../../types/api";

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

export type Schemas = components["schemas"];

// Extract commonly used parameter types from API endpoints
export type UserSearchParams =
  paths["/api/Users"]["get"]["parameters"]["query"];
export type InterviewConflictParams =
  paths["/api/interviews/conflicts"]["get"]["parameters"]["query"];

// ============================================================================
// CACHE KEY FACTORY
// ============================================================================

/**
 * Cache key factory for staff-related queries
 *
 * CACHE STRATEGY: Short cache times for staff data
 * - Staff users need fresh data across many candidates
 * - Data changes from multiple sources (candidates, other staff)
 * - Frequent invalidation ensures data accuracy
 */
export const staffKeys = {
  all: ["staff"] as const,

  // Job Applications (Primary staff workflow)
  jobApplications: () => [...staffKeys.all, "job-applications"] as const,
  jobApplicationsByJob: (jobId: string) =>
    [...staffKeys.jobApplications(), "job", jobId] as const,
  jobApplicationsByStatus: (status: string) =>
    [...staffKeys.jobApplications(), "status", status] as const,
  jobApplicationsByCandidate: (candidateId: string) =>
    [...staffKeys.jobApplications(), "candidate", candidateId] as const,
  jobApplicationsByRecruiter: (recruiterId: string) =>
    [...staffKeys.jobApplications(), "recruiter", recruiterId] as const,
  jobApplicationSearch: (filters: Record<string, unknown>) =>
    [...staffKeys.jobApplications(), "search", filters] as const,

  // Candidate Management (On-demand access)
  candidates: () => [...staffKeys.all, "candidates"] as const,
  candidateProfile: (candidateId: string) =>
    [...staffKeys.candidates(), "profile", candidateId] as const,
  candidateResume: (candidateId: string) =>
    [...staffKeys.candidates(), "resume", candidateId] as const,
  candidateSearch: (filters: Record<string, unknown>) =>
    [...staffKeys.candidates(), "search", filters] as const,

  // Job Positions (Staff management)
  jobPositions: () => [...staffKeys.all, "job-positions"] as const,
  jobPositionById: (jobId: string) =>
    [...staffKeys.jobPositions(), jobId] as const,
  jobPositionsByStatus: (status: string) =>
    [...staffKeys.jobPositions(), "status", status] as const,
  jobPositionsByDepartment: (department: string) =>
    [...staffKeys.jobPositions(), "department", department] as const,
  jobPositionsSearch: (filters: Record<string, unknown>) =>
    [...staffKeys.jobPositions(), "search", filters] as const,

  // Job Offers (Staff management)
  jobOffers: () => [...staffKeys.all, "job-offers"] as const,
  jobOfferById: (offerId: string) =>
    [...staffKeys.jobOffers(), offerId] as const,
  jobOfferByApplication: (applicationId: string) =>
    [...staffKeys.jobOffers(), "application", applicationId] as const,
  jobOffersByStatus: (status: string) =>
    [...staffKeys.jobOffers(), "status", status] as const,
  jobOffersRequiringAction: (pageNumber: number, pageSize: number) =>
    [
      ...staffKeys.jobOffers(),
      "requiring-action",
      pageNumber,
      pageSize,
    ] as const,
  jobOffersSearch: (filters: Record<string, unknown>) =>
    [...staffKeys.jobOffers(), "search", filters] as const,
  jobOffersExpiring: (
    daysAhead: number,
    pageNumber: number,
    pageSize: number
  ) =>
    [
      ...staffKeys.jobOffers(),
      "expiring",
      daysAhead,
      pageNumber,
      pageSize,
    ] as const,
  jobOffersExpired: (pageNumber: number, pageSize: number) =>
    [...staffKeys.jobOffers(), "expired", pageNumber, pageSize] as const,
  jobOfferStatusDistribution: () =>
    [...staffKeys.jobOffers(), "status-distribution"] as const,
  jobOfferAvgOfferAmount: (jobPositionId: string) =>
    [...staffKeys.jobOffers(), "average-offer-amount", jobPositionId] as const,
  jobOfferAcceptanceRates: (fromDate: string, toDate: string) =>
    [...staffKeys.jobOffers(), "acceptance-rates", fromDate, toDate] as const,
  jobOfferAvgResponseTime: () =>
    [...staffKeys.jobOffers(), "average-response-time"] as const,
  jobOfferTrends: (
    fromDate: string,
    toDate: string,
    pageNumber: number,
    pageSize: number
  ) =>
    [
      ...staffKeys.jobOffers(),
      "trends",
      fromDate,
      toDate,
      pageNumber,
      pageSize,
    ] as const,

  // Interviews (Recruiter management)
  interviewsByApplication: (jobApplicationId: string) =>
    [...staffKeys.all, "interviews", "application", jobApplicationId] as const,

  // Analytics (Admin dashboards)
  analytics: () => [...staffKeys.all, "analytics"] as const,
  applicationAnalytics: (filters: Record<string, unknown>) =>
    [...staffKeys.analytics(), "applications", filters] as const,
  offerAnalytics: (filters: Record<string, unknown>) =>
    [...staffKeys.analytics(), "offers", filters] as const,

  // Staff Management (HR/Admin)
  staff: () => [...staffKeys.all, "staff-profiles"] as const,
  staffSearch: (filters: Record<string, unknown>) =>
    [...staffKeys.staff(), "search", filters] as const,
  staffProfile: (staffId: string) =>
    [...staffKeys.staff(), "profile", staffId] as const,
  staffProfileByUserId: (userId: string) =>
    [...staffKeys.staff(), "user", userId] as const,
  myStaffProfile: () => [...staffKeys.staff(), "my-profile"] as const,

  // User Management (Admin/HR)
  users: () => [...staffKeys.all, "users"] as const,
  userSearch: (filters: Record<string, unknown>) =>
    [...staffKeys.users(), "search", filters] as const,
  userDetails: (userId: string) =>
    [...staffKeys.users(), "details", userId] as const,
} as const;

// ============================================================================
// CACHE CONFIGURATION
// ============================================================================

/**
 * Default cache configuration for admin queries
 *
 * RATIONALE:
 * - Short stale time: Admin data changes frequently from multiple sources
 * - Short garbage collection: Don't keep stale admin data in memory
 * - Background refetch: Keep data fresh while browsing
 */
export const staffCacheConfig = {
  // Data is considered stale after 30 seconds
  staleTime: 30 * 1000,

  // Remove from cache after 2 minutes of inactivity
  gcTime: 2 * 60 * 1000,

  // Refetch data in background when stale
  refetchOnWindowFocus: true,

  // Retry failed requests
  retry: 2,

  // Retry delay
  retryDelay: (attemptIndex: number) =>
    Math.min(1000 * 2 ** attemptIndex, 30000),
} as const;

/**
 * Even shorter cache for frequently changing data (like search results)
 */
export const adminSearchCacheConfig = {
  staleTime: 10 * 1000, // 10 seconds
  gcTime: 1 * 60 * 1000, // 1 minute
  refetchOnWindowFocus: true,
  retry: 1,
} as const;

/**
 * Longer cache for relatively stable data (like job positions)
 */
export const adminStableCacheConfig = {
  staleTime: 2 * 60 * 1000, // 2 minutes
  gcTime: 5 * 60 * 1000, // 5 minutes
  refetchOnWindowFocus: false,
  retry: 2,
} as const;
