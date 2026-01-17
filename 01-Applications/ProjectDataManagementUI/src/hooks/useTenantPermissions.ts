import { useAuth } from '../context/AuthContext';
import { hasPermission, PermissionCodes } from '../constants/roleCodes';

/**
 * Hook do wygodnego sprawdzania uprawnień użytkownika w aktywnym tenancie
 * 
 * @returns Obiekt z bool flagami dla każdego uprawnienia
 */
export function useTenantPermissions() {
  const { user } = useAuth();
  
  if (!user || !user.activeTenantId) {
    return {
      canView: false,
      canEdit: false,
      canManageMembers: false,
      canCreateProject: false,
      canManageStatus: false,
      allPermissions: [],
    };
  }
  
  const permissions = user.activeTenantPermissions || [];
  
  return {
    // Tenant basic permissions
    canView: hasPermission(permissions, PermissionCodes.TENANT_VIEW),
    canEdit: hasPermission(permissions, PermissionCodes.TENANT_EDIT),
    
    // Tenant members permissions
    canManageMembers: hasPermission(permissions, PermissionCodes.TENANT_MEMBERS_MANAGE),
    
    // Tenant project permissions
    canCreateProject: hasPermission(permissions, PermissionCodes.TENANT_PROJECT_CREATE),
    
    // Tenant status
    canManageStatus: hasPermission(permissions, PermissionCodes.TENANT_STATUS_MANAGE),
    
    // Raw permissions
    allPermissions: permissions,
  };
}
