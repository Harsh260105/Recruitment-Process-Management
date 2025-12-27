import { apiClient } from "./apiClient";
import type { components, paths } from "../types/api";
import type { ApiResponse } from "../types/http";

type Schemas = components["schemas"];
type Paths = paths;
type JobOfferDto = Schemas["JobOfferDto"];
type JobOfferSummaryPagedResult = Schemas["JobOfferSummaryDtoPagedResult"];
type OfferStatus = Schemas["OfferStatus"];
type OfferStatusDistribution =
  Schemas["OfferStatusInt32DictionaryApiResponse"]["data"];
type JobOfferSearchQuery =
  Paths["/api/job-offers/search"]["get"]["parameters"]["query"];
type JobOfferListQuery =
  Paths["/api/job-offers/status/{status}"]["get"]["parameters"]["query"];
type ExpiringOfferQuery =
  Paths["/api/job-offers/expiring"]["get"]["parameters"]["query"];
type OfferTrendsQuery =
  Paths["/api/job-offers/analytics/trends"]["get"]["parameters"]["query"];
type ApiResult<T> = Promise<ApiResponse<T>>;

type EmptyBody = Record<string, never>;

const buildQueryString = (params?: Record<string, unknown>): string => {
  if (!params) return "";

  const query = new URLSearchParams();

  Object.entries(params).forEach(([key, value]) => {
    if (value === undefined || value === null) return;

    if (Array.isArray(value)) {
      value.forEach((item) => {
        if (item === undefined || item === null) return;
        query.append(key, String(item));
      });
      return;
    }

    query.append(key, String(value));
  });

  const qs = query.toString();
  return qs ? `?${qs}` : "";
};

class JobOfferService {
  getOffer(id: string): ApiResult<JobOfferDto> {
    return apiClient.get<JobOfferDto>(`/api/job-offers/${id}`);
  }

  getOfferByApplication(applicationId: string): ApiResult<JobOfferDto> {
    return apiClient.get<JobOfferDto>(
      `/api/job-offers/application/${applicationId}`
    );
  }

  extendOffer(data: Schemas["JobOfferExtendDto"]): ApiResult<JobOfferDto> {
    return apiClient.post<JobOfferDto>("/api/job-offers/extend", data);
  }

  withdrawOffer(
    id: string,
    data?: Schemas["JobOfferWithdrawDto"]
  ): ApiResult<JobOfferDto> {
    return apiClient.put<JobOfferDto>(
      `/api/job-offers/${id}/withdraw`,
      data ?? ({} as EmptyBody)
    );
  }

  acceptOffer(id: string): ApiResult<JobOfferDto> {
    return apiClient.put<JobOfferDto>(
      `/api/job-offers/${id}/accept`,
      {} as EmptyBody
    );
  }

  rejectOffer(
    id: string,
    data?: Schemas["JobOfferRejectDto"]
  ): ApiResult<JobOfferDto> {
    return apiClient.put<JobOfferDto>(
      `/api/job-offers/${id}/reject`,
      data ?? ({} as EmptyBody)
    );
  }

  submitCounterOffer(
    id: string,
    data: Schemas["JobOfferCounterDto"]
  ): ApiResult<JobOfferDto> {
    return apiClient.put<JobOfferDto>(`/api/job-offers/${id}/counter`, data);
  }

  respondToCounterOffer(
    id: string,
    data: Schemas["JobOfferRespondToCounterDto"]
  ): ApiResult<JobOfferDto> {
    return apiClient.put<JobOfferDto>(
      `/api/job-offers/${id}/respond-counter`,
      data
    );
  }

  extendOfferExpiry(
    id: string,
    data: Schemas["JobOfferExtendExpiryDto"]
  ): ApiResult<JobOfferDto> {
    return apiClient.put<JobOfferDto>(
      `/api/job-offers/${id}/extend-expiry`,
      data
    );
  }

  reviseOffer(
    id: string,
    data: Schemas["JobOfferReviseDto"]
  ): ApiResult<JobOfferDto> {
    return apiClient.put<JobOfferDto>(`/api/job-offers/${id}/revise`, data);
  }

  searchOffers(
    params?: JobOfferSearchQuery
  ): ApiResult<JobOfferSummaryPagedResult> {
    return apiClient.get<JobOfferSummaryPagedResult>(
      `/api/job-offers/search${buildQueryString(params)}`
    );
  }

  getOffersByStatus(
    status: OfferStatus,
    params?: JobOfferListQuery
  ): ApiResult<JobOfferSummaryPagedResult> {
    return apiClient.get<JobOfferSummaryPagedResult>(
      `/api/job-offers/status/${status}${buildQueryString(params)}`
    );
  }

  getOffersExtendedByUser(
    extendedByUserId: string,
    params?: JobOfferListQuery
  ): ApiResult<JobOfferSummaryPagedResult> {
    return apiClient.get<JobOfferSummaryPagedResult>(
      `/api/job-offers/extended-by/${extendedByUserId}${buildQueryString(
        params
      )}`
    );
  }

  getOffersRequiringAction(
    params?: JobOfferListQuery
  ): ApiResult<JobOfferSummaryPagedResult> {
    return apiClient.get<JobOfferSummaryPagedResult>(
      `/api/job-offers/requiring-action${buildQueryString(params)}`
    );
  }

  getMyOffers(
    params?: JobOfferListQuery
  ): ApiResult<JobOfferSummaryPagedResult> {
    return apiClient.get<JobOfferSummaryPagedResult>(
      `/api/job-offers/my-offers${buildQueryString(params)}`
    );
  }

  getExpiringOffers(
    params?: ExpiringOfferQuery
  ): ApiResult<JobOfferSummaryPagedResult> {
    return apiClient.get<JobOfferSummaryPagedResult>(
      `/api/job-offers/expiring${buildQueryString(params)}`
    );
  }

  getExpiredOffers(
    params?: JobOfferListQuery
  ): ApiResult<JobOfferSummaryPagedResult> {
    return apiClient.get<JobOfferSummaryPagedResult>(
      `/api/job-offers/expired${buildQueryString(params)}`
    );
  }

  markOfferExpired(id: string): ApiResult<JobOfferDto> {
    return apiClient.put<JobOfferDto>(
      `/api/job-offers/${id}/mark-expired`,
      {} as EmptyBody
    );
  }

  getStatusDistribution(): ApiResult<OfferStatusDistribution> {
    return apiClient.get<OfferStatusDistribution>(
      "/api/job-offers/analytics/status-distribution"
    );
  }

  getAverageOfferAmount(jobPositionId?: string): ApiResult<number> {
    const query = buildQueryString({ jobPositionId });
    return apiClient.get<number>(
      `/api/job-offers/analytics/average-amount${query}`
    );
  }

  getOfferAcceptanceRate(params?: {
    fromDate?: string;
    toDate?: string;
  }): ApiResult<number> {
    return apiClient.get<number>(
      `/api/job-offers/analytics/acceptance-rate${buildQueryString(params)}`
    );
  }

  getAverageResponseTime(): ApiResult<string> {
    return apiClient.get<string>("/api/job-offers/analytics/response-time");
  }

  getOfferTrends(
    params: OfferTrendsQuery
  ): ApiResult<JobOfferSummaryPagedResult> {
    return apiClient.get<JobOfferSummaryPagedResult>(
      `/api/job-offers/analytics/trends${buildQueryString(params)}`
    );
  }
}

export const jobOfferService = new JobOfferService();
