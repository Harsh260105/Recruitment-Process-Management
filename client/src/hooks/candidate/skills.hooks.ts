import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { candidateService } from "../../services/candidateService";
import { skillService } from "../../services/skillService";
import { useAuth } from "../../store";
import { candidateKeys, type Schemas } from "./types";

type SkillOperation =
  | { type: "add"; data: Schemas["CreateCandidateSkillDto"] }
  | { type: "update"; id: string; data: Schemas["UpdateCandidateSkillDto"] }
  | { type: "delete"; id: string };

// ============================================================================
// SKILLS QUERIES & CRUD OPERATIONS
// ============================================================================

export const useSkillCatalog = () => {
  const isAuthenticated = useAuth((state) => state.auth.isAuthenticated);

  return useQuery({
    queryKey: candidateKeys.skillCatalog(),
    queryFn: async () => {
      const data = await skillService.getSkills();
      if (!data.success || !data.data) {
        throw new Error(
          data.errors?.join(", ") || "Failed to fetch skill catalog"
        );
      }
      return data.data;
    },
    enabled: !!isAuthenticated,
    staleTime: 1000 * 60 * 30, // Skill catalog rarely changes
  });
};

/**
 * Fetch current user's skills separately
 *
 * WHEN TO USE:
 * - Lazy loading UI (load skills section on demand)
 * - Component isolation (skills component only)
 * - Performance optimization (don't need full profile)
 *
 * NOTE: Profile endpoint also includes skills. Choose based on your use case:
 * - Use this for granular control
 * - Use useCandidateProfile().skills for complete profile data
 */
export const useCandidateSkills = () => {
  const isAuthenticated = useAuth((state) => state.auth.isAuthenticated);
  const queryClient = useQueryClient();

  return useQuery({
    queryKey: candidateKeys.skills(),
    queryFn: async () => {
      const data = await candidateService.getMySkills();
      if (!data.success || !data.data) {
        throw new Error(data.errors?.join(", ") || "Failed to fetch skills");
      }
      return data.data;
    },
    enabled: !!isAuthenticated,
    // Smart caching: If profile is already cached, use that data
    initialData: () => {
      const profile = queryClient.getQueryData<
        Schemas["CandidateProfileResponseDto"]
      >(candidateKeys.profile());
      return profile?.skills;
    },
  });
};

/**
 * Add new skills
 */
export const useAddCandidateSkills = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (data: Schemas["CreateCandidateSkillDto"][]) => {
      const response = await candidateService.addSkills(data);

      if (!response.success || !response.data) {
        throw new Error(response.errors?.join(", ") || "Failed to add skills");
      }

      return response;
    },
    onSuccess: (response) => {
      // Update skills cache directly for immediate UI feedback
      queryClient.setQueryData(candidateKeys.skills(), response.data);

      // Also invalidate profile cache to keep it in sync
      queryClient.invalidateQueries({ queryKey: candidateKeys.profile() });
    },
    onError: (error) => {
      console.error("Failed to add candidate skills:", error);
    },
  });
};

/**
 * Update a specific skill
 */
export const useUpdateCandidateSkill = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (params: {
      skillId: string;
      data: Schemas["UpdateCandidateSkillDto"];
    }) => {
      const response = await candidateService.updateSkill(
        Number(params.skillId),
        params.data
      );
      if (!response.success || !response.data) {
        throw new Error(
          response.errors?.join(", ") || "Failed to update skill"
        );
      }
      return response;
    },
    onSuccess: (response) => {
      // Update skills cache directly for immediate UI feedback
      queryClient.setQueryData<Schemas["CandidateSkillDto"][]>(
        candidateKeys.skills(),
        (oldSkills) => {
          if (!oldSkills) return [response.data!];
          return oldSkills.map((skill) =>
            skill.id === response.data!.id ? response.data! : skill
          );
        }
      );

      // Also invalidate profile cache to keep it in sync
      queryClient.invalidateQueries({ queryKey: candidateKeys.profile() });
    },
    onError: (error) => {
      console.error("Failed to update candidate skill:", error);
    },
  });
};

/**
 * Delete a skill
 */
export const useDeleteCandidateSkill = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (skillId: string) => {
      const response = await candidateService.deleteSkill(Number(skillId));

      if (!response.success) {
        throw new Error(
          response.errors?.join(", ") || "Failed to delete skill"
        );
      }

      return response;
    },
    onSuccess: (_response, skillId) => {
      // Update skills cache directly for immediate UI feedback
      queryClient.setQueryData<Schemas["CandidateSkillDto"][]>(
        candidateKeys.skills(),
        (oldSkills) => {
          if (!oldSkills) return [];
          return oldSkills.filter((skill) => String(skill.id) !== skillId);
        }
      );

      // Also invalidate profile cache to keep it in sync
      queryClient.invalidateQueries({ queryKey: candidateKeys.profile() });
    },
    onError: (error) => {
      console.error("Failed to delete candidate skill:", error);
    },
  });
};

/**
 * Bulk skills operations
 */
export const useBulkSkillsUpdate = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (operations: SkillOperation[]) => {
      const results: Array<{ operation: SkillOperation; result?: unknown }> =
        [];
      const errors: Array<{ operation: SkillOperation; error: string }> = [];

      const handleResponse = (
        response: {
          success: boolean;
          data?: unknown;
          errors?: string[] | null;
        },
        operation: SkillOperation
      ) => {
        if (response.success) {
          results.push({ operation, result: response.data });
        } else {
          errors.push({
            operation,
            error: response.errors?.join(", ") || "Unknown error",
          });
        }
      };

      for (const operation of operations) {
        try {
          switch (operation.type) {
            case "add": {
              const response = await candidateService.addSkills([
                operation.data,
              ]);
              handleResponse(response, operation);
              break;
            }
            case "update": {
              const response = await candidateService.updateSkill(
                Number(operation.id),
                operation.data
              );
              handleResponse(response, operation);
              break;
            }
            case "delete": {
              const response = await candidateService.deleteSkill(
                Number(operation.id)
              );
              handleResponse(response, operation);
              break;
            }
            default: {
              const exhaustiveCheck: never = operation;
              throw new Error(`Unknown operation type: ${exhaustiveCheck}`);
            }
          }
        } catch (error) {
          errors.push({
            operation,
            error: error instanceof Error ? error.message : "Unknown error",
          });
        }
      }

      return { results, errors };
    },
    onSuccess: ({ results, errors }) => {
      // Only invalidate if we have successful operations
      if (results.length > 0) {
        queryClient.invalidateQueries({ queryKey: candidateKeys.profile() });
      }

      // Log any errors that occurred
      if (errors.length > 0) {
        console.warn("Some bulk operations failed:", errors);
      }
    },
    onError: (error) => {
      console.error("Bulk skills update failed:", error);
    },
  });
};
