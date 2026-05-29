import { useProjectPermissions } from "./useProjectPermissions";

/**
 * Centralna logika uprawnień dla zasobów projektu (pliki, koszty, harmonogramy, kosztorysy)
 * 
 * ZASADY UPRAWNIEŃ:
 * 
 * ZAKŁADKI (każda zakładka = jedno uprawnienie READ, żadnych dodatkowych warunków):
 * - "Wszystkie" -> READ_ALL (WRITE_ALL tylko dla akcji edycji/usuwania)
 * - "Moje" -> READ (WRITE tylko dla akcji edycji/usuwania/tworzenia)
 * - "Udostępnione" -> READ_SHARED (WRITE_SHARED tylko dla akcji edycji)
 * 
 * PRZYCISKI AKCJI:
 * - Dodaj/Utwórz w "Moje" -> WRITE
 * - Edytuj/Usuń w "Moje" -> WRITE
 * - Dodaj/Utwórz w "Wszystkie" -> WRITE_ALL
 * - Edytuj/Usuń w "Wszystkie" -> WRITE_ALL
 * - Edytuj w "Udostępnione" -> WRITE_SHARED
 * - Udostępnij (grupowo/pojedynczo) -> SHARE
 */

export interface ResourcePermissions {
  tabs: {
    showAll: boolean;
    showMine: boolean;
    showShared: boolean;
    showPendingApproval: boolean;
  };
  mine: {
    canCreate: boolean;
    canEdit: boolean;
    canDelete: boolean;
    canShare: boolean;
    canManageShare: boolean;
  };
  all: {
    canCreate: boolean;
    canEdit: boolean;
    canDelete: boolean;
    canShare: boolean;
    canManageShare: boolean;
  };
  shared: {
    canEdit: boolean;
    canReadOnly: boolean;
  };
  hasAnyAccess: boolean;
  raw: any;
}

export const useResourcePermissions = (projectId: string | undefined): ResourcePermissions => {
  const permissions = useProjectPermissions(projectId);

  return {
    // ==================== ZAKŁADKI ====================
    tabs: {
      /** Zakładka "Wszystkie" - widoczna tylko dla admina projektu, TenantAdmin i SuperAdmin */
      showAll: permissions.canViewAllResources,
      
      /** Zakładka "Moje" - widoczna gdy user ma dostęp do plików */
      showMine: permissions.canViewFiles,
      
      /** Zakładka "Udostępnione" - widoczna gdy user ma dostęp do plików */
      showShared: permissions.canViewFiles,

      /** Zakładka "Do akceptacji" - widoczna tylko dla adminów projektu */
      showPendingApproval: permissions.canViewAllResources,
    },

    // ==================== AKCJE W "MOJE" ====================
    mine: {
      /** Czy user może dodawać nowe zasoby w zakładce "Moje" */
      canCreate: permissions.canViewFiles,
      
      /** Czy user może edytować zasoby w zakładce "Moje" */
      canEdit: permissions.canViewFiles,
      
      /** Czy user może usuwać zasoby w zakładce "Moje" */
      canDelete: permissions.canViewFiles,
      
      /** Czy user może udostępniać zasoby */
      canShare: permissions.canViewFiles,
      
      /** Czy user może zarządzać udostępnieniem */
      canManageShare: permissions.canViewFiles,
    },

    // ==================== AKCJE W "WSZYSTKIE" ====================
    all: {
      /** Czy user może dodawać nowe zasoby w zakładce "Wszystkie" */
      canCreate: permissions.canViewAllResources,
      
      /** Czy user może edytować zasoby w zakładce "Wszystkie" */
      canEdit: permissions.canViewAllResources,
      
      /** Czy user może usuwać zasoby w zakładce "Wszystkie" */
      canDelete: permissions.canViewAllResources,
      
      /** Czy user może udostępniać zasoby w zakładce "Wszystkie" */
      canShare: permissions.canViewAllResources,
      
      /** Czy user może zarządzać udostępnieniem w zakładce "Wszystkie" */
      canManageShare: permissions.canViewAllResources,
    },

    // ==================== AKCJE W "UDOSTĘPNIONE" ====================
    shared: {
      /** Czy user może edytować udostępnione zasoby */
      canEdit: permissions.canViewFiles,
      
      /** Czy user może tylko czytać udostępnione zasoby */
      canReadOnly: permissions.canViewFiles,
    },

    // ==================== OGÓLNE ====================
    /** Czy user ma jakikolwiek dostęp do zasobów */
    hasAnyAccess: permissions.hasAnyResourceAccess,
    
    /** Surowe uprawnienia z useProjectPermissions */
    raw: permissions,
  };
};
