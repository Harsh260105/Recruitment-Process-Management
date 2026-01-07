import { useState, useMemo } from "react";
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
} from "@/components/ui/dialog";
import { Checkbox } from "@/components/ui/checkbox";
import { LoadingSpinner } from "@/components/ui/LoadingSpinner";
import { Alert, AlertDescription } from "@/components/ui/alert";
import {
  useUserSearch,
  useUserDetails,
  useUpdateUserInfo,
  useUpdateUserStatus,
  useEndUserLockout,
  useAdminResetPassword,
  useManageUserRoles,
} from "@/hooks/staff/userManagement.hooks";
import { useDebounce } from "@/hooks/useDebounce";
import { getErrorMessage } from "@/utils/error";
import { formatDateToLocal, formatDateTimeToLocal } from "@/utils/dateUtils";
import {
  Search,
  Filter,
  UserCog,
  Key,
  Unlock,
  RefreshCw,
  Edit,
  CheckCircle2,
  XCircle,
} from "lucide-react";
import type { components } from "@/types/api";

type Schemas = components["schemas"];

const ROLE_OPTIONS = [
  { value: "all", label: "All Roles" },
  { value: "Candidate", label: "Candidate" },
  { value: "Recruiter", label: "Recruiter" },
  { value: "HR", label: "HR" },
  { value: "Admin", label: "Admin" },
  { value: "SuperAdmin", label: "Super Admin" },
];

const STATUS_OPTIONS = [
  { value: "all", label: "All Status" },
  { value: "active", label: "Active" },
  { value: "inactive", label: "Inactive" },
];

const PROFILE_OPTIONS = [
  { value: "all", label: "All Users" },
  { value: "with-profile", label: "With Profile" },
  { value: "without-profile", label: "Without Profile" },
];

const AVAILABLE_ROLES = [
  { value: "Recruiter", label: "Recruiter" },
  { value: "HR", label: "HR" },
  { value: "Admin", label: "Admin" },
];

