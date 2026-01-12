import { Link, useNavigate, useSearchParams } from "react-router-dom";
import { useForm } from "react-hook-form";
import { useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useResetPassword } from "@/hooks/auth";
import type { components } from "@/types/api";

type Schemas = components["schemas"];
type ResetPasswordFormValues = Schemas["ResetPasswordDto"];

export const ResetPasswordPage = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const [isValidToken, setIsValidToken] = useState<boolean | null>(null);

  const token = searchParams.get("token");
  const userId = searchParams.get("userId");

  const {
    register,
    handleSubmit,
    formState: { errors },
    watch,
    reset
  } = useForm<ResetPasswordFormValues>({
    defaultValues: {
      token: token || "",
      userId: userId || "",
      newPassword: "",
      confirmNewPassword: "",
    },
  });

  const newPassword = watch("newPassword");

  const resetPassword = useResetPassword();

  // Validate token on component mount
  useEffect(() => {
    if (!token || !userId) {
      setIsValidToken(false);
      return;
    }

    setIsValidToken(true);
  }, [token, userId]);

  useEffect(() => {
    if (resetPassword.isSuccess) {
      const successMessage =
        resetPassword.data?.message ||
        "Password reset successfully. Please sign in with your new password.";

      reset();

      setTimeout(() => {
        navigate("/auth/login", {
          state: { message: successMessage },
        });
      }, 2000);
    }
  }, [resetPassword.isSuccess, resetPassword.data, navigate]);

  const onSubmit = handleSubmit((data) => {
    resetPassword.mutate(data);
  });

  if (isValidToken === null) {
    return (
      <div className="space-y-6">
        <div className="text-center">
          <h2 className="text-2xl font-semibold">Validating reset link...</h2>
          <p className="text-sm text-muted-foreground">Please wait</p>
        </div>
      </div>
    );
  }

  if (isValidToken === false) {
    return (
      <div className="space-y-6">
        <div className="space-y-1">
          <h2 className="text-2xl font-semibold">Invalid Reset Link</h2>
          <p className="text-sm text-muted-foreground">
            This password reset link is invalid or has expired
          </p>
        </div>

        <div className="text-center">
          <p className="text-sm text-muted-foreground">
            <Link to="/auth/forgot-password" className="text-primary">
              Request a new password reset
            </Link>
          </p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="space-y-1">
        <h2 className="text-2xl font-semibold">Reset your password</h2>
        <p className="text-sm text-muted-foreground">
          Enter your new password below
        </p>
      </div>

      {resetPassword.isSuccess && (
        <div className="rounded-md border border-emerald-200 bg-emerald-50 px-3 py-2 text-sm text-emerald-700">
          {resetPassword.data?.message ||
            "Password reset successfully. Please sign in with your new password."}
          <br />
          <span className="text-xs opacity-75">
            Redirecting to login page...
          </span>
        </div>
      )}

      {resetPassword.isError && (
        <div className="rounded-md border border-destructive/50 bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {resetPassword.error?.message ||
            "Unable to reset password. Please try again."}
        </div>
      )}

      {resetPassword.isPending && (
        <div className="rounded-md border border-slate-200 bg-slate-50 px-3 py-2 text-sm text-slate-700">
          Resetting your password...
        </div>
      )}

      <form className="space-y-4" onSubmit={onSubmit} noValidate>
        <div className="space-y-2">
          <Label htmlFor="newPassword">New Password</Label>
          <Input
            id="newPassword"
            type="password"
            autoComplete="new-password"
            aria-invalid={Boolean(errors.newPassword)}
            disabled={resetPassword.isPending}
            {...register("newPassword", {
              required: "Password is required",
              minLength: {
                value: 8,
                message: "Password must be at least 8 characters",
              },
              pattern: {
                value:
                  /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$/,
                message:
                  "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character",
              },
            })}
          />
          {errors.newPassword && (
            <p className="text-sm text-destructive">
              {errors.newPassword.message}
            </p>
          )}
        </div>

        <div className="space-y-2">
          <Label htmlFor="confirmPassword">Confirm New Password</Label>
          <Input
            id="confirmPassword"
            type="password"
            autoComplete="new-password"
            aria-invalid={Boolean(errors.confirmNewPassword)}
            disabled={resetPassword.isPending}
            {...register("confirmNewPassword", {
              required: "Please confirm your password",
              validate: (value) =>
                value === newPassword || "Passwords do not match",
            })}
          />
          {errors.confirmNewPassword && (
            <p className="text-sm text-destructive">
              {errors.confirmNewPassword.message}
            </p>
          )}
        </div>

        <Button
          className="w-full"
          type="submit"
          disabled={resetPassword.isPending}
        >
          {resetPassword.isPending ? "Resetting..." : "Reset password"}
        </Button>
      </form>

      <p className="text-center text-sm text-muted-foreground">
        Remember your password?{" "}
        <Link to="/auth/login" className="text-primary">
          Back to login
        </Link>
      </p>
    </div>
  );
};
