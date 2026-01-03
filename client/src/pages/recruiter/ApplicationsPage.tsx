import { useMemo, useState, useEffect, useRef } from "react";
import { useNavigate } from "react-router-dom";
import { useJobApplicationSearch } from "@/hooks/staff/jobApplications.hooks";
import { useStaffJobSummaries } from "@/hooks/staff/jobPositions.hooks";
import { useDebounce } from "@/hooks/useDebounce";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Input } from "@/components/ui/input";
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
import { useAuth } from "@/store";
import {
  APPLICATION_STATUS_OPTIONS,
  getStatusMeta,
} from "@/constants/applicationStatus";
import { formatDateToLocal } from "@/utils/dateUtils";
import { getErrorMessage } from "@/utils/error";
import { ArrowUpDown, Search } from "lucide-react";

const FILTER_STATUS_OPTIONS = [
  { value: "all", label: "All statuses" },
  ...APPLICATION_STATUS_OPTIONS,
];

// type Schemas = components["schemas"];

type SortField = "appliedDate" | "status" | "candidateName";

const toIsoDateString = (value: string) => {
  if (!value) return undefined;
  const parsed = new Date(value);
  return Number.isNaN(parsed.getTime()) ? undefined : parsed.toISOString();
};

const parseNumericInput = (value: string) => {
  if (!value.trim()) return undefined;
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : undefined;
};

