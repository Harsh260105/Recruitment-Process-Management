import { useState } from "react";
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
import { useChangePassword } from "@/hooks/auth";
import { Lock, ShieldCheck, AlertCircle } from "lucide-react";
import { AccountInfoCard } from "@/components/Account/AccountInfoCard";
import { getErrorMessage } from "@/utils/error";

export const AccountSettingsPage = () => {
  const [passwordForm, setPasswordForm] = useState({
    currentPassword: "",
    newPassword: "",
    confirmNewPassword: "",
  });
  const [passwordErrors, setPasswordErrors] = useState<Record<string, string>>(
    {}
  );
  const [passwordSuccess, setPasswordSuccess] = useState<string | null>(null);
  const [passwordError, setPasswordError] = useState<string | null>(null);

  const changePassword = useChangePassword();

  const handlePasswordChange =
    (field: keyof typeof passwordForm) =>
    (e: React.ChangeEvent<HTMLInputElement>) => {
      setPasswordForm((prev) => ({
        ...prev,
        [field]: e.target.value,
      }));
      // Clear error for this field when user starts typing
      if (passwordErrors[field]) {
        setPasswordErrors((prev) => {
          const newErrors = { ...prev };
          delete newErrors[field];
          return newErrors;
        });
      }
    };

  const validatePasswordForm = () => {
    const errors: Record<string, string> = {};

    if (!passwordForm.currentPassword) {
      errors.currentPassword = "Current password is required";
    }

    if (!passwordForm.newPassword) {
      errors.newPassword = "New password is required";
    } else if (passwordForm.newPassword.length < 8) {
      errors.newPassword = "Password must be at least 8 characters";
    } else if (
      !/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$/.test(
        passwordForm.newPassword
      )
    ) {
      errors.newPassword =
        "Password must contain uppercase, lowercase, number, and special character";
    }

    if (!passwordForm.confirmNewPassword) {
      errors.confirmNewPassword = "Please confirm your new password";
    } else if (passwordForm.newPassword !== passwordForm.confirmNewPassword) {
      errors.confirmNewPassword = "Passwords do not match";
    }

    setPasswordErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handlePasswordSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!validatePasswordForm()) {
      return;
    }

    setPasswordSuccess(null);
    setPasswordError(null);

    try {
      const response = await changePassword.mutateAsync({
        currentPassword: passwordForm.currentPassword,
        newPassword: passwordForm.newPassword,
        confirmNewPassword: passwordForm.confirmNewPassword,
      });

      setPasswordSuccess(response.message || "Password changed successfully");

      // Reset form on success
      setPasswordForm({
        currentPassword: "",
        newPassword: "",
        confirmNewPassword: "",
      });
      setPasswordErrors({});
    } catch (error) {
      setPasswordError(getErrorMessage(error) || "Failed to change password");
    }
  };

  return (
    <div className="container mx-auto max-w-4xl py-8 space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Account Settings</h1>
        <p className="text-muted-foreground mt-2">
          Manage your account information and security
        </p>
      </div>

      {/* Account Information Card */}
      <AccountInfoCard />

      {/* Change Password Card */}
      <Card>
        <CardHeader>
          <div className="flex items-center space-x-2">
            <Lock className="h-5 w-5" />
            <CardTitle>Change Password</CardTitle>
          </div>
          <CardDescription>
            Update your password to keep your account secure
          </CardDescription>
        </CardHeader>

        <CardContent>
          {passwordSuccess && (
            <Alert className="mb-4 bg-green-50 text-green-800 border-green-200">
              <ShieldCheck className="h-4 w-4" />
              <AlertDescription>{passwordSuccess}</AlertDescription>
            </Alert>
          )}
          {passwordError && (
            <Alert className="mb-4 bg-red-50 text-red-800 border-red-200">
              <AlertCircle className="h-4 w-4" />
              <AlertDescription>{passwordError}</AlertDescription>
            </Alert>
          )}
          <form onSubmit={handlePasswordSubmit} className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="currentPassword">Current Password</Label>
              <Input
                id="currentPassword"
                type="password"
                value={passwordForm.currentPassword}
                onChange={handlePasswordChange("currentPassword")}
                placeholder="Enter current password"
                autoComplete="current-password"
                aria-invalid={Boolean(passwordErrors.currentPassword)}
              />
              {passwordErrors.currentPassword && (
                <p className="text-sm text-destructive">
                  {passwordErrors.currentPassword}
                </p>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="newPassword">New Password</Label>
              <Input
                id="newPassword"
                type="password"
                value={passwordForm.newPassword}
                onChange={handlePasswordChange("newPassword")}
                placeholder="Enter new password"
                autoComplete="new-password"
                aria-invalid={Boolean(passwordErrors.newPassword)}
              />
              {passwordErrors.newPassword && (
                <p className="text-sm text-destructive">
                  {passwordErrors.newPassword}
                </p>
              )}
              <p className="text-xs text-muted-foreground">
                Must be at least 8 characters with uppercase, lowercase, number,
                and special character
              </p>
            </div>

            <div className="space-y-2">
              <Label htmlFor="confirmNewPassword">Confirm New Password</Label>
              <Input
                id="confirmNewPassword"
                type="password"
                value={passwordForm.confirmNewPassword}
                onChange={handlePasswordChange("confirmNewPassword")}
                placeholder="Confirm new password"
                autoComplete="new-password"
                aria-invalid={Boolean(passwordErrors.confirmNewPassword)}
              />
              {passwordErrors.confirmNewPassword && (
                <p className="text-sm text-destructive">
                  {passwordErrors.confirmNewPassword}
                </p>
              )}
            </div>

            {changePassword.error && (
              <Alert variant="destructive">
                <AlertCircle className="h-4 w-4" />
                <AlertDescription>
                  {getErrorMessage(changePassword.error) ||
                    "Failed to change password. Please try again."}
                </AlertDescription>
              </Alert>
            )}

            <div className="flex justify-end pt-4">
              <Button
                type="submit"
                disabled={changePassword.isPending}
                className="flex items-center space-x-2"
              >
                {changePassword.isPending && <LoadingSpinner />}
                <span>
                  {changePassword.isPending
                    ? "Changing Password..."
                    : "Change Password"}
                </span>
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
};
