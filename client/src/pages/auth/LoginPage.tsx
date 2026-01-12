import { Link, Navigate, useNavigate } from "react-router-dom";
import { useForm } from "react-hook-form";
import { useMutation } from "@tanstack/react-query";
import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { authService } from "@/services/authService";
import { getErrorMessage } from "@/utils/error";
import { useAuth } from "@/store";
import type { components } from "@/types/api";

const STAFF_ROLES = new Set(["SuperAdmin", "Admin", "HR", "Recruiter"]);

const getDefaultRoute = (roles?: string[]) => {
  if (roles?.some((role) => STAFF_ROLES.has(role))) {
    if (roles.includes("Recruiter")) {
      return "/recruiter/dashboard";
    } else {
      return "/admin/hr-dashboard";
    }
  }
  return "/candidate/dashboard";
};

type Schemas = components["schemas"];
type LoginFormValues = Schemas["LoginDto"];

export const LoginPage = () => {
  const navigate = useNavigate();
  const setAuth = useAuth((state) => state.auth.setAuth);
  const isAuthenticated = useAuth((state) => state.auth.isAuthenticated);
  const roles = useAuth((state) => state.auth.roles);

  const [loginState, setLoginState] = useState<{
    status: "idle" | "loading" | "success" | "error" | "locked";
    message?: string;
    isLocked?: boolean;
  }>({ status: "idle" });

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginFormValues>({
    defaultValues: {
      email: "",
      password: "",
      rememberMe: true,
    },
  });

  const mutation = useMutation({
    mutationFn: (payload: LoginFormValues) => authService.login(payload),

    onSuccess: (response) => {
      if (!response.data) {
        setLoginState({
          status: "error",
          message: "Invalid response from server.",
        });
        return;
      }

      const token = response.data.token;
      const user = response.data.user;

      if (token && user) {
        setAuth(token, user, user.roles ?? undefined);

        setLoginState({
          status: "success",
          message: response.message ?? "Sign in successful!",
        });

        setTimeout(() => {
          navigate(getDefaultRoute(user.roles ?? undefined), { replace: true });
        }, 1000);
      } else {
        setLoginState({
          status: "error",
          message:
            response.errors?.join(", ") ?? "Invalid response from server.",
        });
      }
    },

    onError: (error) => {
      const message = getErrorMessage(error);

      const normalizedMessage = message.toLowerCase();
      const hasRemainingWarning = normalizedMessage.includes("remaining");
      const isLocked =
        !hasRemainingWarning &&
        (normalizedMessage.includes("locked") ||
          normalizedMessage.includes("lockout"));

      setLoginState({
        status: isLocked ? "locked" : "error",
        message,
        isLocked,
      });
    },
  });

  const onSubmit = handleSubmit((values) => {
    setLoginState({ status: "loading" });
    mutation.mutate(values);
  });

  if (isAuthenticated) {
    return <Navigate to={getDefaultRoute(roles)} replace />;
  }

  return (
    <div className="space-y-6">
      <div className="space-y-1">
        <h2 className="text-2xl font-semibold">Welcome back</h2>
        <p className="text-sm text-muted-foreground">
          Sign in with your email and password
        </p>
      </div>

      {loginState.message === "Network Error" && (
        <div className="rounded-lg border border-destructive/50 bg-destructive/10 p-4 text-sm">
          <p className="font-medium">Network Error</p>
          <p className="mt-1">Unable to connect to the server.</p>
        </div>
      )}

      {loginState.status === "success" && (
        <div className="rounded-lg border border-emerald-200 bg-emerald-50 p-4 text-sm text-emerald-700">
          <p className="font-medium">{loginState.message}</p>
          <p className="mt-1 text-xs opacity-75">Redirecting to dashboard...</p>
        </div>
      )}

      {loginState.status === "locked" && (
        <div className="rounded-lg border border-amber-200 bg-amber-50 p-4 text-sm text-amber-900">
          <p className="font-medium">Account temporarily locked</p>
          <p className="mt-1">{loginState.message}</p>
          <div className="mt-3 flex flex-col gap-2 text-xs">
            <p>You can:</p>
            <ul className="ml-4 list-disc space-y-1">
              <li>Wait 15 minutes for automatic unlock</li>
              <li>
                <Link
                  to="/auth/forgot-password"
                  className="text-amber-700 underline"
                >
                  Reset your password
                </Link>
              </li>
              <li>Contact support for immediate assistance</li>
            </ul>
          </div>
        </div>
      )}

      {loginState.status === "error" && loginState.message !== "Network Error" && !loginState.isLocked && (
        <div
          className={`rounded-lg border p-4 text-sm ${
            loginState.message?.includes("remaining")
              ? "border-amber-200 bg-amber-50 text-amber-900"
              : "border-destructive/50 bg-destructive/10 text-destructive"
          }`}
        >
          <p className="font-medium">Sign in failed</p>
          <p className="mt-1">{loginState.message}</p>
          {loginState.message?.includes("remaining") && (
            <p className="mt-2 text-xs font-medium">
              ⚠️ Your account will be locked for 15 minutes after all attempts
              are used.
            </p>
          )}
          {!loginState.message?.includes("remaining") && (
            <p className="mt-2 text-xs opacity-75">
              Caution: After 5 failed attempts, your account will be locked for
              15 minutes.
            </p>
          )}
        </div>
      )}

      {loginState.status === "loading" && (
        <div className="rounded-lg border border-slate-200 bg-slate-50 p-4 text-sm text-slate-700">
          <div className="flex items-center gap-2">
            <svg
              className="h-4 w-4 animate-spin"
              xmlns="http://www.w3.org/2000/svg"
              fill="none"
              viewBox="0 0 24 24"
            >
              <circle
                className="opacity-25"
                cx="12"
                cy="12"
                r="10"
                stroke="currentColor"
                strokeWidth="4"
              ></circle>
              <path
                className="opacity-75"
                fill="currentColor"
                d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
              ></path>
            </svg>
            <span>Signing in...</span>
          </div>
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
          {mutation.isPending ? "Signing in..." : "Sign in"}
        </Button>
      </form>

      <p className="text-center text-sm text-muted-foreground">
        Don&apos;t have an account?{" "}
        <Link to="/auth/register" className="text-primary">
          Create one
        </Link>
      </p>
    </div>
  );
};
