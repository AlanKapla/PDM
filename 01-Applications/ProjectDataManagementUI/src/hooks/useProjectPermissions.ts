import { useAuth } from '../context/AuthContext';
import { useProjectDetails } from './queries';
import { hasPermission, PermissionCodes } from '../constants/roleCodes';

/**
 * Hook do wygodnego sprawdzania uprawnień użytkownika w projekcie.
 * Wykorzystuje useProjectDetails (React Query) — cache współdzielony
 * z innymi miejscami w aplikacji.
 *
 * @param projectId - ID projektu do sprawdzenia uprawnień
 * @returns Obiekt z bool flagami dla każdego uprawnienia
 */
export function useProjectPermissions(projectId: string | undefined) {
  const { user } = useAuth();
  const { data: projectDetails, isLoading } = useProjectDetails(
    user?.activeTenantId ?? undefined,
    projectId
  );

  const permissions = projectDetails?.userPermissions ?? [];
  const isAdmin = projectDetails?.isAdmin ?? false;
  const canViewAllResources = projectDetails?.canViewAllResources ?? false;
  const loading = isLoading;

  if (!projectId || !user || loading) {
    return {
      canView: false,
      canEdit: false,
      canManageStatus: false,
      canDashboardTracker: false,
      canViewFiles: false,
      canViewEstimates: false,
      canViewCosts: false,
      canViewSchedule: false,
      hasAnyResourceAccess: false,
      isAdmin: false,
      canViewAllResources: false,
      allPermissions: [],
      loading,
    };
  }

  return {
    // Settings
    canView: canViewAllResources || hasPermission(permissions, PermissionCodes.ProjectSettings),
    canEdit: canViewAllResources || hasPermission(permissions, PermissionCodes.ProjectSettings),
    canManageStatus: canViewAllResources || hasPermission(permissions, PermissionCodes.ProjectSettings),

    // Dashboard & Tracker
    canDashboardTracker: canViewAllResources || hasPermission(permissions, PermissionCodes.ProjectDashboardTracker),

    // Files
    canViewFiles: canViewAllResources || hasPermission(permissions, PermissionCodes.ProjectFiles),

    // Estimates
    canViewEstimates: canViewAllResources || hasPermission(permissions, PermissionCodes.ProjectEstimates),

    // Costs
    canViewCosts: canViewAllResources || hasPermission(permissions, PermissionCodes.ProjectCosts),

    // Schedule
    canViewSchedule: canViewAllResources || hasPermission(permissions, PermissionCodes.ProjectSchedule),

    // Derived
    hasAnyResourceAccess:
      canViewAllResources ||
      hasPermission(permissions, PermissionCodes.ProjectFiles) ||
      hasPermission(permissions, PermissionCodes.ProjectEstimates) ||
      hasPermission(permissions, PermissionCodes.ProjectCosts) ||
      hasPermission(permissions, PermissionCodes.ProjectSchedule),

    isAdmin,
    canViewAllResources,
    allPermissions: permissions,
    loading,
  };
}
