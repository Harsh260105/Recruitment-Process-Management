import { type ReactNode } from "react";
import { useCandidateProfile } from "@/hooks/candidate";
import { Button } from "@/components/ui/button";
import { Link } from "react-router-dom";
import { AlertCircle } from "lucide-react";

interface ProfileRequiredWrapperProps {
  children: ReactNode;
}

export const ProfileRequiredWrapper = ({
  children,
}: ProfileRequiredWrapperProps) => {
  const { data: profile } = useCandidateProfile();

  if (!profile) {
    return (
      <div className="flex min-h-[400px] items-center justify-center">
        <div className="max-w-md text-center">
          <AlertCircle className="mx-auto mb-4 h-12 w-12 text-amber-600" />
          <h2 className="mb-2 text-2xl font-bold">Profile Required</h2>
          <p className="mb-4 text-muted-foreground">
            You need to create your profile before you can access this page.
          </p>
          <Button asChild>
            <Link to="/candidate/profile">Create Profile</Link>
          </Button>
        </div>
      </div>
    );
  }

  return <>{children}</>;
};
