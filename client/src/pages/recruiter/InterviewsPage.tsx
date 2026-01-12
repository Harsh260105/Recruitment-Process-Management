import { useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { LoadingSpinner } from "@/components/ui/LoadingSpinner";
import {
  interviewTypeLabels,
  interviewModeLabels,
  getInterviewStatusMeta,
  getInterviewOutcomeMeta,
} from "@/constants/interviewEvaluations";
import {
  usePendingEvaluations,
  useSearchInterviews,
  useInterviewsRequiringAction,
} from "@/hooks/staff/interviews.hooks";
import { formatDateTimeToLocal } from "@/utils/dateUtils";
import { getErrorMessage } from "@/utils/error";
import type { components } from "@/types/api";
import {
  Calendar,
  Filter,
  ArrowRight,
  CheckCircle,
  XCircle,
  AlertTriangle,
  Search,
} from "lucide-react";

type Schemas = components["schemas"];
type InterviewListItem = Schemas["InterviewSummaryDto"];

const PAGE_SIZE_OPTIONS = [10, 25, 50];

const createDefaultFilters = () => {
  const today = new Date();
  const oneWeekAfter = new Date();
  oneWeekAfter.setDate(today.getDate() + 7);
  const twoWeeksAgo = new Date();
  twoWeeksAgo.setDate(today.getDate() - 14);
  return {
    status: "all",
    interviewType: "all",
    mode: "all",
    fromDate: twoWeeksAgo.toISOString().split("T")[0],
    toDate: oneWeekAfter.toISOString().split("T")[0],
    jobApplicationId: "",
  };
};

const toBoundaryIso = (value?: string, boundary: "start" | "end" = "start") => {
  if (!value) return undefined;
  const date = new Date(value + "T00:00:00"); // Treat as local date
  if (Number.isNaN(date.getTime())) return undefined;
  if (boundary === "start") {
    date.setHours(0, 0, 0, 0);
  } else {
    date.setHours(23, 59, 59, 999);
  }
  return date.toISOString();
};

const statusOptions = [
  { value: "all", label: "Any status" },
  { value: "1", label: "Scheduled" },
  { value: "2", label: "Completed" },
  { value: "3", label: "Cancelled" },
  { value: "4", label: "No-show" },
];

const typeOptions = [
  { value: "all", label: "Any type" },
  ...Object.entries(interviewTypeLabels).map(([value, label]) => ({
    value,
    label,
  })),
];

const modeOptions = [
  { value: "all", label: "Any mode" },
  ...Object.entries(interviewModeLabels).map(([value, label]) => ({
    value,
    label,
  })),
];

const isGuid = (value: string) =>
  /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value);

