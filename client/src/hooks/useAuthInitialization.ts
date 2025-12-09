import { useCallback, useEffect, useState } from "react";
import { authService } from "@/services/authService";
import { isTokenExpiredOrNearExpiry } from "@/utils/tokenUtils";
import { useAuth } from "@/store";

export function useAuthInitialization() {
  const [isInitialized, setIsInitialized] = useState(false);
  const { setToken, logout } = useAuth((state) => state.auth);

  const initializeAuth = useCallback(async (): Promise<void> => {
    try {
      const token =
        useAuth.getState().auth.token ?? localStorage.getItem("token");

      if (!token) {
        // No token in store or localStorage; try refresh assuming cookie exists
        const refreshResult = await authService.refreshToken();
        if (refreshResult.success && refreshResult.data?.token) {
          setToken(refreshResult.data.token);
          localStorage.setItem("token", refreshResult.data.token);
        } else {
          logout();
          return;
        }
      } else if (isTokenExpiredOrNearExpiry(token)) {
        const refreshResult = await authService.refreshToken();
        if (refreshResult.success && refreshResult.data?.token) {
          setToken(refreshResult.data.token);
          localStorage.setItem("token", refreshResult.data.token);
        } else {
          logout();
          localStorage.removeItem("token");
          return;
        }
      } else {
        setToken(token);
      }
    } catch (error) {
      console.error("Auth initialization failed:", error);
      logout();
      localStorage.removeItem("token");
    } finally {
      setIsInitialized(true);
    }
  }, [logout, setToken]);

  useEffect(() => {
    void initializeAuth();
  }, [initializeAuth]);

  return {
    isInitialized,
    isAuthenticated: useAuth.getState().auth.isAuthenticated,
  };
}
