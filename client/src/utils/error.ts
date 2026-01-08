import { isAxiosError } from "axios";
import type { ApiResponse } from "@/types/http";

export const getErrorMessage = (error: unknown): string => {
  if (isAxiosError<ApiResponse>(error)) {
    if (error.response?.status === 429) {
      return "You've reached the application limit. Please wait before submitting another application.";
    }

    const payload = error.response?.data;

    if (payload) {
      // Handle errors as array (check both lowercase and uppercase for compatibility)
      const errors = payload.errors || (payload as any).Errors;
      if (Array.isArray(errors)) {
        const detailedMessage = errors.filter(Boolean).join(", ");
        if (detailedMessage) {
          return detailedMessage;
        }
      }

      if (errors && typeof errors === "object") {
        const errorMessages = Object.values(errors)
          .flat()
          .filter(Boolean)
          .join(", ");
        if (errorMessages) {
          return errorMessages;
        }
      }

      // Check message (both lowercase and uppercase for compatibility)
      const message = payload.message || (payload as any).Message;
      if (message) {
        return message;
      }
    }

    // Fallback for HTTP status errors without proper payload
    if (error.response?.status === 403) {
      return "You don't have permission to access this resource.";
    }
    if (error.response?.status === 404) {
      return "The requested resource was not found.";
    }
  }

  if (error instanceof Error) {
    return error.message;
  }

  return "Unexpected error. Please try again.";
};
