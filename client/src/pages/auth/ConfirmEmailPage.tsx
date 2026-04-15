import { useEffect, useMemo, useState } from "react";
import { Link, useSearchParams, useNavigate } from "react-router-dom";
import { useForm } from "react-hook-form";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useConfirmEmailQuery, useResendVerification } from "@/hooks/auth";
import { getErrorMessage } from "@/utils/error";
import type { components } from "@/types/api";

type Schemas = components["schemas"];
type ResendVerificationFormValues = Schemas["ResendVerificationDto"];
const RESEND_COOLDOWN_MS = 90_000;

const formatCountdown = (totalSeconds: number) => {
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;
  return `${minutes}:${seconds.toString().padStart(2, "0")}`;
};

export const ConfirmEmailPage = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const userId = searchParams.get("userId");
  const token = searchParams.get("token")?.replace(/ /g, "+") ?? null;
  const missingParams = !userId || !token;

  const confirmPayload = useMemo(
    () => (missingParams ? null : { userId, token }),
    [missingParams, token, userId]
  );

  const confirmEmail = useConfirmEmailQuery(confirmPayload);
  const resendVerification = useResendVerification();
  const [resendCooldownUntil, setResendCooldownUntil] = useState<number | null>(
    null
  );
  const [now, setNow] = useState(() => Date.now());
  const isResendCooldownActive =
    resendCooldownUntil !== null && now < resendCooldownUntil;
  const resendCooldownRemainingSeconds = resendCooldownUntil
    ? Math.max(0, Math.ceil((resendCooldownUntil - now) / 1000))
    : 0;

  useEffect(() => {
    if (!isResendCooldownActive) {
      return;
    }

    const intervalId = window.setInterval(() => {
      setNow(Date.now());
    }, 1000);

    return () => {
      window.clearInterval(intervalId);
    };
  }, [isResendCooldownActive]);

  useEffect(() => {
    if (resendCooldownUntil !== null && now >= resendCooldownUntil) {
      setResendCooldownUntil(null);
    }
  }, [now, resendCooldownUntil]);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<ResendVerificationFormValues>({
    defaultValues: { email: "" },
  });

  const onResend = handleSubmit((data) => {
    if (isResendCooldownActive) {
      return;
    }

    resendVerification.mutate(data, {
      onSuccess: () => {
        reset();

        const cooldownUntil = Date.now() + RESEND_COOLDOWN_MS;
        setNow(Date.now());
        setResendCooldownUntil(cooldownUntil);
      },
    });
  });

  return (
    <div className="space-y-6">
      <div className="space-y-1">
        <h2 className="text-2xl font-semibold">Verify your email</h2>
        <p className="text-sm text-muted-foreground">
          Confirm your email address to finish activating your account.
        </p>
      </div>

      {missingParams && (
        <div className="rounded-md border border-amber-200 bg-amber-50 px-3 py-2 text-sm text-amber-800">
          We could not find the verification details in this link. Please use
          the button below to request a new confirmation email or open the link
          directly from your inbox.
        </div>
      )}

      {!missingParams && confirmEmail.isSuccess && (
        <div className="rounded-md border border-emerald-200 bg-emerald-50 px-3 py-2 text-sm text-emerald-700">
          {confirmEmail.data?.message ||
            "Email verified successfully! Please sign in."}
          <br />
          <button
            type="button"
            className="text-xs underline opacity-90 cursor-pointer"
            onClick={() =>
              navigate("/auth/login", {
                state: {
                  message:
                    confirmEmail.data?.message ||
                    "Email verified successfully! Please sign in.",
                },
              })
            }
          >
            Continue to login
          </button>
        </div>
      )}

      {!missingParams && confirmEmail.isError && (
        <div className="rounded-md border border-destructive/50 bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {getErrorMessage(confirmEmail.error) ||
            "Unable to confirm email. Please try again or request a new verification link."}
        </div>
      )}

      {!missingParams && confirmEmail.isPending && (
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
              disabled={resendVerification.isPending}
              {...register("email", {
                required: "Email is required",
                pattern: {
                  value: /^\S+@\S+$/i,
                  message: "Invalid email format",
                },
              })}
            />
            {errors.email && (
              <p className="text-sm text-red-600">{errors.email.message}</p>
            )}
          </div>

          {resendVerification.isSuccess && (
            <p className="text-sm text-emerald-700">
              {resendVerification.data?.message ||
                "If the email you entered is registered, a new verification link has been sent."}
            </p>
          )}

          {isResendCooldownActive && (
            <p className="text-sm text-amber-700">
              Didn't recieved mail yet? Try again in {formatCountdown(resendCooldownRemainingSeconds)}
            </p>
          )}

          {resendVerification.isError && (
            <p className="text-sm text-destructive">
              {getErrorMessage(resendVerification.error) ||
                "Unable to send verification email. Please try again."}
            </p>
          )}

          <Button
            type="submit"
            className="w-full"
            disabled={resendVerification.isPending || isResendCooldownActive}
          >
            {resendVerification.isPending
              ? "Sending..."
              : isResendCooldownActive
                ? `Try again in ${formatCountdown(
                    resendCooldownRemainingSeconds
                  )}`
                : "Resend verification email"}
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
