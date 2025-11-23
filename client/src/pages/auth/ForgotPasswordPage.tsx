import { Link } from "react-router-dom";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { useMutation } from "@tanstack/react-query";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { authService } from "@/services/authService";
import type { components } from "@/types/api";

type Schemas = components["schemas"];
type ForgotPasswordFormValues = Schemas["ForgotPasswordDto"];

export const ForgotPasswordPage = () => {
  
  const [forgotState, setForgotState] = useState<{
    status: "idle" | "loading" | "success" | "error";
    message?: string;
  }>({ status: "idle" });

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<ForgotPasswordFormValues>({
    defaultValues: {
      email: "",
    },
  });

  const mutation = useMutation({
    
    mutationFn: (payload: ForgotPasswordFormValues) =>authService.forgotPassword(payload),
    
    onSuccess: (response) => {
      
      if (!response.success) {
        
        const message =
          response.errors?.join(", ") ??
          response.message ??
          "Unable to send reset link. Please try again.";
        
          setForgotState({ status: "error", message });
        return;
      }

      const successMessage =
        response.message ??
        "If your email is registered, you will receive a password reset link.";
      
        setForgotState({ status: "success", message: successMessage });
    },
    
    onError: (error) => {
      const message =
        error instanceof Error
          ? error.message
          : "Unexpected error. Please try again.";
      setForgotState({ status: "error", message });
    }
  });

  const onSubmit = handleSubmit((data) => {
    setForgotState({ status: "loading" });
    mutation.mutate(data);
  });

  return (
    <div className="space-y-6">
      <div className="space-y-1">
        <h2 className="text-2xl font-semibold">Forgot password</h2>
        <p className="text-sm text-muted-foreground">
          Enter your email address and we&apos;ll send you reset instructions
        </p>
      </div>

      {forgotState.status === "success" && (
        <div className="rounded-md border border-emerald-200 bg-emerald-50 px-3 py-2 text-sm text-emerald-700">
          {forgotState.message}
        </div>
      )}

      {forgotState.status === "error" && (
        <div className="rounded-md border border-destructive/50 bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {forgotState.message}
        </div>
      )}

      {forgotState.status === "loading" && (
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
            {...register("email", {
              required: "Email is required",
              pattern: { value: /^\S+@\S+$/i, message: "Invalid email format" },
            })}
          />
          {errors.email && (
            <p className="text-sm text-destructive">{errors.email.message}</p>
          )}
        </div>

        <Button className="w-full" type="submit" disabled={mutation.isPending}>
          {mutation.isPending ? "Sending..." : "Send reset link"}
        </Button>
      </form>

      <div className="text-center space-y-2">
        <p className="text-sm text-muted-foreground">
          Remembered your password?{" "}
          <Link to="/auth/login" className="text-primary">
            Candidate login
          </Link>{" "}
          or{" "}
          <Link to="/staff/login" className="text-primary">
            Staff login
          </Link>
        </p>
      </div>
    </div>
  );
};
