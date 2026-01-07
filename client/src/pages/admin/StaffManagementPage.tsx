import { useState, useMemo, useRef, useEffect } from "react";
import { useStaffSearch } from "@/hooks/staff/staffProfile.hooks";
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
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { Checkbox } from "@/components/ui/checkbox";
import { LoadingSpinner } from "@/components/ui/LoadingSpinner";
import { getErrorMessage } from "@/utils/error";
import { formatDateToLocal } from "@/utils/dateUtils";
import { Search, UserPlus, Mail, MapPin, Briefcase, Phone } from "lucide-react";
import { useStaffRoles } from "@/hooks/staff";
import { authService } from "@/services/authService";
import type { components } from "@/types/api";

const ROLE_OPTIONS = [
  { value: "all", label: "All Roles" },
  { value: "Recruiter", label: "Recruiter" },
  { value: "HR", label: "HR" },
  { value: "Admin", label: "Admin" },
  { value: "SuperAdmin", label: "Super Admin" },
];

const AVAILABLE_ROLES = [
  { value: "Recruiter", label: "Recruiter" },
  { value: "HR", label: "HR" },
  { value: "Admin", label: "Admin" },
];

type Schemas = components["schemas"];

export const StaffManagementPage = () => {
  const [searchTerm, setSearchTerm] = useState("");
  const [roleFilter, setRoleFilter] = useState("all");
  const [departmentFilter, setDepartmentFilter] = useState("");
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(25);

  // Determine which roles current user can view/manage
  const { isSuperAdmin, isAdmin, isHR, isRecruiter } = useStaffRoles();

  const allowedRoleValues = useMemo(() => {
    if (isSuperAdmin) return ["SuperAdmin", "Admin", "HR", "Recruiter"];
    if (isAdmin) return ["Admin", "HR", "Recruiter"];
    if (isHR) return ["HR", "Recruiter"];
    if (isRecruiter) return ["Recruiter"];
    return [] as string[];
  }, [isSuperAdmin, isAdmin, isHR, isRecruiter]);

  const visibleRoleOptions = useMemo(() => {
    return ROLE_OPTIONS.filter(
      (o) => o.value === "all" || allowedRoleValues.includes(o.value)
    );
  }, [allowedRoleValues]);

  const assignableRoles = useMemo(() => {
    return AVAILABLE_ROLES.filter((r) => allowedRoleValues.includes(r.value));
  }, [allowedRoleValues]);

  // Add Staff Dialog State
  const [isAddDialogOpen, setIsAddDialogOpen] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [formData, setFormData] = useState<Schemas["RegisterStaffDto"]>({
    firstName: "",
    lastName: "",
    email: "",
    password: "",
    confirmPassword: "",
    phoneNumber: "",
    roles: [],
  });

  const searchParams = useMemo(() => {
    const params: {
      query?: string;
      roles?: string[];
      department?: string;
      pageNumber?: number;
      pageSize?: number;
    } = {
      pageNumber,
      pageSize,
    };

    if (searchTerm.trim()) {
      params.query = searchTerm.trim();
    }

    if (roleFilter !== "all") {
      params.roles = [roleFilter];
    }

    if (departmentFilter.trim()) {
      params.department = departmentFilter.trim();
    }

    return params;
  }, [searchTerm, roleFilter, departmentFilter, pageNumber, pageSize]);

  const staffQuery = useStaffSearch(searchParams);
  const staffList = staffQuery.data?.items ?? [];

  // Reset to page 1 when filters change
  useEffect(() => {
    setPageNumber(1);
  }, [searchTerm, roleFilter, departmentFilter]);

  // Capture static totals from first unfiltered query
  const totalCaptured = useRef(false);
  const [totalStaffCount, setTotalStaffCount] = useState(0);
  const [totalDepartmentsCount, setTotalDepartmentsCount] = useState(0);
  const [totalLocationsCount, setTotalLocationsCount] = useState(0);

  // Check if current query is unfiltered
  const isUnfilteredQuery = useMemo(() => {
    return (
      !searchTerm.trim() && roleFilter === "all" && !departmentFilter.trim()
    );
  }, [searchTerm, roleFilter, departmentFilter]);

  useEffect(() => {
    if (staffQuery.isSuccess && isUnfilteredQuery && !totalCaptured.current) {
      const fullList = staffQuery.data?.items ?? [];
      setTotalStaffCount(staffQuery.data?.totalCount ?? 0);

      const depts = new Set(
        fullList.map((staff) => staff.department).filter(Boolean)
      );
      setTotalDepartmentsCount(depts.size);

      const locs = new Set(
        fullList.map((staff) => staff.location).filter(Boolean)
      );
      setTotalLocationsCount(locs.size);

      totalCaptured.current = true;
    }
  }, [staffQuery.isSuccess]);

  const canResetFilters =
    searchTerm.trim() !== "" ||
    roleFilter !== "all" ||
    departmentFilter.trim() !== "";

  const handleResetFilters = () => {
    setSearchTerm("");
    setRoleFilter("all");
    setDepartmentFilter("");
    setPageNumber(1);
  };

  const handleRoleToggle = (role: string) => {
    setFormData((prev) => ({
      ...prev,
      roles: prev.roles.includes(role)
        ? prev.roles.filter((r) => r !== role)
        : [...prev.roles, role],
    }));
  };

  const handleSubmitStaff = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitError(null);

    if (formData.password !== formData.confirmPassword) {
      setSubmitError("Passwords do not match");
      return;
    }

    if (formData.roles.length === 0) {
      setSubmitError("Please select at least one role");
      return;
    }

    setIsSubmitting(true);

    try {
      const response = await authService.registerStaff(formData);

      if (response.success) {
        // Reset form and close dialog
        setFormData({
          firstName: "",
          lastName: "",
          email: "",
          password: "",
          confirmPassword: "",
          phoneNumber: "",
          roles: [],
        });
        setIsAddDialogOpen(false);
        // Refetch staff list
        staffQuery.refetch();
      } else {
        setSubmitError(
          response.errors?.join(", ") || "Failed to register staff"
        );
      }
    } catch (error) {
      setSubmitError(getErrorMessage(error));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="space-y-6">
      <div className="space-y-1">
        <h1 className="text-3xl font-semibold text-foreground">
          Staff Management
        </h1>
        <p className="text-muted-foreground">
          Manage recruiters, HR staff, and administrators
        </p>
      </div>

      {/* Stats */}
      <div className="grid gap-4 md:grid-cols-3">
        <Card>
          <CardHeader className="pb-2">
            <p className="text-sm text-muted-foreground">Total Staff</p>
            <CardTitle className="text-3xl">
              {staffQuery.isLoading && !totalCaptured.current ? (
                <LoadingSpinner />
              ) : (
                totalStaffCount
              )}
            </CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-xs text-muted-foreground">Active team members</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="pb-2">
            <p className="text-sm text-muted-foreground">Departments</p>
            <CardTitle className="text-3xl">
              {staffQuery.isLoading && !totalCaptured.current ? (
                <LoadingSpinner />
              ) : (
                totalDepartmentsCount
              )}
            </CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-xs text-muted-foreground">Various departments</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="pb-2">
            <p className="text-sm text-muted-foreground">Locations</p>
            <CardTitle className="text-3xl">
              {staffQuery.isLoading && !totalCaptured.current ? (
                <LoadingSpinner />
              ) : (
                totalLocationsCount
              )}
            </CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-xs text-muted-foreground">Office locations</p>
          </CardContent>
        </Card>
      </div>

      {/* Filters */}
      <Card>
        <CardHeader>
          <CardTitle className="text-base font-semibold">
            Search & Filter
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 md:grid-cols-4">
            <div className="space-y-2">
              <Label htmlFor="search">Search</Label>
              <div className="relative">
                <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                <Input
                  id="search"
                  placeholder="Name or email..."
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  className="pl-9"
                />
              </div>
            </div>

            <div className="space-y-2">
              <Label htmlFor="role-filter">Role</Label>
              <Select value={roleFilter} onValueChange={setRoleFilter}>
                <SelectTrigger id="role-filter">
                  <SelectValue placeholder="Select role" />
                </SelectTrigger>
                <SelectContent>
                  {visibleRoleOptions.map((option) => (
                    <SelectItem key={option.value} value={option.value}>
                      {option.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <Label htmlFor="department-filter">Department</Label>
              <Input
                id="department-filter"
                placeholder="e.g. Engineering..."
                value={departmentFilter}
                onChange={(e) => setDepartmentFilter(e.target.value)}
              />
            </div>

            <div className="flex items-end space-y-2">
              <Button
                variant="outline"
                onClick={handleResetFilters}
                disabled={!canResetFilters}
                className="w-full"
              >
                Reset Filters
              </Button>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Staff List */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle className="text-base font-semibold">
                Staff Members
              </CardTitle>
              <CardDescription>
                {staffQuery.isLoading
                  ? "Loading..."
                  : `${staffQuery.data?.totalCount ?? 0} staff member${
                      (staffQuery.data?.totalCount ?? 0) !== 1 ? "s" : ""
                    } found`}
              </CardDescription>
            </div>
            <div className="flex items-center gap-4">
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
              <Dialog open={isAddDialogOpen} onOpenChange={setIsAddDialogOpen}>
                <DialogTrigger asChild>
                  <Button>
                    <UserPlus className="mr-2 h-4 w-4" />
                    Add Staff
                  </Button>
                </DialogTrigger>
                <DialogContent className="max-w-2xl">
                  <form onSubmit={handleSubmitStaff}>
                    <DialogHeader>
                      <DialogTitle>Add New Staff Member</DialogTitle>
                      <DialogDescription>
                        Register a new staff member with appropriate roles
                      </DialogDescription>
                    </DialogHeader>

                    <div className="grid gap-4 py-4">
                      {submitError && (
                        <div className="rounded-md border border-destructive/40 bg-destructive/5 p-3 text-sm text-destructive">
                          {submitError}
                        </div>
                      )}

                      <div className="grid gap-4 md:grid-cols-2">
                        <div className="space-y-2">
                          <Label htmlFor="firstName">
                            First Name{" "}
                            <span className="text-destructive">*</span>
                          </Label>
                          <Input
                            id="firstName"
                            value={formData.firstName}
                            onChange={(e) =>
                              setFormData((prev) => ({
                                ...prev,
                                firstName: e.target.value,
                              }))
                            }
                            required
                          />
                        </div>

                        <div className="space-y-2">
                          <Label htmlFor="lastName">
                            Last Name{" "}
                            <span className="text-destructive">*</span>
                          </Label>
                          <Input
                            id="lastName"
                            value={formData.lastName}
                            onChange={(e) =>
                              setFormData((prev) => ({
                                ...prev,
                                lastName: e.target.value,
                              }))
                            }
                            required
                          />
                        </div>
                      </div>

                      <div className="space-y-2">
                        <Label htmlFor="email">
                          Email <span className="text-destructive">*</span>
                        </Label>
                        <Input
                          id="email"
                          type="email"
                          value={formData.email}
                          onChange={(e) =>
                            setFormData((prev) => ({
                              ...prev,
                              email: e.target.value,
                            }))
                          }
                          required
                        />
                      </div>

                      <div className="space-y-2">
                        <Label htmlFor="phoneNumber">Phone Number</Label>
                        <Input
                          id="phoneNumber"
                          type="tel"
                          value={formData.phoneNumber || ""}
                          onChange={(e) =>
                            setFormData((prev) => ({
                              ...prev,
                              phoneNumber: e.target.value,
                            }))
                          }
                        />
                      </div>

                      <div className="grid gap-4 md:grid-cols-2">
                        <div className="space-y-2">
                          <Label htmlFor="password">
                            Password <span className="text-destructive">*</span>
                          </Label>
                          <Input
                            id="password"
                            type="password"
                            value={formData.password}
                            onChange={(e) =>
                              setFormData((prev) => ({
                                ...prev,
                                password: e.target.value,
                              }))
                            }
                            required
                          />
                        </div>

                        <div className="space-y-2">
                          <Label htmlFor="confirmPassword">
                            Confirm Password{" "}
                            <span className="text-destructive">*</span>
                          </Label>
                          <Input
                            id="confirmPassword"
                            type="password"
                            value={formData.confirmPassword}
                            onChange={(e) =>
                              setFormData((prev) => ({
                                ...prev,
                                confirmPassword: e.target.value,
                              }))
                            }
                            required
                          />
                        </div>
                      </div>

                      <div className="space-y-2">
                        <Label>
                          Roles <span className="text-destructive">*</span>
                        </Label>
                        <div className="rounded-md border p-4 space-y-3">
                          {assignableRoles.map((role) => (
                            <div
                              key={role.value}
                              className="flex items-center space-x-2"
                            >
                              <Checkbox
                                id={`role-${role.value}`}
                                checked={formData.roles.includes(role.value)}
                                onCheckedChange={() =>
                                  handleRoleToggle(role.value)
                                }
                              />
                              <Label
                                htmlFor={`role-${role.value}`}
                                className="cursor-pointer font-normal"
                              >
                                {role.label}
                              </Label>
                            </div>
                          ))}
                        </div>
                        {formData.roles.length > 0 && (
                          <p className="text-xs text-muted-foreground">
                            Selected: {formData.roles.join(", ")}
                          </p>
                        )}
                      </div>
                    </div>

                    <DialogFooter>
                      <Button
                        type="button"
                        variant="outline"
                        onClick={() => setIsAddDialogOpen(false)}
                        disabled={isSubmitting}
                      >
                        Cancel
                      </Button>
                      <Button type="submit" disabled={isSubmitting}>
                        {isSubmitting ? (
                          <>
                            <LoadingSpinner />
                            Creating...
                          </>
                        ) : (
                          <>
                            <UserPlus className="mr-2 h-4 w-4" />
                            Create Staff
                          </>
                        )}
                      </Button>
                    </DialogFooter>
                  </form>
                </DialogContent>
              </Dialog>
            </div>
          </div>
        </CardHeader>
        <CardContent>
          {staffQuery.isLoading && (
            <div className="flex justify-center py-12">
              <LoadingSpinner />
            </div>
          )}

          {staffQuery.isError && (
            <p className="rounded-md border border-destructive/40 bg-destructive/5 p-4 text-sm text-destructive">
              {getErrorMessage(staffQuery.error)}
            </p>
          )}

          {!staffQuery.isLoading &&
            !staffQuery.isError &&
            staffList.length === 0 && (
              <div className="py-12 text-center">
                <p className="text-sm text-muted-foreground">
                  No staff members found matching your criteria.
                </p>
              </div>
            )}

          {!staffQuery.isLoading &&
            !staffQuery.isError &&
            staffList.length > 0 && (
              <div className="overflow-x-auto">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Name</TableHead>
                      <TableHead>Email</TableHead>
                      <TableHead>Phone</TableHead>
                      <TableHead>Joined Date</TableHead>
                      <TableHead>Department</TableHead>
                      <TableHead>Location</TableHead>
                      <TableHead>Status</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {staffList.map((staff) => (
                      <TableRow key={staff.id}>
                        <TableCell>
                          <div>
                            <p className="font-medium">
                              {staff.firstName} {staff.lastName}
                            </p>
                            <p className="text-xs text-muted-foreground">
                              {staff.employeeCode || "—"}
                            </p>
                          </div>
                        </TableCell>
                        <TableCell>
                          <div className="flex items-center gap-2 text-sm">
                            <Mail className="h-3 w-3 text-muted-foreground" />
                            {staff.email || "—"}
                          </div>
                        </TableCell>
                        <TableCell>
                          <div className="flex items-center gap-2 text-sm">
                            <Phone className="h-3 w-3 text-muted-foreground" />
                            {staff.phoneNumber || "—"}
                          </div>
                        </TableCell>
                        <TableCell>
                          <div className="flex flex-wrap gap-1">
                            <span className="text-xs text-muted-foreground">
                              {staff.joinedDate
                                ? formatDateToLocal(staff.joinedDate)
                                : "—"}
                            </span>
                          </div>
                        </TableCell>
                        <TableCell>
                          <div className="flex items-center gap-2 text-sm">
                            <Briefcase className="h-3 w-3 text-muted-foreground" />
                            {staff.department || "—"}
                          </div>
                        </TableCell>
                        <TableCell>
                          <div className="flex items-center gap-2 text-sm">
                            <MapPin className="h-3 w-3 text-muted-foreground" />
                            {staff.location || "—"}
                          </div>
                        </TableCell>
                        <TableCell>
                          <Badge variant="outline" className="text-xs">
                            {staff.status || "Active"}
                          </Badge>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>
            )}
        </CardContent>
        {staffQuery.data && staffQuery.data.totalCount! > 0 && (
          <CardContent className="pt-0">
            <div className="flex items-center justify-between border-t pt-4">
              <div className="text-sm text-muted-foreground">
                Showing {staffList.length} of {staffQuery.data.totalCount} staff
                members
                {staffQuery.data.totalPages! > 1 && (
                  <span className="ml-2">
                    (Page {staffQuery.data.pageNumber} of{" "}
                    {staffQuery.data.totalPages})
                  </span>
                )}
              </div>
              {staffQuery.data.totalPages! > 1 && (
                <div className="flex items-center gap-2">
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setPageNumber(pageNumber - 1)}
                    disabled={!staffQuery.data?.hasPreviousPage}
                  >
                    Previous
                  </Button>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setPageNumber(pageNumber + 1)}
                    disabled={!staffQuery.data?.hasNextPage}
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
