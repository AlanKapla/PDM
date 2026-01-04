import { useAuth } from '../context/AuthContext';
import { hasPermission, PermissionCodes } from '../constants/roleCodes';

/**
 * Hook do wygodnego sprawdzania uprawnień użytkownika w projekcie
 * 
 * @param projectId - ID projektu do sprawdzenia uprawnień
 * @returns Obiekt z bool flagami dla każdego uprawnienia
 */
export function useProjectPermissions(projectId: string | undefined) {
  const { user } = useAuth();
  
  if (!projectId || !user) {
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
      hasAnyResourceAccess: false,
      canReadMessages: false,
      canWriteMessages: false,
      canDeleteMessages: false,
      canListRoles: false,
      roleCode: undefined,
      allPermissions: [],
    };
  }
  
  const permissions = user.projectPermissions?.[projectId] || [];
  const roleCode = user.projectRoleCodes?.[projectId];
  
  const hasAnyResourceAccess = 
    hasPermission(permissions, PermissionCodes.PROJECT_RESOURCES_READ) ||
    hasPermission(permissions, PermissionCodes.PROJECT_RESOURCES_WRITE) ||
    hasPermission(permissions, PermissionCodes.PROJECT_RESOURCES_READ_SHARED) ||
    hasPermission(permissions, PermissionCodes.PROJECT_RESOURCES_WRITE_SHARED);
  
  return {
    // Project basic permissions
    canView: hasPermission(permissions, PermissionCodes.PROJECT_VIEW),
    canEdit: hasPermission(permissions, PermissionCodes.PROJECT_EDIT),
    
    // Project members permissions
    canViewMembers: hasPermission(permissions, PermissionCodes.PROJECT_MEMBERS_VIEW),
    canManageMembers: hasPermission(permissions, PermissionCodes.PROJECT_MEMBERS_MANAGE),
    
    // Project status
    canManageStatus: hasPermission(permissions, PermissionCodes.PROJECT_STATUS_MANAGE),
    
    // Project resources (files, costs, schedules, estimates) - własne
    canReadResources: hasPermission(permissions, PermissionCodes.PROJECT_RESOURCES_READ),
    canWriteResources: hasPermission(permissions, PermissionCodes.PROJECT_RESOURCES_WRITE),
    
    // Project resources - udostępnione (shared)
    canReadSharedResources: hasPermission(permissions, PermissionCodes.PROJECT_RESOURCES_READ_SHARED),
    canWriteSharedResources: hasPermission(permissions, PermissionCodes.PROJECT_RESOURCES_WRITE_SHARED),
    
    // Combined - has ANY access to resources (own or shared, read or write)
    hasAnyResourceAccess,
    
    // Project messages/chat permissions
    canReadMessages: hasPermission(permissions, PermissionCodes.PROJECT_MESSAGES_READ),
    canWriteMessages: hasPermission(permissions, PermissionCodes.PROJECT_MESSAGES_WRITE),
    canDeleteMessages: hasPermission(permissions, PermissionCodes.PROJECT_MESSAGES_DELETE),
    
    // Role management
    canListRoles: hasPermission(permissions, PermissionCodes.ROLE_LIST),
    
    // Role and raw permissions
    roleCode,
    allPermissions: permissions,
  };
}
