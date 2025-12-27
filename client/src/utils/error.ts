import { isAxiosError } from "axios";
import type { ApiResponse } from "@/types/http";

export const getErrorMessage = (error: unknown): string => {
  if (isAxiosError<ApiResponse>(error)) {
    if (error.response?.status === 429) {
      return "You've reached the application limit. Please wait before submitting another application.";
    }

    const payload = error.response?.data;

    if (payload) {
      // Handle errors as array
      if (Array.isArray(payload.errors)) {
        const detailedMessage = payload.errors.filter(Boolean).join(", ");
        if (detailedMessage) {
          return detailedMessage;
        }
      }

      if (payload.errors && typeof payload.errors === "object") {
        const errorMessages = Object.values(payload.errors)
          .flat()
          .filter(Boolean)
          .join(", ");
        if (errorMessages) {
          return errorMessages;
        }
      }

      if (payload.message) {
        return payload.message;
      }
    }
  }

  if (error instanceof Error) {
    return error.message;
  }

  return "Unexpected error. Please try again.";
};
