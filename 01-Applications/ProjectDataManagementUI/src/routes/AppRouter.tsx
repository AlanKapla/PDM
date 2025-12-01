import { Routes, Route, Navigate } from "react-router-dom";
import Home from "../pages/Home";
import Login from "../pages/Login";
import Register from "../pages/Register";
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
import AcceptInvitation from "../pages/AcceptInvitation";
import Projects from "../pages/Projects";
import ProjectDetails from "../pages/ProjectDetails";

export default function AppRouter() {
  return (
    <Routes>
      {/* Public pages */}
      <Route
        path="/login"
        element={
          <PublicRoute>
            <Login />
          </PublicRoute>
        }
      />

      <Route
        path="/register"
        element={
          <PublicRoute>
            <Register />
          </PublicRoute>
        }
      />

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

      <Route
        path="/tenants/invitations/accept"
        element={
          <ProtectedRoute>
            <AcceptInvitation />
          </ProtectedRoute>
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

      {/* Catch-all */}
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
