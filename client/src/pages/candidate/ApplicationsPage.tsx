import { Link } from "react-router-dom";
import { ProfileRequiredWrapper } from "@/components/common/ProfileRequiredWrapper";

import { Button } from "@/components/ui/button";

import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { LoadingSpinner } from "@/components/ui/LoadingSpinner";
import { useCandidateApplications } from "@/hooks/candidate/applications.hooks";
import { useCandidateProfile } from "@/hooks/candidate/profile.hooks";
import { formatDateToLocal } from "@/utils/dateUtils";
import { getStatusMeta } from "@/constants/applicationStatus";
import { getErrorMessage } from "@/utils/error";

export const CandidateApplicationsPage = () => {
  const { data: profile } = useCandidateProfile();
  const profileId = profile?.id;

  const applicationsQuery = useCandidateApplications(profileId);

  const applications = applicationsQuery.data || [];

  return (
    <ProfileRequiredWrapper>
      <div className="space-y-6">
        <div>
          <h1 className="text-2xl font-bold">My Applications</h1>
          <p className="text-muted-foreground">
            Track your job applications, statuses, and next steps.
          </p>
        </div>

        {applicationsQuery.isLoading ? (
          <div className="flex justify-center py-10">
            <LoadingSpinner />
          </div>
        ) : applicationsQuery.isError ? (
          <Card className="border-destructive/30 bg-destructive/5">
            <CardContent className="py-6 text-destructive">
              {getErrorMessage(applicationsQuery.error)}
            </CardContent>
          </Card>
        ) : !applications.length ? (
          <Card>
            <CardContent className="py-12 text-center">
              <div className="text-6xl mb-4">📋</div>
              <h3 className="text-lg font-semibold mb-2">
                No Applications Yet
              </h3>
              <p className="text-muted-foreground mb-4">
                You haven't applied to any jobs yet. Start exploring
                opportunities!
              </p>
              <Button asChild>
                <Link to="/candidate/jobs">Browse Jobs</Link>
              </Button>
            </CardContent>
          </Card>
        ) : (
          <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
            {applications.map((application) => {
              const statusMeta = getStatusMeta(application.status);
              return (
                <Card
                  key={application.id}
                  className="hover:shadow-lg transition-shadow"
                >
                  <CardHeader>
                    <div className="flex items-start justify-between">
                      <div className="flex-1">
                        <CardTitle className="text-lg line-clamp-2">
                          {application.jobTitle}
                        </CardTitle>
                        <CardDescription className="mt-1">
                          Applied on{" "}
                          {formatDateToLocal(application.appliedDate)}
                        </CardDescription>
                      </div>
                      <Badge
                        variant={statusMeta.variant}
                        className="ml-2 shrink-0"
                      >
                        {statusMeta.label}
                      </Badge>
                    </div>
                  </CardHeader>
                  <CardContent className="space-y-3">
                    <div className="text-sm">
                      <span className="font-medium text-foreground">
                        Recruiter:
                      </span>{" "}
                      <span className="text-muted-foreground">
                        {application.assignedRecruiterName || "Not assigned"}
                      </span>
                    </div>
                    <Button asChild className="w-full">
                      <Link to={`/candidate/applications/${application.id}`}>
                        View Details
                      </Link>
                    </Button>
                  </CardContent>
                </Card>
              );
            })}
          </div>
        )}
      </div>
    </ProfileRequiredWrapper>
  );
};
