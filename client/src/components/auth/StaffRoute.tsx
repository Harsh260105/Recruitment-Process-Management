import { Navigate, Outlet, useLocation } from "react-router-dom";
import type { ReactNode } from "react";
import { useAuth } from "@/store";

const DEFAULT_STAFF_ROLES = ["Recruiter", "HR", "Admin", "SuperAdmin"];

interface StaffRouteProps {
  allowedRoles?: string[];
  redirectTo?: string;
  unauthorizedRedirect?: string;
  children?: ReactNode;
}

export const StaffRoute = ({
  allowedRoles = DEFAULT_STAFF_ROLES,
  redirectTo = "/auth/login",
  unauthorizedRedirect = "/recruiter/dashboard",
  children,
}: StaffRouteProps) => {
  const location = useLocation();
  const isAuthenticated = useAuth((state) => state.auth.isAuthenticated);
  const roles = useAuth((state) => state.auth.roles ?? []);

  if (!isAuthenticated) {
    return (
      <Navigate to={redirectTo} replace state={{ from: location.pathname }} />
    );
  }

  const hasAccess =
    !allowedRoles.length || roles.some((role) => allowedRoles.includes(role));

  if (!hasAccess) {
    return (
      <Navigate
        to={unauthorizedRedirect}
        replace
        state={{ from: location.pathname, reason: "unauthorized" }}
      />
    );
  }

  if (children) {
    return <>{children}</>;
  }

  return <Outlet />;
};
