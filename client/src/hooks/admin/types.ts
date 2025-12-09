import type { components } from "../../types/api";

// ============================================================================
// TYPE DEFINITIONS
// ============================================================================

export type Schemas = components["schemas"];

// ============================================================================
// CACHE KEY FACTORY
// ============================================================================

/**
 * Cache key factory for admin-related queries
 *
 * CACHE STRATEGY: Short cache times for admin data
 * - Admin users need fresh data across many candidates
 * - Data changes from multiple sources (candidates, other admins)
 * - Frequent invalidation ensures data accuracy
 */
export const adminKeys = {
  all: ["admin"] as const,

  // Job Applications (Primary admin workflow)
  jobApplications: () => [...adminKeys.all, "job-applications"] as const,
  jobApplicationsByJob: (jobId: string) =>
    [...adminKeys.jobApplications(), "job", jobId] as const,
  jobApplicationsByStatus: (status: string) =>
    [...adminKeys.jobApplications(), "status", status] as const,
  jobApplicationsByCandidate: (candidateId: string) =>
    [...adminKeys.jobApplications(), "candidate", candidateId] as const,
  jobApplicationsByRecruiter: (recruiterId: string) =>
    [...adminKeys.jobApplications(), "recruiter", recruiterId] as const,
  jobApplicationSearch: (filters: Record<string, any>) =>
    [...adminKeys.jobApplications(), "search", filters] as const,

  // Candidate Management (On-demand access)
  candidates: () => [...adminKeys.all, "candidates"] as const,
  candidateProfile: (candidateId: string) =>
    [...adminKeys.candidates(), "profile", candidateId] as const,
  candidateResume: (candidateId: string) =>
    [...adminKeys.candidates(), "resume", candidateId] as const,

  // Job Positions (Admin management)
  jobPositions: () => [...adminKeys.all, "job-positions"] as const,
  jobPositionById: (jobId: string) =>
    [...adminKeys.jobPositions(), jobId] as const,
  jobPositionsByStatus: (status: string) =>
    [...adminKeys.jobPositions(), "status", status] as const,
  jobPositionsByDepartment: (department: string) =>
    [...adminKeys.jobPositions(), "department", department] as const,
  jobPositionsSearch: (filters: Record<string, any>) =>
    [...adminKeys.jobPositions(), "search", filters] as const,

  // Job Offers (Admin management)
  jobOffers: () => [...adminKeys.all, "job-offers"] as const,
  jobOffersByStatus: (status: string) =>
    [...adminKeys.jobOffers(), "status", status] as const,
  jobOffersRequiringAction: () =>
    [...adminKeys.jobOffers(), "requiring-action"] as const,
  jobOffersSearch: (filters: Record<string, any>) =>
    [...adminKeys.jobOffers(), "search", filters] as const,
  jobOffersExpired: (daysAhead: number) =>
    [...adminKeys.jobOffers(), "expired", daysAhead] as const,

  // Analytics (Admin dashboards)
  analytics: () => [...adminKeys.all, "analytics"] as const,
  applicationAnalytics: (filters: Record<string, any>) =>
    [...adminKeys.analytics(), "applications", filters] as const,
  offerAnalytics: (filters: Record<string, any>) =>
    [...adminKeys.analytics(), "offers", filters] as const,
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
export const adminCacheConfig = {
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
