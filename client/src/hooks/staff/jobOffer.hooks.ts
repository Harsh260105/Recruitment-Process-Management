import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { jobOfferService } from "@/services/jobOfferService";
import type {
  JobOfferSearchQuery,
  JobOfferListQuery,
  ExpiringOfferQuery,
  OfferTrendsQuery,
  JobOfferExtendDto,
} from "@/services/jobOfferService";
import {
  staffKeys,
  staffCacheConfig,
  adminSearchCacheConfig,
  adminStableCacheConfig,
} from "./types";
import { useAuth } from "../../store";

export const useGetOffer = (offerId: string) => {
  const isAuthenticated = useAuth((state) => state.auth.isAuthenticated);

  return useQuery({
    queryKey: staffKeys.jobOfferById(offerId),
    queryFn: async () => {
      const data = await jobOfferService.getOffer(offerId);

      if (!data.success || !data.data) {
        throw new Error(
          data.errors?.join(", ") ||
            data.message ||
            "Couldn't fetch the job offer."
        );
      }

      return data.data;
    },
    enabled: !!isAuthenticated && !!offerId,
    ...staffCacheConfig,
  });
};

export const useGetOfferByApplication = (applicationId: string) => {
  const isAuthenticated = useAuth((state) => state.auth.isAuthenticated);

  return useQuery({
    queryKey: staffKeys.jobOfferByApplication(applicationId),
    queryFn: async () => {
      const data = await jobOfferService.getOfferByApplication(applicationId);

      if (!data.success || !data.data) {
        throw new Error(
          data.errors?.join(", ") ||
            data.message ||
            "Couldn't fetch offer for given application ID."
        );
      }

      return data.data;
    },
    enabled: !!isAuthenticated && !!applicationId,
    ...staffCacheConfig,
  });
};

export const useSearchJobOffers = (
  pageNumber: number,
  pageSize: number,
  params: JobOfferSearchQuery
) => {
  const isAuthenticated = useAuth((state) => state.auth.isAuthenticated);

  return useQuery({
    queryKey: staffKeys.jobOffersSearch({ ...params, pageNumber, pageSize }),
    queryFn: async () => {
      const mergedParams = { ...params, pageNumber, pageSize };
      const data = await jobOfferService.searchOffers(mergedParams);

      if (!data.success || !data.data) {
        throw new Error(
          data.message || data.errors?.join(" ,") || "No matching results found"
        );
      }

      return data.data;
    },
    enabled: !!isAuthenticated,
    ...adminSearchCacheConfig,
  });
};

export const useGetOffersRequiringAction = (params: JobOfferListQuery) => {
  const isAuthenticated = useAuth((state) => state.auth.isAuthenticated);

  return useQuery({
    queryKey: staffKeys.jobOffersRequiringAction(
      params?.pageNumber || 1,
      params?.pageSize || 15
    ),
    queryFn: async () => {
      const data = await jobOfferService.getOffersRequiringAction(params);

      if (!data.success || !data.data) {
        throw new Error(
          data.message ||
            data.errors?.join(", ") ||
            "Couldn't fetch offers requiring action."
        );
      }

      return data.data;
    },
    enabled: !!isAuthenticated,
    ...staffCacheConfig,
  });
};

export const useGetExpiringOffers = (
  daysAhead: number,
  pageNumber: number,
  pageSize: number
) => {
  const isAuthenticated = useAuth((state) => state.auth.isAuthenticated);

  return useQuery({
    queryKey: staffKeys.jobOffersExpiring(daysAhead, pageNumber, pageSize),
    queryFn: async () => {
      const query: ExpiringOfferQuery = {
        daysAhead,
        pageNumber,
        pageSize,
      };
      const data = await jobOfferService.getExpiringOffers(query);

      if (!data.success || !data.data) {
        throw new Error(
          data.message ||
            data.errors?.join(", ") ||
            "Error fetching expiring offers."
        );
      }

      return data.data;
    },
    enabled: !!isAuthenticated,
    ...adminStableCacheConfig,
  });
};

export const useGetExpiredOffers = (params: JobOfferListQuery) => {
  const isAuthenticated = useAuth((state) => state.auth.isAuthenticated);
  return useQuery({
    queryKey: staffKeys.jobOffersExpired(
      params?.pageNumber || 1,
      params?.pageSize || 15
    ),
    queryFn: async () => {
      const data = await jobOfferService.getExpiredOffers(params);
      if (!data.success || !data.data) {
        throw new Error(
          data.message ||
            data.errors?.join(", ") ||
            "Error fetching expired offers."
        );
      }
      return data.data;
    },
    enabled: !!isAuthenticated,
    ...staffCacheConfig,
  });
};

export const useGetStatusDistribution = () => {
  const isAuthenticated = useAuth((state) => state.auth.isAuthenticated);
  return useQuery({
    queryKey: staffKeys.jobOfferStatusDistribution(),
    queryFn: async () => {
      const data = await jobOfferService.getStatusDistribution();
      if (!data.success || !data.data) {
        throw new Error(
          data.message ||
            data.errors?.join(", ") ||
            "Error fetching status distribution."
        );
      }
      return data.data;
    },
    enabled: !!isAuthenticated,
    ...staffCacheConfig,
  });
};

