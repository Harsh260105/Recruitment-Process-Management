import { NavLink, Outlet } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { useAuth } from "@/store";
import { cn } from "@/lib/utils";
import { useStaffRoles } from "@/hooks/staff";
import { useLogout } from "@/hooks/auth";
import { useState } from "react";
import { Menu } from "lucide-react";

const navItems: Array<{
  to: string;
  label: string;
  roles?: readonly string[];
}> = [
  { to: "/recruiter/dashboard", label: "Dashboard" },
  {
    to: "/admin/hr-dashboard",
    label: "HR Dashboard",
    roles: ["HR", "Admin", "SuperAdmin"],
  },
  { to: "/recruiter/applications", label: "Applications" },
  { to: "/recruiter/candidates", label: "Candidates" },
  { to: "/recruiter/interviews", label: "Interviews" },
  {
    to: "/admin/offers",
    label: "Offers",
    roles: ["HR", "Admin", "SuperAdmin"],
  },
  {
    to: "/admin/jobs",
    label: "Job Positions",
    roles: ["HR", "Admin", "SuperAdmin"],
  },
  {
    to: "/admin/staff",
    label: "Staff",
    roles: ["HR", "Admin", "SuperAdmin"],
  },
  {
    to: "/admin/users",
    label: "Users",
    roles: ["Admin", "SuperAdmin"],
  },
  {
    to: "/admin/analytics",
    label: "Analytics",
    roles: ["HR", "Admin", "SuperAdmin"],
  },
  { to: "/recruiter/profile", label: "Profile" },
  { to: "/recruiter/account", label: "Account" },
];

export const StaffLayout = () => {
  const user = useAuth((state) => state.auth.user);
  const performLogout = useLogout({ redirectTo: "/staff/login" });
  const { roles, primaryRoleLabel } = useStaffRoles();
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);

  const visibleNavItems = navItems.filter(
    (item) => !item.roles || item.roles.some((role) => roles.includes(role))
  );

  const handleLogout = () => {
    performLogout();
  };

  return (
    <div className="min-h-screen bg-muted/20 text-foreground">
      <header className="border-b bg-white/90 backdrop-blur">
        <div className="mx-auto flex h-16 w-full max-w-screen-2xl items-center justify-between px-6">
          <div className="flex items-center gap-4">
            <Button
              variant="ghost"
              size="sm"
              className="md:hidden"
              onClick={() => setIsMobileMenuOpen(!isMobileMenuOpen)}
            >
              <Menu className="h-4 w-4" />
            </Button>
            <div>
              <p className="text-lg font-semibold">Roima Staff Portal</p>
              <p className="text-xs text-muted-foreground">
                {primaryRoleLabel
                  ? `${primaryRoleLabel} workspace`
                  : "Hiring control center"}
              </p>
            </div>
          </div>
          <div className="flex items-center gap-4">
            <div className="text-right">
              <p className="text-sm font-medium">
                {user?.firstName
                  ? `${user.firstName} ${user?.lastName ?? ""}`
                  : "Staff"}
              </p>
              <p className="text-xs text-muted-foreground">Staff Portal</p>
            </div>
            <Button variant="outline" size="sm" onClick={handleLogout}>
              Logout
            </Button>
          </div>
        </div>
        <div className="hidden border-t bg-white/95 md:block">
          <div className="mx-auto flex w-full max-w-screen-2xl overflow-x-auto px-4">
            {visibleNavItems.map((item) => (
              <NavLink
                key={item.to}
                to={item.to}
                className={({ isActive }) =>
                  cn(
                    "flex items-center px-3 py-3 text-sm font-medium transition-colors",
                    isActive
                      ? "text-primary border-b-2 border-primary"
                      : "text-muted-foreground hover:text-foreground"
                  )
                }
              >
                {item.label}
              </NavLink>
            ))}
          </div>
        </div>
        {isMobileMenuOpen && (
          <div className="border-t bg-white/95 md:hidden">
            <div className="flex flex-col gap-1 p-4">
              {visibleNavItems.map((item) => (
                <NavLink
                  key={item.to}
                  to={item.to}
                  onClick={() => setIsMobileMenuOpen(false)}
                  className={({ isActive }) =>
                    cn(
                      "flex items-center gap-2 rounded px-3 py-2 text-sm font-medium",
                      isActive
                        ? "bg-primary/10 text-primary"
                        : "text-muted-foreground hover:bg-muted"
                    )
                  }
                >
                  {item.label}
                </NavLink>
              ))}
              <div className="border-t pt-2 mt-2">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => {
                    setIsMobileMenuOpen(false);
                    handleLogout();
                  }}
                  className="w-full justify-start"
                >
                  Logout
                </Button>
              </div>
            </div>
          </div>
        )}
      </header>

      <main className="mx-auto w-full max-w-screen-2xl px-6 py-8 min-h-[calc(100vh-8rem)]">
        <Outlet />
      </main>
    </div>
  );
};
