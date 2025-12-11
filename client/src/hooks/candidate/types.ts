import type { components } from "../../types/api";

export type Schemas = components["schemas"];

// ============================================================================
// QUERY KEYS FACTORY
// ============================================================================
/**
 * Query keys factory pattern - helps with cache management and type safety
 *
 * Benefits:
 * - Centralized key management
 * - Type-safe key generation
 * - Easy cache invalidation patterns
 * - Prevents key duplication bugs
 *
 * Best Practices:
 * - Use hierarchical structure (all -> specific)
 * - Include parameters that affect the data
 * - Use `as const` for literal types
 *
 * NOTE: These keys are for CANDIDATE SELF-ACCESS only
 * Admin access to candidate data uses separate admin query keys
 */
export const candidateKeys = {
  all: ["candidate"] as const,
  profile: () => [...candidateKeys.all, "profile"] as const,
  skills: () => [...candidateKeys.all, "skills"] as const,
  skillCatalog: () => [...candidateKeys.all, "skillCatalog"] as const,
  education: () => [...candidateKeys.all, "education"] as const,
  workExperience: () => [...candidateKeys.all, "workExperience"] as const,
  resume: () => [...candidateKeys.all, "resume"] as const,
  applications: (candidateId?: string) =>
    [...candidateKeys.all, "applications", candidateId ?? "me"] as const,
  interviews: () => [...candidateKeys.all, "interviews"] as const,
  offers: () => [...candidateKeys.all, "offers"] as const,
  jobs: () => [...candidateKeys.all, "jobs"] as const,
  jobSummaries: (filtersKey = "all") =>
    [...candidateKeys.jobs(), "summaries", filtersKey] as const,
  jobDetail: (jobId?: string) =>
    [...candidateKeys.jobs(), "detail", jobId ?? "unknown"] as const,
};