export const useGetAverageOfferAmount = (jobPositionId?: string) => {
  const isAuthenticated = useAuth((state) => state.auth.isAuthenticated);
  return useQuery({
    queryKey: staffKeys.jobOfferAvgOfferAmount(jobPositionId || ""),
    queryFn: async () => {
      const data = await jobOfferService.getAverageOfferAmount(jobPositionId);
      if (!data.success || !data.data) {
        throw new Error(
          data.message ||
            data.errors?.join(", ") ||
            "Error fetching average offer amount."
        );
      }
      return data.data;
    },
    enabled: !!isAuthenticated,
    ...adminStableCacheConfig,
  });
};

export const useGetOfferAcceptanceRate = (params?: {
  fromDate?: string;
  toDate?: string;
}) => {
  const isAuthenticated = useAuth((state) => state.auth.isAuthenticated);
  return useQuery({
    queryKey: staffKeys.jobOfferAcceptanceRates(
      params?.fromDate || "",
      params?.toDate || ""
    ),
    queryFn: async () => {
      const data = await jobOfferService.getOfferAcceptanceRate(params);
      if (!data.success || !data.data) {
        throw new Error(
          data.message ||
            data.errors?.join(", ") ||
            "Error fetching offer acceptance rate."
        );
      }
      return data.data;
    },
    enabled: !!isAuthenticated,
    ...adminStableCacheConfig,
  });
};

export const useGetAverageOfferResponseTime = () => {
  const isAuthenticated = useAuth((state) => state.auth.isAuthenticated);
  return useQuery({
    queryKey: staffKeys.jobOfferAvgResponseTime(),
    queryFn: async () => {
      const data = await jobOfferService.getAverageResponseTime();
      if (!data.success || !data.data) {
        throw new Error(
          data.message ||
            data.errors?.join(", ") ||
            "Error fetching average offer response time."
        );
      }
      return data.data;
    },
    enabled: !!isAuthenticated,
    ...adminStableCacheConfig,
  });
};

export const useGetOfferTrends = (
  fromDate: string,
  toDate: string,
  pageNumber: number,
  pageSize: number
) => {
  const isAuthenticated = useAuth((state) => state.auth.isAuthenticated);

  const isValidDateRange =
    fromDate && toDate && new Date(fromDate) <= new Date(toDate);

  return useQuery({
    queryKey: staffKeys.jobOfferTrends(fromDate, toDate, pageNumber, pageSize),
    queryFn: async () => {
      const query: OfferTrendsQuery = {
        fromDate,
        toDate,
        pageNumber,
        pageSize,
      };
      const data = await jobOfferService.getOfferTrends(query);
      if (!data.success || !data.data) {
        throw new Error(
          data.message ||
            data.errors?.join(", ") ||
            "Error fetching offer trends."
        );
      }
      return data.data;
    },
    enabled: !!isAuthenticated && !!isValidDateRange,
    ...adminStableCacheConfig,
  });
};

export const useExtendOffer = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (params: JobOfferExtendDto) => {
      const response = await jobOfferService.extendOffer(params);

      if (!response.success || !response.data) {
        throw new Error(
          response.message ||
            response.errors?.join(", ") ||
            "Failed to extend offer to candidate"
        );
      }

      return response;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: staffKeys.jobOffers() });
    },
  });
};

export const useWithdrawOffer = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ id, reason }: { id: string; reason?: string }) => {
      const response = await jobOfferService.withdrawOffer(id, { reason });

      if (!response.success || !response.data) {
        throw new Error(
          response.errors?.join(", ") || "Failed to withdraw offer"
        );
      }

      return response;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: staffKeys.jobOffers() });
    },
  });
};

export const useRespondToCounterOffer = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      id,
      accepted,
      revisedSalary,
      response,
    }: {
      id: string;
      accepted: boolean;
      revisedSalary?: number;
      response?: string;
    }) => {
      const responseData = await jobOfferService.respondToCounterOffer(id, {
        accepted,
        revisedSalary,
        response,
      });

      if (!responseData.success || !responseData.data) {
        throw new Error(
          responseData.errors?.join(", ") || "Failed to respond to counter offer"
        );
      }

      return responseData;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: staffKeys.jobOffers() });
    },
  });
};

export const useExtendOfferExpiry = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      id,
      newExpiryDate,
      reason,
    }: {
      id: string;
      newExpiryDate: string;
      reason?: string;
    }) => {
      const response = await jobOfferService.extendOfferExpiry(id, {
        newExpiryDate,
        reason,
      });

      if (!response.success || !response.data) {
        throw new Error(
          response.errors?.join(", ") || "Failed to extend offer expiry"
        );
      }

      return response;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: staffKeys.jobOffers() });
    },
  });
};

export const useReviseOffer = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      id,
      newSalary,
      newBenefits,
      newJoiningDate,
    }: {
      id: string;
      newSalary: number;
      newBenefits?: string;
      newJoiningDate?: string;
    }) => {
      const response = await jobOfferService.reviseOffer(id, {
        newSalary,
        newBenefits,
        newJoiningDate,
      });

      if (!response.success || !response.data) {
        throw new Error(
          response.errors?.join(", ") || "Failed to revise offer"
        );
      }

      return response;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: staffKeys.jobOffers() });
    },
  });
};

export const useMarkOfferExpired = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (id: string) => {
      const response = await jobOfferService.markOfferExpired(id);

      if (!response.success || !response.data) {
        throw new Error(
          response.errors?.join(", ") || "Failed to mark offer as expired"
        );
      }

      return response;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: staffKeys.jobOffers() });
    },
  });
};
