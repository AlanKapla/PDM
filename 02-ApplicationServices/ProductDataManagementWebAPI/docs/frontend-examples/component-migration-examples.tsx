/**
 * EXAMPLE: Component Migration - ProjectDetails
 * Shows before/after for role-based to permission-based UI
 */

// ============================================================================
// ❌ BEFORE - Role-based approach (OLD)
// ============================================================================

import { ProjectRole, isProjectAdmin, canEditProject } from '../types/project.types';

export function ProjectDetailsOLD() {
  const { project } = useProjectContext();
  const { user } = useAuth();
  
  // ❌ OLD: Check roles
  const userIsProjectAdmin = project && isProjectAdmin(project.userRole);
  const userCanEdit = project && canEditProject(project.userRole);
  
  return (
    <Box>
      <Heading>{project.name}</Heading>
      
      {/* ❌ OLD: Role-based UI rendering */}
      <Badge colorScheme={getProjectRoleColor(project.userRole)}>
        {getProjectRoleName(project.userRole)}
      </Badge>
      
      {/* ❌ OLD: Role-based button visibility */}
      {userIsProjectAdmin && (
        <Button onClick={handleManageMembers}>
          Zarządzaj członkami
        </Button>
      )}
      
      {userCanEdit && (
        <Button onClick={handleUploadFile}>
          Prześlij plik
        </Button>
      )}
    </Box>
  );
}

// ============================================================================
// ✅ AFTER - Permission-based approach (NEW)
// ============================================================================

import { getRoleName, getRoleColor, hasPermission, PermissionCodes } from '../constants/roleCodes';
import type { ProjectDetailsWeb } from '../types/project.types';
import type { UserProfile } from '../types/auth.types';

export function ProjectDetailsNEW() {
  const { project } = useProjectContext();  // ProjectDetailsWeb
  const { user } = useAuth();  // UserProfile
  
  // ✅ NEW: Get permissions for this project
  const projectPermissions = user.projectPermissions[project.id] || [];
  
  // ✅ NEW: Check permissions instead of roles
  const canManageMembers = hasPermission(projectPermissions, PermissionCodes.PROJECT_MEMBERS_MANAGE);
  const canUploadFiles = hasPermission(projectPermissions, PermissionCodes.PROJECT_RESOURCES_WRITE);
  const canEditProject = hasPermission(projectPermissions, PermissionCodes.PROJECT_EDIT);
  
  return (
    <Box>
      <Heading>{project.name}</Heading>
      
      {/* ✅ NEW: Display role using roleCode */}
      <Badge colorScheme={getRoleColor(project.userRoleCode)}>
        {getRoleName(project.userRoleCode)}
      </Badge>
      
      {/* ✅ NEW: Permission-based button visibility */}
      {canManageMembers && (
        <Button onClick={handleManageMembers}>
          Zarządzaj członkami
        </Button>
      )}
      
      {canUploadFiles && (
        <Button onClick={handleUploadFile}>
          Prześlij plik
        </Button>
      )}
      
      {canEditProject && (
        <Button onClick={handleEditProject}>
          Edytuj projekt
        </Button>
      )}
    </Box>
  );
}

// ============================================================================
// EXAMPLE: TenantDetails Migration
// ============================================================================

// ❌ BEFORE
export function TenantDetailsOLD() {
  const { tenant } = useTenantContext();
  const { user } = useAuth();
  
  const userIsTenantAdmin = tenant && isTenantAdmin(tenant.role);
  
  return (
    <Box>
      <Heading>{tenant.name}</Heading>
      
      <Badge colorScheme={getTenantRoleColor(tenant.role)}>
        {getTenantRoleName(tenant.role)}
      </Badge>
      
      {userIsTenantAdmin && (
        <Button onClick={handleInviteMember}>
          Zaproś członka
        </Button>
      )}
    </Box>
  );
}

// ✅ AFTER
export function TenantDetailsNEW() {
  const { tenant } = useTenantContext();
  const { user } = useAuth();
  
  // ✅ NEW: Check tenant permissions
  const canManageMembers = hasPermission(
    user.activeTenantPermissions, 
    PermissionCodes.TENANT_MEMBERS_MANAGE
  );
  const canEditTenant = hasPermission(
    user.activeTenantPermissions, 
    PermissionCodes.TENANT_EDIT
  );
  
  return (
    <Box>
      <Heading>{tenant.name}</Heading>
      
      {/* ✅ NEW: Display role using roleCode */}
      <Badge colorScheme={getRoleColor(tenant.roleCode)}>
        {getRoleName(tenant.roleCode)}
      </Badge>
      
      {/* ✅ NEW: Permission-based buttons */}
      {canManageMembers && (
        <Button onClick={handleInviteMember}>
          Zaproś członka
        </Button>
      )}
      
      {canEditTenant && (
        <Button onClick={handleEditTenant}>
          Edytuj organizację
        </Button>
      )}
    </Box>
  );
}

// ============================================================================
// EXAMPLE: Member List Migration
// ============================================================================

