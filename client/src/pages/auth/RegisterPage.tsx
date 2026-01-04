import { Link } from "react-router-dom";
import { useForm } from "react-hook-form";
import { useMutation } from "@tanstack/react-query";
import { useState } from "react";
import { getErrorMessage } from "@/utils/error";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { authService } from "@/services/authService";
import type { components } from "@/types/api";

type Schemas = components["schemas"];
type RegistrationFormValues = Schemas["CandidateRegisterDto"];

export const RegisterPage = () => {
  // state
  const [registerState, setRegisterState] = useState<{
    status: "idle" | "loading" | "success" | "error";
    message?: string;
  }>({ status: "idle" });

  // form
  const {
    register,
    handleSubmit,
    formState: { errors },
    watch,
  } = useForm<RegistrationFormValues>({
    mode: "onBlur",
    defaultValues: {
      firstName: "",
      lastName: "",
      email: "",
      phoneNumber: "",
      password: "",
      confirmPassword: "",
    },
  });

  const password = watch("password");

  const mutation = useMutation({
    mutationFn: (payload: RegistrationFormValues) =>
      authService.registerCandidate(payload),

    onSuccess: (response) => {
      if (!response.success || !response.data) {
        const message =
          response.errors?.join(", ") ??
          response.message ??
          "Unable to register. Please try again.";

        setRegisterState({ status: "error", message });

        return;
      }

      const successMessage =
        response.message ??
        "Account created successfully! Please check your email for verification.";

      setRegisterState({ status: "success", message: successMessage });
    },

    onError: (error) => {
      const message =
        getErrorMessage(error) || "Unexpected error. Please try again.";
      setRegisterState({ status: "error", message });
    },
  });

  const onSubmit = handleSubmit((data) => {
    setRegisterState({ status: "loading" });
    mutation.mutate(data);
  });

  return (
    <div className="space-y-6">
      <div className="space-y-1">
        <h2 className="text-2xl font-semibold">Create candidate account</h2>
        <p className="text-sm text-muted-foreground">
          Register to access your candidate dashboard and apply for jobs.
        </p>
      </div>

      <div className="rounded-md border border-sky-200 bg-sky-50 px-3 py-2 text-sm text-sky-800">
        After signing up you will receive an email with a verification link.
        Please confirm your email before trying to sign in.
      </div>

      {registerState.status === "success" && (
        <div className="rounded-md border border-emerald-200 bg-emerald-50 px-3 py-2 text-sm text-emerald-700">
          {registerState.message}
          <br />
          <span className="text-xs opacity-75">
            Didn&apos;t receive the email?{" "}
            <Link
              to="/auth/confirm-email"
              className="text-emerald-700 underline"
            >
              Resend verification
            </Link>
          </span>
        </div>
      )}

      {registerState.status === "error" && (
        <div className="rounded-md border border-destructive/50 bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {registerState.message}
          <br />
          <span className="text-xs opacity-75">
            Didn&apos;t receive the email?{" "}
            <Link
              to="/auth/confirm-email"
              className="text-destructive underline"
            >
              Resend verification
            </Link>
          </span>
        </div>
      )}

      {registerState.status === "loading" && (
        <div className="rounded-md border border-slate-200 bg-slate-50 px-3 py-2 text-sm text-slate-700">
          Creating account...
        </div>
      )}

      <form className="space-y-4" onSubmit={onSubmit} noValidate>
        <div className="grid gap-4 md:grid-cols-2">
          <div className="space-y-2">
            <Label htmlFor="firstName">First name</Label>
            <Input
              id="firstName"
              autoComplete="given-name"
              aria-invalid={Boolean(errors.firstName)}
              {...register("firstName", { required: "First name is required" })}
            />
            {errors.firstName && (
              <p className="text-sm text-destructive">
                {errors.firstName.message}
              </p>
            )}
          </div>
          <div className="space-y-2">
            <Label htmlFor="lastName">Last name</Label>
            <Input
              id="lastName"
              autoComplete="family-name"
              aria-invalid={Boolean(errors.lastName)}
              {...register("lastName", { required: "Last name is required" })}
            />
            {errors.lastName && (
              <p className="text-sm text-destructive">
                {errors.lastName.message}
              </p>
            )}
          </div>
        </div>

        <div className="space-y-2">
          <Label htmlFor="email">Email address</Label>
          <Input
            id="email"
            type="email"
            autoComplete="email"
            aria-invalid={Boolean(errors.email)}
            {...register("email", {
              required: "Email is required",
              pattern: { value: /^\S+@\S+$/i, message: "Invalid email format" },
            })}
          />
          {errors.email && (
            <p className="text-sm text-destructive">{errors.email.message}</p>
          )}
        </div>

        <div className="space-y-2">
          <Label htmlFor="phoneNumber">Phone number</Label>
          <Input
            id="phoneNumber"
            type="tel"
            autoComplete="tel"
            aria-invalid={Boolean(errors.phoneNumber)}
            {...register("phoneNumber", {
              required: "Phone number is required",
              pattern: {
                value: /^[\+]?[1-9][\d\s\-\(\)]{7,19}$/,
                message:
                  "Please enter a valid phone number (e.g., +91 9898989898)",
              },
            })}
          />
          {errors.phoneNumber && (
            <p className="text-sm text-destructive">
              {errors.phoneNumber.message}
            </p>
          )}
        </div>

        <div className="grid gap-4 md:grid-cols-2">
          <div className="space-y-2">
            <Label htmlFor="password">Password</Label>
            <Input
              id="password"
              type="password"
              autoComplete="new-password"
              aria-invalid={Boolean(errors.password)}
              {...register("password", {
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
            {errors.password && (
              <p className="text-sm text-destructive">
                {errors.password.message}
              </p>
            )}
          </div>
          <div className="space-y-2">
            <Label htmlFor="confirmPassword">Confirm password</Label>
            <Input
              id="confirmPassword"
              type="password"
              autoComplete="new-password"
              aria-invalid={Boolean(errors.confirmPassword)}
              {...register("confirmPassword", {
                required: "Please confirm your password",
                validate: (value) =>
                  value === password || "Passwords do not match",
              })}
            />
            {errors.confirmPassword && (
              <p className="text-sm text-destructive">
                {errors.confirmPassword.message}
              </p>
            )}
          </div>
        </div>

        <Button className="w-full" type="submit" disabled={mutation.isPending}>
          {mutation.isPending ? "Creating account..." : "Create account"}
        </Button>
      </form>

      <p className="text-center text-sm text-muted-foreground">
        Already have an account?{" "}
        <Link to="/auth/login" className="text-primary">
          Sign in
        </Link>
      </p>
    </div>
  );
};
