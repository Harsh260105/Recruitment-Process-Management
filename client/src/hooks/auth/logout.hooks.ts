import { useCallback } from "react";
import { useNavigate } from "react-router-dom";
import { authService } from "@/services/authService";
import { useAuth } from "@/store";

interface UseLogoutOptions {
  redirectTo?: string;
  replace?: boolean;
}

export function useLogout({
  redirectTo,
  replace = true,
}: UseLogoutOptions = {}) {
  const navigate = useNavigate();
  const resetAuth = useAuth((state) => state.auth.logout);

  return useCallback(
    async (overrideRedirect?: string) => {
      try {
        await authService.logout();
      } catch (error) {
        console.error("Failed to logout", error);
      } finally {
        resetAuth();
        const target = overrideRedirect ?? redirectTo;
        if (target) {
          navigate(target, { replace });
        }
      }
    },
    [navigate, redirectTo, replace, resetAuth]
  );
}
