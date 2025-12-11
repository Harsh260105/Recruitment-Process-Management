import { useQuery } from "@tanstack/react-query";
import { jobOfferService } from "../../services/jobOfferService";
import { useAuth } from "../../store";
import { candidateKeys, type Schemas } from "./types";

export const useCandidateOffers = (params?: {
  pageNumber?: number;
  pageSize?: number;
}) => {
  const isAuthenticated = useAuth((state) => state.auth.isAuthenticated);

  return useQuery({
    queryKey: [...candidateKeys.offers(), params],
    enabled: !!isAuthenticated,
    queryFn: async () => {
      const response = await jobOfferService.getMyOffers(params);

      if (!response.success || !response.data) {
        throw new Error(
          response.errors?.join(", ") || "Failed to fetch offers"
        );
      }

      return response.data as Schemas["JobOfferSummaryDtoPagedResult"];
    },
    staleTime: 60 * 1000,
  });
};
