import { apiClient } from "./apiClient";
import type { components } from "../types/api";
import type { ApiResponse } from "../types/http";

type Schemas = components["schemas"];
type ApiResult<T> = Promise<ApiResponse<T>>;

const REFRESH_TOKEN_MESSAGE =
  "Refresh token is managed via secure HTTP-only cookies.";

class AuthService {
  /** Authenticate user and persist the access token locally. */
  async login(credentials: Schemas["LoginDto"]): ApiResult<Schemas["AuthResponseDto"]> {
    
    const response = await apiClient.login(credentials);
    
    if (response.success && response.data?.token) {
      this.setAccessToken(response.data.token);
    }

    return response;
  }

  /** Register a candidate (no automatic login). */
  registerCandidate(data: Schemas["CandidateRegisterDto"]): ApiResult<Schemas["RegisterResponseDto"]> {
    return apiClient.registerCandidate(data);
  }

  /** Register staff and log them in immediately (as per backend behaviour). */
  async registerStaff(data: Schemas["RegisterStaffDto"]): ApiResult<Schemas["AuthResponseDto"]> {
    const response = await apiClient.post<Schemas["AuthResponseDto"]>(
      "/api/Authentication/register/staff",
      data
    );

    if (response.success && response.data?.token) {
      this.setAccessToken(response.data.token);
    }

    return response;
  }

  /** Register the initial super admin, storing the issued access token. */
  async registerInitialAdmin(data: Schemas["InitialAdminDto"]): ApiResult<Schemas["AuthResponseDto"]> {
    
    const response = await apiClient.post<Schemas["AuthResponseDto"]>(
      "/api/Authentication/register/initial-admin",
      data
    );
    
    if (response.success && response.data?.token) {
      this.setAccessToken(response.data.token);
    }

    return response;
  }

  /** Bulk import candidates from an Excel sheet. */
  async bulkRegisterCandidates(file: File): ApiResult<Schemas["RegisterResponseDto"][]> {
    
    const formData = new FormData();
    formData.append("file", file);

    return apiClient.postForm<Schemas["RegisterResponseDto"][]>(
      "/api/Authentication/register/bulk-candidates",
      formData
    );
  }

  /** Refresh JWT using the secure cookie stored by the backend. */
  async refreshToken(): ApiResult<Schemas["AuthResponseDto"]> {
    
    const response = await apiClient.refreshToken();
    
    if (response.success && response.data?.token) {
      this.setAccessToken(response.data.token);
    }

    return response;
  }

  /** Retrieve profile information for the currently authenticated user. */
  getProfile(): ApiResult<Schemas["UserProfileDto"]> {
    return apiClient.getProfile();
  }

  changePassword(data: Schemas["ChangePasswordDto"]): ApiResult<unknown> {
    return apiClient.post<unknown>("/api/Authentication/change-password", data);
  }

  forgotPassword(data: Schemas["ForgotPasswordDto"]): ApiResult<unknown> {
    return apiClient.post<unknown>("/api/Authentication/forgot-password", data);
  }

  resetPassword(data: Schemas["ResetPasswordDto"]): ApiResult<unknown> {
    return apiClient.post<unknown>("/api/Authentication/reset-password", data);
  }

  confirmEmail(data: Schemas["ConfirmEmailDto"]): ApiResult<unknown> {
    return apiClient.post<unknown>("/api/Authentication/confirm-email", data);
  }

  resendVerification(data: Schemas["ResendVerificationDto"]): ApiResult<unknown> {
    return apiClient.post<unknown>("/api/Authentication/resend-verification", data);
  }

  async logout(): ApiResult<void> {
    const response = await apiClient.logout();
    this.clearAccessToken();
    return response;
  }

  getRoles(): ApiResult<string[]> {
    return apiClient.get<string[]>("/api/Authentication/roles");
  }

  deleteUser(userId: string): ApiResult<void> {
    return apiClient.delete<void>(`/api/Authentication/delete-user/${userId}`);
  }

  needsSetup(): ApiResult<boolean> {
    return apiClient.get<boolean>("/api/Authentication/needs-setup");
  }

  unlockAccount(userId: string): ApiResult<void> {
    return apiClient.post<void>(`/api/Authentication/unlock-account/${userId}`);
  }

  isAuthenticated(): boolean {
    return Boolean(this.getToken());
  }

  getToken(): string | null {
    return localStorage.getItem("token");
  }

  getRefreshToken(): string | null {
    console.warn(REFRESH_TOKEN_MESSAGE);
    return null;
  }

  private setAccessToken(token?: string | null): void {
    if (token) {
      localStorage.setItem("token", token);
    }
  }

  private clearAccessToken(): void {
    localStorage.removeItem("token");
  }

  /** Ensure a valid access token exists by attempting a refresh, otherwise logout. */
  async ensureValidToken(): Promise<void> {
    if (!this.isAuthenticated()) {
      throw new Error("Not authenticated");
    }

    try {
      const response = await this.refreshToken();
      if (!response.success) {
        throw new Error(response.message ?? "Token refresh failed");
      }
    } catch (error) {
      this.clearAccessToken();
      window.location.href = "/login";
      throw error;
    }
  }
}

export const authService = new AuthService();
