import { Link } from "react-router-dom";
import { useForm } from "react-hook-form";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useForgotPassword } from "@/hooks/auth";
import type { components } from "@/types/api";

type Schemas = components["schemas"];
type ForgotPasswordFormValues = Schemas["ForgotPasswordDto"];

export const ForgotPasswordPage = () => {
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<ForgotPasswordFormValues>({
    defaultValues: {
      email: "",
    },
  });

  const forgotPassword = useForgotPassword();

  const onSubmit = handleSubmit((data) => {
    forgotPassword.mutate(data);
  });

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

      {forgotPassword.isError && (
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

        <Button
          className="w-full"
          type="submit"
          disabled={forgotPassword.isPending}
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
          <Link to="/staff/login" className="text-primary">
            Staff login
          </Link>
        </p>
      </div>
    </div>
  );
};
