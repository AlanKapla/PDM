import { Routes, Route, Navigate } from "react-router-dom";
import Home from "../pages/Home";
import Login from "../pages/Login";
import Dashboard from "../pages/Dashboard";
import ProtectedRoute from "./ProtectedRoute";
import PublicRoute from "./PublicRoute";
import Profile from "../pages/Profile";
import Tenants from "../pages/Tenants";
import CollaboratingTenants from "../pages/CollaboratingTenants";
import ManagedTenants from "../pages/ManagedTenants";
import ActiveInvitations from "../pages/ActiveInvitations";
import ForgotPassword from "../pages/ForgotPassword";
import ResetPassword from "../pages/ResetPassword";
import ActivateAccount from "../pages/ActivateAccount";
import AuthCallback from "../pages/AuthCallback";
import LoggedOut from "../pages/LoggedOut";
import Projects from "../pages/Projects";
import ProjectDetails from "../pages/ProjectDetails";
import MyFiles from "../pages/MyFiles";
import SharedFiles from "../pages/SharedFiles";
import WorkScheduleView from "../pages/WorkScheduleView";
import AssignedWorks from "../pages/AssignedWorks";
import CostEstimateTemplates from "../pages/CostEstimateTemplates";
import ProjectMembers from "../pages/ProjectMembers";
import ProjectSchedules from "../pages/ProjectSchedules";
import ProjectFiles from "../pages/ProjectFiles";
import ProjectCosts from "../pages/ProjectCosts";
import ProjectSimpleCosts from "../pages/ProjectSimpleCosts";
import { CostEstimateEditor } from "../pages/CostEstimateEditor";

export default function AppRouter() {
  return (
    <Routes>
      {/* OAuth callback route - handles redirect from Azure External ID */}
      <Route path="/auth/callback" element={<AuthCallback />} />

      {/* Post-logout page - handles redirect after MSAL logout */}
      <Route path="/logged-out" element={<LoggedOut />} />

      {/* Public pages */}
      <Route
        path="/login"
        element={
          <PublicRoute>
            <Login />
          </PublicRoute>
        }
      />

      {/* /register redirects to /login - MSAL handles both flows */}
      <Route path="/register" element={<Navigate to="/login" replace />} />

      <Route
        path="/forgot-password"
        element={
          <PublicRoute>
            <ForgotPassword />
          </PublicRoute>
        }
      />

      <Route
        path="/reset-password"
        element={
          <PublicRoute>
            <ResetPassword />
          </PublicRoute>
        }
      />

      <Route
        path="/activate"
        element={
          <PublicRoute>
            <ActivateAccount />
          </PublicRoute>
        }
      />

      {/* 🔥 Swagger — publiczny, bez autoryzacji */}
      <Route path="/swagger" element={<div />} />

      {/* Home page - public */}
      <Route
        path="/"
        element={
          <PublicRoute>
            <Home />
          </PublicRoute>
        }
      />

      {/* Protected pages */}
      <Route
        path="/dashboard"
        element={
          <ProtectedRoute>
            <Dashboard />
          </ProtectedRoute>
        }
      />

      <Route
        path="/profile"
        element={
          <ProtectedRoute>
            <Profile />
          </ProtectedRoute>
        }
      />

      <Route
        path="/tenants"
        element={
          <ProtectedRoute>
            <Tenants />
          </ProtectedRoute>
        }
      />

      <Route
        path="/tenants/invitations"
        element={
          <ProtectedRoute>
            <ActiveInvitations />
          </ProtectedRoute>
        }
      />

      <Route
        path="/tenants/collaborating"
        element={
          <ProtectedRoute>
            <CollaboratingTenants />
          </ProtectedRoute>
        }
      />

      <Route
        path="/tenants/managed"
        element={
          <ProtectedRoute>
            <ManagedTenants />
          </ProtectedRoute>
        }
      />

      <Route
        path="/projects"
        element={
          <ProtectedRoute>
            <Projects />
          </ProtectedRoute>
        }
      />

      <Route
        path="/projects/:projectId"
        element={
          <ProtectedRoute>
            <ProjectDetails />
          </ProtectedRoute>
        }
      />

      <Route
        path="/projects/:projectId/schedules/:workScheduleId"
        element={
          <ProtectedRoute>
            <WorkScheduleView />
          </ProtectedRoute>
        }
      />

      <Route
        path="/cost-estimate-templates"
        element={
          <ProtectedRoute>
            <CostEstimateTemplates />
          </ProtectedRoute>
        }
      />

      <Route
        path="/projects/:projectId/costs"
        element={
          <ProtectedRoute>
            <ProjectSimpleCosts />
          </ProtectedRoute>
        }
      />

      <Route
        path="/projects/:projectId/cost-estimates"
        element={
          <ProtectedRoute>
            <ProjectCosts />
          </ProtectedRoute>
        }
      />

      <Route
        path="/projects/:projectId/members"
        element={
          <ProtectedRoute>
            <ProjectMembers />
          </ProtectedRoute>
        }
      />

      <Route
        path="/projects/:projectId/schedules"
        element={
          <ProtectedRoute>
            <ProjectSchedules />
          </ProtectedRoute>
        }
      />

      <Route
        path="/projects/:projectId/files"
        element={
          <ProtectedRoute>
            <ProjectFiles />
          </ProtectedRoute>
        }
      />

      <Route
        path="/projects/:projectId/cost-estimates/:estimateId"
        element={
          <ProtectedRoute>
            <CostEstimateEditor />
          </ProtectedRoute>
        }
      />

      <Route
        path="/tenants/:tenantId/projects/:projectId/my-files"
        element={
          <ProtectedRoute>
            <MyFiles />
          </ProtectedRoute>
        }
      />

      <Route
        path="/tenants/:tenantId/projects/:projectId/shared-files"
        element={
          <ProtectedRoute>
            <SharedFiles />
          </ProtectedRoute>
        }
      />

      <Route
        path="/assigned-works"
        element={
          <ProtectedRoute>
            <AssignedWorks />
          </ProtectedRoute>
        }
      />

      {/* Catch-all */}
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