// ❌ BEFORE
export function ProjectMembersOLD({ members }: { members: ProjectMemberWeb[] }) {
  return (
    <Table>
      <Tbody>
        {members.map(member => (
          <Tr key={member.userId}>
            <Td>{member.firstName} {member.lastName}</Td>
            <Td>{member.email}</Td>
            
            {/* ❌ OLD: Role number */}
            <Td>
              <Badge colorScheme={getProjectRoleColor(member.role)}>
                {getProjectRoleName(member.role)}
              </Badge>
            </Td>
          </Tr>
        ))}
      </Tbody>
    </Table>
  );
}

// ✅ AFTER
export function ProjectMembersNEW({ members }: { members: ProjectMemberWeb[] }) {
  return (
    <Table>
      <Tbody>
        {members.map(member => (
          <Tr key={member.userId}>
            <Td>{member.firstName} {member.lastName}</Td>
            <Td>{member.email}</Td>
            
            {/* ✅ NEW: Role code */}
            <Td>
              <Badge colorScheme={getRoleColor(member.roleCode)}>
                {getRoleName(member.roleCode)}
              </Badge>
            </Td>
          </Tr>
        ))}
      </Tbody>
    </Table>
  );
}

// ============================================================================
// EXAMPLE: Role Change Dropdown Migration
// ============================================================================

// ❌ BEFORE - Hardcoded enum values
export function RoleSelectOLD({ currentRole, onChange }: RoleSelectProps) {
  return (
    <Select value={currentRole} onChange={(e) => onChange(Number(e.target.value))}>
      <option value={ProjectRole.Admin}>Administrator</option>
      <option value={ProjectRole.Editor}>Edytor</option>
      <option value={ProjectRole.Viewer}>Przeglądający</option>
      <option value={ProjectRole.Member}>Członek</option>
    </Select>
  );
}

// ✅ AFTER - Fetch available roles from API
export function RoleSelectNEW({ 
  currentRoleId, 
  scope,
  onChange 
}: RoleSelectNewProps) {
  const { data: availableRoles } = useQuery({
    queryKey: ['roles', scope],
    queryFn: () => fetchAvailableRoles(scope),  // New API endpoint
  });
  
  return (
    <Select value={currentRoleId} onChange={(e) => onChange(e.target.value)}>
      {availableRoles?.map(role => (
        <option key={role.id} value={role.id}>
          {role.name}
        </option>
      ))}
    </Select>
  );
}

// Supporting API call
async function fetchAvailableRoles(scope: 'tenant' | 'project') {
  const response = await axiosClient.get(`/api/roles?scope=${scope}`);
  return response.data;  // Array of { id: string, code: string, name: string }
}

// ============================================================================
// EXAMPLE: Update Member Role API Call
// ============================================================================

// ❌ BEFORE - Send role number
async function updateProjectMemberRoleOLD(
  tenantId: string,
  projectId: string,
  userId: string,
  role: number  // ❌ Enum number
) {
  return axiosClient.patch(
    `/api/tenants/${tenantId}/projects/${projectId}/members/${userId}/role`,
    { role }
  );
}

// ✅ AFTER - Send roleId (Guid)
async function updateProjectMemberRoleNEW(
  tenantId: string,
  projectId: string,
  userId: string,
  roleId: string  // ✅ Role Guid from database
) {
  return axiosClient.patch(
    `/api/tenants/${tenantId}/projects/${projectId}/members/${userId}/role`,
    { roleId }
  );
}

// ============================================================================
// EXAMPLE: Custom Hook for Permissions
// ============================================================================

/**
 * Custom hook to check project permissions
 */
export function useProjectPermissions(projectId: string) {
  const { user } = useAuth();
  const permissions = user.projectPermissions[projectId] || [];
  
  return {
    canView: hasPermission(permissions, PermissionCodes.PROJECT_VIEW),
    canEdit: hasPermission(permissions, PermissionCodes.PROJECT_EDIT),
    canManageMembers: hasPermission(permissions, PermissionCodes.PROJECT_MEMBERS_MANAGE),
    canUploadFiles: hasPermission(permissions, PermissionCodes.PROJECT_RESOURCES_WRITE),
    canViewSharedFiles: hasPermission(permissions, PermissionCodes.PROJECT_RESOURCES_READ_SHARED),
    canEditSharedFiles: hasPermission(permissions, PermissionCodes.PROJECT_RESOURCES_WRITE_SHARED),
    allPermissions: permissions,
  };
}

// Usage
export function ProjectFilesPage() {
  const { projectId } = useParams();
  const permissions = useProjectPermissions(projectId!);
  
  return (
    <Box>
      {permissions.canUploadFiles && (
        <Button onClick={handleUpload}>Prześlij plik</Button>
      )}
      
      {permissions.canViewSharedFiles && (
        <SharedFilesList />
      )}
    </Box>
  );
}

/**
 * Custom hook to check tenant permissions
 */
export function useTenantPermissions() {
  const { user } = useAuth();
  const permissions = user.activeTenantPermissions || [];
  
  return {
    canView: hasPermission(permissions, PermissionCodes.TENANT_VIEW),
    canEdit: hasPermission(permissions, PermissionCodes.TENANT_EDIT),
    canManageMembers: hasPermission(permissions, PermissionCodes.TENANT_MEMBERS_MANAGE),
    canCreateProjects: hasPermission(permissions, PermissionCodes.TENANT_PROJECT_CREATE),
    allPermissions: permissions,
  };
}
