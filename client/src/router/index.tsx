// router for the application
import { createBrowserRouter } from "react-router-dom";
import { lazy, Suspense } from "react";
import { AuthLayout } from "@/layouts/AuthLayout";
import { CandidateLayout } from "@/layouts/CandidateLayout";
import { StaffLayout } from "@/layouts/StaffLayout";
import { PrivateRoute } from "@/components/auth/PrivateRoute";
import { StaffRoute } from "@/components/auth/StaffRoute";

// Lazy load all pages
const LoginPage = lazy(() =>
  import("@/pages/auth/LoginPage").then((m) => ({ default: m.LoginPage }))
);
const RegisterPage = lazy(() =>
  import("@/pages/auth/RegisterPage").then((m) => ({ default: m.RegisterPage }))
);
const ForgotPasswordPage = lazy(() =>
  import("@/pages/auth/ForgotPasswordPage").then((m) => ({
    default: m.ForgotPasswordPage,
  }))
);
const ResetPasswordPage = lazy(() =>
  import("@/pages/auth/ResetPasswordPage").then((m) => ({
    default: m.ResetPasswordPage,
  }))
);
const StaffLoginPage = lazy(() =>
  import("@/pages/auth/StaffLoginPage").then((m) => ({
    default: m.StaffLoginPage,
  }))
);
const ConfirmEmailPage = lazy(() =>
  import("@/pages/auth/ConfirmEmailPage").then((m) => ({
    default: m.ConfirmEmailPage,
  }))
);

const CandidateDashboardPage = lazy(() =>
  import("@/pages/candidate/DashboardPage").then((m) => ({
    default: m.CandidateDashboardPage,
  }))
);
const CandidateProfilePage = lazy(() =>
  import("@/pages/candidate/ProfilePage").then((m) => ({
    default: m.CandidateProfilePage,
  }))
);
const CandidateSkillsPage = lazy(() =>
  import("@/pages/candidate/SkillsPage").then((m) => ({
    default: m.CandidateSkillsPage,
  }))
);
const CandidateEducationPage = lazy(() =>
  import("@/pages/candidate/EducationPage").then((m) => ({
    default: m.CandidateEducationPage,
  }))
);
const CandidateExperiencePage = lazy(() =>
  import("@/pages/candidate/ExperiencePage").then((m) => ({
    default: m.CandidateExperiencePage,
  }))
);
const CandidateApplicationsPage = lazy(() =>
  import("@/pages/candidate/ApplicationsPage").then((m) => ({
    default: m.CandidateApplicationsPage,
  }))
);
const ApplicationDetailPage = lazy(() =>
  import("@/pages/candidate/ApplicationDetailPage").then((m) => ({
    default: m.ApplicationDetailPage,
  }))
);
const CandidateInterviewsPage = lazy(() =>
  import("@/pages/candidate/InterviewsPage").then((m) => ({
    default: m.CandidateInterviewsPage,
  }))
);
const CandidateInterviewDetailPage = lazy(() =>
  import("@/pages/candidate/InterviewDetailPage").then((m) => ({
    default: m.CandidateInterviewDetailPage,
  }))
);
const CandidateOffersPage = lazy(() =>
  import("@/pages/candidate/OffersPage").then((m) => ({
    default: m.CandidateOffersPage,
  }))
);
const CandidateNotificationsPage = lazy(() =>
  import("@/pages/candidate/NotificationsPage").then((m) => ({
    default: m.CandidateNotificationsPage,
  }))
);
const CandidateJobListPage = lazy(() =>
  import("@/pages/candidate/JobListPage").then((m) => ({
    default: m.CandidateJobListPage,
  }))
);
const CandidateJobDetailPage = lazy(() =>
  import("@/pages/candidate/JobDetailPage").then((m) => ({
    default: m.CandidateJobDetailPage,
  }))
);

const RecruiterDashboardPage = lazy(() =>
  import("@/pages/recruiter/DashboardPage").then((m) => ({
    default: m.RecruiterDashboardPage,
  }))
);
const RecruiterApplicationsPage = lazy(() =>
  import("@/pages/recruiter/ApplicationsPage").then((m) => ({
    default: m.RecruiterApplicationsPage,
  }))
);
const RecruiterApplicationDetailPage = lazy(() =>
  import("@/pages/recruiter/ApplicationDetailPage").then((m) => ({
    default: m.RecruiterApplicationDetailPage,
  }))
);
const RecruiterCandidatesPage = lazy(() =>
  import("@/pages/recruiter/CandidatesPage").then((m) => ({
    default: m.RecruiterCandidatesPage,
  }))
);
const RecruiterInterviewsPage = lazy(() =>
  import("@/pages/recruiter/InterviewsPage").then((m) => ({
    default: m.RecruiterInterviewsPage,
  }))
);
const RecruiterInterviewDetailPage = lazy(() =>
  import("@/pages/recruiter/InterviewDetailPage").then((m) => ({
    default: m.RecruiterInterviewDetailPage,
  }))
);
const RecruiterProfilePage = lazy(() =>
  import("@/pages/recruiter/ProfilePage").then((m) => ({
    default: m.RecruiterProfilePage,
  }))
);

