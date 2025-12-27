import React, { useState } from "react";
import { useUserProfile, useUpdateBasicInfo } from "@/hooks/auth";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { LoadingSpinner } from "@/components/ui/LoadingSpinner";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { User, Edit2, Save, X } from "lucide-react";
import { getErrorMessage } from "@/utils/error";

interface AccountInfoCardProps {
  /** Optional title override */
  title?: string;
  /** Optional description override */
  description?: string;
  /** Whether to show edit functionality */
  editable?: boolean;
  /** Custom className */
  className?: string;
}

export function AccountInfoCard({
  title = "Account Information",
  description = "View and manage your basic account information",
  editable = true,
  className = "",
}: AccountInfoCardProps) {
  const [isEditing, setIsEditing] = useState(false);
  const [formData, setFormData] = useState({
    firstName: "",
    lastName: "",
    phoneNumber: "",
  });

  // Fetch profile data
  const {
    data: profile,
    isLoading: profileLoading,
    error: profileError,
  } = useUserProfile();

  // Update profile mutation
  const updateProfile = useUpdateBasicInfo();

  // Initialize form data when profile loads
  React.useEffect(() => {
    if (profile) {
      setFormData({
        firstName: profile.firstName || "",
        lastName: profile.lastName || "",
        phoneNumber: profile.phoneNumber || "",
      });
    }
  }, [profile]);

  const handleInputChange =
    (field: keyof typeof formData) =>
    (e: React.ChangeEvent<HTMLInputElement>) => {
      setFormData((prev) => ({
        ...prev,
        [field]: e.target.value,
      }));
    };

  const handleSave = async () => {
    try {
      await updateProfile.mutateAsync({
        firstName: formData.firstName.trim(),
        lastName: formData.lastName.trim(),
        phoneNumber: formData.phoneNumber.trim(),
      });
      setIsEditing(false);
    } catch (error) {
      // Error is handled by the mutation
      console.error("Failed to update profile:", error);
    }
  };

  const handleCancel = () => {
    // Reset form data to original values
    if (profile) {
      setFormData({
        firstName: profile.firstName || "",
        lastName: profile.lastName || "",
        phoneNumber: profile.phoneNumber || "",
      });
    }
    setIsEditing(false);
  };

  if (profileLoading) {
    return (
      <Card className={className}>
        <CardContent className="flex items-center justify-center py-8">
          <LoadingSpinner />
        </CardContent>
      </Card>
    );
  }

  if (profileError) {
    return (
      <Card className={className}>
        <CardContent className="py-8">
          <Alert variant="destructive">
            <AlertDescription>
              Failed to load profile information. Please try again later.
            </AlertDescription>
          </Alert>
        </CardContent>
      </Card>
    );
  }

  if (!profile) {
    return (
      <Card className={className}>
        <CardContent className="py-8">
          <Alert>
            <AlertDescription>
              No profile information available.
            </AlertDescription>
          </Alert>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card className={className}>
      <CardHeader>
        <div className="flex items-center justify-between">
          <div className="flex items-center space-x-2">
            <User className="h-5 w-5" />
            <CardTitle>{title}</CardTitle>
          </div>
          {editable && !isEditing && (
            <Button
              variant="outline"
              size="sm"
              onClick={() => setIsEditing(true)}
              className="flex items-center space-x-1"
            >
              <Edit2 className="h-4 w-4" />
              <span>Edit</span>
            </Button>
          )}
        </div>
        <CardDescription>{description}</CardDescription>
      </CardHeader>

      <CardContent className="space-y-6">
        {/* Display Mode */}
        {!isEditing && (
          <div className="space-y-4">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <Label className="text-sm font-medium text-muted-foreground">
                  First Name
                </Label>
                <p className="text-sm font-medium">
                  {profile.firstName || "Not provided"}
                </p>
              </div>
              <div>
                <Label className="text-sm font-medium text-muted-foreground">
                  Last Name
                </Label>
                <p className="text-sm font-medium">
                  {profile.lastName || "Not provided"}
                </p>
              </div>
            </div>

            <div>
              <Label className="text-sm font-medium text-muted-foreground">
                Email
              </Label>
              <p className="text-sm font-medium">{profile.email}</p>
            </div>

            <div>
              <Label className="text-sm font-medium text-muted-foreground">
                Phone Number
              </Label>
              <p className="text-sm font-medium">
                {profile.phoneNumber || "Not provided"}
              </p>
            </div>

            {profile.roles && profile.roles.length > 0 && (
              <div>
                <Label className="text-sm font-medium text-muted-foreground">
                  Roles
                </Label>
                <div className="flex flex-wrap gap-1 mt-1">
                  {profile.roles.map((role) => (
                    <span
                      key={role}
                      className="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-primary/10 text-primary"
                    >
                      {role}
                    </span>
                  ))}
                </div>
              </div>
            )}
          </div>
        )}

        {/* Edit Mode */}
        {isEditing && (
          <div className="space-y-4">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="firstName">First Name</Label>
                <Input
                  id="firstName"
                  value={formData.firstName}
                  onChange={handleInputChange("firstName")}
                  placeholder="Enter your first name"
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="lastName">Last Name</Label>
                <Input
                  id="lastName"
                  value={formData.lastName}
                  onChange={handleInputChange("lastName")}
                  placeholder="Enter your last name"
                />
              </div>
            </div>

            <div className="space-y-2">
              <Label htmlFor="phoneNumber">Phone Number</Label>
              <Input
                id="phoneNumber"
                value={formData.phoneNumber}
                onChange={handleInputChange("phoneNumber")}
                placeholder="Enter your phone number"
                type="tel"
              />
            </div>

            {/* Email is read-only */}
            <div className="space-y-2">
              <Label>Email (Read-only)</Label>
              <Input
                value={profile.email || ""}
                disabled
                className="bg-muted"
              />
            </div>

            {/* Action Buttons */}
            <div className="flex justify-end space-x-2 pt-4">
              <Button
                variant="outline"
                onClick={handleCancel}
                disabled={updateProfile.isPending}
                className="flex items-center space-x-1"
              >
                <X className="h-4 w-4" />
                <span>Cancel</span>
              </Button>
              <Button
                onClick={handleSave}
                disabled={updateProfile.isPending}
                className="flex items-center space-x-1"
              >
                {updateProfile.isPending ? (
                  <LoadingSpinner />
                ) : (
                  <Save className="h-4 w-4" />
                )}
                <span>
                  {updateProfile.isPending ? "Saving..." : "Save Changes"}
                </span>
              </Button>
            </div>
          </div>
        )}

        {/* Error Display */}
        {updateProfile.error && (
          <Alert variant="destructive">
            <AlertDescription>
              {getErrorMessage(updateProfile.error) ||
                "Failed to update profile. Please try again."}
            </AlertDescription>
          </Alert>
        )}

        {/* Success Message */}
        {updateProfile.isSuccess && !isEditing && (
          <Alert>
            <AlertDescription>
              {updateProfile.data?.message || "Profile updated successfully!"}
            </AlertDescription>
          </Alert>
        )}
      </CardContent>
    </Card>
  );
}
