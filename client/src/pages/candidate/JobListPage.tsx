import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { Briefcase, CalendarDays, MapPin, Search } from "lucide-react";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { LoadingSpinner } from "@/components/ui/LoadingSpinner";
import { ProfileRequiredWrapper } from "@/components/common/ProfileRequiredWrapper";
import { usePublicJobSummaries, type JobListFilters } from "@/hooks/candidate";
import { formatDateToLocal } from "@/utils/dateUtils";
import { getErrorMessage } from "@/utils/error";

const EXPERIENCE_LEVELS = [
  "Internship",
  "Entry",
  "Junior",
  "Mid",
  "Senior",
  "Lead",
];

const EXPERIENCE_ANY_VALUE = "__all";

const PAGE_SIZE = 10;

const useDebouncedValue = (value: string, delay = 700) => {
  const [debounced, setDebounced] = useState(value);

  useEffect(() => {
    const handler = setTimeout(() => setDebounced(value), delay);
    return () => clearTimeout(handler);
  }, [value, delay]);

  return debounced;
};

export const CandidateJobListPage = () => {
  const [searchTerm, setSearchTerm] = useState("");
  const [location, setLocation] = useState("");
  const [department, setDepartment] = useState("");
  const [experienceLevel, setExperienceLevel] = useState("");
  const [page, setPage] = useState(1);

  const debouncedSearch = useDebouncedValue(searchTerm);
  const debouncedLocation = useDebouncedValue(location);
  const debouncedDepartment = useDebouncedValue(department);

  useEffect(() => {
    setPage(1);
  }, [
    debouncedSearch,
    debouncedLocation,
    debouncedDepartment,
    experienceLevel,
  ]);

  const filters = useMemo<JobListFilters>(
    () => ({
      pageNumber: page,
      pageSize: PAGE_SIZE,
      searchTerm: debouncedSearch || undefined,
      department: debouncedDepartment || undefined,
      location: debouncedLocation || undefined,
      experienceLevel: experienceLevel || undefined,
    }),
    [
      page,
      debouncedSearch,
      debouncedDepartment,
      debouncedLocation,
      experienceLevel,
    ]
  );

  const jobQuery = usePublicJobSummaries(filters);

  const jobs = jobQuery.data?.items ?? [];
  const totalCount = jobQuery.data?.totalCount ?? 0;
  const hasFilters = Boolean(
    debouncedSearch ||
      debouncedLocation ||
      debouncedDepartment ||
      experienceLevel
  );

  const resetFilters = () => {
    setSearchTerm("");
    setLocation("");
    setDepartment("");
    setExperienceLevel("");
  };

  const normalizedExperienceSelectValue =
    experienceLevel || EXPERIENCE_ANY_VALUE;

  return (
    <ProfileRequiredWrapper>
      <div className="space-y-6">
        <div className="flex flex-wrap items-center justify-between gap-4">
          <div>
            <h1 className="text-2xl font-bold">Explore roles</h1>
            <p className="text-muted-foreground">
              Find openings that match your skills and interests.
            </p>
          </div>
          {hasFilters && (
            <Button variant="ghost" onClick={resetFilters} size="sm">
              Reset filters
            </Button>
          )}
        </div>

        <Card>
          <CardHeader>
            <CardTitle>Filters</CardTitle>
            <CardDescription>
              Narrow results by keywords and details.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
              <div className="space-y-2">
                <label className="text-sm font-medium text-foreground">
                  Keywords
                </label>
                <div className="relative">
                  <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                  <Input
                    value={searchTerm}
                    onChange={(event) => setSearchTerm(event.target.value)}
                    className="pl-9"
                    placeholder="Search title or skills"
                  />
                </div>
              </div>
              <div className="space-y-2">
                <label className="text-sm font-medium text-foreground">
                  Location
                </label>
                <Input
                  value={location}
                  onChange={(event) => setLocation(event.target.value)}
                  placeholder="Any"
                />
              </div>
              <div className="space-y-2">
                <label className="text-sm font-medium text-foreground">
                  Department
                </label>
                <Input
                  value={department}
                  onChange={(event) => setDepartment(event.target.value)}
                  placeholder="Any"
                />
              </div>
              <div className="space-y-2">
                <label className="text-sm font-medium text-foreground">
                  Experience level
                </label>
                <Select
                  value={normalizedExperienceSelectValue}
                  onValueChange={(value) =>
                    setExperienceLevel(
                      value === EXPERIENCE_ANY_VALUE ? "" : value
                    )
                  }
                >
                  <SelectTrigger>
                    <SelectValue placeholder="Any" />
                  </SelectTrigger>
                  <SelectContent className="bg-emerald-50">
                    <SelectItem value={EXPERIENCE_ANY_VALUE}>Any</SelectItem>
                    {EXPERIENCE_LEVELS.map((level) => (
                      <SelectItem key={level} value={level}>
                        {level}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            </div>
          </CardContent>
        </Card>

        {jobQuery.isLoading ? (
          <div className="flex justify-center py-10">
            <LoadingSpinner />
          </div>
        ) : jobQuery.isError ? (
          <Card className="border-destructive/30 bg-destructive/5">
            <CardContent className="py-6 text-destructive">
              {getErrorMessage(jobQuery.error)}
            </CardContent>
          </Card>
        ) : !jobs.length ? (
          <Card>
            <CardContent className="py-6 text-center text-sm text-muted-foreground">
              No roles match your filters yet. Try adjusting your search terms.
            </CardContent>
          </Card>
        ) : (
          <div className="space-y-4">
            <p className="text-sm text-muted-foreground">
              Showing {jobs.length} of {totalCount} roles
            </p>
            <div className="grid gap-4">
              {jobs.map((job) => (
                <Card
                  key={job.id ?? job.title}
                  className="shadow-sm hover:shadow-md transition-shadow duration-300 border-l-3 border-l-primary/20 hover:border-l-primary"
                >
                  <CardHeader className="gap-2 pb-3">
                    <div className="flex flex-wrap items-start justify-between gap-3">
                      <div className="flex-1 min-w-0">
                        <CardTitle className="text-lg leading-tight truncate">
                          {job.title ?? "Untitled role"}
                        </CardTitle>
                        <CardDescription className="text-sm">
                          {job.department || "General"}
                        </CardDescription>
                      </div>
                      {job.applicationDeadline && (
                        <Badge variant="secondary" className="shrink-0">
                          <CalendarDays className="h-3 w-3 mr-1" />
                          Apply by {formatDateToLocal(job.applicationDeadline)}
                        </Badge>
                      )}
                    </div>
                  </CardHeader>
                  <CardContent className="space-y-4">
                    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3 text-sm text-muted-foreground">
                      <div className="flex items-center gap-2">
                        <MapPin className="h-4 w-4 text-muted-foreground/70" />
                        <span className="truncate">
                          {job.location || "Remote"}
                        </span>
                      </div>
                      <div className="flex items-center gap-2">
                        <Briefcase className="h-4 w-4 text-muted-foreground/70" />
                        <span className="truncate">
                          {job.employmentType || "Flexible"}
                        </span>
                      </div>
                      <div className="flex items-center gap-2">
                        <CalendarDays className="h-4 w-4 text-muted-foreground/70" />
                        <span className="truncate">
                          {job.experienceLevel || "Any level"}
                        </span>
                      </div>
                      {job.minExperience !== undefined &&
                        job.minExperience !== null && (
                          <div className="flex items-center gap-2 col-span-full sm:col-span-1">
                            <span className="font-medium text-foreground">
                              {job.minExperience === 0
                                ? "Fresher"
                                : `${job.minExperience}+ yrs experience`}
                            </span>
                          </div>
                        )}
                      {job.salaryRange && (
                        <div className="flex items-center gap-2 col-span-full sm:col-span-1">
                          <span className="font-medium text-green-600 dark:text-green-400">
                            {job.salaryRange}
                          </span>
                        </div>
                      )}
                    </div>

                    {job.skills?.length ? (
                      <div className="flex flex-wrap gap-2">
                        {job.skills.slice(0, 5).map((skill, index) => (
                          <Badge
                            key={`${job.id}-${
                              skill.skillId ?? skill.skillName ?? index
                            }`}
                            variant="outline"
                            className="text-xs"
                          >
                            {skill.skillName ?? "Skill"}
                          </Badge>
                        ))}
                        {job.skills.length > 5 && (
                          <Badge
                            variant="outline"
                            className="text-xs text-muted-foreground"
                          >
                            +{job.skills.length - 5} more
                          </Badge>
                        )}
                      </div>
                    ) : null}

                    <div className="flex flex-wrap gap-3 pt-2">
                      <Button asChild size="sm">
                        <Link to={`/candidate/jobs/${job.id}`}>
                          View details
                        </Link>
                      </Button>
                      <Button variant="outline" asChild size="sm">
                        <Link to={`/candidate/jobs/${job.id}`}>Apply now</Link>
                      </Button>
                    </div>
                  </CardContent>
                </Card>
              ))}
            </div>

            <div className="flex flex-wrap items-center justify-between gap-3 border-t pt-4 text-sm">
              <p>
                Page {jobQuery.data?.pageNumber ?? page} of{" "}
                {jobQuery.data?.totalPages ?? 1}
              </p>
              <div className="flex gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  disabled={!jobQuery.data?.hasPreviousPage}
                  onClick={() => setPage((current) => Math.max(1, current - 1))}
                >
                  Previous
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  disabled={!jobQuery.data?.hasNextPage}
                  onClick={() => setPage((current) => current + 1)}
                >
                  Next
                </Button>
              </div>
            </div>
          </div>
        )}
      </div>
    </ProfileRequiredWrapper>
  );
};
