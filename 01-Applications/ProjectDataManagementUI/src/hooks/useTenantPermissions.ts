import { useAuth } from '../context/AuthContext';

/**
 * Hook do sprawdzania uprawnień użytkownika w aktywnym tenancie.
 * Uprawnienia bazują na fladze isActiveTenantAdmin zamiast permission codes.
 */
export function useTenantPermissions() {
  const { user } = useAuth();

  if (!user || !user.activeTenantId) {
    return {
      isAdmin: false,
      canView: false,
      canEdit: false,
      canManageMembers: false,
      canCreateProject: false,
    };
  }

  const isAdmin = user.isActiveTenantAdmin ?? false;

  return {
    // Czy user jest administratorem aktywnego tenanta
    isAdmin,

    // Wszyscy członkowie mogą przeglądać
    canView: true,

    // Tylko admin może edytować ustawienia tenanta
    canEdit: isAdmin,

    // Tylko admin może zarządzać członkami
    canManageMembers: isAdmin,

    // Tylko admin może tworzyć projekty
    canCreateProject: isAdmin,
  };
}
