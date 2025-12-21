// router for the application
import { createBrowserRouter } from "react-router-dom";
import { AuthLayout } from "@/layouts/AuthLayout";
import { CandidateLayout } from "@/layouts/CandidateLayout";
import { StaffLayout } from "@/layouts/StaffLayout";
import { PrivateRoute } from "@/components/auth/PrivateRoute";
import { StaffRoute } from "@/components/auth/StaffRoute";
import { LoginPage } from "@/pages/auth/LoginPage";
import { RegisterPage } from "@/pages/auth/RegisterPage";
import { ForgotPasswordPage } from "@/pages/auth/ForgotPasswordPage";
import { ResetPasswordPage } from "@/pages/auth/ResetPasswordPage";
import { StaffLoginPage } from "@/pages/auth/StaffLoginPage";
import { ConfirmEmailPage } from "@/pages/auth/ConfirmEmailPage";
import { CandidateDashboardPage } from "@/pages/candidate/DashboardPage";
import { CandidateProfilePage } from "@/pages/candidate/ProfilePage";
import { CandidateSkillsPage } from "@/pages/candidate/SkillsPage";
import { CandidateEducationPage } from "@/pages/candidate/EducationPage";
import { CandidateExperiencePage } from "@/pages/candidate/ExperiencePage";
import { CandidateApplicationsPage } from "@/pages/candidate/ApplicationsPage";
import { ApplicationDetailPage } from "@/pages/candidate/ApplicationDetailPage";
import { CandidateInterviewsPage } from "@/pages/candidate/InterviewsPage";
import { CandidateInterviewDetailPage } from "@/pages/candidate/InterviewDetailPage";
import { CandidateOffersPage } from "@/pages/candidate/OffersPage";
import { CandidateNotificationsPage } from "@/pages/candidate/NotificationsPage";
import { CandidateJobListPage } from "@/pages/candidate/JobListPage";
import { CandidateJobDetailPage } from "@/pages/candidate/JobDetailPage";
import { RecruiterDashboardPage } from "@/pages/recruiter/DashboardPage";
import { RecruiterApplicationsPage } from "@/pages/recruiter/ApplicationsPage";
import { RecruiterApplicationDetailPage } from "@/pages/recruiter/ApplicationDetailPage";
import { RecruiterCandidatesPage } from "@/pages/recruiter/CandidatesPage";
import { RecruiterInterviewsPage } from "@/pages/recruiter/InterviewsPage";
import { RecruiterInterviewDetailPage } from "@/pages/recruiter/InterviewDetailPage";
import { RecruiterProfilePage } from "@/pages/recruiter/ProfilePage";
import { RecruiterOffersPage } from "@/pages/admin/OffersPage";
import { RecruiterJobPositionsPage } from "@/pages/admin/JobPositionsPage";
import { HRDashboardPage } from "@/pages/admin/HRDashboardPage";
import { StaffManagementPage } from "@/pages/admin/StaffManagementPage";
import { UserManagementPage } from "@/pages/admin/UserManagementPage";
import { AnalyticsPage } from "@/pages/admin/AnalyticsPage";
import { AccountSettingsPage } from "@/pages/common/AccountSettingsPage";

