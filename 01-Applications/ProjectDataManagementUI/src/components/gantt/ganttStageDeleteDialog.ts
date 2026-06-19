export interface StageDeleteDialogCopy {
  title: string;
  message: string;
  confirmText: string;
}

/** Teksty dialogu usuwania etapu / podetapu — spójne z kosztorysem. */
export function getStageDeleteDialogCopy(depth: number): StageDeleteDialogCopy {
  const isSubStage = depth > 0;
  const entity = isSubStage ? 'podetap' : 'etap';

  return {
    title: `Usuń ${entity}`,
    message: `Czy na pewno chcesz usunąć ten ${entity}? Wszystkie podetapy i zakresy pracy zostaną trwale usunięte. Tej operacji nie można cofnąć.`,
    confirmText: `Usuń ${entity}`,
  };
}