export const RecruiterApplicationsPage = () => {
  const [statusFilter, setStatusFilter] = useState("all");
  const [searchTerm, setSearchTerm] = useState("");
  const [jobFilter, setJobFilter] = useState("all");
  const [appliedFromDate, setAppliedFromDate] = useState("");
  const [appliedToDate, setAppliedToDate] = useState("");
  const [minTestScoreInput, setMinTestScoreInput] = useState("");
  const [maxTestScoreInput, setMaxTestScoreInput] = useState("");
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [sortConfig, setSortConfig] = useState<{
    field: SortField;
    direction: "asc" | "desc";
  }>({ field: "appliedDate", direction: "desc" });
  const userId = useAuth((state) => state.auth.user?.id ?? null);
  const userRoles = useAuth((state) => state.auth.roles ?? []);
  const navigate = useNavigate();
  const debouncedSearchTerm = useDebounce(searchTerm, 300);
  const jobSummaryParams = useMemo(
    () => ({
      pageNumber: 1,
      pageSize: 50,
    }),
    []
  );
  const jobPositionsQuery = useStaffJobSummaries(jobSummaryParams);

  const jobOptions = jobPositionsQuery.data?.items ?? [];
  const canResetFilters =
    statusFilter !== "all" ||
    jobFilter !== "all" ||
    Boolean(searchTerm.trim()) ||
    Boolean(appliedFromDate) ||
    Boolean(appliedToDate) ||
    Boolean(minTestScoreInput.trim()) ||
    Boolean(maxTestScoreInput.trim());

  const handleResetFilters = () => {
    setStatusFilter("all");
    setSearchTerm("");
    setJobFilter("all");
    setAppliedFromDate("");
    setAppliedToDate("");
    setMinTestScoreInput("");
    setMaxTestScoreInput("");
    setPageNumber(1);
  };

  const searchParams = useMemo(() => {
    const params: Record<string, unknown> = {
      pageNumber,
      pageSize,
    };

    if (statusFilter !== "all") {
      params.status = Number(statusFilter);
    }

    // Only filtering by assigned recruiter if not SuperAdmin, Admin, or HR
    if (
      userId &&
      !userRoles.some((role) => ["SuperAdmin", "Admin", "HR"].includes(role))
    ) {
      params.assignedRecruiterId = userId;
    }

    if (jobFilter !== "all") {
      params.jobPositionId = jobFilter;
    }

    const fromIso = toIsoDateString(appliedFromDate);
    if (fromIso) {
      params.appliedFromDate = fromIso;
    }

    const toIso = toIsoDateString(appliedToDate);
    if (toIso) {
      params.appliedToDate = toIso;
    }

    const minScore = parseNumericInput(minTestScoreInput);
    if (minScore !== undefined) {
      params.minTestScore = minScore;
    }

    const maxScore = parseNumericInput(maxTestScoreInput);
    if (maxScore !== undefined) {
      params.maxTestScore = maxScore;
    }

    if (debouncedSearchTerm.trim()) {
      params.searchTerm = debouncedSearchTerm.trim();
    }

    return params;
  }, [
    statusFilter,
    debouncedSearchTerm,
    userId,
    userRoles,
    jobFilter,
    appliedFromDate,
    appliedToDate,
    minTestScoreInput,
    maxTestScoreInput,
    pageNumber,
    pageSize,
  ]);

  const applicationsQuery = useJobApplicationSearch(searchParams);

  const items = applicationsQuery.data?.items ?? [];
  const totalCount = applicationsQuery.data?.totalCount ?? 0;

  // Reset to page 1 when filters change
  useEffect(() => {
    setPageNumber(1);
  }, [
    statusFilter,
    debouncedSearchTerm,
    jobFilter,
    appliedFromDate,
    appliedToDate,
    minTestScoreInput,
    maxTestScoreInput,
  ]);

  // Capture total count from first successful query
  const totalCaptured = useRef(false);
  const [totalApplicationsCount, setTotalApplicationsCount] = useState(0);

  useEffect(() => {
    if (applicationsQuery.isSuccess && !totalCaptured.current) {
      setTotalApplicationsCount(applicationsQuery.data?.totalCount ?? 0);
      totalCaptured.current = true;
    }
  }, [applicationsQuery.isSuccess]);

  const processedItems = useMemo(() => {
    const base = [...items];

    const sorted = base.sort((a, b) => {
      const directionFactor = sortConfig.direction === "asc" ? 1 : -1;

      if (sortConfig.field === "appliedDate") {
        const dateA = a?.appliedDate ? new Date(a.appliedDate).getTime() : 0;
        const dateB = b?.appliedDate ? new Date(b.appliedDate).getTime() : 0;
        return (dateA - dateB) * directionFactor;
      }

      if (sortConfig.field === "status") {
        const statusA = a?.status ?? 0;
        const statusB = b?.status ?? 0;
        return (statusA - statusB) * directionFactor;
      }

      const nameA = a?.candidateName ?? "";
      const nameB = b?.candidateName ?? "";
      return nameA.localeCompare(nameB) * directionFactor;
    });

    return sorted;
  }, [items, sortConfig]);

  const displayItems = processedItems;

  const handleRowClick = (id?: string) => {
    if (!id) return;
    navigate(`/recruiter/applications/${id}`);
  };

  const handleSortChange = (field: SortField) => {
    setSortConfig((prev) => {
      if (prev.field === field) {
        return {
          field,
          direction: prev.direction === "asc" ? "desc" : "asc",
        };
      }
      return { field, direction: "desc" };
    });
  };

  const renderSortableHeader = (label: string, field: SortField) => {
    const isActive = sortConfig.field === field;
    const directionLabel = isActive
      ? sortConfig.direction === "asc"
        ? "↑"
        : "↓"
      : "";
    return (
      <button
        type="button"
        onClick={() => handleSortChange(field)}
        className="inline-flex items-center gap-1 text-left text-sm font-medium text-muted-foreground transition-colors hover:text-foreground"
      >
        {label}
        <ArrowUpDown
          className={`h-3 w-3 ${
            isActive ? "text-foreground" : "text-muted-foreground"
          }`}
        />
        <span className="text-[10px] text-muted-foreground">
          {directionLabel}
        </span>
      </button>
    );
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h1 className="text-3xl font-semibold text-foreground">
            Applications workspace
          </h1>
          <p className="text-muted-foreground text-sm">
            Filter, prioritize, and take action on applications assigned to you.
          </p>
        </div>
        <div className="flex items-center gap-3">
          <Button variant="outline" onClick={() => applicationsQuery.refetch()}>
            Refresh
          </Button>
        </div>
      </div>

      <div className="grid gap-4">
        {/* Total Applications Summary */}
        <Card className="border-l-4 border-l-blue-500">
          <CardContent className="pt-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-muted-foreground">
                  Total Applications in Talent Pool
                </p>
                <p className="text-2xl font-bold text-foreground">
                  {totalApplicationsCount}
                </p>
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Filters</CardTitle>
            <CardDescription>Narrow down your personal queue.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="grid gap-4 md:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="search-input">Search</Label>
                <div className="relative">
                  <Input
                    id="search-input"
                    placeholder="Search candidate or job title"
                    value={searchTerm}
                    onChange={(event) => setSearchTerm(event.target.value)}
                    className="pl-9"
                  />
                  <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                </div>
              </div>

              <div className="space-y-2">
                <Label htmlFor="status-filter">Status</Label>
                <Select
                  value={statusFilter}
                  onValueChange={(value) => setStatusFilter(value)}
                >
                  <SelectTrigger id="status-filter">
                    <SelectValue placeholder="All statuses" />
                  </SelectTrigger>
                  <SelectContent className="bg-emerald-50">
                    {FILTER_STATUS_OPTIONS.map((option) => (
                      <SelectItem key={option.value} value={option.value}>
                        {option.label}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            </div>

            <div className="grid gap-4 md:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="job-filter">Job position</Label>
                <Select
                  value={jobFilter}
                  onValueChange={(value) => setJobFilter(value)}
                >
                  <SelectTrigger id="job-filter">
                    <SelectValue placeholder="All jobs" />
                  </SelectTrigger>
                  <SelectContent className="bg-emerald-50">
                    <SelectItem value="all">All roles</SelectItem>
                    {jobPositionsQuery.isLoading && (
                      <SelectItem value="__loading" disabled>
                        Loading job positions...
                      </SelectItem>
                    )}
                    {jobOptions
                      .filter((job) => Boolean(job?.id))
                      .map((job) => (
                        <SelectItem key={job?.id} value={job?.id as string}>
                          {job?.title ?? "Untitled role"}
                        </SelectItem>
                      ))}
                  </SelectContent>
                </Select>
              </div>

              <div className="flex flex-wrap gap-2 pt-1">
                <Button
                  type="button"
                  size="sm"
                  variant="outline"
                  onClick={handleResetFilters}
                  disabled={!canResetFilters}
                >
                  Reset filters
                </Button>
              </div>
            </div>

            <div className="grid gap-3 md:grid-cols-4">
              <div className="space-y-2">
                <Label htmlFor="applied-from">Applied from</Label>
                <Input
                  id="applied-from"
                  type="date"
                  value={appliedFromDate}
                  onChange={(event) => setAppliedFromDate(event.target.value)}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="applied-to">Applied to</Label>
                <Input
                  id="applied-to"
                  type="date"
                  value={appliedToDate}
                  onChange={(event) => setAppliedToDate(event.target.value)}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="min-score">Min score</Label>
                <Input
                  id="min-score"
                  type="number"
                  inputMode="numeric"
                  min="0"
                  max="100"
                  placeholder="e.g. 60"
                  value={minTestScoreInput}
                  onChange={(event) => setMinTestScoreInput(event.target.value)}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="max-score">Max score</Label>
                <Input
                  id="max-score"
                  type="number"
                  inputMode="numeric"
                  min="0"
                  max="100"
                  placeholder="e.g. 90"
                  value={maxTestScoreInput}
                  onChange={(event) => setMaxTestScoreInput(event.target.value)}
                />
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle>
                Applications{" "}
                <span className="text-base">({totalCount} results)</span>
              </CardTitle>
              <CardDescription>Applications assigned to you</CardDescription>
            </div>
            <div className="flex items-center gap-2">
              <Label
                htmlFor="pageSize"
                className="text-xs text-muted-foreground"
              >
                Per page:
              </Label>
              <Select
                value={String(pageSize)}
                onValueChange={(value) => {
                  setPageSize(Number(value));
                  setPageNumber(1);
                }}
              >
                <SelectTrigger id="pageSize" className="w-20">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="10">10</SelectItem>
                  <SelectItem value="25">25</SelectItem>
                  <SelectItem value="50">50</SelectItem>
                  <SelectItem value="100">100</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
        </CardHeader>
        <CardContent className="space-y-4">
          {applicationsQuery.isLoading && (
            <div className="flex justify-center py-6">
              <LoadingSpinner />
            </div>
          )}

          {applicationsQuery.isError && (
            <p className="rounded-md border border-destructive/40 bg-destructive/5 p-3 text-sm text-destructive">
              {getErrorMessage(applicationsQuery.error)}
            </p>
          )}

          {!applicationsQuery.isLoading &&
            !applicationsQuery.isError &&
            !items.length && (
              <p className="text-sm text-muted-foreground">
                No applications match the current filters.
              </p>
            )}

          {!applicationsQuery.isLoading &&
            !applicationsQuery.isError &&
            !!items.length &&
            !displayItems.length && (
              <p className="text-sm text-muted-foreground">
                No applications match the current search or sorting.
              </p>
            )}

          {Boolean(displayItems.length) && (
            <div className="rounded-lg border border-slate-100">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Application ID</TableHead>
                    <TableHead>
                      {renderSortableHeader("Candidate", "candidateName")}
                    </TableHead>
                    <TableHead>Job</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead>
                      {renderSortableHeader("Applied", "appliedDate")}
                    </TableHead>
                    <TableHead>Assigned recruiter</TableHead>
                    <TableHead className="text-right">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {displayItems.map((application, index) => {
                    const statusMeta = getStatusMeta(
                      application?.status as number
                    );
                    return (
                      <TableRow
                        key={
                          application?.id ??
                          `${application?.candidateName ?? "candidate"}-${
                            application?.jobTitle ?? "role"
                          }-${index}`
                        }
                        className="transition-colors hover:bg-muted/40"
                      >
                        <TableCell className="font-mono text-xs text-muted-foreground">
                          {application?.id ?? "—"}
                        </TableCell>
                        <TableCell className="font-medium">
                          {application?.candidateName ?? "Unknown"}
                        </TableCell>
                        <TableCell>{application?.jobTitle ?? "—"}</TableCell>
                        <TableCell>
                          <Badge variant={statusMeta.variant}>
                            {statusMeta.label}
                          </Badge>
                        </TableCell>
                        <TableCell>
                          {formatDateToLocal(application?.appliedDate)}
                        </TableCell>
                        <TableCell>
                          {application?.assignedRecruiterName || "Unassigned"}
                        </TableCell>
                        <TableCell className="text-right">
                          <Button
                            size="sm"
                            variant="ghost"
                            onClick={() => handleRowClick(application?.id)}
                            className="cursor-pointer"
                          >
                            View
                          </Button>
                        </TableCell>
                      </TableRow>
                    );
                  })}
                </TableBody>
              </Table>
            </div>
          )}
        </CardContent>
        {applicationsQuery.data && applicationsQuery.data.totalCount! > 0 && (
          <CardContent className="pt-0">
            <div className="flex items-center justify-between border-t pt-4">
              <div className="text-sm text-muted-foreground">
                Showing {displayItems.length} of{" "}
                {applicationsQuery.data.totalCount} applications
                {applicationsQuery.data.totalPages! > 1 && (
                  <span className="ml-2">
                    (Page {applicationsQuery.data.pageNumber} of{" "}
                    {applicationsQuery.data.totalPages})
                  </span>
                )}
              </div>
              {applicationsQuery.data.totalPages! > 1 && (
                <div className="flex items-center gap-2">
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setPageNumber(pageNumber - 1)}
                    disabled={!applicationsQuery.data?.hasPreviousPage}
                  >
                    Previous
                  </Button>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setPageNumber(pageNumber + 1)}
                    disabled={!applicationsQuery.data?.hasNextPage}
                  >
                    Next
                  </Button>
                </div>
              )}
            </div>
          </CardContent>
        )}
      </Card>
    </div>
  );
};
