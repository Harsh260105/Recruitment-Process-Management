import {
  useApplicationsRequiringAction,
  useMyAssignedApplications,
  useRecentApplications,
} from "@/hooks/staff/jobApplications.hooks";
import { useInterviewsRequiringAction } from "@/hooks/staff/interviews.hooks";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { LoadingSpinner } from "@/components/ui/LoadingSpinner";
import type { components } from "@/types/api";
import { formatDateToLocal } from "@/utils/dateUtils";
import { getStatusMeta } from "@/constants/applicationStatus";
import { getInterviewStatusMeta } from "@/constants/interviewEvaluations";
import { getErrorMessage } from "@/utils/error";

type Schemas = components["schemas"];

type PagedResult = Schemas["JobApplicationSummaryDtoPagedResult"];
type InterviewPagedResult = Schemas["InterviewSummaryDtoPagedResult"];

const ListSection = ({
  title,
  description,
  query,
  renderItem,
}: {
  title: string;
  description: string;
  query: {
    isLoading: boolean;
    isError: boolean;
    error: unknown;
    data?: PagedResult | InterviewPagedResult;
  };
  renderItem: (item: any) => React.ReactNode;
}) => {
  const items = query.data?.items ?? [];

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base font-semibold">{title}</CardTitle>
        <p className="text-sm text-muted-foreground">{description}</p>
      </CardHeader>
      <CardContent className="space-y-4">
        {query.isLoading && (
          <div className="flex justify-center py-6">
            <LoadingSpinner />
          </div>
        )}

        {query.isError && (
          <p className="rounded-md border border-destructive/40 bg-destructive/5 p-3 text-sm text-destructive">
            {getErrorMessage(query.error)}
          </p>
        )}

        {!query.isLoading && !query.isError && !items.length && (
          <p className="text-sm text-muted-foreground">
            Nothing to show right now.
          </p>
        )}

        <div className="space-y-3">
          {items.slice(0, 5).map((item) => renderItem(item))}
        </div>
      </CardContent>
    </Card>
  );
};

export const RecruiterDashboardPage = () => {
  const myAssignedQuery = useMyAssignedApplications();
  const actionQuery = useApplicationsRequiringAction({ pageSize: 5 });
  const interviewsActionQuery = useInterviewsRequiringAction({ pageSize: 5 });
  const recentQuery = useRecentApplications({ pageSize: 5 });

  const myAssignedCount = myAssignedQuery.data?.length ?? 0;
  const actionCount = actionQuery.data?.totalCount ?? 0;
  const interviewsActionCount = interviewsActionQuery.data?.totalCount ?? 0;
  const totalRecent = recentQuery.data?.totalCount ?? 0;

  return (
    <div className="space-y-6">
      <div className="space-y-1">
        <h1 className="text-3xl font-semibold text-foreground">
          Staff Dashboard
        </h1>
        <p className="text-muted-foreground">
          Monitor your pipeline, unblock stalled applications, and plan your
          day.
        </p>
      </div>

      <div className="grid gap-4 md:grid-cols-4">
        <Card>
          <CardHeader className="pb-2">
            <p className="text-sm text-muted-foreground">Assigned to me</p>
            <CardTitle className="text-3xl">{myAssignedCount}</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-xs text-muted-foreground">
              Applications currently owned by you
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="pb-2">
            <p className="text-sm text-muted-foreground">
              Applications need action
            </p>
            <CardTitle className="text-3xl">{actionCount}</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-xs text-muted-foreground">
              Applications waiting for recruiter updates
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="pb-2">
            <p className="text-sm text-muted-foreground">
              Interviews need action
            </p>
            <CardTitle className="text-3xl">{interviewsActionCount}</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-xs text-muted-foreground">
              Interviews requiring evaluation or follow-up
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="pb-2">
            <p className="text-sm text-muted-foreground">New this week</p>
            <CardTitle className="text-3xl">{totalRecent}</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-xs text-muted-foreground">
              Recently submitted applications in the last refresh
            </p>
          </CardContent>
        </Card>
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        <ListSection
          title="Applications requiring attention"
          description="Pending actions like review, test feedback, or interview scheduling"
          query={actionQuery}
          renderItem={(item) => (
            <div
              key={item?.id}
              className="flex flex-wrap items-center justify-between gap-3 rounded-lg border border-slate-100 bg-white/80 px-4 py-3"
            >
              <div>
                <p className="font-medium text-sm">
                  {item?.candidateName ?? "Unknown candidate"}
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
          )}
        />
        <ListSection
          title="Interviews requiring attention"
          description="Overdue interviews or completed interviews missing evaluations"
          query={interviewsActionQuery}
          renderItem={(item) => (
            <div
              key={item?.id}
              className="flex flex-wrap items-center justify-between gap-3 rounded-lg border border-slate-100 bg-white/80 px-4 py-3"
            >
              <div>
                <p className="font-medium text-sm">
                  {item?.title ?? "Untitled interview"}
                </p>
                <p className="text-xs text-muted-foreground">
                  Round {item?.roundNumber} •{" "}
                  {item?.candidateName ?? "Unknown candidate"}
                </p>
              </div>
              <div className="text-right">
                <Badge
                  variant={getInterviewStatusMeta(item?.status).variant}
                  className="text-xs"
                >
                  {getInterviewStatusMeta(item?.status).label}
                </Badge>
                <p className="text-[11px] text-muted-foreground">
                  {formatDateToLocal(item?.scheduledDateTime)}
                </p>
              </div>
            </div>
          )}
        />
        <Card>
          <CardHeader>
            <CardTitle className="text-base font-semibold">
              Recent applications
            </CardTitle>
            <p className="text-sm text-muted-foreground">
              Latest candidates entering the pipeline
            </p>
          </CardHeader>
          <CardContent className="space-y-4">
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
              !(recentQuery.data?.items?.length ?? 0) && (
                <p className="text-sm text-muted-foreground">
                  No new applications found.
                </p>
              )}

            <div className="space-y-3">
              {recentQuery.data?.items?.slice(0, 6).map((item) => (
                <div
                  key={item?.id}
                  className="flex flex-wrap items-center justify-between gap-3 rounded-lg border border-slate-100 bg-white/80 px-4 py-3"
                >
                  <div>
                    <p className="font-medium text-sm">
                      {item?.candidateName ?? "Unknown candidate"}
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
            </div>
          </CardContent>
        </Card>
      </div>

      <Card className="border-dashed">
        <CardContent className="flex flex-wrap items-center justify-between gap-4 py-6">
          <div>
            <p className="text-sm font-medium text-foreground">
              Ready to dive deeper?
            </p>
            <p className="text-sm text-muted-foreground">
              Head over to the applications workspace for detailed filters and
              actions.
            </p>
          </div>
          <Button asChild>
            <a href="/recruiter/applications">Go to Applications</a>
          </Button>
        </CardContent>
      </Card>
    </div>
  );
};
