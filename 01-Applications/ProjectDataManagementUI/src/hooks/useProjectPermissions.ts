import { useState, useEffect } from 'react';
import { useAuth } from '../context/AuthContext';
import { useProjectCache } from '../context/ProjectCacheContext';
import { hasPermission, PermissionCodes } from '../constants/roleCodes';

/**
 * Hook do wygodnego sprawdzania uprawnień użytkownika w projekcie
 * Fetches project details to get user's permissions for the specific project
 * Uses ProjectCacheContext for caching to avoid redundant API calls
 * 
 * @param projectId - ID projektu do sprawdzenia uprawnień
 * @returns Obiekt z bool flagami dla każdego uprawnienia
 */
export function useProjectPermissions(projectId: string | undefined) {
  const { user } = useAuth();
  const { getProjectDetails } = useProjectCache();
  const [permissions, setPermissions] = useState<string[]>([]);
  const [roleCode, setRoleCode] = useState<string | undefined>(undefined);
  const [loading, setLoading] = useState(true);
  
  useEffect(() => {
    const fetchProjectPermissions = async () => {
      if (!projectId || !user?.activeTenantId || !user?.id) {
        setPermissions([]);
        setRoleCode(undefined);
        setLoading(false);
        return;
      }

      try {
        setLoading(true);
        const projectDetails = await getProjectDetails(user.activeTenantId, projectId, user.id);
        setPermissions(projectDetails.userPermissions || []);
        setRoleCode(projectDetails.userRoleCode);
      } catch (error) {
        console.error('Error fetching project permissions:', error);
        setPermissions([]);
        setRoleCode(undefined);
      } finally {
        setLoading(false);
      }
    };

    fetchProjectPermissions();
  }, [projectId, user?.activeTenantId, user?.id, getProjectDetails]);
  
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
    
    // Project resources - wszystkie (only ProjectAdmin)
    canReadAllResources: hasPermission(permissions, PermissionCodes.PROJECT_RESOURCES_READ_ALL),
    canWriteAllResources: hasPermission(permissions, PermissionCodes.PROJECT_RESOURCES_WRITE_ALL),
    
    // Project resources - sharing
    canShareResources: hasPermission(permissions, PermissionCodes.PROJECT_RESOURCES_SHARE),
    
    // Combined - has ANY access to resources (own or shared or all, read or write)
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
    loading,
  };
}
