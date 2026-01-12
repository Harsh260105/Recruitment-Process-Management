import { useState, useEffect } from "react";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { LoadingSpinner } from "@/components/ui/LoadingSpinner";
import { Alert, AlertDescription } from "@/components/ui/alert";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  useMyStaffProfile,
  useCreateStaffProfile,
  useUpdateStaffProfile,
} from "@/hooks/staff/staffProfile.hooks";
import { formatDateToLocal } from "@/utils/dateUtils";
import { Briefcase, Edit2, Save, X, Plus } from "lucide-react";
import { getErrorMessage } from "@/utils/error";

export const RecruiterProfilePage = () => {
  const [isEditing, setIsEditing] = useState(false);
  const [formData, setFormData] = useState({
    employeeCode: "",
    department: "",
    location: "",
    status: "Active",
    joinedDate: "",
  });
  const [validationErrors, setValidationErrors] = useState<
    Record<string, string>
  >({});

  // Fetch staff profile
  const {
    data: staffProfile,
    isLoading: profileLoading,
    error: profileError,
  } = useMyStaffProfile();

  // Mutations
  const createProfile = useCreateStaffProfile();
  const updateProfile = useUpdateStaffProfile();

  // Check if profile exists
  const hasProfile = !!staffProfile;

  // Initialize form data when profile loads
  useEffect(() => {
    if (staffProfile) {
      setFormData({
        employeeCode: staffProfile.employeeCode || "",
        department: staffProfile.department || "",
        location: staffProfile.location || "",
        status: staffProfile.status || "Active",
        joinedDate: staffProfile.joinedDate
          ? new Date(staffProfile.joinedDate).toISOString().split("T")[0]
          : "",
      });
    }
  }, [staffProfile]);

  const handleInputChange =
    (field: keyof typeof formData) =>
    (e: React.ChangeEvent<HTMLInputElement>) => {
      setFormData((prev) => ({
        ...prev,
        [field]: e.target.value,
      }));
      // Clear validation error when user types
      if (validationErrors[field]) {
        setValidationErrors((prev) => {
          const newErrors = { ...prev };
          delete newErrors[field];
          return newErrors;
        });
      }
    };

  const handleStatusChange = (value: string) => {
    setFormData((prev) => ({
      ...prev,
      status: value,
    }));
    // Clear validation error
    if (validationErrors.status) {
      setValidationErrors((prev) => {
        const newErrors = { ...prev };
        delete newErrors.status;
        return newErrors;
      });
    }
  };

  const validateForm = () => {
    const errors: Record<string, string> = {};

    if (!hasProfile && !formData.employeeCode.trim()) {
      errors.employeeCode = "Employee code is required";
    }

    if (!formData.department.trim()) {
      errors.department = "Department is required";
    }

    if (!formData.location.trim()) {
      errors.location = "Location is required";
    }

    if (!formData.status.trim()) {
      errors.status = "Status is required";
    }

    setValidationErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleSave = async () => {
    if (!validateForm()) {
      return;
    }

    try {
      const joinedDateValue = formData.joinedDate
        ? new Date(formData.joinedDate).toISOString()
        : null;

      let response;
      if (hasProfile && staffProfile?.id) {
        // Update existing profile
        response = await updateProfile.mutateAsync({
          id: staffProfile.id,
          data: {
            department: formData.department.trim(),
            location: formData.location.trim(),
            status: formData.status.trim(),
            joinedDate: joinedDateValue,
          },
        });
      } else {
        // Create new profile
        response = await createProfile.mutateAsync({
          employeeCode: formData.employeeCode.trim(),
          department: formData.department.trim(),
          location: formData.location.trim(),
          status: formData.status.trim(),
          joinedDate: joinedDateValue,
        });
      }
      setIsEditing(false);
      setValidationErrors({});
      // You can add a toast notification here with response.message
      console.log(response.message || "Profile saved successfully");
    } catch (error) {
      console.error("Failed to save staff profile:", getErrorMessage(error));
    }
  };

  const handleCancel = () => {
    // Reset form data to original values
    if (staffProfile) {
      setFormData({
        employeeCode: staffProfile.employeeCode || "",
        department: staffProfile.department || "",
        location: staffProfile.location || "",
        status: staffProfile.status || "Active",
        joinedDate: staffProfile.joinedDate
          ? new Date(staffProfile.joinedDate).toISOString().split("T")[0]
          : "",
      });
    } else {
      setFormData({
        employeeCode: "",
        department: "",
        location: "",
        status: "Active",
        joinedDate: "",
      });
    }
    setIsEditing(false);
    setValidationErrors({});
  };

  if (profileLoading) {
    return (
      <div className="container mx-auto max-w-4xl py-8">
        <Card>
          <CardContent className="flex items-center justify-center py-8">
            <LoadingSpinner />
          </CardContent>
        </Card>
      </div>
    );
  }

  const mutation = hasProfile ? updateProfile : createProfile;

  return (
    <div className="container mx-auto max-w-4xl py-8 space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Staff Profile</h1>
        <p className="text-muted-foreground mt-2">
          {hasProfile
            ? "Manage your organizational and employment information"
            : "Set up your staff profile with organizational details"}
        </p>
      </div>

      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div className="flex items-center space-x-2">
              <Briefcase className="h-5 w-5" />
              <CardTitle>
                {hasProfile ? "Staff Information" : "Create Staff Profile"}
              </CardTitle>
            </div>
            {hasProfile && !isEditing && (
              <Button
                variant="outline"
                size="sm"
                onClick={() => {
                  setIsEditing(true);
                  // Clear any previous messages when starting edit
                  setValidationErrors({});
                }}
                className="flex items-center space-x-1"
              >
                <Edit2 className="h-4 w-4" />
                <span>Edit</span>
              </Button>
            )}
            {!hasProfile && !isEditing && (
              <Button
                variant="default"
                size="sm"
                onClick={() => {
                  setIsEditing(true);
                  // Clear any previous messages when starting create
                  setValidationErrors({});
                }}
                className="flex items-center space-x-1"
              >
                <Plus className="h-4 w-4" />
                <span>Create Profile</span>
              </Button>
            )}
          </div>
          <CardDescription>
            {hasProfile
              ? "View and update your organizational and employment details"
              : "Enter your employee code, department, location and other organizational details"}
          </CardDescription>
        </CardHeader>

        <CardContent className="space-y-6">
          {/* No Profile Message */}
          {!hasProfile && !isEditing && (
            <Alert>
              <AlertDescription>
                You don't have a staff profile yet. Click "Create Profile" to
                set up your staff information.
              </AlertDescription>
            </Alert>
          )}

          {/* Display Mode */}
          {hasProfile && !isEditing && (
            <div className="space-y-4">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <Label className="text-sm font-medium text-muted-foreground">
                    Employee Code
                  </Label>
                  <p className="text-sm font-medium">
                    {staffProfile?.employeeCode || "Not provided"}
                  </p>
                </div>
                <div>
                  <Label className="text-sm font-medium text-muted-foreground">
                    Status
                  </Label>
                  <p className="text-sm font-medium">
                    <span
                      className={`inline-flex items-center px-2 py-1 rounded-full text-xs font-medium ${
                        staffProfile?.status === "Active"
                          ? "bg-green-100 text-green-800"
                          : "bg-gray-100 text-gray-800"
                      }`}
                    >
                      {staffProfile?.status || "Not set"}
                    </span>
                  </p>
                </div>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <Label className="text-sm font-medium text-muted-foreground">
                    Department
                  </Label>
                  <p className="text-sm font-medium">
                    {staffProfile?.department || "Not provided"}
                  </p>
                </div>
                <div>
                  <Label className="text-sm font-medium text-muted-foreground">
                    Location
                  </Label>
                  <p className="text-sm font-medium">
                    {staffProfile?.location || "Not provided"}
                  </p>
                </div>
              </div>

              <div>
                <Label className="text-sm font-medium text-muted-foreground">
                  Joined Date
                </Label>
                <p className="text-sm font-medium">
                  {staffProfile?.joinedDate
                    ? formatDateToLocal(staffProfile.joinedDate)
                    : "Not provided"}
                </p>
              </div>
            </div>
          )}

          {/* Edit/Create Mode */}
          {isEditing && (
            <div className="space-y-4">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div className="space-y-2">
                  <Label htmlFor="employeeCode">
                    Employee Code <span className="text-destructive">*</span>
                  </Label>
                  <Input
                    id="employeeCode"
                    value={formData.employeeCode}
                    onChange={handleInputChange("employeeCode")}
                    placeholder="e.g., EMP001"
                    disabled={hasProfile}
                    className={hasProfile ? "bg-muted" : ""}
                  />
                  {validationErrors.employeeCode && !hasProfile && (
                    <p className="text-sm text-destructive">
                      {validationErrors.employeeCode}
                    </p>
                  )}
                  {hasProfile && (
                    <p className="text-xs text-muted-foreground">
                      Employee code cannot be changed after creation
                    </p>
                  )}
                </div>
                <div className="space-y-2">
                  <Label htmlFor="status">
                    Status <span className="text-destructive">*</span>
                  </Label>
                  <Select
                    value={formData.status}
                    onValueChange={handleStatusChange}
                  >
                    <SelectTrigger id="status">
                      <SelectValue placeholder="Select status" />
                    </SelectTrigger>
                    <SelectContent className="bg-emerald-50">
                      <SelectItem value="Active">Active</SelectItem>
                      <SelectItem value="Inactive">Inactive</SelectItem>
                    </SelectContent>
                  </Select>
                  {validationErrors.status && (
                    <p className="text-sm text-destructive">
                      {validationErrors.status}
                    </p>
                  )}
                </div>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div className="space-y-2">
                  <Label htmlFor="department">
                    Department <span className="text-destructive">*</span>
                  </Label>
                  <Input
                    id="department"
                    value={formData.department}
                    onChange={handleInputChange("department")}
                    placeholder="e.g., Human Resources"
                  />
                  {validationErrors.department && (
                    <p className="text-sm text-destructive">
                      {validationErrors.department}
                    </p>
                  )}
                </div>
                <div className="space-y-2">
                  <Label htmlFor="location">
                    Location <span className="text-destructive">*</span>
                  </Label>
                  <Input
                    id="location"
                    value={formData.location}
                    onChange={handleInputChange("location")}
                    placeholder="e.g., New York Office"
                  />
                  {validationErrors.location && (
                    <p className="text-sm text-destructive">
                      {validationErrors.location}
                    </p>
                  )}
                </div>
              </div>

              <div className="space-y-2">
                <Label htmlFor="joinedDate">Joined Date</Label>
                <Input
                  id="joinedDate"
                  type="date"
                  value={formData.joinedDate}
                  onChange={handleInputChange("joinedDate")}
                />
              </div>

              {/* Action Buttons */}
              <div className="flex justify-end space-x-2 pt-4">
                <Button
                  variant="outline"
                  onClick={handleCancel}
                  disabled={mutation.isPending}
                  className="flex items-center space-x-1"
                >
                  <X className="h-4 w-4" />
                  <span>Cancel</span>
                </Button>
                <Button
                  onClick={handleSave}
                  disabled={mutation.isPending}
                  className="flex items-center space-x-1"
                >
                  {mutation.isPending ? (
                    <LoadingSpinner />
                  ) : hasProfile ? (
                    <Save className="h-4 w-4" />
                  ) : (
                    <Plus className="h-4 w-4" />
                  )}
                  <span>
                    {mutation.isPending
                      ? hasProfile
                        ? "Saving..."
                        : "Creating..."
                      : hasProfile
                      ? "Save Changes"
                      : "Create Profile"}
                  </span>
                </Button>
              </div>
            </div>
          )}

          {/* Error Display */}
          {(profileError || mutation.error) && (
            <Alert variant="destructive">
              <AlertDescription>
                {profileError
                  ? getErrorMessage(profileError)
                  : mutation.error
                  ? getErrorMessage(mutation.error)
                  : `Failed to ${
                      hasProfile ? "update" : "create"
                    } profile. Please try again.`}
              </AlertDescription>
            </Alert>
          )}

          {/* Success Message */}
          {mutation.isSuccess && !isEditing && (
            <Alert>
              <AlertDescription>
                Staff profile {hasProfile ? "updated" : "created"} successfully!
              </AlertDescription>
            </Alert>
          )}
        </CardContent>
      </Card>
    </div>
  );
};
