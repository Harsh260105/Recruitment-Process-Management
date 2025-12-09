import { useCallback, useEffect, useRef, useState } from "react";
import { Link, useSearchParams, useNavigate } from "react-router-dom";
import { useMutation } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { isAxiosError } from "axios";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { authService } from "@/services/authService";
import type { components } from "@/types/api";
import type { ApiResponse } from "@/types/http";

type Schemas = components["schemas"];
type ResendVerificationFormValues = Schemas["ResendVerificationDto"];

type ApiError = {
  message?: string;
};

const getErrorMessage = (error: unknown, fallback: string) => {
  if (!error) {
    return fallback;
  }

  if (isAxiosError<ApiResponse<unknown>>(error)) {
    const payload = error.response?.data;
    if (payload) {
      const apiMessage = payload.errors?.join(", ") ?? payload.message;
      if (apiMessage) {
        return apiMessage;
      }
    }
  }

  if (typeof error === "object" && "message" in (error as ApiError)) {
    return (error as ApiError).message ?? fallback;
  }

  if (error instanceof Error) {
    return error.message;
  }

  return fallback;
};

export const ConfirmEmailPage = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const userId = searchParams.get("userId");
  const token = searchParams.get("token");

  const hasTriggered = useRef(false);
  const missingParams = !userId || !token;

  const [confirmState, setConfirmState] = useState<{
    status: "idle" | "loading" | "success" | "error";
    message?: string;
  }>({ status: "idle" });

  const confirmEmail = useCallback(async () => {
    if (!userId || !token) return;

    setConfirmState({ status: "loading" });

    try {
      const response = await authService.confirmEmail({
        userId,
        token: encodeURIComponent(token),
      });

      if (response.success) {
        const successMessage =
          response.message ?? "Email verified successfully! Please sign in.";

        setConfirmState({ status: "success", message: successMessage });

        // Redirect to login after 2 seconds
        setTimeout(() => {
          navigate("/auth/login", {
            state: { message: successMessage },
          });
        }, 2000);
      } else {
        const errorMessage =
          response.errors?.join(", ") ??
          response.message ??
          "Unable to confirm email.";

        setConfirmState({ status: "error", message: errorMessage });
      }
    } catch (error) {
      const errorMessage = getErrorMessage(
        error,
        "Unable to confirm email at the moment."
      );
      setConfirmState({ status: "error", message: errorMessage });
    }
  }, [navigate, token, userId]);

  useEffect(() => {
    if (missingParams || hasTriggered.current) {
      return;
    }

    hasTriggered.current = true;
    confirmEmail();
  }, [confirmEmail, missingParams]);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<ResendVerificationFormValues>({
    defaultValues: { email: "" },
  });

  const {
    mutate: resendVerification,
    data: resendResponse,
    isPending: isResending,
    isSuccess: resendSuccess,
    isError: resendError,
    error: resendErrorValue,
  } = useMutation({
    mutationFn: async (payload: ResendVerificationFormValues) =>
      authService.resendVerification(payload),

    onSuccess: (response) => {
      if (!response.success) {
        throw new Error(
          response.errors?.join(", ") ??
            response.message ??
            "Unable to send verification email."
        );
      }

      reset();

      return response;
    },
  });

  const onResend = handleSubmit((data) => resendVerification(data));

  return (
    <div className="space-y-6">
      <div className="space-y-1">
        <h2 className="text-2xl font-semibold">Verify your email</h2>
        <p className="text-sm text-muted-foreground">
          Confirm your email address to finish activating your candidate
          account.
        </p>
      </div>

      {missingParams && (
        <div className="rounded-md border border-amber-200 bg-amber-50 px-3 py-2 text-sm text-amber-800">
          We could not find the verification details in this link. Please use
          the button below to request a new confirmation email or open the link
          directly from your inbox.
        </div>
      )}

      {!missingParams && confirmState.status === "success" && (
        <div className="rounded-md border border-emerald-200 bg-emerald-50 px-3 py-2 text-sm text-emerald-700">
          {confirmState.message}
          <br />
          <span className="text-xs opacity-75">
            Redirecting to login page...
          </span>
        </div>
      )}

      {!missingParams && confirmState.status === "error" && (
        <div className="rounded-md border border-destructive/50 bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {confirmState.message}
        </div>
      )}

      {!missingParams && confirmState.status === "loading" && (
        <div className="rounded-md border border-slate-200 bg-slate-50 px-3 py-2 text-sm text-slate-700">
          Verifying your email address...
        </div>
      )}

      <div className="rounded-md border border-slate-200 p-4">
        <div className="space-y-1">
          <h3 className="text-base font-medium">
            Need a new verification email?
          </h3>
          <p className="text-sm text-muted-foreground">
            Enter your registered email and we&apos;ll send another confirmation
            link.
          </p>
        </div>

        <form className="mt-4 space-y-3" onSubmit={onResend} noValidate>
          <div className="space-y-2">
            <Label htmlFor="email">Email address</Label>
            <Input
              id="email"
              type="email"
              autoComplete="email"
              aria-invalid={Boolean(errors.email)}
              {...register("email", {
                required: "Email is required",
                pattern: {
                  value: /^\S+@\S+$/i,
                  message: "Invalid email format",
                },
              })}
            />
            {errors.email && (
              <p className="text-sm text-destructive">{errors.email.message}</p>
            )}
          </div>

          {resendSuccess && (
            <p className="text-sm text-emerald-700">
              {resendResponse?.message ??
                "If the email you entered is registered, a new verification link has been sent."}
            </p>
          )}

          {resendError && (
            <p className="text-sm text-destructive">
              {getErrorMessage(
                resendErrorValue,
                "Unable to send verification email. Please try again."
              )}
            </p>
          )}

          <Button type="submit" className="w-full" disabled={isResending}>
            {isResending ? "Sending..." : "Resend verification email"}
          </Button>
        </form>
      </div>

      <div className="text-center text-sm text-muted-foreground">
        Ready to sign in?{" "}
        <Link to="/auth/login" className="text-primary">
          Go to login
        </Link>
      </div>
    </div>
  );
};