const RecruiterOffersPage = lazy(() =>
  import("@/pages/admin/OffersPage").then((m) => ({
    default: m.RecruiterOffersPage,
  }))
);
const RecruiterJobPositionsPage = lazy(() =>
  import("@/pages/admin/JobPositionsPage").then((m) => ({
    default: m.RecruiterJobPositionsPage,
  }))
);
const HRDashboardPage = lazy(() =>
  import("@/pages/admin/HRDashboardPage").then((m) => ({
    default: m.HRDashboardPage,
  }))
);
const StaffManagementPage = lazy(() =>
  import("@/pages/admin/StaffManagementPage").then((m) => ({
    default: m.StaffManagementPage,
  }))
);
const UserManagementPage = lazy(() =>
  import("@/pages/admin/UserManagementPage").then((m) => ({
    default: m.UserManagementPage,
  }))
);
const AnalyticsPage = lazy(() =>
  import("@/pages/admin/AnalyticsPage").then((m) => ({
    default: m.AnalyticsPage,
  }))
);

const AccountSettingsPage = lazy(() =>
  import("@/pages/common/AccountSettingsPage").then((m) => ({
    default: m.AccountSettingsPage,
  }))
);

// Loading component
const LoadingFallback = () => (
  <div className="flex items-center justify-center min-h-screen">
    <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
  </div>
);

