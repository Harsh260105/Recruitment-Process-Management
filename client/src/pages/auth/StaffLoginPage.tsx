import { Link, Navigate, useNavigate } from "react-router-dom";
import { useForm } from "react-hook-form";
import { useMutation } from "@tanstack/react-query";
import { useState } from "react";
import { isAxiosError } from "axios";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { authService } from "@/services/authService";
import { useAuth } from "@/store";
import type { components } from "@/types/api";
import type { ApiResponse } from "@/types/http";
import { getErrorMessage } from "@/utils/error";

type Schemas = components["schemas"];
type StaffLoginFormValues = Schemas["LoginDto"];

const STAFF_ROLES = new Set(["SuperAdmin", "Admin", "HR", "Recruiter"]);

export const StaffLoginPage = () => {
  const navigate = useNavigate();
  const setAuth = useAuth((state) => state.auth.setAuth);
  const isAuthenticated = useAuth((state) => state.auth.isAuthenticated);
  const roles = useAuth((state) => state.auth.roles);

  const [loginState, setLoginState] = useState<{
    status: "idle" | "loading" | "success" | "error";
    message?: string;
  }>({ status: "idle" });

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<StaffLoginFormValues>({
    defaultValues: {
      email: "",
      password: "",
      rememberMe: true,
    },
  });

  const mutation = useMutation({
    mutationFn: (payload: StaffLoginFormValues) => authService.login(payload),

    onSuccess: (response) => {
      if (!response.success || !response.data) {
        const message =
          response.errors?.join(", ") ??
          response.message ??
          "Unable to sign in. Please try again.";

        setLoginState({ status: "error", message });
        return;
      }

      const token = response.data.token;
      const user = response.data.user ?? null;

      if (token && user) {
        const userRoles = user.roles ?? [];
        const isStaff = userRoles.some((role) => STAFF_ROLES.has(role));

        if (!isStaff) {
          setLoginState({
            status: "error",
            message: "Access denied. Staff credentials required.",
          });
          return;
        }

        setAuth(token, user, userRoles);
        setLoginState({
          status: "success",
          message: response.message ?? "Sign in successful!",
        });

        setTimeout(() => {
          navigate("/recruiter/dashboard");
        }, 1000);
      } else {
        setLoginState({
          status: "error",
          message: "Invalid response from server.",
        });
      }
    },

    onError: (error) => {
      const message = (() => {
        if (isAxiosError<ApiResponse>(error)) {
          const payload = error.response?.data;
          if (payload) {
            const detailedMessage =
              payload.errors?.filter(Boolean).join(", ") ?? payload.message;
            if (detailedMessage) {
              return detailedMessage;
            }
          }
        }

        return getErrorMessage(error) || "Unexpected error. Please try again.";
      })();

      setLoginState({ status: "error", message });
    },
  });

  const onSubmit = handleSubmit((values) => {
    setLoginState({ status: "loading" });
    mutation.mutate(values);
  });

  if (isAuthenticated) {
    const hasStaffRole = roles?.some((role) => STAFF_ROLES.has(role));
    return (
      <Navigate
        to={hasStaffRole ? "/recruiter/dashboard" : "/candidate/dashboard"}
        replace
      />
    );
  }

  return (
    <div className="space-y-6">
      <div className="space-y-1">
        <h2 className="text-2xl font-semibold">Staff Portal</h2>
        <p className="text-sm text-muted-foreground">
          Sign in to access the recruitment management dashboard
        </p>
      </div>

      {loginState.status === "success" && (
        <div className="rounded-md border border-emerald-200 bg-emerald-50 px-3 py-2 text-sm text-emerald-700">
          {loginState.message}
          <br />
          <span className="text-xs opacity-75">
            Redirecting to dashboard...
          </span>
        </div>
      )}

      {loginState.status === "error" && (
        <div className="rounded-md border border-destructive/50 bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {loginState.message}
        </div>
      )}

      {loginState.status === "loading" && (
        <div className="rounded-md border border-slate-200 bg-slate-50 px-3 py-2 text-sm text-slate-700">
          Signing in...
        </div>
      )}

      <form className="space-y-4" onSubmit={onSubmit} noValidate>
        <div className="space-y-2">
          <Label htmlFor="email">Email address</Label>
          <Input
            id="email"
            type="email"
            autoComplete="email"
            aria-invalid={Boolean(errors.email)}
            {...register("email", { required: "Email is required" })}
          />
          {errors.email && (
            <p className="text-sm text-destructive">{errors.email.message}</p>
          )}
        </div>

        <div className="space-y-2">
          <Label htmlFor="password">Password</Label>
          <Input
            id="password"
            type="password"
            autoComplete="current-password"
            aria-invalid={Boolean(errors.password)}
            {...register("password", { required: "Password is required" })}
          />
          {errors.password && (
            <p className="text-sm text-destructive">
              {errors.password.message}
            </p>
          )}
          <div className="flex items-center justify-end">
            <Link to="/auth/forgot-password" className="text-sm text-primary">
              Forgot password?
            </Link>
          </div>
        </div>

        <label className="flex items-center gap-2 text-sm text-muted-foreground">
          <input
            type="checkbox"
            className="h-4 w-4 rounded border border-input"
            {...register("rememberMe")}
          />
          Remember me on this device
        </label>

        <Button className="w-full" type="submit" disabled={mutation.isPending}>
          {mutation.isPending ? "Signing in..." : "Sign in to dashboard"}
        </Button>
      </form>

      <div className="text-center space-y-2">
        <p className="text-sm text-muted-foreground">
          Need a staff account? Contact your system administrator to be
          provisioned.
        </p>
        <p className="text-sm text-muted-foreground">
          Looking for candidate portal?{" "}
          <Link to="/auth/login" className="text-primary">
            Candidate login
          </Link>
        </p>
      </div>
    </div>
  );
};
