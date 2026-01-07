import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { LoadingSpinner } from "@/components/ui/LoadingSpinner";
import { useNavigate } from "react-router-dom";
import { useGetOffersRequiringAction } from "@/hooks/staff/jobOffer.hooks";
import {
  useApplicationStatusDistribution,
  useApplicationsRequiringAction,
  useRecentApplications,
} from "@/hooks/staff/jobApplications.hooks";
import { useInterviewsRequiringAction } from "@/hooks/staff/interviews.hooks";
import {
  APPLICATION_STATUS_ENUM_MAP,
  getStatusMeta,
} from "@/constants/applicationStatus";
import { formatDateToLocal } from "@/utils/dateUtils";
import { getErrorMessage } from "@/utils/error";
import {
  Users,
  Briefcase,
  FileText,
  Calendar,
  TrendingUp,
  AlertCircle,
  CheckCircle,
} from "lucide-react";

export const HRDashboardPage = () => {
  const navigate = useNavigate();

  // Fetch status distribution using hook
  const statusDistQuery = useApplicationStatusDistribution();

  // Fetch applications requiring action using hook
  const actionQuery = useApplicationsRequiringAction({
    pageSize: 10,
  });

  // Fetch recent applications using hook
  const recentQuery = useRecentApplications({
    pageSize: 10,
  });

  // Fetch offers requiring action using hook
  const offersActionQuery = useGetOffersRequiringAction({
    pageNumber: 1,
    pageSize: 5,
  });

  // Fetch interviews requiring action using hook
  const interviewsActionQuery = useInterviewsRequiringAction({
    pageSize: 5,
  });

  // Calculate totals
  const totalApplications = Object.values(statusDistQuery.data || {}).reduce(
    (sum, count) => sum + (count as number),
    0
  );
  const actionCount = actionQuery.data?.totalCount ?? 0;
  const offersActionCount = offersActionQuery.data?.totalCount ?? 0;
  const interviewsActionCount = interviewsActionQuery.data?.totalCount ?? 0;

  return (
    <div className="space-y-6">
      <div className="space-y-1">
        <h1 className="text-3xl font-semibold text-foreground">HR Dashboard</h1>
        <p className="text-muted-foreground">
          Organization-wide recruitment metrics and actionable insights
        </p>
      </div>

      {/* Key Metrics */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardHeader className="pb-2">
            <div className="flex items-center justify-between">
              <p className="text-sm text-muted-foreground">
                Total Applications
              </p>
              <FileText className="h-4 w-4 text-muted-foreground" />
            </div>
            <CardTitle className="text-3xl">
              {statusDistQuery.isLoading ? (
                <LoadingSpinner />
              ) : (
                totalApplications
              )}
            </CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-xs text-muted-foreground">
              Across all job positions
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="pb-2">
            <div className="flex items-center justify-between">
              <p className="text-sm text-muted-foreground">Needs Action</p>
              <AlertCircle className="h-4 w-4 text-orange-600" />
            </div>
            <CardTitle className="text-3xl">
              {actionQuery.isLoading ? <LoadingSpinner /> : actionCount}
            </CardTitle>
          </CardHeader>
          <CardContent>
            <Button
              variant="link"
              className="h-auto p-0 text-xs"
              onClick={() => navigate("/recruiter/applications")}
            >
              View all →
            </Button>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="pb-2">
            <div className="flex items-center justify-between">
              <p className="text-sm text-muted-foreground">Pending Offers</p>
              <CheckCircle className="h-4 w-4 text-green-600" />
            </div>
            <CardTitle className="text-3xl">
              {offersActionQuery.isLoading ? (
                <LoadingSpinner />
              ) : (
                offersActionCount
              )}
            </CardTitle>
          </CardHeader>
          <CardContent>
            <Button
              variant="link"
              className="h-auto p-0 text-xs"
              onClick={() => navigate("/admin/offers")}
            >
              View offers →
            </Button>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="pb-2">
            <div className="flex items-center justify-between">
              <p className="text-sm text-muted-foreground">
                Interviews Pending
              </p>
              <Calendar className="h-4 w-4 text-blue-600" />
            </div>
            <CardTitle className="text-3xl">
              {interviewsActionQuery.isLoading ? (
                <LoadingSpinner />
              ) : (
                interviewsActionCount
              )}
            </CardTitle>
          </CardHeader>
          <CardContent>
            <Button
              variant="link"
              className="h-auto p-0 text-xs"
              onClick={() => navigate("/recruiter/interviews")}
            >
              View schedule →
            </Button>
          </CardContent>
        </Card>
      </div>

      {/* Status Distribution */}
      <Card>
        <CardHeader>
          <CardTitle className="text-base font-semibold">
            Application Pipeline
          </CardTitle>
          <CardDescription>
            Current distribution across application stages
          </CardDescription>
        </CardHeader>
        <CardContent>
          {statusDistQuery.isLoading && (
            <div className="flex justify-center py-6">
              <LoadingSpinner />
            </div>
          )}

          {statusDistQuery.isError && (
            <p className="rounded-md border border-destructive/40 bg-destructive/5 p-3 text-sm text-destructive">
              {getErrorMessage(statusDistQuery.error)}
            </p>
          )}

          {!statusDistQuery.isLoading && !statusDistQuery.isError && (
            <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
              {Object.entries(statusDistQuery.data || {}).map(
                ([status, count]) => {
                  const statusNum = isNaN(Number(status))
                    ? APPLICATION_STATUS_ENUM_MAP[status] || 0
                    : Number(status);
                  const meta = getStatusMeta(statusNum);
                  return (
                    <div
                      key={status}
                      className="flex items-center justify-between rounded-lg border bg-white/80 px-4 py-3"
                    >
                      <div className="flex items-center gap-3">
                        <Badge variant={meta.variant} className="text-xs">
                          {meta.label}
                        </Badge>
                      </div>
                      <span className="text-2xl font-semibold">
                        {count as number}
                      </span>
                    </div>
                  );
                }
              )}
            </div>
          )}
        </CardContent>
      </Card>

      <div className="grid gap-4 lg:grid-cols-2">
        {/* Applications Requiring Action */}
        <Card>
          <CardHeader>
            <CardTitle className="text-base font-semibold">
              Applications Requiring Action
            </CardTitle>
            <CardDescription>
              Applications waiting for recruiter updates
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-3">
            {actionQuery.isLoading && (
              <div className="flex justify-center py-6">
                <LoadingSpinner />
              </div>
            )}

            {actionQuery.isError && (
              <p className="rounded-md border border-destructive/40 bg-destructive/5 p-3 text-sm text-destructive">
                {getErrorMessage(actionQuery.error)}
              </p>
            )}

            {!actionQuery.isLoading &&
              !actionQuery.isError &&
              !actionQuery.data?.items?.length && (
                <p className="text-sm text-muted-foreground">
                  No applications requiring action right now.
                </p>
              )}

            {actionQuery.data?.items?.slice(0, 5).map((item) => (
              <div
                key={item?.id}
                className="flex cursor-pointer flex-wrap items-center justify-between gap-3 rounded-lg border border-slate-100 bg-white/80 px-4 py-3 transition-colors hover:bg-slate-50"
                onClick={() => navigate(`/recruiter/applications/${item?.id}`)}
              >
                <div>
                  <p className="font-medium text-sm">
                    {item?.candidateName ?? "Unknown"}
                  </p>
                  <p className="text-xs text-muted-foreground">
                    {item?.jobTitle ?? "—"}
                  </p>
                </div>
                <div className="text-right">
                  <Badge
                    variant={getStatusMeta(item?.status).variant}
                    className="text-xs"
                  >
                    {getStatusMeta(item?.status).label}
                  </Badge>
                  <p className="text-[11px] text-muted-foreground">
                    {formatDateToLocal(item?.appliedDate)}
                  </p>
                </div>
              </div>
            ))}

            {(actionQuery.data?.totalCount ?? 0) > 5 && (
              <Button
                variant="outline"
                className="w-full"
                onClick={() => navigate("/recruiter/applications")}
              >
                View all {actionQuery.data?.totalCount} applications
              </Button>
            )}
          </CardContent>
        </Card>

        {/* Recent Applications */}
        <Card>
          <CardHeader>
            <CardTitle className="text-base font-semibold">
              Recent Applications
            </CardTitle>
            <CardDescription>Latest candidate submissions</CardDescription>
          </CardHeader>
          <CardContent className="space-y-3">
            {recentQuery.isLoading && (
              <div className="flex justify-center py-6">
                <LoadingSpinner />
              </div>
            )}

            {recentQuery.isError && (
              <p className="rounded-md border border-destructive/40 bg-destructive/5 p-3 text-sm text-destructive">
                {getErrorMessage(recentQuery.error)}
              </p>
            )}

            {!recentQuery.isLoading &&
              !recentQuery.isError &&
              !recentQuery.data?.items?.length && (
                <p className="text-sm text-muted-foreground">
                  No recent applications.
                </p>
              )}

            {recentQuery.data?.items?.slice(0, 5).map((item) => (
              <div
                key={item?.id}
                className="flex cursor-pointer flex-wrap items-center justify-between gap-3 rounded-lg border border-slate-100 bg-white/80 px-4 py-3 transition-colors hover:bg-slate-50"
                onClick={() => navigate(`/recruiter/applications/${item?.id}`)}
              >
                <div>
                  <p className="font-medium text-sm">
                    {item?.candidateName ?? "Unknown"}
                  </p>
                  <p className="text-xs text-muted-foreground">
                    {item?.jobTitle ?? "—"}
                  </p>
                </div>
                <div className="text-right">
                  <Badge
                    variant={getStatusMeta(item?.status).variant}
                    className="text-xs"
                  >
                    {getStatusMeta(item?.status).label}
                  </Badge>
                  <p className="text-[11px] text-muted-foreground">
                    {formatDateToLocal(item?.appliedDate)}
                  </p>
                </div>
              </div>
            ))}
          </CardContent>
        </Card>
      </div>

      {/* Quick Actions */}
      <Card>
        <CardHeader>
          <CardTitle className="text-base font-semibold">
            Quick Actions
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
            <Button
              variant="outline"
              className="justify-start"
              onClick={() => navigate("/recruiter/applications")}
            >
              <FileText className="mr-2 h-4 w-4" />
              All Applications
            </Button>
            <Button
              variant="outline"
              className="justify-start"
              onClick={() => navigate("/recruiter/candidates")}
            >
              <Users className="mr-2 h-4 w-4" />
              Candidate Pool
            </Button>
            <Button
              variant="outline"
              className="justify-start"
              onClick={() => navigate("/admin/jobs")}
            >
              <Briefcase className="mr-2 h-4 w-4" />
              Job Positions
            </Button>
            <Button
              variant="outline"
              className="justify-start"
              onClick={() => navigate("/admin/analytics")}
            >
              <TrendingUp className="mr-2 h-4 w-4" />
              Analytics
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  );
};
