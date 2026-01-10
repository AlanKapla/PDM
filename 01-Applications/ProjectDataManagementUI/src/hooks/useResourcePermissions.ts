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
      /** Zakładka "Wszystkie" - widoczna gdy user ma READ_ALL */
      showAll: permissions.canReadAllResources,
      
      /** Zakładka "Moje" - widoczna gdy user ma READ */
      showMine: permissions.canReadResources,
      
      /** Zakładka "Udostępnione" - widoczna gdy user ma READ_SHARED */
      showShared: permissions.canReadSharedResources,
    },

    // ==================== AKCJE W "MOJE" ====================
    mine: {
      /** Czy user może dodawać nowe zasoby w zakładce "Moje" - wymaga WRITE */
      canCreate: permissions.canWriteResources,
      
      /** Czy user może edytować zasoby w zakładce "Moje" - wymaga WRITE */
      canEdit: permissions.canWriteResources,
      
      /** Czy user może usuwać zasoby w zakładce "Moje" - wymaga WRITE */
      canDelete: permissions.canWriteResources,
      
      /** Czy user może udostępniać zasoby (grupowo i pojedynczo) - wymaga SHARE */
      canShare: permissions.canShareResources,
      
      /** Czy user może zarządzać udostępnieniem (pojedynczy zasób) - wymaga SHARE */
      canManageShare: permissions.canShareResources,
    },

    // ==================== AKCJE W "WSZYSTKIE" ====================
    all: {
      /** Czy user może dodawać nowe zasoby w zakładce "Wszystkie" - wymaga WRITE_ALL */
      canCreate: permissions.canWriteAllResources,
      
      /** Czy user może edytować zasoby w zakładce "Wszystkie" - wymaga WRITE_ALL */
      canEdit: permissions.canWriteAllResources,
      
      /** Czy user może usuwać zasoby w zakładce "Wszystkie" - wymaga WRITE_ALL */
      canDelete: permissions.canWriteAllResources,
      
      /** Czy user może udostępniać zasoby w zakładce "Wszystkie" - wymaga SHARE */
      canShare: permissions.canShareResources,
      
      /** Czy user może zarządzać udostępnieniem w zakładce "Wszystkie" - wymaga SHARE */
      canManageShare: permissions.canShareResources,
    },

    // ==================== AKCJE W "UDOSTĘPNIONE" ====================
    shared: {
      /** Czy user może edytować udostępnione zasoby - wymaga WRITE_SHARED */
      canEdit: permissions.canWriteSharedResources,
      
      /** Czy user może tylko czytać udostępnione zasoby */
      canReadOnly: permissions.canReadSharedResources && !permissions.canWriteSharedResources,
    },

    // ==================== OGÓLNE ====================
    /** Czy user ma jakikolwiek dostęp do zasobów */
    hasAnyAccess: permissions.hasAnyResourceAccess,
    
    /** Surowe uprawnienia z useProjectPermissions */
    raw: permissions,
  };
};
