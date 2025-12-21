import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { jobOfferService } from "../../services/jobOfferService";
import { useAuth } from "../../store";
import { candidateKeys, type Schemas } from "./types";

type PaginationParams = {
  pageNumber?: number;
  pageSize?: number;
};

const useIsAuthenticated = () => useAuth((state) => state.auth.isAuthenticated);

export const useCandidateOffers = (params?: PaginationParams) => {
  const isAuthenticated = useIsAuthenticated();

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

export const useAcceptOffer = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (offerId: string) => {
      const response = await jobOfferService.acceptOffer(offerId);
      if (!response.success || !response.data) {
        throw new Error(
          response.errors?.join(", ") || "Failed to accept offer"
        );
      }
      return response.data;
    },
    onSuccess: () => {
      // Invalidate offers query to refresh the list
      queryClient.invalidateQueries({ queryKey: candidateKeys.offers() });
    },
  });
};

export const useRejectOffer = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      offerId,
      rejectionReason,
    }: {
      offerId: string;
      rejectionReason?: string;
    }) => {
      const response = await jobOfferService.rejectOffer(
        offerId,
        rejectionReason ? { rejectionReason } : undefined
      );
      if (!response.success || !response.data) {
        throw new Error(
          response.errors?.join(", ") || "Failed to reject offer"
        );
      }
      return response.data;
    },
    onSuccess: () => {
      // Invalidate offers query to refresh the list
      queryClient.invalidateQueries({ queryKey: candidateKeys.offers() });
    },
  });
};

export const useCounterOffer = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      offerId,
      counterAmount,
      counterNotes,
    }: {
      offerId: string;
      counterAmount: number;
      counterNotes?: string;
    }) => {
      const response = await jobOfferService.submitCounterOffer(offerId, {
        counterAmount,
        counterNotes,
      });
      if (!response.success || !response.data) {
        throw new Error(
          response.errors?.join(", ") || "Failed to submit counter offer"
        );
      }
      return response.data;
    },
    onSuccess: () => {
      // Invalidate offers query to refresh the list
      queryClient.invalidateQueries({ queryKey: candidateKeys.offers() });
    },
  });
};
