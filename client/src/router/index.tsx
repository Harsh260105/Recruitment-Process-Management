// router for the application
import { createBrowserRouter } from "react-router-dom";
import { ExampleRecruitmentUI } from "../components/ExampleRecruitmentUI";
import { AuthLayout } from "@/layouts/AuthLayout";
import { LoginPage } from "@/pages/auth/LoginPage";
import { RegisterPage } from "@/pages/auth/RegisterPage";
import { ForgotPasswordPage } from "@/pages/auth/ForgotPasswordPage";
import { ResetPasswordPage } from "@/pages/auth/ResetPasswordPage";
import { StaffLoginPage } from "@/pages/auth/StaffLoginPage";
import { ConfirmEmailPage } from "@/pages/auth/ConfirmEmailPage";

export const router = createBrowserRouter([
  {
    path: "/",
    element: <ExampleRecruitmentUI />,
  },
  {
    path: "/auth",
    element: <AuthLayout />,
    children: [
      { path: "login", element: <LoginPage /> },
      { path: "register", element: <RegisterPage /> },
      { path: "forgot-password", element: <ForgotPasswordPage /> },
      { path: "reset-password", element: <ResetPasswordPage /> },
      { path: "confirm-email", element: <ConfirmEmailPage /> },
    ],
  },
  {
    path: "/staff",
    element: <AuthLayout />,
    children: [{ path: "login", element: <StaffLoginPage /> }],
  },
]);
