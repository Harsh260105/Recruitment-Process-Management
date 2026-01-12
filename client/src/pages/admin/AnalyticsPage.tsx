import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { LoadingSpinner } from "@/components/ui/LoadingSpinner";
import {
  useGetStatusDistribution,
  useGetAverageOfferAmount,
  useGetOfferAcceptanceRate,
  useGetAverageOfferResponseTime,
} from "@/hooks/staff/jobOffer.hooks";
import { useApplicationStatusDistribution } from "@/hooks/staff/jobApplications.hooks";
import {
  useInterviewStatusDistribution,
  useInterviewTypeDistribution,
} from "@/hooks/staff/interviews.hooks";
import {
  getStatusMeta,
  APPLICATION_STATUS_ENUM_MAP,
} from "@/constants/applicationStatus";
import { getErrorMessage } from "@/utils/error";
import {
  TrendingUp,
  TrendingDown,
  Users,
  FileText,
  CheckCircle,
  XCircle,
  Calendar,
  DollarSign,
  Clock,
  BarChart3,
  PieChart,
} from "lucide-react";

const StatCard = ({
  title,
  value,
  subtitle,
  icon: Icon,
  trend,
  isLoading,
}: {
  title: string;
  value: string | number;
  subtitle?: string;
  icon: React.ElementType;
  trend?: { direction: "up" | "down"; value: string };
  isLoading?: boolean;
}) => (
  <Card>
    <CardHeader className="pb-2">
      <div className="flex items-center justify-between">
        <p className="text-sm text-muted-foreground">{title}</p>
        <Icon className="h-4 w-4 text-muted-foreground" />
      </div>
      <CardTitle className="text-3xl">
        {isLoading ? <LoadingSpinner /> : value}
      </CardTitle>
    </CardHeader>
    <CardContent>
      <div className="flex items-center gap-2">
        {trend && (
          <div
            className={`flex items-center gap-1 text-xs ${
              trend.direction === "up" ? "text-green-600" : "text-red-600"
            }`}
          >
            {trend.direction === "up" ? (
              <TrendingUp className="h-3 w-3" />
            ) : (
              <TrendingDown className="h-3 w-3" />
            )}
            {trend.value}
          </div>
        )}
        {subtitle && (
          <p className="text-xs text-muted-foreground">{subtitle}</p>
        )}
      </div>
    </CardContent>
  </Card>
);

