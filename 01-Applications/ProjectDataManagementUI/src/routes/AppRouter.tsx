import { Routes, Route, Navigate } from "react-router-dom";
import Home from "../pages/Home";
import Dashboard from "../pages/Dashboard";
import ProtectedRoute from "./ProtectedRoute";
import PublicRoute from "./PublicRoute";
import Profile from "../pages/Profile";
import TenantDetails from "../pages/TenantDetails";
import CollaboratingTenants from "../pages/CollaboratingTenants";
import ManagedTenants from "../pages/ManagedTenants";
import ActiveInvitations from "../pages/ActiveInvitations";
import AuthCallback from "../pages/AuthCallback";
import LoggedOut from "../pages/LoggedOut";
import Projects from "../pages/Projects";
import ProjectDetails from "../pages/ProjectDetails";
import WorkScheduleView from "../pages/WorkScheduleView";
import AssignedWorks from "../pages/AssignedWorks";
import CostEstimateTemplates from "../pages/CostEstimateTemplates";
// TemplateVersionHistory removed - versioning no longer supported
import CostEstimateTemplateEditor from "../pages/CostEstimateTemplateEditor";
import CostEstimateTemplateNew from "../pages/CostEstimateTemplateNew";
import CostEstimateTemplateSelector from "../pages/CostEstimateTemplateSelector";
import ProjectMembers from "../pages/ProjectMembers";
import ProjectSchedules from "../pages/ProjectSchedules";
import ProjectFiles from "../pages/ProjectFiles";
import ProjectParameters from "../pages/ProjectParameters";
import ProjectCosts from "../pages/ProjectCosts";
import ProjectSimpleCosts from "../pages/ProjectSimpleCosts";
import { CostEstimateEditPage } from "../pages/CostEstimateEditPage";
import ChatPage from "../pages/ChatPage";
import ProjectBudgetPage from "../pages/ProjectBudgetPage";

export default function AppRouter() {
  return (
    <Routes>
      {/* OAuth callback route - handles redirect from Azure External ID */}
      <Route path="/auth/callback" element={<AuthCallback />} />

      {/* Post-logout page - handles redirect after MSAL logout */}
      <Route
        path="/logged-out"
        element={
          <PublicRoute>
            <LoggedOut />
          </PublicRoute>
        }
      />

      
      {/* /register redirects to /home - MSAL handles both flows */}
      <Route path="/register" element={<Navigate to="/" replace />} />

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
        path="/tenants/:tenantId"
        element={
          <ProtectedRoute>
            <TenantDetails />
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
        path="/cost-estimate-templates/new"
        element={
          <ProtectedRoute>
            <CostEstimateTemplateNew />
          </ProtectedRoute>
        }
      />

      <Route
        path="/cost-estimate-templates/select"
        element={
          <ProtectedRoute>
            <CostEstimateTemplateSelector />
          </ProtectedRoute>
        }
      />

      <Route
        path="/cost-estimate-templates/:templateId/edit"
        element={
          <ProtectedRoute>
            <CostEstimateTemplateEditor />
          </ProtectedRoute>
        }
      />

      {/* Template version history removed - versioning no longer supported */}

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
        path="/projects/:projectId/parameters"
        element={
          <ProtectedRoute>
            <ProjectParameters />
          </ProtectedRoute>
        }
      />

      <Route
        path="/projects/:projectId/cost-estimates/:estimateId"
        element={
          <ProtectedRoute>
            <CostEstimateEditPage />
          </ProtectedRoute>
        }
      />

      <Route
        path="/projects/:projectId/dashboard"
        element={
          <ProtectedRoute>
            <ProjectBudgetPage />
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

      <Route
        path="/chat"
        element={
          <ProtectedRoute>
            <ChatPage />
          </ProtectedRoute>
        }
      />

      <Route
        path="/chat/:chatId"
        element={
          <ProtectedRoute>
            <ChatPage />
          </ProtectedRoute>
        }
      />

      {/* Catch-all */}
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
