import { Link } from "react-router-dom";
import { useForm } from "react-hook-form";
import { useState, useEffect } from "react";
import Countdown from "react-countdown";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useForgotPassword } from "@/hooks/auth";
import type { components } from "@/types/api";

type Schemas = components["schemas"];
type ForgotPasswordFormValues = Schemas["ForgotPasswordDto"];

export const ForgotPasswordPage = () => {
  const [cooldownEnd, setCooldownEnd] = useState<number | undefined>(undefined);
  const [currentEmail, setCurrentEmail] = useState<string>("");

  const {
    register,
    handleSubmit,
    formState: { errors },
    watch,
  } = useForm<ForgotPasswordFormValues>({
    defaultValues: {
      email: "",
    },
  });

  const watchedEmail = watch("email");

  const forgotPassword = useForgotPassword();

  // Load cooldown from localStorage on mount and email change
  useEffect(() => {
    if (watchedEmail) {
      const stored = localStorage.getItem(
        `passwordResetCooldown_${watchedEmail.toLowerCase()}`
      );
      if (stored) {
        const endTime = parseInt(stored);
        if (endTime > Date.now()) {
          setCooldownEnd(endTime);
          setCurrentEmail(watchedEmail);
        } else {
          localStorage.removeItem(
            `passwordResetCooldown_${watchedEmail.toLowerCase()}`
          );
        }
      }
    }
  }, [watchedEmail]);

  // Start countdown on successful submission
  useEffect(() => {
    if (forgotPassword.isSuccess && watchedEmail) {
      const endTime = Date.now() + 59 * 1000; // 59 seconds
      setCooldownEnd(endTime);
      setCurrentEmail(watchedEmail);
      localStorage.setItem(
        `passwordResetCooldown_${watchedEmail.toLowerCase()}`,
        endTime.toString()
      );
    }
  }, [forgotPassword.isSuccess, watchedEmail]);

  useEffect(() => {
    if (
      forgotPassword.isError &&
      forgotPassword.error?.message &&
      watchedEmail
    ) {
      const match = forgotPassword.error.message.match(/wait (\d+) seconds/);
      if (match) {
        const remainingSeconds = parseInt(match[1]);
        const endTime = Date.now() + remainingSeconds * 1000;
        setCooldownEnd(endTime);
        setCurrentEmail(watchedEmail);
        localStorage.setItem(
          `passwordResetCooldown_${watchedEmail.toLowerCase()}`,
          endTime.toString()
        );
      }
    }
  }, [forgotPassword.isError, forgotPassword.error, watchedEmail]);

  const onSubmit = handleSubmit((data) => {
    forgotPassword.mutate(data);
  });

  const countdownRenderer = ({
    seconds,
    completed,
  }: {
    seconds: number;
    completed: boolean;
  }) => {
    if (completed) {
      setCooldownEnd(undefined);
      if (currentEmail) {
        localStorage.removeItem(
          `passwordResetCooldown_${currentEmail.toLowerCase()}`
        );
      }
      return null;
    }
    return <span>{seconds}s</span>;
  };

  return (
    <div className="space-y-6">
      <div className="space-y-1">
        <h2 className="text-2xl font-semibold">Forgot password</h2>
        <p className="text-sm text-muted-foreground">
          Enter your email address and we&apos;ll send you reset instructions
        </p>
      </div>

      {forgotPassword.isSuccess && (
        <div className="rounded-md border border-emerald-200 bg-emerald-50 px-3 py-2 text-sm text-emerald-700">
          {forgotPassword.data?.message ||
            "If your email is registered, you will receive a password reset link."}
        </div>
      )}

      {forgotPassword.isError && !cooldownEnd && (
        <div className="rounded-md border border-destructive/50 bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {forgotPassword.error?.message ||
            "Unable to send reset link. Please try again."}
        </div>
      )}

      {forgotPassword.isPending && (
        <div className="rounded-md border border-slate-200 bg-slate-50 px-3 py-2 text-sm text-slate-700">
          Sending reset link...
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
            disabled={forgotPassword.isPending}
            {...register("email", {
              required: "Email is required",
              pattern: { value: /^\S+@\S+$/i, message: "Invalid email format" },
            })}
          />
          {errors.email && (
            <p className="text-sm text-destructive">{errors.email.message}</p>
          )}
        </div>

        {cooldownEnd && (
          <div className="text-center text-sm text-muted-foreground flex">
            Wait: <Countdown date={cooldownEnd} renderer={countdownRenderer} />
          </div>
        )}

        <Button
          className="w-full"
          type="submit"
          disabled={forgotPassword.isPending || cooldownEnd !== undefined}
        >
          {forgotPassword.isPending ? "Sending..." : "Send reset link"}
        </Button>
      </form>

      <div className="text-center space-y-2">
        <p className="text-sm text-muted-foreground">
          Remembered your password?{" "}
          <Link to="/auth/login" className="text-primary">
            Candidate login
          </Link>{" "}
          or{" "}
          <Link to="/auth/login" className="text-primary">
            Back to login
          </Link>
        </p>
      </div>
    </div>
  );
};