export const router = createBrowserRouter([
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
  {
    path: "/candidate",
    element: <PrivateRoute />,
    children: [
      {
        element: <CandidateLayout />,
        children: [
          { index: true, element: <CandidateDashboardPage /> },
          { path: "dashboard", element: <CandidateDashboardPage /> },
          { path: "profile", element: <CandidateProfilePage /> },
          { path: "jobs", element: <CandidateJobListPage /> },
          { path: "jobs/:jobId", element: <CandidateJobDetailPage /> },
          { path: "skills", element: <CandidateSkillsPage /> },
          { path: "education", element: <CandidateEducationPage /> },
          { path: "experience", element: <CandidateExperiencePage /> },
          { path: "applications", element: <CandidateApplicationsPage /> },
          { path: "applications/:id", element: <ApplicationDetailPage /> },
          { path: "interviews", element: <CandidateInterviewsPage /> },
          { path: "interviews/:id", element: <CandidateInterviewDetailPage /> },
          { path: "offers", element: <CandidateOffersPage /> },
          { path: "notifications", element: <CandidateNotificationsPage /> },
          { path: "account", element: <AccountSettingsPage /> },
        ],
      },
    ],
  },
  {
    path: "/recruiter",
    element: <StaffRoute />,
    children: [
      {
        element: <StaffLayout />,
        children: [
          { index: true, element: <RecruiterDashboardPage /> },
          { path: "dashboard", element: <RecruiterDashboardPage /> },
          { path: "profile", element: <RecruiterProfilePage /> },
          { path: "account", element: <AccountSettingsPage /> },
          {
            path: "applications",
            element: (
              <StaffRoute
                allowedRoles={["Recruiter", "HR", "Admin", "SuperAdmin"]}
                unauthorizedRedirect="/recruiter/dashboard"
              >
                <RecruiterApplicationsPage />
              </StaffRoute>
            ),
          },
          {
            path: "applications/:id",
            element: (
              <StaffRoute
                allowedRoles={["Recruiter", "HR", "Admin", "SuperAdmin"]}
                unauthorizedRedirect="/recruiter/dashboard"
              >
                <RecruiterApplicationDetailPage />
              </StaffRoute>
            ),
          },
          {
            path: "candidates",
            element: (
              <StaffRoute
                allowedRoles={["Recruiter", "HR", "Admin", "SuperAdmin"]}
                unauthorizedRedirect="/recruiter/dashboard"
              >
                <RecruiterCandidatesPage />
              </StaffRoute>
            ),
          },
          {
            path: "interviews",
            element: (
              <StaffRoute
                allowedRoles={["Recruiter", "HR", "Admin", "SuperAdmin"]}
                unauthorizedRedirect="/recruiter/dashboard"
              >
                <RecruiterInterviewsPage />
              </StaffRoute>
            ),
          },
          {
            path: "interviews/:id",
            element: (
              <StaffRoute
                allowedRoles={["Recruiter", "HR", "Admin", "SuperAdmin"]}
                unauthorizedRedirect="/recruiter/dashboard"
              >
                <RecruiterInterviewDetailPage />
              </StaffRoute>
            ),
          },
          {
            path: "offers",
            element: (
              <StaffRoute
                allowedRoles={["HR", "Admin", "SuperAdmin"]}
                unauthorizedRedirect="/recruiter/dashboard"
              >
                <RecruiterOffersPage />
              </StaffRoute>
            ),
          },
          {
            path: "jobs",
            element: (
              <StaffRoute
                allowedRoles={["HR", "Admin", "SuperAdmin"]}
                unauthorizedRedirect="/recruiter/dashboard"
              >
                <RecruiterJobPositionsPage />
              </StaffRoute>
            ),
          },
          {
            path: "hr-dashboard",
            element: (
              <StaffRoute
                allowedRoles={["HR", "Admin", "SuperAdmin"]}
                unauthorizedRedirect="/recruiter/dashboard"
              >
                <HRDashboardPage />
              </StaffRoute>
            ),
          },
          {
            path: "staff",
            element: (
              <StaffRoute
                allowedRoles={["HR", "Admin", "SuperAdmin"]}
                unauthorizedRedirect="/recruiter/dashboard"
              >
                <StaffManagementPage />
              </StaffRoute>
            ),
          },
          {
            path: "users",
            element: (
              <StaffRoute
                allowedRoles={["Admin", "SuperAdmin"]}
                unauthorizedRedirect="/recruiter/dashboard"
              >
                <UserManagementPage />
              </StaffRoute>
            ),
          },
          {
            path: "analytics",
            element: (
              <StaffRoute
                allowedRoles={["HR", "Admin", "SuperAdmin"]}
                unauthorizedRedirect="/recruiter/dashboard"
              >
                <AnalyticsPage />
              </StaffRoute>
            ),
          },
        ],
      },
    ],
  },
]);