export const AnalyticsPage = () => {
  // Application analytics using hooks
  const statusDistQuery = useApplicationStatusDistribution();

  // Interview analytics using hooks
  const interviewStatusDistQuery = useInterviewStatusDistribution();
  const interviewTypeDistQuery = useInterviewTypeDistribution();

  // Offer analytics using hooks
  const offerStatusDistQuery = useGetStatusDistribution();
  const offerAcceptanceRateQuery = useGetOfferAcceptanceRate();
  const avgOfferAmountQuery = useGetAverageOfferAmount();
  const avgResponseTimeQuery = useGetAverageOfferResponseTime();

  // Calculate totals
  const totalApplications = Object.values(statusDistQuery.data || {}).reduce(
    (sum, count) => sum + (count as number),
    0
  );

  const totalInterviews = Object.values(
    interviewStatusDistQuery.data || {}
  ).reduce((sum, count) => sum + (count as number), 0);

  const totalOffers = Object.values(offerStatusDistQuery.data || {}).reduce(
    (sum, count) => sum + (count as number),
    0
  );

  const acceptanceRate = offerAcceptanceRateQuery.data || 0;
  const avgOfferAmount = avgOfferAmountQuery.data || 0;
  const avgResponseTime = avgResponseTimeQuery.data || "";

  return (
    <div className="space-y-6">
      <div className="space-y-1">
        <h1 className="text-3xl font-semibold text-foreground">
          Analytics & Reports
        </h1>
        <p className="text-muted-foreground">
          Comprehensive insights into recruitment performance and trends
        </p>
      </div>

      {/* Key Performance Indicators */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <StatCard
          title="Total Applications"
          value={totalApplications}
          subtitle="All time"
          icon={FileText}
          isLoading={statusDistQuery.isLoading}
        />
        <StatCard
          title="Total Interviews"
          value={totalInterviews}
          subtitle="Scheduled & completed"
          icon={Calendar}
          isLoading={interviewStatusDistQuery.isLoading}
        />
        <StatCard
          title="Offers Extended"
          value={totalOffers}
          subtitle="All statuses"
          icon={CheckCircle}
          isLoading={offerStatusDistQuery.isLoading}
        />
        <StatCard
          title="Acceptance Rate"
          value={`${acceptanceRate.toFixed(1)}%`}
          subtitle="Offer success rate"
          icon={TrendingUp}
          isLoading={offerAcceptanceRateQuery.isLoading}
        />
      </div>

      {/* Offer Metrics */}
      <div className="grid gap-4 md:grid-cols-2">
        <StatCard
          title="Avg. Offer Amount"
          value={
            avgOfferAmount > 0
              ? `₹${avgOfferAmount.toLocaleString("en-IN")}`
              : "₹0"
          }
          subtitle="Compensation package"
          icon={DollarSign}
          isLoading={avgOfferAmountQuery.isLoading}
        />
        <StatCard
          title="Avg. Response Time"
          value={avgResponseTime ? `${avgResponseTime.substring(0, 8)}` : "—"}
          subtitle="Time to candidate response"
          icon={Clock}
          isLoading={avgResponseTimeQuery.isLoading}
        />
      </div>

      {/* Application Pipeline */}
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <BarChart3 className="h-5 w-5 text-primary" />
            <CardTitle className="text-base font-semibold">
              Application Pipeline Distribution
            </CardTitle>
          </div>
          <CardDescription>
            Current applications across different stages
          </CardDescription>
        </CardHeader>
        <CardContent>
          {statusDistQuery.isLoading && (
            <div className="flex justify-center py-8">
              <LoadingSpinner />
            </div>
          )}

          {statusDistQuery.isError && (
            <p className="rounded-md border border-destructive/40 bg-destructive/5 p-3 text-sm text-destructive">
              {getErrorMessage(statusDistQuery.error)}
            </p>
          )}

          {!statusDistQuery.isLoading && !statusDistQuery.isError && (
            <div className="space-y-4">
              <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
                {Object.entries(statusDistQuery.data || {}).map(
                  ([status, count]) => {
                    const statusNum =
                      APPLICATION_STATUS_ENUM_MAP[status] ?? Number(status);
                    const meta = getStatusMeta(statusNum);
                    const percentage =
                      totalApplications > 0
                        ? ((count as number) / totalApplications) * 100
                        : 0;

                    return (
                      <div
                        key={status}
                        className="rounded-lg border bg-white p-4"
                      >
                        <div className="mb-2 flex items-center justify-between">
                          <Badge variant="outline" className="text-xs">
                            {meta.label}
                          </Badge>
                          <span className="text-2xl font-semibold">
                            {count as number}
                          </span>
                        </div>
                        <div className="h-2 w-full overflow-hidden rounded-full bg-slate-100">
                          <div
                            className="h-full bg-emerald-300 transition-all"
                            style={{ width: `${percentage}%` }}
                          />
                        </div>
                        <p className="mt-1 text-xs text-muted-foreground">
                          {percentage.toFixed(1)}% of total
                        </p>
                      </div>
                    );
                  }
                )}
              </div>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Interview Analytics */}
      <div className="grid gap-4 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <div className="flex items-center gap-2">
              <Calendar className="h-5 w-5 text-primary" />
              <CardTitle className="text-base font-semibold">
                Interview Status Distribution
              </CardTitle>
            </div>
            <CardDescription>
              Breakdown by interview completion status
            </CardDescription>
          </CardHeader>
          <CardContent>
            {interviewStatusDistQuery.isLoading && (
              <div className="flex justify-center py-8">
                <LoadingSpinner />
              </div>
            )}

            {interviewStatusDistQuery.isError && (
              <p className="rounded-md border border-destructive/40 bg-destructive/5 p-3 text-sm text-destructive">
                {getErrorMessage(interviewStatusDistQuery.error)}
              </p>
            )}

            {!interviewStatusDistQuery.isLoading &&
              !interviewStatusDistQuery.isError && (
                <div className="space-y-3">
                  {Object.entries(interviewStatusDistQuery.data || {}).map(
                    ([status, count]) => {
                      const statusLabels: Record<string, string> = {
                        "1": "Scheduled",
                        "2": "Completed",
                        "3": "Cancelled",
                        "4": "No Show",
                      };
                      const statusColors: Record<string, string> = {
                        "1": "bg-blue-500",
                        "2": "bg-green-500",
                        "3": "bg-gray-500",
                        "4": "bg-red-500",
                      };

                      return (
                        <div
                          key={status}
                          className="flex items-center justify-between rounded-lg border bg-white px-4 py-3"
                        >
                          <div className="flex items-center gap-3">
                            <div
                              className={`h-3 w-3 rounded-full ${
                                statusColors[status] || "bg-slate-500"
                              }`}
                            />
                            <span className="text-sm font-medium">
                              {statusLabels[status] || `Status ${status}`}
                            </span>
                          </div>
                          <span className="text-xl font-semibold">
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

        <Card>
          <CardHeader>
            <div className="flex items-center gap-2">
              <PieChart className="h-5 w-5 text-primary" />
              <CardTitle className="text-base font-semibold">
                Interview Type Distribution
              </CardTitle>
            </div>
            <CardDescription>Breakdown by interview type</CardDescription>
          </CardHeader>
          <CardContent>
            {interviewTypeDistQuery.isLoading && (
              <div className="flex justify-center py-8">
                <LoadingSpinner />
              </div>
            )}

            {interviewTypeDistQuery.isError && (
              <p className="rounded-md border border-destructive/40 bg-destructive/5 p-3 text-sm text-destructive">
                {getErrorMessage(interviewTypeDistQuery.error)}
              </p>
            )}

            {!interviewTypeDistQuery.isLoading &&
              !interviewTypeDistQuery.isError && (
                <div className="space-y-3">
                  {Object.entries(interviewTypeDistQuery.data || {}).map(
                    ([type, count]) => {
                      const typeLabels: Record<string, string> = {
                        "1": "Screening",
                        "2": "Technical",
                        "3": "Cultural",
                        "4": "Final",
                      };

                      return (
                        <div
                          key={type}
                          className="flex items-center justify-between rounded-lg border bg-white px-4 py-3"
                        >
                          <span className="text-sm font-medium">
                            {typeLabels[type] || `Type ${type}`}
                          </span>
                          <span className="text-xl font-semibold">
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
      </div>

      {/* Offer Status Distribution */}
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <CheckCircle className="h-5 w-5 text-primary" />
            <CardTitle className="text-base font-semibold">
              Offer Status Distribution
            </CardTitle>
          </div>
          <CardDescription>
            Current status of all extended offers
          </CardDescription>
        </CardHeader>
        <CardContent>
          {offerStatusDistQuery.isLoading && (
            <div className="flex justify-center py-8">
              <LoadingSpinner />
            </div>
          )}

          {offerStatusDistQuery.isError && (
            <p className="rounded-md border border-destructive/40 bg-destructive/5 p-3 text-sm text-destructive">
              {getErrorMessage(offerStatusDistQuery.error)}
            </p>
          )}

          {!offerStatusDistQuery.isLoading && !offerStatusDistQuery.isError && (
            <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
              {Object.entries(offerStatusDistQuery.data || {}).map(
                ([status, count]) => {
                  const statusLabels: Record<string, string> = {
                    "1": "Pending",
                    "2": "Accepted",
                    "3": "Rejected",
                    "4": "Countered",
                    "5": "Expired",
                    "6": "Withdrawn",
                  };
                  const statusIcons: Record<string, React.ElementType> = {
                    "1": Clock,
                    "2": CheckCircle,
                    "3": XCircle,
                    "4": Users,
                    "5": Calendar,
                    "6": XCircle,
                  };
                  const StatusIcon = statusIcons[status] || FileText;

                  return (
                    <div
                      key={status}
                      className="flex items-center justify-between rounded-lg border bg-white p-4"
                    >
                      <div className="flex items-center gap-3">
                        <StatusIcon className="h-5 w-5 text-muted-foreground" />
                        <span className="text-sm font-medium">
                          {statusLabels[status] || `Status ${status}`}
                        </span>
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
    </div>
  );
};
