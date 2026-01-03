import { useMemo } from "react";
import { useAuth } from "@/store";

const ROLE_PRIORITY = ["SuperAdmin", "Admin", "HR", "Recruiter"] as const;
const ORG_WIDE_ROLES = ["HR", "Admin", "SuperAdmin"] as const;

export const useStaffRoles = () => {
  const roles = useAuth((state) => state.auth.roles ?? []);

  const normalizedRoles = useMemo(() => roles.filter(Boolean), [roles]);

  const hasRole = useMemo(
    () => (role: string) => normalizedRoles.includes(role),
    [normalizedRoles]
  );

  const hasAnyRole = useMemo(
    () => (targetRoles: readonly string[]) =>
      targetRoles.some((role) => normalizedRoles.includes(role)),
    [normalizedRoles]
  );

  const isSuperAdmin = hasRole("SuperAdmin");
  const isAdmin = hasRole("Admin") || isSuperAdmin;
  const isHR = hasRole("HR") || isAdmin;
  const isRecruiter = hasRole("Recruiter");

  const canViewOrgWidePipeline = hasAnyRole(ORG_WIDE_ROLES);
  const canManageOffers = canViewOrgWidePipeline;
  const canManageJobPositions = canViewOrgWidePipeline;

  const primaryRoleLabel =
    ROLE_PRIORITY.find((role) => normalizedRoles.includes(role)) ??
    normalizedRoles[0] ??
    null;

  return {
    roles: normalizedRoles,
    hasRole,
    hasAnyRole,
    isSuperAdmin,
    isAdmin,
    isHR,
    isRecruiter,
    canViewOrgWidePipeline,
    canManageOffers,
    canManageJobPositions,
    primaryRoleLabel,
  } as const;
};
