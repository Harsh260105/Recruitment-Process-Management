import { useQueryClient } from "@tanstack/react-query";
import { candidateService } from "../../services/candidateService";
import { useAuth } from "../../store";
import { candidateKeys } from "./types";

// ============================================================================
// ADVANCED PATTERNS & UTILITIES
// ============================================================================

/**
 * Prefetch candidate profile
 */
export const usePrefetchCandidateProfile = () => {
  const queryClient = useQueryClient();
  const isAuthenticated = useAuth((state) => state.auth.isAuthenticated);

  return {
    prefetchMyProfile: () => {
      if (!isAuthenticated) return;
      queryClient.prefetchQuery({
        queryKey: candidateKeys.profile(),
        queryFn: async () => {
          const data = await candidateService.getMyProfile();
          if (!data.success || !data.data) {
            throw new Error(
              data.errors?.join(", ") || "Failed to fetch profile"
            );
          }
          return data.data;
        },
        staleTime: 2 * 60 * 1000, // 2 minutes
      });
    },
    // NOTE: Prefetching *any* candidate profile by ID is an admin concern
    // (hover previews / quick views). The admin hooks expose
    // `usePrefetchCandidateProfile` which uses `staffKeys.candidateProfile(id)`.
    // Keep `prefetchMyProfile` here for the candidate self-view prefetch.
    // NOTE: Removed prefetchRelatedData() because profile endpoint
    // already includes skills, education, and work experience.
    // Prefetching separate endpoints would create redundant cache entries.
  };
};
