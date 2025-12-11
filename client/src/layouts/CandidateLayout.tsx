import { NavLink, Outlet, useNavigate } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { useProfileCompletion } from "@/hooks/useProfileCompletion";
import { useCandidateProfile } from "@/hooks/candidate";
import { useEffect, useState } from "react";
import { X, AlertCircle, LogOut, Menu } from "lucide-react";
import { useLogout } from "@/hooks/useLogout";

const navItems = [
  { to: "/candidate/dashboard", label: "Dashboard", requiresProfile: false },
  { to: "/candidate/profile", label: "Profile", requiresProfile: false },
  { to: "/candidate/skills", label: "Skills", requiresProfile: true },
  { to: "/candidate/education", label: "Education", requiresProfile: true },
  { to: "/candidate/experience", label: "Experience", requiresProfile: true },
  { to: "/candidate/jobs", label: "Jobs", requiresProfile: true },
  {
    to: "/candidate/applications",
    label: "Applications",
    requiresProfile: true,
  },
  { to: "/candidate/interviews", label: "Interviews", requiresProfile: true },
  { to: "/candidate/offers", label: "Offers", requiresProfile: true },
  {
    to: "/candidate/notifications",
    label: "Notifications",
    requiresProfile: false,
  },
];

export const CandidateLayout = () => {
  const { data: profile } = useCandidateProfile();
  const completion = useProfileCompletion();
  const navigate = useNavigate();
  const [showBanner, setShowBanner] = useState(true);
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);
  const logout = useLogout({ redirectTo: "/auth/login" });

  useEffect(() => {
    // If no profile exists and user is not on profile page, show banner
    if (!profile && window.location.pathname !== "/candidate/profile") {
      setShowBanner(true);
    }
  }, [profile]);

  const shouldShowCompletionBanner =
    !profile || completion.completionPercentage < 100;

  return (
    <div className="min-h-screen bg-muted/20 text-foreground">
      <header className="border-b bg-white/80 backdrop-blur">
        <div className="mx-auto flex h-14 w-full max-w-screen-2xl items-center justify-between px-6">
          <div className="flex items-center gap-4">
            <Button
              variant="ghost"
              size="sm"
              className="md:hidden"
              onClick={() => setIsMobileMenuOpen(!isMobileMenuOpen)}
            >
              <Menu className="h-4 w-4" />
            </Button>
            <div className="font-semibold">Roima Candidate</div>
          </div>
          <nav className="hidden gap-3 md:flex">
            {navItems.map((item) => {
              const isLocked = item.requiresProfile && !profile;
              return (
                <NavLink
                  key={item.to}
                  to={item.to}
                  className={({ isActive }) =>
                    `flex items-center gap-1 text-sm font-medium ${
                      isActive ? "text-primary" : "text-muted-foreground"
                    } ${isLocked ? "opacity-50" : ""}`
                  }
                  title={
                    isLocked
                      ? "Create your profile first to access this section"
                      : undefined
                  }
                >
                  {item.label}
                </NavLink>
              );
            })}
          </nav>
          <div className="flex items-center gap-2">
            <Button variant="ghost" size="sm" onClick={() => logout()}>
              <LogOut className="mr-2 h-4 w-4" />
              Logout
            </Button>
          </div>
        </div>
        {isMobileMenuOpen && (
          <div className="border-t bg-white/95 md:hidden">
            <div className="flex flex-col gap-1 p-4">
              {navItems.map((item) => {
                const isLocked = item.requiresProfile && !profile;
                return (
                  <NavLink
                    key={item.to}
                    to={item.to}
                    onClick={() => setIsMobileMenuOpen(false)}
                    className={({ isActive }) =>
                      `flex items-center gap-2 rounded px-3 py-2 text-sm font-medium ${
                        isActive
                          ? "bg-primary/10 text-primary"
                          : "text-muted-foreground hover:bg-muted"
                      } ${isLocked ? "opacity-50" : ""}`
                    }
                    title={
                      isLocked
                        ? "Create your profile first to access this section"
                        : undefined
                    }
                  >
                    {item.label}
                  </NavLink>
                );
              })}
            </div>
          </div>
        )}
      </header>

      {shouldShowCompletionBanner && showBanner && (
        <div className="border-b bg-gradient-to-r from-amber-50 to-orange-50">
          <div className="mx-auto flex w-full max-w-screen-2xl items-center justify-between gap-4 px-6 py-3">
            <div className="flex items-center gap-3">
              <AlertCircle className="h-5 w-5 text-amber-600" />
              <div className="flex-1">
                <p className="text-sm font-medium text-amber-900">
                  {!profile
                    ? "Complete your profile to unlock all features"
                    : `Profile ${completion.completionPercentage}% complete`}
                </p>
                <p className="text-xs text-amber-700">
                  {completion.missingSteps.slice(0, 2).join(" • ")}
                  {completion.missingSteps.length > 2 &&
                    ` • +${completion.missingSteps.length - 2} more`}
                </p>
              </div>
            </div>
            <div className="flex items-center gap-2">
              {!profile && (
                <Button
                  size="sm"
                  variant="default"
                  onClick={() => navigate("/candidate/profile")}
                >
                  Create Profile
                </Button>
              )}
              {profile && completion.completionPercentage < 100 && (
                <div className="flex items-center gap-2">
                  <div className="h-2 w-32 overflow-hidden rounded-full bg-amber-200">
                    <div
                      className="h-full bg-amber-600 transition-all duration-300"
                      style={{ width: `${completion.completionPercentage}%` }}
                    />
                  </div>
                  <span className="text-xs font-medium text-amber-700">
                    {completion.completionPercentage}%
                  </span>
                </div>
              )}
              <Button
                size="sm"
                variant="ghost"
                onClick={() => setShowBanner(false)}
              >
                <X className="h-4 w-4" />
              </Button>
            </div>
          </div>
        </div>
      )}

      <main className="mx-auto w-full max-w-screen-2xl px-6 py-8">
        <Outlet />
      </main>
    </div>
  );
};
