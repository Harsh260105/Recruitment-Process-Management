import { apiClient } from "./apiClient";
import type { ApiResponse } from "@/types/http";

export type NotificationItem = {
  notificationId: string;
  title: string;
  message: string;
  type?: string | null;
  createdAt: string;
};

type MarkAllResult = {
  markedCount: number;
};

type ApiResult<T> = Promise<ApiResponse<T>>;

class NotificationService {
  getMyNotifications(): ApiResult<NotificationItem[]> {
    return apiClient.get<NotificationItem[]>("/api/notifications/me");
  }

  markAsRead(notificationId: string): ApiResult<void> {
    return apiClient.patch<void>(`/api/notifications/${notificationId}/read`);
  }

  markAllAsRead(): ApiResult<MarkAllResult> {
    return apiClient.patch<MarkAllResult>("/api/notifications/read-all");
  }
}

export const notificationService = new NotificationService();