export const UserManagementPage = () => {
  // Search and filter state
  const [searchTerm, setSearchTerm] = useState("");
  const [roleFilter, setRoleFilter] = useState("all");
  const [statusFilter, setStatusFilter] = useState("all");
  const [profileFilter, setProfileFilter] = useState("all");
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(25);

  // Selection state
  const [selectedUsers, setSelectedUsers] = useState<Set<string>>(new Set());

  // Dialog states
  const [isEditDialogOpen, setIsEditDialogOpen] = useState(false);
  const [isRoleDialogOpen, setIsRoleDialogOpen] = useState(false);
  const [isDetailsDialogOpen, setIsDetailsDialogOpen] = useState(false);
  const [selectedUserId, setSelectedUserId] = useState<string | null>(null);

  // Edit form state
  const [editForm, setEditForm] = useState<Schemas["UpdateUserInfoRequest"]>({
    firstName: "",
    lastName: "",
    phoneNumber: "",
  });

  // Role management state
  const [rolesToAdd, setRolesToAdd] = useState<string[]>([]);
  const [rolesToRemove, setRolesToRemove] = useState<string[]>([]);

  // Message state
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  // Build search params
  const searchParams = useMemo(() => {
    const params: {
      Search?: string;
      Roles?: string[];
      IsActive?: boolean;
      HasProfile?: boolean;
      PageNumber: number;
      PageSize: number;
    } = {
      PageNumber: pageNumber,
      PageSize: pageSize,
    };

    if (searchTerm.trim()) {
      params.Search = searchTerm.trim();
    }

    if (roleFilter !== "all") {
      params.Roles = [roleFilter];
    }

    if (statusFilter !== "all") {
      params.IsActive = statusFilter === "active";
    }

    if (profileFilter === "with-profile") {
      params.HasProfile = true;
    } else if (profileFilter === "without-profile") {
      params.HasProfile = false;
    }

    return params;
  }, [
    searchTerm,
    roleFilter,
    statusFilter,
    profileFilter,
    pageNumber,
    pageSize,
  ]);

  const debouncedSearchParams = useDebounce(searchParams, 400);

  // Queries
  const {
    data: usersData,
    isLoading,
    error,
  } = useUserSearch(debouncedSearchParams);
  const { data: userDetails, error: userDetailsError } = useUserDetails(
    selectedUserId || "",
    {
      enabled: !!selectedUserId && isDetailsDialogOpen,
    }
  );

  // Mutations
  const updateUserInfo = useUpdateUserInfo();
  const updateUserStatus = useUpdateUserStatus();
  const endUserLockout = useEndUserLockout();
  const adminResetPassword = useAdminResetPassword();
  const manageUserRoles = useManageUserRoles();

  const users = usersData?.items ?? [];
  const totalCount = usersData?.totalCount ?? 0;

  // Selection handlers
  const handleSelectAll = (checked: boolean) => {
    if (checked) {
      setSelectedUsers(new Set(users.map((u) => u.userId!)));
    } else {
      setSelectedUsers(new Set());
    }
  };

  const handleSelectUser = (userId: string, checked: boolean) => {
    const newSelection = new Set(selectedUsers);
    if (checked) {
      newSelection.add(userId);
    } else {
      newSelection.delete(userId);
    }
    setSelectedUsers(newSelection);
  };

  // Open dialogs
  const handleEditUser = (user: Schemas["UserSummaryDto"]) => {
    setSelectedUserId(user.userId!);
    setEditForm({
      firstName: user.firstName || "",
      lastName: user.lastName || "",
      phoneNumber: user.phoneNumber || "",
    });
    setIsEditDialogOpen(true);
  };

  const handleViewDetails = (userId: string) => {
    setSelectedUserId(userId);
    setIsDetailsDialogOpen(true);
  };

  const handleManageRoles = (user: Schemas["UserSummaryDto"]) => {
    setSelectedUserId(user.userId!);
    setRolesToAdd([]);
    setRolesToRemove([]);
    setIsRoleDialogOpen(true);
  };

  // Mutation handlers
  const handleSaveUserInfo = async () => {
    if (!selectedUserId) return;

    setSuccessMessage(null);
    setErrorMessage(null);

    try {
      const response = await updateUserInfo.mutateAsync({
        userId: selectedUserId,
        data: editForm,
      });
      setSuccessMessage(
        response.message || "User information updated successfully"
      );
      setIsEditDialogOpen(false);
      setSelectedUserId(null);
    } catch (error) {
      setErrorMessage(getErrorMessage(error) || "Failed to update user");
    }
  };

  const handleActivateUsers = async () => {
    if (selectedUsers.size === 0) return;

    setSuccessMessage(null);
    setErrorMessage(null);

    try {
      const response = await updateUserStatus.mutateAsync({
        userIds: Array.from(selectedUsers),
        isActive: true,
      });

      let message = response.message || "Users activated successfully";

      // Add detailed error information if available
      if (response.data?.errors && response.data.errors.length > 0) {
        message += `\n\nErrors:\n${response.data.errors.join("\n")}`;
      }

      setSuccessMessage(message);
      setSelectedUsers(new Set());
    } catch (error) {
      setErrorMessage(getErrorMessage(error) || "Failed to activate users");
    }
  };

  const handleDeactivateUsers = async () => {
    if (selectedUsers.size === 0) return;

    setSuccessMessage(null);
    setErrorMessage(null);

    try {
      const response = await updateUserStatus.mutateAsync({
        userIds: Array.from(selectedUsers),
        isActive: false,
      });

      let message = response.message || "Users deactivated successfully";

      // Add detailed error information if available
      if (response.data?.errors && response.data.errors.length > 0) {
        message += `\n\nErrors:\n${response.data.errors.join("\n")}`;
      }

      setSuccessMessage(message);
      setSelectedUsers(new Set());
    } catch (error) {
      setErrorMessage(getErrorMessage(error) || "Failed to deactivate users");
    }
  };

  const handleUnlockUsers = async () => {
    if (selectedUsers.size === 0) return;

    setSuccessMessage(null);
    setErrorMessage(null);

    try {
      const response = await endUserLockout.mutateAsync({
        userIds: Array.from(selectedUsers),
      });

      let message = response.message || "Users unlocked successfully";

      // Add detailed error information if available
      if (response.data?.errors && response.data.errors.length > 0) {
        message += `\n\nErrors:\n${response.data.errors.join("\n")}`;
      }

      setSuccessMessage(message);
      setSelectedUsers(new Set());
    } catch (error) {
      setErrorMessage(getErrorMessage(error) || "Failed to unlock users");
    }
  };

  const handleResetPasswords = async () => {
    if (selectedUsers.size === 0) return;

    setSuccessMessage(null);
    setErrorMessage(null);

    try {
      const response = await adminResetPassword.mutateAsync({
        userIds: Array.from(selectedUsers),
      });

      let message = response.message || "Passwords reset successfully";

      // Add detailed error information if available
      if (response.data?.errors && response.data.errors.length > 0) {
        message += `\n\nErrors:\n${response.data.errors.join("\n")}`;
      }

      setSuccessMessage(message);
      setSelectedUsers(new Set());
    } catch (error) {
      setErrorMessage(getErrorMessage(error) || "Failed to reset passwords");
    }
  };

  const handleSaveRoles = async () => {
    if (!selectedUserId) return;

    setSuccessMessage(null);
    setErrorMessage(null);

    try {
      const response = await manageUserRoles.mutateAsync({
        userIds: [selectedUserId],
        rolesToAdd,
        rolesToRemove,
      });

      let message = response.message || "User roles updated successfully";

      // Add detailed error information if available
      if (response.data?.errors && response.data.errors.length > 0) {
        message += `\n\nErrors:\n${response.data.errors.join("\n")}`;
      }

      setSuccessMessage(message);
      setIsRoleDialogOpen(false);
      setSelectedUserId(null);
      setRolesToAdd([]);
      setRolesToRemove([]);
    } catch (error) {
      setErrorMessage(getErrorMessage(error) || "Failed to manage roles");
    }
  };

  const getRoleBadgeVariant = (role: string) => {
    switch (role) {
      case "SuperAdmin":
        return "destructive";
      case "Admin":
        return "default";
      case "HR":
        return "secondary";
      case "Recruiter":
        return "outline";
      default:
        return "outline";
    }
  };

  if (error) {
    return (
      <div className="p-8">
        <Alert variant="destructive">
          <AlertDescription>
            Error loading users: {getErrorMessage(error)}
          </AlertDescription>
        </Alert>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-3xl font-bold">User Management</h1>
        <p className="text-muted-foreground mt-2">
          Manage all users, roles, and permissions across the system
        </p>
      </div>

      {/* Success Message */}
      {successMessage && (
        <Alert className="bg-green-50 border-green-200">
          <CheckCircle2 className="h-4 w-4 text-green-600" />
          <AlertDescription className="text-green-800">
            {successMessage}
          </AlertDescription>
        </Alert>
      )}

      {/* Error Message */}
      {errorMessage && (
        <Alert variant="destructive">
          <XCircle className="h-4 w-4" />
          <AlertDescription>{errorMessage}</AlertDescription>
        </Alert>
      )}

      {/* Search and Filters */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Filter className="h-5 w-5" />
            Search and Filter
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          {/* Search input */}
          <div className="flex gap-4">
            <div className="flex-1">
              <Label htmlFor="search">Search Users</Label>
              <div className="relative">
                <Search className="absolute left-3 top-3 h-4 w-4 text-muted-foreground" />
                <Input
                  id="search"
                  placeholder="Search by name or email..."
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  className="pl-9"
                />
              </div>
            </div>
          </div>

          {/* Filters */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div>
              <Label htmlFor="role-filter">Role</Label>
              <Select value={roleFilter} onValueChange={setRoleFilter}>
                <SelectTrigger id="role-filter">
                  <SelectValue placeholder="All Roles" />
                </SelectTrigger>
                <SelectContent>
                  {ROLE_OPTIONS.map((option) => (
                    <SelectItem key={option.value} value={option.value}>
                      {option.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div>
              <Label htmlFor="status-filter">Status</Label>
              <Select value={statusFilter} onValueChange={setStatusFilter}>
                <SelectTrigger id="status-filter">
                  <SelectValue placeholder="All Status" />
                </SelectTrigger>
                <SelectContent>
                  {STATUS_OPTIONS.map((option) => (
                    <SelectItem key={option.value} value={option.value}>
                      {option.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div>
              <Label htmlFor="profile-filter">Profile</Label>
              <Select value={profileFilter} onValueChange={setProfileFilter}>
                <SelectTrigger id="profile-filter">
                  <SelectValue placeholder="All Users" />
                </SelectTrigger>
                <SelectContent>
                  {PROFILE_OPTIONS.map((option) => (
                    <SelectItem key={option.value} value={option.value}>
                      {option.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Bulk Actions */}
      {selectedUsers.size > 0 && (
        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center justify-between">
              <p className="text-sm text-muted-foreground">
                {selectedUsers.size} user(s) selected
              </p>
              <div className="flex gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={handleActivateUsers}
                  disabled={updateUserStatus.isPending}
                >
                  <CheckCircle2 className="h-4 w-4 mr-2" />
                  Activate
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={handleDeactivateUsers}
                  disabled={updateUserStatus.isPending}
                >
                  <XCircle className="h-4 w-4 mr-2" />
                  Deactivate
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={handleUnlockUsers}
                  disabled={endUserLockout.isPending}
                >
                  <Unlock className="h-4 w-4 mr-2" />
                  Unlock
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={handleResetPasswords}
                  disabled={adminResetPassword.isPending}
                >
                  <Key className="h-4 w-4 mr-2" />
                  Reset Password
                </Button>
              </div>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Users Table */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle>Users ({totalCount})</CardTitle>
              <CardDescription>
                {isLoading
                  ? "Loading..."
                  : `${users.length} of ${totalCount} users shown`}
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
        <CardContent>
          {isLoading ? (
            <div className="flex justify-center py-8">
              <LoadingSpinner />
            </div>
          ) : users.length === 0 ? (
            <div className="text-center py-8 text-muted-foreground">
              No users found
            </div>
          ) : (
            <>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead className="w-12">
                      <Checkbox
                        checked={
                          selectedUsers.size === users.length &&
                          users.length > 0
                        }
                        onCheckedChange={handleSelectAll}
                      />
                    </TableHead>
                    <TableHead>Name</TableHead>
                    <TableHead>Email</TableHead>
                    <TableHead>Phone</TableHead>
                    <TableHead>Roles</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead>Lockout</TableHead>
                    <TableHead>Profile</TableHead>
                    <TableHead>Created</TableHead>
                    <TableHead className="text-right">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {users.map((user) => (
                    <TableRow key={user.userId}>
                      <TableCell>
                        <Checkbox
                          checked={selectedUsers.has(user.userId!)}
                          onCheckedChange={(checked) =>
                            handleSelectUser(user.userId!, checked as boolean)
                          }
                        />
                      </TableCell>
                      <TableCell className="font-medium">
                        {user.firstName} {user.lastName}
                      </TableCell>
                      <TableCell>{user.email}</TableCell>
                      <TableCell>{user.phoneNumber || "-"}</TableCell>
                      <TableCell>
                        <div className="flex flex-wrap gap-1">
                          {user.roles?.map((role) => (
                            <Badge
                              key={role}
                              variant={getRoleBadgeVariant(role)}
                              className="text-xs"
                            >
                              {role}
                            </Badge>
                          ))}
                        </div>
                      </TableCell>
                      <TableCell>
                        <Badge
                          variant={user.isActive ? "default" : "secondary"}
                        >
                          {user.isActive ? "Active" : "Inactive"}
                        </Badge>
                      </TableCell>
                      <TableCell>
                        <Badge
                          variant={
                            user.isCurrentlyLockedOut
                              ? "destructive"
                              : "outline"
                          }
                        >
                          {user.isCurrentlyLockedOut ? "Locked" : "Not Locked"}
                        </Badge>
                      </TableCell>
                      <TableCell>
                        {user.hasCandidateProfile || user.hasStaffProfile ? (
                          <Badge variant="outline">Yes</Badge>
                        ) : (
                          <Badge variant="secondary">No</Badge>
                        )}
                      </TableCell>
                      <TableCell>
                        {user.registeredAt
                          ? formatDateToLocal(user.registeredAt)
                          : "-"}
                      </TableCell>
                      <TableCell className="text-right">
                        <div className="flex justify-end gap-2">
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => handleViewDetails(user.userId!)}
                          >
                            View
                          </Button>
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => handleEditUser(user)}
                          >
                            <Edit className="h-4 w-4" />
                          </Button>
                          {!user.roles?.includes("Candidate") && (
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => handleManageRoles(user)}
                            >
                              <UserCog className="h-4 w-4" />
                            </Button>
                          )}
                        </div>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </>
          )}
        </CardContent>
        {usersData && usersData.totalCount! > 0 && (
          <CardContent className="pt-0">
            <div className="flex items-center justify-between border-t pt-4">
              <div className="text-sm text-muted-foreground">
                Showing {users.length} of {usersData.totalCount} users
                {usersData.totalPages! > 1 && (
                  <span className="ml-2">
                    (Page {usersData.pageNumber} of {usersData.totalPages})
                  </span>
                )}
              </div>
              {usersData.totalPages! > 1 && (
                <div className="flex items-center gap-2">
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setPageNumber(pageNumber - 1)}
                    disabled={!usersData?.hasPreviousPage}
                  >
                    Previous
                  </Button>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setPageNumber(pageNumber + 1)}
                    disabled={!usersData?.hasNextPage}
                  >
                    Next
                  </Button>
                </div>
              )}
            </div>
          </CardContent>
        )}
      </Card>

      {/* Edit User Dialog */}
      <Dialog open={isEditDialogOpen} onOpenChange={setIsEditDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Edit User Information</DialogTitle>
            <DialogDescription>
              Update the user's basic information
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4 py-4">
            <div>
              <Label htmlFor="firstName">First Name</Label>
              <Input
                id="firstName"
                value={editForm.firstName || ""}
                onChange={(e) =>
                  setEditForm({ ...editForm, firstName: e.target.value })
                }
              />
            </div>

            <div>
              <Label htmlFor="lastName">Last Name</Label>
              <Input
                id="lastName"
                value={editForm.lastName || ""}
                onChange={(e) =>
                  setEditForm({ ...editForm, lastName: e.target.value })
                }
              />
            </div>

            <div>
              <Label htmlFor="phoneNumber">Phone Number</Label>
              <Input
                id="phoneNumber"
                value={editForm.phoneNumber || ""}
                onChange={(e) =>
                  setEditForm({ ...editForm, phoneNumber: e.target.value })
                }
              />
            </div>

            {updateUserInfo.isError && (
              <Alert variant="destructive">
                <AlertDescription>
                  {getErrorMessage(updateUserInfo.error)}
                </AlertDescription>
              </Alert>
            )}
          </div>

          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setIsEditDialogOpen(false)}
              disabled={updateUserInfo.isPending}
            >
              Cancel
            </Button>
            <Button
              onClick={handleSaveUserInfo}
              disabled={updateUserInfo.isPending}
            >
              {updateUserInfo.isPending && (
                <RefreshCw className="mr-2 h-4 w-4 animate-spin" />
              )}
              Save Changes
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Manage Roles Dialog */}
      <Dialog open={isRoleDialogOpen} onOpenChange={setIsRoleDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Manage User Roles</DialogTitle>
            <DialogDescription>
              Add or remove roles for this user
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4 py-4">
            <div>
              <Label>Add Roles</Label>
              <div className="space-y-2 mt-2">
                {AVAILABLE_ROLES.map((role) => (
                  <div key={role.value} className="flex items-center space-x-2">
                    <Checkbox
                      id={`add-${role.value}`}
                      checked={rolesToAdd.includes(role.value)}
                      onCheckedChange={(checked) => {
                        if (checked) {
                          setRolesToAdd([...rolesToAdd, role.value]);
                          setRolesToRemove(
                            rolesToRemove.filter((r) => r !== role.value)
                          );
                        } else {
                          setRolesToAdd(
                            rolesToAdd.filter((r) => r !== role.value)
                          );
                        }
                      }}
                    />
                    <Label htmlFor={`add-${role.value}`}>{role.label}</Label>
                  </div>
                ))}
              </div>
            </div>

            <div>
              <Label>Remove Roles</Label>
              <div className="space-y-2 mt-2">
                {AVAILABLE_ROLES.map((role) => (
                  <div key={role.value} className="flex items-center space-x-2">
                    <Checkbox
                      id={`remove-${role.value}`}
                      checked={rolesToRemove.includes(role.value)}
                      onCheckedChange={(checked) => {
                        if (checked) {
                          setRolesToRemove([...rolesToRemove, role.value]);
                          setRolesToAdd(
                            rolesToAdd.filter((r) => r !== role.value)
                          );
                        } else {
                          setRolesToRemove(
                            rolesToRemove.filter((r) => r !== role.value)
                          );
                        }
                      }}
                    />
                    <Label htmlFor={`remove-${role.value}`}>{role.label}</Label>
                  </div>
                ))}
              </div>
            </div>

            {manageUserRoles.isError && (
              <Alert variant="destructive">
                <AlertDescription>
                  {getErrorMessage(manageUserRoles.error)}
                </AlertDescription>
              </Alert>
            )}
          </div>

          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setIsRoleDialogOpen(false)}
              disabled={manageUserRoles.isPending}
            >
              Cancel
            </Button>
            <Button
              onClick={handleSaveRoles}
              disabled={manageUserRoles.isPending}
            >
              {manageUserRoles.isPending && (
                <RefreshCw className="mr-2 h-4 w-4 animate-spin" />
              )}
              Save Roles
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* User Details Dialog */}
      <Dialog open={isDetailsDialogOpen} onOpenChange={setIsDetailsDialogOpen}>
        <DialogContent className="max-w-2xl">
          <DialogHeader>
            <DialogTitle>User Details</DialogTitle>
            <DialogDescription>
              View detailed information about the selected user.
            </DialogDescription>
          </DialogHeader>

          {userDetailsError ? (
            <div className="py-4">
              <Alert variant="destructive">
                <AlertDescription>
                  Failed to load user details:{" "}
                  {getErrorMessage(userDetailsError)}
                </AlertDescription>
              </Alert>
            </div>
          ) : userDetails ? (
            <div className="space-y-4 py-4">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <Label className="text-muted-foreground">Email</Label>
                  <p className="font-medium">{userDetails.email}</p>
                </div>
                <div>
                  <Label className="text-muted-foreground">Name</Label>
                  <p className="font-medium">
                    {userDetails.firstName} {userDetails.lastName}
                  </p>
                </div>
                <div>
                  <Label className="text-muted-foreground">Phone</Label>
                  <p className="font-medium">
                    {userDetails.phoneNumber || "N/A"}
                  </p>
                </div>
                <div>
                  <Label className="text-muted-foreground mr-2">Status</Label>
                  <Badge
                    variant={userDetails.isActive ? "default" : "secondary"}
                  >
                    {userDetails.isActive ? "Active" : "Inactive"}
                  </Badge>
                </div>
                <div>
                  <Label className="text-muted-foreground mr-2">
                    Email Confirmed
                  </Label>
                  <Badge
                    variant={
                      userDetails.emailConfirmed ? "default" : "secondary"
                    }
                  >
                    {userDetails.emailConfirmed ? "Yes" : "No"}
                  </Badge>
                </div>
                <div>
                  <Label className="text-muted-foreground mr-2">
                    Lockout Status
                  </Label>
                  <Badge
                    variant={
                      userDetails.isCurrentlyLockedOut
                        ? "destructive"
                        : "default"
                    }
                  >
                    {userDetails.isCurrentlyLockedOut ? "Locked" : "Not Locked"}
                  </Badge>
                </div>
              </div>

              <div>
                <Label className="text-muted-foreground">Roles</Label>
                <div className="flex flex-wrap gap-2 mt-2">
                  {userDetails.roles?.map((role) => (
                    <Badge key={role} variant={getRoleBadgeVariant(role)}>
                      {role}
                    </Badge>
                  ))}
                </div>
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <Label className="text-muted-foreground">Registered At</Label>
                  <p className="font-medium">
                    {userDetails.registeredAt
                      ? formatDateToLocal(userDetails.registeredAt)
                      : "N/A"}
                  </p>
                </div>
                <div>
                  <Label className="text-muted-foreground">Updated At</Label>
                  <p className="font-medium">
                    {userDetails.updatedAt
                      ? formatDateToLocal(userDetails.updatedAt)
                      : "Never"}
                  </p>
                </div>
              </div>

              <div>
                <Label className="text-muted-foreground mr-2">
                  Failed Login Attempts
                </Label>
                <Badge variant="outline">{userDetails.accessFailedCount}</Badge>
              </div>

              {userDetails.lockoutEnd && (
                <div>
                  <Label className="text-muted-foreground">Lockout Ends</Label>
                  <p className="font-medium">
                    {formatDateTimeToLocal(userDetails.lockoutEnd)}
                  </p>
                </div>
              )}
            </div>
          ) : (
            <div className="flex justify-center py-8">
              <LoadingSpinner />
            </div>
          )}

          <DialogFooter>
            <Button onClick={() => setIsDetailsDialogOpen(false)}>Close</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
};