export const router = createBrowserRouter([
  {
    path: "/auth",
    element: <AuthLayout />,
    children: [
      {
        path: "login",
        element: (
          <Suspense fallback={<LoadingFallback />}>
            <LoginPage />
          </Suspense>
        ),
      },
      {
        path: "register",
        element: (
          <Suspense fallback={<LoadingFallback />}>
            <RegisterPage />
          </Suspense>
        ),
      },
      {
        path: "forgot-password",
        element: (
          <Suspense fallback={<LoadingFallback />}>
            <ForgotPasswordPage />
          </Suspense>
        ),
      },
      {
        path: "reset-password",
        element: (
          <Suspense fallback={<LoadingFallback />}>
            <ResetPasswordPage />
          </Suspense>
        ),
      },
      {
        path: "confirm-email",
        element: (
          <Suspense fallback={<LoadingFallback />}>
            <ConfirmEmailPage />
          </Suspense>
        ),
      },
    ],
  },
  {
    path: "/staff",
    element: <AuthLayout />,
    children: [
      {
        path: "login",
        element: (
          <Suspense fallback={<LoadingFallback />}>
            <StaffLoginPage />
          </Suspense>
        ),
      },
    ],
  },
  {
    path: "/candidate",
    element: <PrivateRoute />,
    children: [
      {
        element: <CandidateLayout />,
        children: [
          {
            index: true,
            element: (
              <Suspense fallback={<LoadingFallback />}>
                <CandidateDashboardPage />
              </Suspense>
            ),
          },
          {
            path: "dashboard",
            element: (
              <Suspense fallback={<LoadingFallback />}>
                <CandidateDashboardPage />
              </Suspense>
            ),
          },
          {
            path: "profile",
            element: (
              <Suspense fallback={<LoadingFallback />}>
                <CandidateProfilePage />
              </Suspense>
            ),
          },
          {
            path: "jobs",
            element: (
              <Suspense fallback={<LoadingFallback />}>
                <CandidateJobListPage />
              </Suspense>
            ),
          },
          {
            path: "jobs/:jobId",
            element: (
              <Suspense fallback={<LoadingFallback />}>
                <CandidateJobDetailPage />
              </Suspense>
            ),
          },
          {
            path: "skills",
            element: (
              <Suspense fallback={<LoadingFallback />}>
                <CandidateSkillsPage />
              </Suspense>
            ),
          },
          {
            path: "education",
            element: (
              <Suspense fallback={<LoadingFallback />}>
                <CandidateEducationPage />
              </Suspense>
            ),
          },
          {
            path: "experience",
            element: (
              <Suspense fallback={<LoadingFallback />}>
                <CandidateExperiencePage />
              </Suspense>
            ),
          },
          {
            path: "applications",
            element: (
              <Suspense fallback={<LoadingFallback />}>
                <CandidateApplicationsPage />
              </Suspense>
            ),
          },
          {
            path: "applications/:id",
            element: (
              <Suspense fallback={<LoadingFallback />}>
                <ApplicationDetailPage />
              </Suspense>
            ),
          },
          {
            path: "interviews",
            element: (
              <Suspense fallback={<LoadingFallback />}>
                <CandidateInterviewsPage />
              </Suspense>
            ),
          },
          {
            path: "interviews/:id",
            element: (
              <Suspense fallback={<LoadingFallback />}>
                <CandidateInterviewDetailPage />
              </Suspense>
            ),
          },
          {
            path: "offers",
            element: (
              <Suspense fallback={<LoadingFallback />}>
                <CandidateOffersPage />
              </Suspense>
            ),
          },
          {
            path: "notifications",
            element: (
              <Suspense fallback={<LoadingFallback />}>
                <CandidateNotificationsPage />
              </Suspense>
            ),
          },
          {
            path: "account",
            element: (
              <Suspense fallback={<LoadingFallback />}>
                <AccountSettingsPage />
              </Suspense>
            ),
          },
        ],
      },
    ],
  },
  // Recruiter routes (core staff functionality)
  {
    path: "/recruiter",
    element: <StaffRoute />,
    children: [
      {
        element: <StaffLayout />,
        children: [
          {
            index: true,
            element: (
              <Suspense fallback={<LoadingFallback />}>
                <RecruiterDashboardPage />
              </Suspense>
            ),
          },
          {
            path: "dashboard",
            element: (
              <Suspense fallback={<LoadingFallback />}>
                <RecruiterDashboardPage />
              </Suspense>
            ),
          },
          {
            path: "profile",
            element: (
              <Suspense fallback={<LoadingFallback />}>
                <RecruiterProfilePage />
              </Suspense>
            ),
          },
          {
            path: "account",
            element: (
              <Suspense fallback={<LoadingFallback />}>
                <AccountSettingsPage />
              </Suspense>
            ),
          },
          {
            path: "applications",
            element: (
              <Suspense fallback={<LoadingFallback />}>
                <RecruiterApplicationsPage />
              </Suspense>
            ),
          },
          {
            path: "applications/:id",
            element: (
              <Suspense fallback={<LoadingFallback />}>
                <RecruiterApplicationDetailPage />
              </Suspense>
            ),
          },
          {
            path: "candidates",
            element: (
              <Suspense fallback={<LoadingFallback />}>
                <RecruiterCandidatesPage />
              </Suspense>
            ),
          },
          {
            path: "interviews",
            element: (
              <Suspense fallback={<LoadingFallback />}>
                <RecruiterInterviewsPage />
              </Suspense>
            ),
          },
          {
            path: "interviews/:id",
            element: (
              <Suspense fallback={<LoadingFallback />}>
                <RecruiterInterviewDetailPage />
              </Suspense>
            ),
          },
        ],
      },
    ],
  },
  // Admin routes (management functionality)
  {
    path: "/admin",
    element: (
      <StaffRoute
        allowedRoles={["HR", "Admin", "SuperAdmin"]}
        unauthorizedRedirect="/recruiter/dashboard"
      />
    ),
    children: [
      {
        element: <StaffLayout />,
        children: [
          {
            index: true,
            element: (
              <Suspense fallback={<LoadingFallback />}>
                <HRDashboardPage />
              </Suspense>
            ),
          },
          {
            path: "hr-dashboard",
            element: (
              <Suspense fallback={<LoadingFallback />}>
                <HRDashboardPage />
              </Suspense>
            ),
          },
          {
            path: "offers",
            element: (
              <Suspense fallback={<LoadingFallback />}>
                <RecruiterOffersPage />
              </Suspense>
            ),
          },
          {
            path: "jobs",
            element: (
              <Suspense fallback={<LoadingFallback />}>
                <RecruiterJobPositionsPage />
              </Suspense>
            ),
          },
          {
            path: "staff",
            element: (
              <Suspense fallback={<LoadingFallback />}>
                <StaffManagementPage />
              </Suspense>
            ),
          },
          {
            path: "users",
            element: (
              <Suspense fallback={<LoadingFallback />}>
                <StaffRoute allowedRoles={["Admin", "SuperAdmin"]}>
                  <UserManagementPage />
                </StaffRoute>
              </Suspense>
            ),
          },
          {
            path: "analytics",
            element: (
              <Suspense fallback={<LoadingFallback />}>
                <AnalyticsPage />
              </Suspense>
            ),
          },
        ],
      },
    ],
  },
]);