export const RecruiterInterviewsPage = () => {
  const navigate = useNavigate();
  const [filters, setFilters] = useState(createDefaultFilters);
  const [appliedFilters, setAppliedFilters] = useState(createDefaultFilters);
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(PAGE_SIZE_OPTIONS[0]);
  const [showRequiringAction, setShowRequiringAction] = useState(false);
  const pendingEvaluations = usePendingEvaluations();

  const jobApplicationInput = appliedFilters.jobApplicationId.trim();
  const jobApplicationId = isGuid(jobApplicationInput)
    ? jobApplicationInput
    : "";
  const jobApplicationIdInvalid =
    Boolean(jobApplicationInput) && !jobApplicationId;
  const jobApplicationFieldInvalid =
    Boolean(filters.jobApplicationId.trim()) &&
    !isGuid(filters.jobApplicationId.trim());
  const searchPayload = useMemo(() => {
    const payload: Schemas["InterviewSearchDto"] = {
      pageNumber,
      pageSize,
    };

    if (appliedFilters.status !== "all") {
      payload.status = Number(
        appliedFilters.status
      ) as Schemas["InterviewStatus"];
    }

    if (appliedFilters.interviewType !== "all") {
      payload.interviewType = Number(
        appliedFilters.interviewType
      ) as Schemas["InterviewType"];
    }

    if (appliedFilters.mode !== "all") {
      payload.mode = Number(appliedFilters.mode) as Schemas["InterviewMode"];
    }

    const fromIso = toBoundaryIso(appliedFilters.fromDate, "start");
    if (fromIso) {
      payload.scheduledFromDate = fromIso;
    }

    const toIso = toBoundaryIso(appliedFilters.toDate, "end");
    if (toIso) {
      payload.scheduledToDate = toIso;
    }

    if (jobApplicationId) {
      payload.jobApplicationId = jobApplicationId;
    }

    return payload;
  }, [appliedFilters, jobApplicationId, pageNumber, pageSize]);

  const interviewsQuery = showRequiringAction
    ? useInterviewsRequiringAction({ pageNumber, pageSize })
    : useSearchInterviews(searchPayload);

  const isJobApplicationView = Boolean(jobApplicationId);
  const interviews = interviewsQuery.data?.items ?? ([] as InterviewListItem[]);

  const totalPages = interviewsQuery.data?.totalPages ?? 1;
  const hasNextPage =
    interviewsQuery.data?.hasNextPage ?? pageNumber < totalPages;
  const hasPreviousPage =
    interviewsQuery.data?.hasPreviousPage ?? pageNumber > 1;

  const isSearching = interviewsQuery.isLoading || interviewsQuery.isFetching;
  const searchError = interviewsQuery.error
    ? getErrorMessage(interviewsQuery.error)
    : null;

  const canResetFilters = useMemo(() => {
    const defaults = createDefaultFilters();
    return (
      JSON.stringify(filters) !== JSON.stringify(defaults) ||
      showRequiringAction
    );
  }, [filters, showRequiringAction]);

  const statusCounts = useMemo(() => {
    return interviews.reduce(
      (acc, interview) => {
        switch (interview?.status) {
          case 1:
            acc.scheduled += 1;
            break;
          case 2:
            acc.completed += 1;
            break;
          case 3:
            acc.cancelled += 1;
            break;
          case 4:
            acc.noShow += 1;
            break;
          default:
            break;
        }
        return acc;
      },
      { scheduled: 0, completed: 0, cancelled: 0, noShow: 0 }
    );
  }, [interviews]);

  const handleApplyFilters = () => {
    setAppliedFilters(filters);
    setPageNumber(1);
  };

  const handleResetFilters = () => {
    const defaults = createDefaultFilters();
    setFilters(defaults);
    setAppliedFilters(defaults);
    setPageNumber(1);
    setShowRequiringAction(false);
  };

  const handleToggleRequiringAction = () => {
    setShowRequiringAction(!showRequiringAction);
    setPageNumber(1);
  };

  const statusCards = [
    {
      label: "Scheduled",
      value: statusCounts.scheduled,
      icon: Calendar,
    },
    {
      label: "Completed",
      value: statusCounts.completed,
      icon: CheckCircle,
    },
    {
      label: "Cancelled",
      value: statusCounts.cancelled,
      icon: XCircle,
    },
    {
      label: "No-show",
      value: statusCounts.noShow,
      icon: AlertTriangle,
    },
  ];

  return (
    <div className="space-y-6">
      <div className="space-y-1">
        <h1 className="text-3xl font-semibold text-foreground">
          Interview operations
        </h1>
        <p className="text-sm text-muted-foreground">
          Track progress, unblock interviewers, and dive into any conversation
          with a single click.
        </p>
      </div>

      <div className="grid gap-4 md:grid-cols-4">
        {statusCards.map(({ label, value, icon: Icon }) => (
          <Card key={label}>
            <CardHeader className="flex flex-row items-center justify-between pb-2">
              <div>
                <p className="text-xs uppercase text-muted-foreground">
                  {label}
                </p>
                <CardTitle className="text-3xl">{value}</CardTitle>
              </div>
              <Icon className="h-8 w-8 text-muted-foreground" />
            </CardHeader>
            <CardContent>
              <p className="text-xs text-muted-foreground">
                Counts reflect the currently visible interviews
              </p>
            </CardContent>
          </Card>
        ))}
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-base">
            <Filter className="h-4 w-4" /> Interview filters
          </CardTitle>
          <CardDescription>
            Filters apply server-side to any interview you're allowed to see,
            including those you host or applications assigned to you.
          </CardDescription>
          <div className="flex items-center space-x-2 pt-2">
            <input
              type="checkbox"
              id="requiring-action-toggle"
              checked={showRequiringAction}
              onChange={handleToggleRequiringAction}
              className="h-4 w-4 rounded border-gray-300 text-primary focus:ring-primary"
            />
            <Label htmlFor="requiring-action-toggle" className="text-sm">
              Show only interviews requiring action
            </Label>
          </div>
        </CardHeader>
        <CardContent className="space-y-4">
          {!showRequiringAction && (
            <>
              <div className="grid gap-4 md:grid-cols-3">
                <div className="space-y-2">
                  <Label htmlFor="status-select">Status</Label>
                  <Select
                    value={filters.status}
                    onValueChange={(value) =>
                      setFilters((prev) => ({ ...prev, status: value }))
                    }
                  >
                    <SelectTrigger id="status-select">
                      <SelectValue placeholder="Status" />
                    </SelectTrigger>
                    <SelectContent className="bg-emerald-50">
                      {statusOptions.map((option) => (
                        <SelectItem key={option.value} value={option.value}>
                          {option.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
                <div className="space-y-2">
                  <Label htmlFor="type-select">Interview type</Label>
                  <Select
                    value={filters.interviewType}
                    onValueChange={(value) =>
                      setFilters((prev) => ({ ...prev, interviewType: value }))
                    }
                  >
                    <SelectTrigger id="type-select">
                      <SelectValue placeholder="Interview type" />
                    </SelectTrigger>
                    <SelectContent className="bg-emerald-50">
                      {typeOptions.map((option) => (
                        <SelectItem key={option.value} value={option.value}>
                          {option.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
                <div className="space-y-2">
                  <Label htmlFor="mode-select">Mode</Label>
                  <Select
                    value={filters.mode}
                    onValueChange={(value) =>
                      setFilters((prev) => ({ ...prev, mode: value }))
                    }
                  >
                    <SelectTrigger id="mode-select">
                      <SelectValue placeholder="Mode" />
                    </SelectTrigger>
                    <SelectContent className="bg-emerald-50">
                      {modeOptions.map((option) => (
                        <SelectItem key={option.value} value={option.value}>
                          {option.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              </div>

              <div className="grid gap-4 md:grid-cols-4">
                <div className="space-y-2">
                  <Label htmlFor="from-date">From</Label>
                  <Input
                    id="from-date"
                    type="date"
                    value={filters.fromDate}
                    onChange={(event) =>
                      setFilters((prev) => ({
                        ...prev,
                        fromDate: event.target.value,
                      }))
                    }
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="to-date">To</Label>
                  <Input
                    id="to-date"
                    type="date"
                    value={filters.toDate}
                    onChange={(event) =>
                      setFilters((prev) => ({
                        ...prev,
                        toDate: event.target.value,
                      }))
                    }
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="application-id">Job application ID</Label>
                  <div className="relative">
                    <Input
                      id="application-id"
                      placeholder="Optional filter"
                      value={filters.jobApplicationId}
                      onChange={(event) =>
                        setFilters((prev) => ({
                          ...prev,
                          jobApplicationId: event.target.value,
                        }))
                      }
                      className="pl-9"
                    />
                    <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                  </div>
                  {jobApplicationFieldInvalid && (
                    <p className="text-xs text-destructive">
                      Enter a valid application ID (GUID) before applying
                      filters.
                    </p>
                  )}
                </div>
              </div>

              <div className="flex flex-wrap items-center gap-3">
                <Button
                  type="button"
                  onClick={handleApplyFilters}
                  disabled={jobApplicationFieldInvalid}
                >
                  Apply filters
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  disabled={!canResetFilters}
                  onClick={handleResetFilters}
                >
                  Reset
                </Button>
                <div className="ml-auto flex items-center gap-2">
                  <Label
                    htmlFor="page-size"
                    className="text-xs text-muted-foreground"
                  >
                    Rows per page
                  </Label>
                  <Select
                    value={String(pageSize)}
                    onValueChange={(value) => {
                      setPageSize(Number(value));
                      setPageNumber(1);
                    }}
                  >
                    <SelectTrigger id="page-size" className="h-9 w-24">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent className="bg-emerald-50">
                      {PAGE_SIZE_OPTIONS.map((size) => (
                        <SelectItem key={size} value={String(size)}>
                          {size}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              </div>
            </>
          )}
        </CardContent>
      </Card>

      <div className="grid gap-6 lg:grid-cols-3">
        <Card className="lg:col-span-2">
          <CardHeader>
            <CardTitle>Recruiter-facing interviews</CardTitle>
            <CardDescription>
              All recruiter-visible interviews that match your selected filters.
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            {jobApplicationIdInvalid && (
              <p className="rounded-md border border-amber-400/60 bg-amber-50/80 p-3 text-sm text-amber-900">
                The supplied job application ID is not valid. Showing
                recruiter-visible interviews instead.
              </p>
            )}
            {searchError && (
              <p className="rounded-md border border-destructive/40 bg-destructive/5 p-3 text-sm text-destructive">
                {searchError}
              </p>
            )}

            <div className="overflow-x-auto">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Interview</TableHead>
                    <TableHead>Candidate</TableHead>
                    <TableHead>Schedule</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead>Outcome</TableHead>
                    <TableHead className="text-right">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {isSearching && (
                    <TableRow>
                      <TableCell colSpan={6}>
                        <div className="flex justify-center py-8">
                          <LoadingSpinner />
                        </div>
                      </TableCell>
                    </TableRow>
                  )}

                  {!isSearching && !interviews.length && (
                    <TableRow>
                      <TableCell colSpan={6}>
                        <p className="py-6 text-center text-sm text-muted-foreground">
                          No interviews match the current filters.
                        </p>
                      </TableCell>
                    </TableRow>
                  )}

                  {interviews.map((interview) => {
                    const participantCount = interview?.participantCount ?? 0;
                    const outcomeValue = interview?.outcome ?? null;

                    return (
                      <TableRow key={interview?.id}>
                        <TableCell>
                          <div className="space-y-1">
                            <p className="font-semibold">
                              {interview?.title ?? "Untitled"}
                            </p>
                            <p className="text-xs text-muted-foreground">
                              Round {interview?.roundNumber ?? 1} ·{" "}
                              {
                                interviewTypeLabels[
                                  interview?.interviewType ?? 1
                                ]
                              }
                            </p>
                          </div>
                        </TableCell>
                        <TableCell>
                          <p className="text-sm">
                            {interview?.candidateName ?? "Unknown candidate"}
                          </p>
                        </TableCell>
                        <TableCell>
                          <div className="space-y-1">
                            <p className="text-sm font-medium">
                              {formatDateTimeToLocal(
                                interview?.scheduledDateTime
                              )}
                            </p>
                            <p className="text-xs text-muted-foreground">
                              {interviewModeLabels[interview?.mode ?? 2]}
                              {participantCount > 0 &&
                                ` · ${participantCount} participants`}
                            </p>
                          </div>
                        </TableCell>
                        <TableCell>
                          <Badge
                            variant={
                              getInterviewStatusMeta(interview?.status).variant
                            }
                          >
                            {getInterviewStatusMeta(interview?.status).label}
                          </Badge>
                        </TableCell>
                        <TableCell>
                          {outcomeValue ? (
                            <Badge
                              variant={
                                getInterviewOutcomeMeta(outcomeValue).variant
                              }
                            >
                              {getInterviewOutcomeMeta(outcomeValue).label}
                            </Badge>
                          ) : (
                            <span className="text-xs text-muted-foreground">
                              {isJobApplicationView
                                ? "Pending"
                                : "Interview not completed"}
                            </span>
                          )}
                        </TableCell>
                        <TableCell>
                          <div className="flex justify-end">
                            <Button
                              size="sm"
                              variant="outline"
                              onClick={() =>
                                navigate(
                                  `/recruiter/interviews/${interview?.id}`
                                )
                              }
                              disabled={!interview?.id}
                            >
                              View details
                            </Button>
                          </div>
                        </TableCell>
                      </TableRow>
                    );
                  })}
                </TableBody>
              </Table>
            </div>

            {interviewsQuery.data && interviewsQuery.data.totalCount! > 0 && (
              <div className="border-t pt-4 mt-4">
                <div className="flex items-center justify-between">
                  <div className="text-sm text-muted-foreground">
                    Showing {interviews.length} of{" "}
                    {interviewsQuery.data.totalCount} interviews
                    {totalPages > 1 && (
                      <span className="ml-2">
                        (Page {pageNumber} of {totalPages})
                      </span>
                    )}
                  </div>
                  {totalPages > 1 && (
                    <div className="flex items-center gap-2">
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => setPageNumber(pageNumber - 1)}
                        disabled={!hasPreviousPage || isSearching}
                      >
                        Previous
                      </Button>
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => setPageNumber(pageNumber + 1)}
                        disabled={!hasNextPage || isSearching}
                      >
                        Next
                      </Button>
                    </div>
                  )}
                </div>
              </div>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="text-base">Needs attention</CardTitle>
            <CardDescription>your outstanding evaluations.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-3">
            {pendingEvaluations.isLoading && (
              <div className="flex justify-center py-4">
                <LoadingSpinner />
              </div>
            )}

            {pendingEvaluations.isError && (
              <p className="rounded-md border border-destructive/40 bg-destructive/5 p-3 text-xs text-destructive">
                {getErrorMessage(pendingEvaluations.error)}
              </p>
            )}

            {!pendingEvaluations.isLoading &&
            !(pendingEvaluations.data?.length ?? 0) ? (
              <p className="text-sm text-muted-foreground">
                No pending interview actions right now.
              </p>
            ) : (
              <div className="space-y-3">
                {pendingEvaluations.data?.map((item) => (
                  <div
                    key={item?.id}
                    className="rounded-lg border px-4 py-3 text-sm"
                  >
                    <p className="font-semibold">
                      {item?.title ?? "Unnamed interview"}
                    </p>
                    <p className="text-xs text-muted-foreground">
                      {item?.jobApplication?.candidateName ??
                        "Unknown candidate"}
                    </p>
                    <p className="text-xs text-muted-foreground">
                      {formatDateTimeToLocal(item?.scheduledDateTime)} ·{" "}
                      {getInterviewStatusMeta(item?.status).label}
                    </p>
                    <div className="mt-2 flex justify-end">
                      <Button
                        size="sm"
                        variant="ghost"
                        onClick={() =>
                          navigate(`/recruiter/interviews/${item?.id}`)
                        }
                        disabled={!item?.id}
                      >
                        Review <ArrowRight className="ml-1 h-4 w-4" />
                      </Button>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
};
