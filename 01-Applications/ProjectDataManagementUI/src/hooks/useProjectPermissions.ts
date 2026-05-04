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
  const roleCode = projectDetails?.userRoleCode;
  const loading = isLoading;

  if (!projectId || !user || loading) {
    return {
      canView: false,
      canEdit: false,
      canViewMembers: false,
      canManageMembers: false,
      canManageStatus: false,
      canReadResources: false,
      canWriteResources: false,
      canReadSharedResources: false,
      canWriteSharedResources: false,
      canReadAllResources: false,
      canWriteAllResources: false,
      canShareResources: false,
      hasAnyResourceAccess: false,
      canReadMessages: false,
      canWriteMessages: false,
      canDeleteMessages: false,
      canListRoles: false,
      roleCode: undefined,
      allPermissions: [],
      loading,
    };
  }

  const hasAnyResourceAccess =
    hasPermission(permissions, PermissionCodes.PROJECT_RESOURCES_READ) ||
    hasPermission(permissions, PermissionCodes.PROJECT_RESOURCES_WRITE) ||
    hasPermission(permissions, PermissionCodes.PROJECT_RESOURCES_READ_SHARED) ||
    hasPermission(permissions, PermissionCodes.PROJECT_RESOURCES_WRITE_SHARED) ||
    hasPermission(permissions, PermissionCodes.PROJECT_RESOURCES_READ_ALL) ||
    hasPermission(permissions, PermissionCodes.PROJECT_RESOURCES_WRITE_ALL);

  return {
    canView: hasPermission(permissions, PermissionCodes.PROJECT_VIEW),
    canEdit: hasPermission(permissions, PermissionCodes.PROJECT_EDIT),
    canViewMembers: hasPermission(permissions, PermissionCodes.PROJECT_MEMBERS_VIEW),
    canManageMembers: hasPermission(permissions, PermissionCodes.PROJECT_MEMBERS_MANAGE),
    canManageStatus: hasPermission(permissions, PermissionCodes.PROJECT_STATUS_MANAGE),
    canReadResources: hasPermission(permissions, PermissionCodes.PROJECT_RESOURCES_READ),
    canWriteResources: hasPermission(permissions, PermissionCodes.PROJECT_RESOURCES_WRITE),
    canReadSharedResources: hasPermission(permissions, PermissionCodes.PROJECT_RESOURCES_READ_SHARED),
    canWriteSharedResources: hasPermission(permissions, PermissionCodes.PROJECT_RESOURCES_WRITE_SHARED),
    canReadAllResources: hasPermission(permissions, PermissionCodes.PROJECT_RESOURCES_READ_ALL),
    canWriteAllResources: hasPermission(permissions, PermissionCodes.PROJECT_RESOURCES_WRITE_ALL),
    canShareResources: hasPermission(permissions, PermissionCodes.PROJECT_RESOURCES_SHARE),
    hasAnyResourceAccess,
    canReadMessages: hasPermission(permissions, PermissionCodes.PROJECT_MESSAGES_READ),
    canWriteMessages: hasPermission(permissions, PermissionCodes.PROJECT_MESSAGES_WRITE),
    canDeleteMessages: hasPermission(permissions, PermissionCodes.PROJECT_MESSAGES_DELETE),
    canListRoles: hasPermission(permissions, PermissionCodes.ROLE_LIST),
    roleCode,
    allPermissions: permissions,
    loading,
  };
}
