import { useState, useEffect } from "react";
import { getErrorMessage } from "@/utils/error";
import {
  useStaffJobSummaries,
  useCreateJobPosition,
  useUpdateJobPosition,
  useDeleteJobPosition,
  useJobPositionById,
} from "@/hooks/staff/jobPositions.hooks";
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
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
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
import type { components } from "@/types/api";
import { Plus, Edit, Trash2, X, Search, DoorOpen, Eye } from "lucide-react";
import {
  formatDateToLocal,
  convertLocalDateToUTC,
  convertUTCToLocalDateString,
} from "@/utils/dateUtils";

type Schemas = components["schemas"];

const JOB_STATUS_OPTIONS = [
  { value: "Active", label: "Active" },
  { value: "Closed", label: "Closed" },
];

export const JobPositionsPage = () => {
  const [searchTerm, setSearchTerm] = useState("");
  const [statusFilter, setStatusFilter] = useState("all");
  const [departmentFilter, setDepartmentFilter] = useState("");
  const [locationFilter, setLocationFilter] = useState("");
  const [experienceLevelFilter, setExperienceLevelFilter] = useState("all");
  const [createdFromDate, setCreatedFromDate] = useState("");
  const [createdToDate, setCreatedToDate] = useState("");
  const [deadlineFromDate, setDeadlineFromDate] = useState("");
  const [deadlineToDate, setDeadlineToDate] = useState("");
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [isCreateDialogOpen, setIsCreateDialogOpen] = useState(false);
  const [isEditDialogOpen, setIsEditDialogOpen] = useState(false);
  const [isReopenDialogOpen, setIsReopenDialogOpen] = useState(false);
  const [isCloseDialogOpen, setIsCloseDialogOpen] = useState(false);
  const [isDetailsDialogOpen, setIsDetailsDialogOpen] = useState(false);
  const [reopeningPosition, setReopeningPosition] = useState<
    Schemas["JobPositionStaffSummaryDto"] | null
  >(null);
  const [closingPosition, setClosingPosition] = useState<
    Schemas["JobPositionStaffSummaryDto"] | null
  >(null);
  const [reopenDeadline, setReopenDeadline] = useState("");
  const [closeDeadline, setCloseDeadline] = useState("");
  const [editingPosition, setEditingPosition] = useState<
    Schemas["JobPositionStaffSummaryDto"] | null
  >(null);
  const [selectedPositionId, setSelectedPositionId] = useState<string | null>(
    null
  );
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [createForm, setCreateForm] = useState({
    title: "",
    description: "",
    department: "",
    location: "",
    employmentType: "",
    experienceLevel: "",
    salaryMin: "",
    salaryMax: "",
    jobResponsibilities: "",
    requiredQualifications: "",
    applicationDeadline: "",
  });
  const [editForm, setEditForm] = useState({
    title: "",
    description: "",
    department: "",
    location: "",
    employmentType: "",
    experienceLevel: "",
    salaryMin: "",
    salaryMax: "",
    jobResponsibilities: "",
    requiredQualifications: "",
    applicationDeadline: "",
  });

  const jobPositionsQuery = useStaffJobSummaries({
    pageNumber,
    pageSize,
    query: {
      SearchTerm: searchTerm.trim() || undefined,
      Status: statusFilter !== "all" ? statusFilter : undefined,
      Department: departmentFilter.trim() || undefined,
      Location: locationFilter.trim() || undefined,
      ExperienceLevel:
        experienceLevelFilter !== "all" ? experienceLevelFilter : undefined,
      CreatedFromDate: createdFromDate
        ? convertLocalDateToUTC(createdFromDate)
        : undefined,
      CreatedToDate: createdToDate
        ? convertLocalDateToUTC(createdToDate)
        : undefined,
      DeadlineFromDate: deadlineFromDate
        ? convertLocalDateToUTC(deadlineFromDate)
        : undefined,
      DeadlineToDate: deadlineToDate
        ? convertLocalDateToUTC(deadlineToDate)
        : undefined,
    },
  });

  const createMutation = useCreateJobPosition();
  const updateMutation = useUpdateJobPosition();
  const deleteMutation = useDeleteJobPosition();
  const jobDetailsQuery = useJobPositionById(selectedPositionId || "");

  const { auth } = useAuth();
  const canCreateJobs =
    auth.roles.includes("HR") ||
    auth.roles.includes("Admin") ||
    auth.roles.includes("SuperAdmin");

  // Reset to page 1 when filters change
  useEffect(() => {
    setPageNumber(1);
  }, [
    searchTerm,
    statusFilter,
    departmentFilter,
    locationFilter,
    experienceLevelFilter,
    createdFromDate,
    createdToDate,
    deadlineFromDate,
    deadlineToDate,
  ]);

  const positions = jobPositionsQuery.data?.items ?? [];

  const canResetFilters =
    searchTerm.trim() ||
    statusFilter !== "all" ||
    departmentFilter.trim() ||
    locationFilter.trim() ||
    experienceLevelFilter !== "all" ||
    createdFromDate ||
    createdToDate ||
    deadlineFromDate ||
    deadlineToDate;

  const handleResetFilters = () => {
    setSearchTerm("");
    setStatusFilter("all");
    setDepartmentFilter("");
    setLocationFilter("");
    setExperienceLevelFilter("all");
    setCreatedFromDate("");
    setCreatedToDate("");
    setDeadlineFromDate("");
    setDeadlineToDate("");
    setPageNumber(1);
  };

  const handleCreatePosition = async () => {
    setSuccessMessage(null);
    setErrorMessage(null);
    try {
      const response = await createMutation.mutateAsync({
        title: createForm.title,
        description: createForm.description,
        department: createForm.department,
        location: createForm.location,
        employmentType: createForm.employmentType,
        experienceLevel: createForm.experienceLevel,
        salaryRange:
          createForm.salaryMin && createForm.salaryMax
            ? `${createForm.salaryMin}-${createForm.salaryMax}`
            : undefined,
        status: "Draft",
        applicationDeadline: createForm.applicationDeadline
          ? convertLocalDateToUTC(createForm.applicationDeadline)
          : undefined,
        jobResponsibilities: createForm.jobResponsibilities,
        requiredQualifications: createForm.requiredQualifications,
      });

      setSuccessMessage(
        response.message || "Job position created successfully"
      );

      setIsCreateDialogOpen(false);
      setCreateForm({
        title: "",
        description: "",
        department: "",
        location: "",
        employmentType: "",
        experienceLevel: "",
        salaryMin: "",
        salaryMax: "",
        jobResponsibilities: "",
        requiredQualifications: "",
        applicationDeadline: "",
      });
    } catch (error) {
      setErrorMessage(
        getErrorMessage(error) || "Failed to create job position"
      );
    }
  };

  const handleEditPosition = async () => {
    if (!editingPosition || !editingPosition.id) return;
    setSuccessMessage(null);
    setErrorMessage(null);

    try {
      const response = await updateMutation.mutateAsync({
        id: editingPosition.id,
        data: {
          title: editForm.title,
          description: editForm.description,
          department: editForm.department,
          location: editForm.location,
          employmentType: editForm.employmentType,
          experienceLevel: editForm.experienceLevel,
          salaryRange:
            editForm.salaryMin && editForm.salaryMax
              ? `${editForm.salaryMin}-${editForm.salaryMax}`
              : undefined,
          applicationDeadline: editForm.applicationDeadline
            ? convertLocalDateToUTC(editForm.applicationDeadline)
            : undefined,
          jobResponsibilities: editForm.jobResponsibilities,
          requiredQualifications: editForm.requiredQualifications,
        },
      });

      setSuccessMessage(
        response.message || "Job position updated successfully"
      );

      setIsEditDialogOpen(false);
      setEditingPosition(null);
    } catch (error) {
      setErrorMessage(
        getErrorMessage(error) || "Failed to update job position"
      );
    }
  };

  const handleDeletePosition = async (id: string) => {
    if (!confirm("Are you sure you want to delete this job position?")) return;
    setSuccessMessage(null);
    setErrorMessage(null);

    try {
      const response = await deleteMutation.mutateAsync(id);
      setSuccessMessage(
        response.message || "Job position deleted successfully"
      );
    } catch (error) {
      setErrorMessage(
        getErrorMessage(error) || "Failed to delete job position"
      );
    }
  };

  const openCloseDialog = (position: Schemas["JobPositionStaffSummaryDto"]) => {
    setClosingPosition(position);
    // Set default close deadline to today
    setCloseDeadline(new Date().toISOString().split("T")[0]);
    setIsCloseDialogOpen(true);
  };

  const handleCloseConfirm = async () => {
    if (!closingPosition?.id || !closeDeadline) return;

    try {
      await updateMutation.mutateAsync({
        id: closingPosition.id,
        data: {
          status: "Closed",
          applicationDeadline: convertLocalDateToUTC(closeDeadline),
        },
      });

      console.log("Job position closed successfully");
      setIsCloseDialogOpen(false);
      setClosingPosition(null);
      setCloseDeadline("");
    } catch (error) {
      console.error("Error closing job position:", getErrorMessage(error));
    }
  };

  const handleReopenPosition = async (id: string, newDeadline: string) => {
    if (!newDeadline) {
      alert("Please set a new application deadline to reopen this position.");
      return;
    }

    try {
      await updateMutation.mutateAsync({
        id,
        data: {
          status: "Active",
          applicationDeadline: convertLocalDateToUTC(newDeadline),
        },
      });

      console.log("Job position reopened successfully");
    } catch (error) {
      console.error("Error reopening job position:", getErrorMessage(error));
    }
  };

  const openEditDialog = (position: Schemas["JobPositionStaffSummaryDto"]) => {
    setEditingPosition(position);
    setEditForm({
      title: position.title || "",
      description: "", // Not available in summary DTO
      department: position.department || "",
      location: position.location || "",
      employmentType: position.employmentType || "",
      experienceLevel: position.experienceLevel || "",
      salaryMin: position.salaryRange?.split("-")[0] || "",
      salaryMax: position.salaryRange?.split("-")[1] || "",
      jobResponsibilities: "", // Not available in summary DTO
      requiredQualifications: "", // Not available in summary DTO
      applicationDeadline: position.applicationDeadline
        ? convertUTCToLocalDateString(position.applicationDeadline)
        : "",
    });
    setIsEditDialogOpen(true);
  };

  const openReopenDialog = (
    position: Schemas["JobPositionStaffSummaryDto"]
  ) => {
    setReopeningPosition(position);
    setReopenDeadline("");
    setIsReopenDialogOpen(true);
  };

  const openDetailsDialog = (id: string) => {
    setSelectedPositionId(id);
    setIsDetailsDialogOpen(true);
  };

  const handleReopenConfirm = async () => {
    if (!reopeningPosition?.id || !reopenDeadline) return;

    try {
      await handleReopenPosition(reopeningPosition.id, reopenDeadline);
      setIsReopenDialogOpen(false);
      setReopeningPosition(null);
      setReopenDeadline("");
    } catch (error) {
      // Error already handled in handleReopenPosition
    }
  };

  if (jobPositionsQuery.isLoading) {
    return (
      <div className="flex justify-center py-12">
        <LoadingSpinner />
      </div>
    );
  }

  if (jobPositionsQuery.isError) {
    return (
      <div className="space-y-6">
        <div>
          <h1 className="text-3xl font-semibold text-foreground">
            Job positions
          </h1>
          <p className="text-muted-foreground">
            Manage requisitions, hiring targets, and publishing pipelines
          </p>
        </div>
        <Card className="border-destructive/40">
          <CardContent className="pt-6">
            <p className="text-sm text-destructive">
              {getErrorMessage(jobPositionsQuery.error)}
            </p>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {successMessage && (
        <div className="bg-green-50 text-green-800 p-3 rounded-md text-sm">
          {successMessage}
        </div>
      )}
      {errorMessage && (
        <div className="bg-red-50 text-red-800 p-3 rounded-md text-sm">
          {errorMessage}
        </div>
      )}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-semibold text-foreground">
            Job positions
          </h1>
          <p className="text-muted-foreground">
            Manage requisitions, hiring targets, and publishing pipelines
          </p>
        </div>
        {canCreateJobs && (
          <Dialog
            open={isCreateDialogOpen}
            onOpenChange={setIsCreateDialogOpen}
          >
            <DialogTrigger asChild>
              <Button>
                <Plus className="mr-2 h-4 w-4" />
                Create Position
              </Button>
            </DialogTrigger>
            <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
              <DialogHeader>
                <DialogTitle>Create Job Position</DialogTitle>
                <DialogDescription>
                  Fill in the details to create a new job position.
                </DialogDescription>
              </DialogHeader>
              <div className="grid gap-4 py-4">
                <div className="grid grid-cols-2 gap-4">
                  <div className="space-y-2">
                    <Label htmlFor="create-title">Title *</Label>
                    <Input
                      id="create-title"
                      value={createForm.title}
                      onChange={(e) =>
                        setCreateForm({ ...createForm, title: e.target.value })
                      }
                      placeholder="e.g. Senior Software Engineer"
                    />
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="create-department">Department *</Label>
                    <Input
                      id="create-department"
                      value={createForm.department}
                      onChange={(e) =>
                        setCreateForm({
                          ...createForm,
                          department: e.target.value,
                        })
                      }
                      placeholder="e.g. Engineering"
                    />
                  </div>
                </div>
                <div className="grid grid-cols-2 gap-4">
                  <div className="space-y-2">
                    <Label htmlFor="create-location">Location</Label>
                    <Input
                      id="create-location"
                      value={createForm.location}
                      onChange={(e) =>
                        setCreateForm({
                          ...createForm,
                          location: e.target.value,
                        })
                      }
                      placeholder="e.g. New York, NY"
                    />
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="create-employment-type">
                      Employment Type
                    </Label>
                    <Select
                      value={createForm.employmentType}
                      onValueChange={(value) =>
                        setCreateForm({ ...createForm, employmentType: value })
                      }
                    >
                      <SelectTrigger>
                        <SelectValue placeholder="Select type" />
                      </SelectTrigger>
                      <SelectContent className="bg-emerald-50">
                        <SelectItem value="Full-time">Full-time</SelectItem>
                        <SelectItem value="Part-time">Part-time</SelectItem>
                        <SelectItem value="Contract">Contract</SelectItem>
                        <SelectItem value="Internship">Internship</SelectItem>
                      </SelectContent>
                    </Select>
                  </div>
                </div>
                <div className="grid grid-cols-2 gap-4">
                  <div className="space-y-2">
                    <Label htmlFor="create-experience-level">
                      Experience Level
                    </Label>
                    <Select
                      value={createForm.experienceLevel}
                      onValueChange={(value) =>
                        setCreateForm({ ...createForm, experienceLevel: value })
                      }
                    >
                      <SelectTrigger>
                        <SelectValue placeholder="Select level" />
                      </SelectTrigger>
                      <SelectContent className="bg-emerald-50">
                        <SelectItem value="Entry">Entry Level</SelectItem>
                        <SelectItem value="Mid">Mid Level</SelectItem>
                        <SelectItem value="Senior">Senior Level</SelectItem>
                        <SelectItem value="Lead">Lead/Principal</SelectItem>
                        <SelectItem value="Executive">Executive</SelectItem>
                      </SelectContent>
                    </Select>
                  </div>
                </div>
                <div className="grid grid-cols-2 gap-4">
                  <div className="space-y-2">
                    <Label htmlFor="create-salary-min">Salary Min</Label>
                    <Input
                      id="create-salary-min"
                      type="number"
                      value={createForm.salaryMin}
                      onChange={(e) =>
                        setCreateForm({
                          ...createForm,
                          salaryMin: e.target.value,
                        })
                      }
                      placeholder="e.g. 80000"
                    />
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="create-salary-max">Salary Max</Label>
                    <Input
                      id="create-salary-max"
                      type="number"
                      value={createForm.salaryMax}
                      onChange={(e) =>
                        setCreateForm({
                          ...createForm,
                          salaryMax: e.target.value,
                        })
                      }
                      placeholder="e.g. 120000"
                    />
                  </div>
                </div>
                <div className="space-y-2">
                  <Label htmlFor="create-application-deadline">
                    Application Deadline
                  </Label>
                  <Input
                    id="create-application-deadline"
                    type="date"
                    value={createForm.applicationDeadline}
                    onChange={(e) =>
                      setCreateForm({
                        ...createForm,
                        applicationDeadline: e.target.value,
                      })
                    }
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="create-description">Description</Label>
                  <textarea
                    id="create-description"
                    value={createForm.description}
                    onChange={(e) =>
                      setCreateForm({
                        ...createForm,
                        description: e.target.value,
                      })
                    }
                    placeholder="Job description..."
                    rows={4}
                    className="flex min-h-[80px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="create-job-responsibilities">
                    Job Responsibilities
                  </Label>
                  <textarea
                    id="create-job-responsibilities"
                    value={createForm.jobResponsibilities}
                    onChange={(e) =>
                      setCreateForm({
                        ...createForm,
                        jobResponsibilities: e.target.value,
                      })
                    }
                    placeholder="Job responsibilities..."
                    rows={4}
                    className="flex min-h-[80px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="create-required-qualifications">
                    Required Qualifications
                  </Label>
                  <textarea
                    id="create-required-qualifications"
                    value={createForm.requiredQualifications}
                    onChange={(e: React.ChangeEvent<HTMLTextAreaElement>) =>
                      setCreateForm({
                        ...createForm,
                        requiredQualifications: e.target.value,
                      })
                    }
                    placeholder="Required qualifications..."
                    rows={4}
                    className="flex min-h-[80px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
                  />
                </div>
              </div>
              <DialogFooter>
                <Button
                  variant="outline"
                  onClick={() => setIsCreateDialogOpen(false)}
                >
                  Cancel
                </Button>
                <Button
                  onClick={handleCreatePosition}
                  disabled={
                    createMutation.isPending ||
                    !createForm.title ||
                    !createForm.department
                  }
                >
                  {createMutation.isPending ? "Creating..." : "Create Position"}
                </Button>
              </DialogFooter>
            </DialogContent>
          </Dialog>
        )}
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Filters</CardTitle>
          <CardDescription>
            Filter job positions by various criteria
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid gap-4 md:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor="search-input">Search</Label>
              <div className="relative">
                <Input
                  id="search-input"
                  placeholder="Search by title or department"
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  className="pl-9"
                />
                <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              </div>
            </div>

            <div className="space-y-2">
              <Label htmlFor="status-filter">Status</Label>
              <Select value={statusFilter} onValueChange={setStatusFilter}>
                <SelectTrigger id="status-filter">
                  <SelectValue placeholder="All statuses" />
                </SelectTrigger>
                <SelectContent className="bg-emerald-50">
                  <SelectItem value="all">All statuses</SelectItem>
                  {JOB_STATUS_OPTIONS.map((option) => (
                    <SelectItem key={option.value} value={option.value}>
                      {option.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>

          <div className="grid gap-3 md:grid-cols-3">
            <div className="space-y-2">
              <Label htmlFor="department-filter">Department</Label>
              <Input
                id="department-filter"
                placeholder="Filter by department"
                value={departmentFilter}
                onChange={(e) => setDepartmentFilter(e.target.value)}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="location-filter">Location</Label>
              <Input
                id="location-filter"
                placeholder="Filter by location"
                value={locationFilter}
                onChange={(e) => setLocationFilter(e.target.value)}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="experience-level-filter">Experience Level</Label>
              <Select
                value={experienceLevelFilter}
                onValueChange={setExperienceLevelFilter}
              >
                <SelectTrigger id="experience-level-filter">
                  <SelectValue placeholder="All levels" />
                </SelectTrigger>
                <SelectContent className="bg-emerald-50">
                  <SelectItem value="all">All levels</SelectItem>
                  <SelectItem value="Entry">Entry Level</SelectItem>
                  <SelectItem value="Mid">Mid Level</SelectItem>
                  <SelectItem value="Senior">Senior Level</SelectItem>
                  <SelectItem value="Lead">Lead/Principal</SelectItem>
                  <SelectItem value="Executive">Executive</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>

          <div className="grid gap-3 md:grid-cols-4">
            <div className="space-y-2">
              <Label htmlFor="created-from">Created from</Label>
              <Input
                id="created-from"
                type="date"
                value={createdFromDate}
                onChange={(e) => setCreatedFromDate(e.target.value)}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="created-to">Created to</Label>
              <Input
                id="created-to"
                type="date"
                value={createdToDate}
                onChange={(e) => setCreatedToDate(e.target.value)}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="deadline-from">Deadline from</Label>
              <Input
                id="deadline-from"
                type="date"
                value={deadlineFromDate}
                onChange={(e) => setDeadlineFromDate(e.target.value)}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="deadline-to">Deadline to</Label>
              <Input
                id="deadline-to"
                type="date"
                value={deadlineToDate}
                onChange={(e) => setDeadlineToDate(e.target.value)}
              />
            </div>
          </div>

          <Button
            type="button"
            variant="outline"
            onClick={handleResetFilters}
            disabled={!canResetFilters}
          >
            Reset filters
          </Button>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle>All Positions</CardTitle>
              <CardDescription>
                {jobPositionsQuery.isLoading
                  ? "Loading..."
                  : `${jobPositionsQuery.data?.totalCount ?? 0} position${
                      (jobPositionsQuery.data?.totalCount ?? 0) !== 1 ? "s" : ""
                    } found`}
              </CardDescription>
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
                <SelectContent className="bg-emerald-50">
                  <SelectItem value="10">10</SelectItem>
                  <SelectItem value="25">25</SelectItem>
                  <SelectItem value="50">50</SelectItem>
                  <SelectItem value="100">100</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
        </CardHeader>
        <CardContent>
          <div className="rounded-md border">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Title</TableHead>
                  <TableHead>Department</TableHead>
                  <TableHead>Location</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Applications</TableHead>
                  <TableHead>Deadline</TableHead>
                  <TableHead>Created</TableHead>
                  <TableHead className="w-32">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {positions.length === 0 ? (
                  <TableRow>
                    <TableCell
                      colSpan={8}
                      className="text-center py-8 text-muted-foreground"
                    >
                      No job positions found
                    </TableCell>
                  </TableRow>
                ) : (
                  positions.map((position) => (
                    <TableRow key={position.id}>
                      <TableCell className="font-medium">
                        {position.title}
                      </TableCell>
                      <TableCell>{position.department}</TableCell>
                      <TableCell>{position.location || "—"}</TableCell>
                      <TableCell>
                        <Badge
                          variant={
                            position.status === "Active"
                              ? "default"
                              : position.status === "Closed"
                              ? "secondary"
                              : "outline"
                          }
                        >
                          {position.status}
                        </Badge>
                      </TableCell>
                      <TableCell>{position.totalApplicants || 0}</TableCell>
                      <TableCell>
                        {formatDateToLocal(position.applicationDeadline)}
                      </TableCell>
                      <TableCell>
                        {formatDateToLocal(position.createdAt)}
                      </TableCell>
                      <TableCell>
                        <div className="flex items-center space-x-2">
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() =>
                              position.id && openDetailsDialog(position.id)
                            }
                          >
                            <Eye className="h-4 w-4" />
                          </Button>
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => openEditDialog(position)}
                          >
                            <Edit className="h-4 w-4" />
                          </Button>
                          {position.status === "Active" && (
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => openCloseDialog(position)}
                              disabled={
                                updateMutation.isPending || !position.id
                              }
                            >
                              <X className="h-4 w-4" />
                            </Button>
                          )}
                          {position.status === "Closed" && (
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => openReopenDialog(position)}
                              disabled={updateMutation.isPending}
                            >
                              <DoorOpen className="h-4 w-4" />
                            </Button>
                          )}
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() =>
                              position.id && handleDeletePosition(position.id)
                            }
                            disabled={deleteMutation.isPending || !position.id}
                          >
                            <Trash2 className="h-4 w-4" />
                          </Button>
                        </div>
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </div>
        </CardContent>
        {jobPositionsQuery.data && jobPositionsQuery.data.totalCount! > 0 && (
          <CardContent className="pt-0">
            <div className="flex items-center justify-between border-t pt-4">
              <div className="text-sm text-muted-foreground">
                Showing {positions.length} of{" "}
                {jobPositionsQuery.data.totalCount} positions
                {jobPositionsQuery.data.totalPages! > 1 && (
                  <span className="ml-2">
                    (Page {jobPositionsQuery.data.pageNumber} of{" "}
                    {jobPositionsQuery.data.totalPages})
                  </span>
                )}
              </div>
              {jobPositionsQuery.data.totalPages! > 1 && (
                <div className="flex items-center gap-2">
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setPageNumber(pageNumber - 1)}
                    disabled={!jobPositionsQuery.data?.hasPreviousPage}
                  >
                    Previous
                  </Button>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setPageNumber(pageNumber + 1)}
                    disabled={!jobPositionsQuery.data?.hasNextPage}
                  >
                    Next
                  </Button>
                </div>
              )}
            </div>
          </CardContent>
        )}
      </Card>

      {/* Edit Dialog */}
      <Dialog open={isEditDialogOpen} onOpenChange={setIsEditDialogOpen}>
        <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>Edit Job Position</DialogTitle>
            <DialogDescription>
              Update the job position details.
            </DialogDescription>
          </DialogHeader>
          <div className="grid gap-4 py-4">
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="edit-title">Title *</Label>
                <Input
                  id="edit-title"
                  value={editForm.title}
                  onChange={(e) =>
                    setEditForm({ ...editForm, title: e.target.value })
                  }
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="edit-department">Department *</Label>
                <Input
                  id="edit-department"
                  value={editForm.department}
                  onChange={(e) =>
                    setEditForm({ ...editForm, department: e.target.value })
                  }
                />
              </div>
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="edit-location">Location</Label>
                <Input
                  id="edit-location"
                  value={editForm.location}
                  onChange={(e) =>
                    setEditForm({ ...editForm, location: e.target.value })
                  }
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="edit-employment-type">Employment Type</Label>
                <Select
                  value={editForm.employmentType}
                  onValueChange={(value) =>
                    setEditForm({ ...editForm, employmentType: value })
                  }
                >
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent className="bg-emerald-50">
                    <SelectItem value="Full-time">Full-time</SelectItem>
                    <SelectItem value="Part-time">Part-time</SelectItem>
                    <SelectItem value="Contract">Contract</SelectItem>
                    <SelectItem value="Internship">Internship</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div className="space-y-2">
                <Label htmlFor="edit-experience-level">Experience Level</Label>
                <Select
                  value={editForm.experienceLevel}
                  onValueChange={(value) =>
                    setEditForm({ ...editForm, experienceLevel: value })
                  }
                >
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent className="bg-emerald-50">
                    <SelectItem value="Entry">Entry Level</SelectItem>
                    <SelectItem value="Mid">Mid Level</SelectItem>
                    <SelectItem value="Senior">Senior Level</SelectItem>
                    <SelectItem value="Lead">Lead/Principal</SelectItem>
                    <SelectItem value="Executive">Executive</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="edit-salary-min">Salary Min</Label>
                <Input
                  id="edit-salary-min"
                  type="number"
                  value={editForm.salaryMin}
                  onChange={(e) =>
                    setEditForm({ ...editForm, salaryMin: e.target.value })
                  }
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="edit-salary-max">Salary Max</Label>
                <Input
                  id="edit-salary-max"
                  type="number"
                  value={editForm.salaryMax}
                  onChange={(e) =>
                    setEditForm({ ...editForm, salaryMax: e.target.value })
                  }
                />
              </div>
            </div>
            <div className="space-y-2">
              <Label htmlFor="edit-application-deadline">
                Application Deadline
              </Label>
              <Input
                id="edit-application-deadline"
                type="date"
                value={editForm.applicationDeadline}
                onChange={(e) =>
                  setEditForm({
                    ...editForm,
                    applicationDeadline: e.target.value,
                  })
                }
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="edit-description">Description</Label>
              <textarea
                id="edit-description"
                value={editForm.description}
                onChange={(e: React.ChangeEvent<HTMLTextAreaElement>) =>
                  setEditForm({ ...editForm, description: e.target.value })
                }
                placeholder="Job description..."
                rows={4}
                className="flex min-h-[80px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="edit-job-responsibilities">
                Job Responsibilities
              </Label>
              <textarea
                id="edit-job-responsibilities"
                value={editForm.jobResponsibilities}
                onChange={(e: React.ChangeEvent<HTMLTextAreaElement>) =>
                  setEditForm({
                    ...editForm,
                    jobResponsibilities: e.target.value,
                  })
                }
                placeholder="Job responsibilities..."
                rows={4}
                className="flex min-h-[80px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="edit-required-qualifications">
                Required Qualifications
              </Label>
              <textarea
                id="edit-required-qualifications"
                value={editForm.requiredQualifications}
                onChange={(e: React.ChangeEvent<HTMLTextAreaElement>) =>
                  setEditForm({
                    ...editForm,
                    requiredQualifications: e.target.value,
                  })
                }
                placeholder="Required qualifications..."
                rows={4}
                className="flex min-h-[80px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
              />
            </div>
          </div>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setIsEditDialogOpen(false)}
            >
              Cancel
            </Button>
            <Button
              onClick={handleEditPosition}
              disabled={
                updateMutation.isPending ||
                !editForm.title ||
                !editForm.department
              }
            >
              {updateMutation.isPending ? "Updating..." : "Update Position"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={isReopenDialogOpen} onOpenChange={setIsReopenDialogOpen}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>Reopen Job Position</DialogTitle>
            <DialogDescription>
              Set a new application deadline to reopen "
              {reopeningPosition?.title}".
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="reopen-deadline">
                New Application Deadline *
              </Label>
              <Input
                id="reopen-deadline"
                type="date"
                value={reopenDeadline}
                onChange={(e) => setReopenDeadline(e.target.value)}
                min={new Date().toISOString().split("T")[0]}
              />
            </div>
          </div>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setIsReopenDialogOpen(false)}
            >
              Cancel
            </Button>
            <Button
              onClick={handleReopenConfirm}
              disabled={updateMutation.isPending || !reopenDeadline}
            >
              {updateMutation.isPending ? "Reopening..." : "Reopen Position"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={isCloseDialogOpen} onOpenChange={setIsCloseDialogOpen}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>Close Job Position</DialogTitle>
            <DialogDescription>
              Set the closing deadline for "{closingPosition?.title}". This will
              mark the position as closed.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="close-deadline">Closing Deadline *</Label>
              <Input
                id="close-deadline"
                type="date"
                value={closeDeadline}
                onChange={(e) => setCloseDeadline(e.target.value)}
                max={new Date().toISOString().split("T")[0]}
              />
              <p className="text-sm text-muted-foreground">
                Set to today or earlier to indicate the position is no longer
                accepting applications.
              </p>
            </div>
          </div>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setIsCloseDialogOpen(false)}
            >
              Cancel
            </Button>
            <Button
              onClick={handleCloseConfirm}
              disabled={updateMutation.isPending || !closeDeadline}
            >
              {updateMutation.isPending ? "Closing..." : "Close Position"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog
        open={isDetailsDialogOpen}
        onOpenChange={(open) => {
          setIsDetailsDialogOpen(open);
          if (!open) setSelectedPositionId(null);
        }}
      >
        <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>Job Position Details</DialogTitle>
            <DialogDescription>
              View complete details of the job position.
            </DialogDescription>
          </DialogHeader>
          {jobDetailsQuery.isLoading ? (
            <div className="flex justify-center py-8">
              <LoadingSpinner />
            </div>
          ) : jobDetailsQuery.isError ? (
            <div className="text-center py-8 text-destructive">
              Failed to load job details
            </div>
          ) : jobDetailsQuery.data ? (
            <div className="grid gap-4 py-4">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <Label className="text-sm font-medium">Title</Label>
                  <p className="text-sm">{jobDetailsQuery.data.title}</p>
                </div>
                <div>
                  <Label className="text-sm font-medium">Department</Label>
                  <p className="text-sm">{jobDetailsQuery.data.department}</p>
                </div>
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <Label className="text-sm font-medium">Location</Label>
                  <p className="text-sm">
                    {jobDetailsQuery.data.location || "—"}
                  </p>
                </div>
                <div>
                  <Label className="text-sm font-medium">Employment Type</Label>
                  <p className="text-sm">
                    {jobDetailsQuery.data.employmentType || "—"}
                  </p>
                </div>
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <Label className="text-sm font-medium">
                    Experience Level
                  </Label>
                  <p className="text-sm">
                    {jobDetailsQuery.data.experienceLevel || "—"}
                  </p>
                </div>
                <div>
                  <Label className="text-sm font-medium">Salary Range</Label>
                  <p className="text-sm">
                    {jobDetailsQuery.data.salaryRange || "—"}
                  </p>
                </div>
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <Label className="text-sm font-medium">Status: </Label>
                  <Badge
                    variant={
                      jobDetailsQuery.data.status === "Active"
                        ? "default"
                        : jobDetailsQuery.data.status === "Closed"
                        ? "secondary"
                        : "outline"
                    }
                  >
                    {jobDetailsQuery.data.status}
                  </Badge>
                </div>
                <div>
                  <Label className="text-sm font-medium">
                    Application Deadline
                  </Label>
                  <p className="text-sm">
                    {formatDateToLocal(
                      jobDetailsQuery.data.applicationDeadline
                    )}
                  </p>
                </div>
              </div>
              <div>
                <Label className="text-sm font-medium">Description</Label>
                <p className="text-sm mt-1 whitespace-pre-wrap">
                  {jobDetailsQuery.data.description ||
                    "No description provided"}
                </p>
              </div>
              <div>
                <Label className="text-sm font-medium">
                  Required Qualifications
                </Label>
                <p className="text-sm mt-1 whitespace-pre-wrap">
                  {jobDetailsQuery.data.requiredQualifications ||
                    "No qualifications specified"}
                </p>
              </div>
              <div>
                <Label className="text-sm font-medium">
                  Job Responsibilities
                </Label>
                <p className="text-sm mt-1 whitespace-pre-wrap">
                  {jobDetailsQuery.data.jobResponsibilities ||
                    "No responsibilities specified"}
                </p>
              </div>
            </div>
          ) : null}
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setIsDetailsDialogOpen(false)}
            >
              Close
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
};
