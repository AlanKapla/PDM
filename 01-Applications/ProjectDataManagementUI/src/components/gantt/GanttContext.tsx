import React, { createContext, useContext, useState, useCallback, useRef, useMemo, useEffect } from "react";
import { Box, Center, VStack, Spinner, Text, Alert, AlertIcon, Button } from "@chakra-ui/react";
import type { WorkScheduleDetailsWeb, WorkScheduleStageWeb, WorkScheduleStageWorkWeb, WorkScheduleStageWorkPeriodWeb, WorkScheduleWorkDependencyWeb } from "../../types/workSchedule.types";
import type { ProjectMemberWeb } from "../../types/project.types";
import { workScheduleApi } from "../../api/workScheduleApi";
import { projectApi } from "../../api/projectApi";
import { handleApiError } from "../../utils/handleApiError";
import { useToastNotification } from "../../hooks/useToastNotification";
import type { ResourcePermissions } from "../../hooks/useResourcePermissions";
import { AuthContext } from "../../context/AuthContext";
import { collectExpandableStageIdsForSearch } from "./ganttRowUtils";

// ─── Typy pomocnicze ──────────────────────────────────────────────────────────

export interface GanttMember {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
}

export type GanttMode = "view" | "edit";

// ─── Uprawnienia Gantt ────────────────────────────────────────────────────────

export interface GanttPermissions {
  canEditNames: boolean;
  canAddStages: boolean;
  canDeleteStages: boolean;
  canReorderStages: boolean;
  canAddWorks: boolean;
  canDeleteWorks: boolean;
  canReorderWorks: boolean;
  canEditPeriods: boolean;
  canToggleWorkClosed: boolean;
  canTogglePeriodClosed: boolean;
  canAddComments: boolean;
  canEditOwnComments: boolean;
  canDeleteOwnComments: boolean;
  canManageAssignees: boolean;
  canManageDependencies: boolean;
  canChangeColor: boolean;
}

export const GANTT_PERMISSIONS = {
  full: {
    canEditNames: true, canAddStages: true, canDeleteStages: true, canReorderStages: true,
    canAddWorks: true, canDeleteWorks: true, canReorderWorks: true, canEditPeriods: true,
    canToggleWorkClosed: true, canTogglePeriodClosed: true,
    canAddComments: true, canEditOwnComments: true, canDeleteOwnComments: true,
    canManageAssignees: true, canManageDependencies: true, canChangeColor: true,
  } as GanttPermissions,
  readonly: {
    canEditNames: false, canAddStages: false, canDeleteStages: false, canReorderStages: false,
    canAddWorks: false, canDeleteWorks: false, canReorderWorks: false, canEditPeriods: false,
    canToggleWorkClosed: false, canTogglePeriodClosed: false,
    canAddComments: false, canEditOwnComments: false, canDeleteOwnComments: false,
    canManageAssignees: false, canManageDependencies: false, canChangeColor: false,
  } as GanttPermissions,
  "my-works": {
    canEditNames: false, canAddStages: false, canDeleteStages: false, canReorderStages: false,
    canAddWorks: false, canDeleteWorks: false, canReorderWorks: false, canEditPeriods: false,
    canToggleWorkClosed: true, canTogglePeriodClosed: true,
    canAddComments: true, canEditOwnComments: true, canDeleteOwnComments: true,
    canManageAssignees: false, canManageDependencies: false, canChangeColor: false,
  } as GanttPermissions,
} as const;

/** Aktywny modal mobile */
export type MobileModal =
  | { type: "stageForm"; stageId?: string }
  | { type: "renameStage"; stageId: string; initialName: string }
  | { type: "stagesOrder" }
  | { type: "moveStage"; stageId: string }
  | { type: "workForm"; stageId: string }
  | { type: "editWork"; stageId: string; work: WorkScheduleStageWorkWeb }
  | { type: "worksOrder"; stageId: string }
  | { type: "moveWork"; stageId: string; workId: string }
  | { type: "periods"; stageId: string; work: WorkScheduleStageWorkWeb }
  | { type: "assignments"; stageId: string; work: WorkScheduleStageWorkWeb }
  | { type: "comments"; stageId: string; work: WorkScheduleStageWorkWeb }
  | { type: "dependencies" }
  | null;

// ─── Interfejs kontekstu ──────────────────────────────────────────────────────

interface GanttContextValue {
  // Dane
  schedule: WorkScheduleDetailsWeb | null;
  members: GanttMember[];
  isLoading: boolean;
  isMutating: Set<string>; // klucze mutujących się elementów – do spinnerów

  // Uprawnienia i tryb
  mode: GanttMode;
  canEdit: boolean;
  permissions: ResourcePermissions | null;
  ganttPermissions: GanttPermissions;

  // Identyfikatory routingu
  tenantId: string;
  projectId: string;
  workScheduleId: string;

  // UI State
  expandedStages: Set<string>;
  collapsedWorks: Set<string>;
  showComments: boolean;
  showDependencies: boolean;
  mobileModal: MobileModal;
  searchQuery: string;
  setSearchQuery: (query: string) => void;

  // Operacje ładowania
  fetchSchedule: () => Promise<void>;
  refreshSchedule: (data: WorkScheduleDetailsWeb) => void;

  // UI akcje
  setMode: (mode: GanttMode) => void;
  toggleStage: (stageId: string) => void;
  expandAll: (stages: WorkScheduleStageWeb[]) => void;
  collapseAll: () => void;
  toggleWorkPeriods: (workId: string) => void;
  setShowComments: (v: boolean) => void;
  setShowDependencies: (v: boolean) => void;
  openMobileModal: (modal: MobileModal) => void;
  closeMobileModal: () => void;

  // Mutacje — granularne
  renameSchedule: (name: string) => Promise<void>;
  addStage: (name: string, parentStageId?: string | null) => Promise<void>;
  deleteStage: (stageId: string) => Promise<void>;
  renameStage: (stageId: string, name: string) => Promise<void>;
  reorderStages: (orderedStageIds: string[]) => Promise<void>;
  moveStage: (stageId: string, parentStageId: string | null) => Promise<void>;
  addWork: (stageId: string, name: string, colorRgb: string) => Promise<void>;
  deleteWork: (stageId: string, workId: string) => Promise<void>;
  renameWork: (stageId: string, workId: string, name: string) => Promise<void>;
  reorderWorks: (stageId: string, orderedWorkIds: string[]) => Promise<void>;
  moveWork: (stageId: string, workId: string, targetStageId: string, targetOrder: number) => Promise<void>;
  setPeriods: (stageId: string, workId: string, periods: Array<{ startDate: string; endDate: string; isClosed: boolean }>) => Promise<void>;
  setWorkColor: (stageId: string, workId: string, colorRgb: string) => Promise<void>;
  setWorkIsClosed: (stageId: string, workId: string, isClosed: boolean) => Promise<void>;
  setPeriodIsClosed: (stageId: string, workId: string, periodId: string, isClosed: boolean) => Promise<void>;
  setAssignments: (stageId: string, workId: string, userIds: string[]) => Promise<void>;
  addComment: (stageId: string, workId: string, content: string) => Promise<void>;
  updateComment: (stageId: string, workId: string, commentId: string, content: string) => Promise<void>;
  deleteComment: (stageId: string, workId: string, commentId: string) => Promise<void>;
  setDependencies: (dependencies: WorkScheduleWorkDependencyWeb[]) => Promise<WorkScheduleDetailsWeb>;
  syncWithEstimate: () => Promise<void>;
}

// ─── Kontekst ─────────────────────────────────────────────────────────────────

const GanttContext = createContext<GanttContextValue | null>(null);

export function useGantt(): GanttContextValue {
  const ctx = useContext(GanttContext);
  if (!ctx) throw new Error("useGantt must be used within GanttProvider");
  return ctx;
}

// ─── Helpery ──────────────────────────────────────────────────────────────────

function collectStageIds(stages: WorkScheduleStageWeb[]): string[] {
  return stages.flatMap(s => [s.id, ...collectStageIds(s.childStages ?? [])]);
}

function findStageForWork(stages: WorkScheduleStageWeb[], workId: string): string | null {
  for (const s of stages) {
    if ((s.works ?? []).some(w => w.id === workId)) return s.id;
    const found = findStageForWork(s.childStages ?? [], workId);
    if (found) return found;
  }
  return null;
}

// ─── Provider ─────────────────────────────────────────────────────────────────

interface GanttProviderProps {
  tenantId: string;
  projectId: string;
  workScheduleId: string;
  permissions: ResourcePermissions;
  onAfterInitialLoad?: () => void;
  /** Gdy podane — pomiń fetch i użyj tych danych jako stanu początkowego */
  preloadedSchedule?: WorkScheduleDetailsWeb;
  /** Uprawnienia Gantt — domyślnie GANTT_PERMISSIONS.full */
  ganttPermissions?: GanttPermissions;
  searchQuery?: string;
  onSearchChange?: (query: string) => void;
  children: React.ReactNode;
}

export function GanttProvider({
  tenantId, projectId, workScheduleId,
  permissions,
  onAfterInitialLoad,
  preloadedSchedule,
  ganttPermissions: ganttPermissionsFromProps,
  searchQuery: searchQueryFromProps = '',
  onSearchChange,
  children,
}: GanttProviderProps) {
  const { showError } = useToastNotification();
  const { user } = useContext(AuthContext);
  const isPreloaded = !!preloadedSchedule;
  const resolvedGanttPermissions: GanttPermissions = ganttPermissionsFromProps ?? GANTT_PERMISSIONS.full;

  const [schedule, setSchedule] = useState<WorkScheduleDetailsWeb | null>(
    preloadedSchedule ?? null
  );
  // Ref synchronizowany z schedule — pozwala callbackom mutacji nie mieć schedule w deps
  const scheduleRef = useRef<WorkScheduleDetailsWeb | null>(null);
  scheduleRef.current = schedule;
  const [isLoading, setIsLoading] = useState(!isPreloaded);
  const [initialError, setInitialError] = useState<string | null>(null);
  const [members, setMembers] = useState<GanttMember[]>([]);
  const [isMutating, setIsMutating] = useState<Set<string>>(new Set());
  const canEditPermission = permissions?.mine.canEdit || permissions?.all.canEdit || permissions?.shared.canEdit;
  // W trybie "my-works" (preloaded) wymuszamy tryb view — edycja jest kontrolowana przez ganttPermissions
  const [mode, setModeState] = useState<GanttMode>("view");
  // Flaga zapobiega ponownemu auto-przełączeniu trybu po tym, jak użytkownik ręcznie go zmienił
  const autoSwitchedToEdit = useRef(false);
  // Gdy permissions się załadują i użytkownik ma prawo edycji, jednorazowo przełącz na tryb edit
  useEffect(() => {
    if (!isPreloaded && canEditPermission && !autoSwitchedToEdit.current) {
      autoSwitchedToEdit.current = true;
      setModeState("edit");
    }
  }, [isPreloaded, canEditPermission]);
  const [expandedStages, setExpandedStages] = useState<Set<string>>(
    isPreloaded ? new Set(collectStageIds(preloadedSchedule?.stages ?? [])) : new Set()
  );
  const [collapsedWorks, setCollapsedWorks] = useState<Set<string>>(new Set());
  const [showComments, setShowComments] = useState(true);
  const [showDependencies, setShowDependencies] = useState(true);
  const [mobileModal, setMobileModal] = useState<MobileModal>(null);
  const [internalSearchQuery, setInternalSearchQuery] = useState('');
  const isSearchControlled = onSearchChange !== undefined;
  const searchQuery = isSearchControlled ? searchQueryFromProps : internalSearchQuery;
  const setSearchQuery = isSearchControlled ? onSearchChange : setInternalSearchQuery;

  const canEdit = permissions?.mine.canEdit || permissions?.all.canEdit || permissions?.shared.canEdit;

  // Ref do callbacku po pierwszym załadowaniu — stabilna referencja bez potrzeby deps w useEffect
  const onAfterInitialLoadRef = useRef(onAfterInitialLoad);
  onAfterInitialLoadRef.current = onAfterInitialLoad;
  const isFirstLoad = useRef(true);

  // ─── Klucze mutacji ─────────────────────────────────────────────────────────
  const startMutation = (key: string) => setIsMutating(prev => new Set([...prev, key]));
  const endMutation = (key: string) => setIsMutating(prev => { const s = new Set(prev); s.delete(key); return s; });

  // Stan oczekujących wywołań API z debounce — grupuje szybkie zmiany (wpisywanie, color picker, drag)
  const pendingDebounces = useRef(new Map<string, {
    timer: ReturnType<typeof setTimeout>;
    rollbackState: WorkScheduleDetailsWeb;
  }>());

  /**
   * Odracza wywołanie API o `delay` ms (debounce per klucz). Optimistic update
   * powinien być zastosowany przez wywołującego przed tym wywołaniem.
   * Stan rollback zachowywany z pierwszego wywołania w serii — kolejne resety nie nadpisują.
   */
  const runDebounced = useCallback((
    key: string,
    delay: number,
    apiFn: () => Promise<unknown>,
    rollbackState: WorkScheduleDetailsWeb,
    options: { onSuccess?: () => Promise<void> } = {}
  ) => {
    const existing = pendingDebounces.current.get(key);
    const savedRollback = existing ? existing.rollbackState : rollbackState;
    if (existing) clearTimeout(existing.timer);

    const timer = setTimeout(async () => {
      pendingDebounces.current.delete(key);
      setIsMutating(prev => new Set([...prev, key]));
      try {
        await apiFn();
        await options.onSuccess?.();
      } catch (err) {
        set(savedRollback);
        const { title, description } = handleApiError(err);
        showError(title, description);
      } finally {
        setIsMutating(prev => { const s = new Set(prev); s.delete(key); return s; });
      }
    }, delay);

    pendingDebounces.current.set(key, { timer, rollbackState: savedRollback });
  }, [showError]);

  // ─── Fetch / refresh ────────────────────────────────────────────────────────
  const fetchSchedule = useCallback(async () => {
    setIsLoading(true);
    try {
      const res = await workScheduleApi.getDetails(tenantId, projectId, workScheduleId);
      set(res.data);
      if (isFirstLoad.current) {
        isFirstLoad.current = false;
        setInitialError(null);
        setExpandedStages(new Set(collectStageIds(res.data.stages ?? [])));
        setTimeout(() => onAfterInitialLoadRef.current?.(), 150);
      }
    } catch (err) {
      const { title, description } = handleApiError(err);
      showError(title, description);
      if (isFirstLoad.current) {
        setInitialError(`${title}: ${description}`);
      }
    } finally {
      setIsLoading(false);
    }
  }, [tenantId, projectId, workScheduleId, showError]);

  // ─── Inicjalne ładowanie danych ──────────────────────────────────────────────
  useEffect(() => {
    // Gdy dane przekazane z zewnątrz — pomijamy fetch, rozwijamy etapy jednorazowo
    if (isPreloaded) {
      setTimeout(() => onAfterInitialLoadRef.current?.(), 150);
      return;
    }
    if (!tenantId || !projectId || !workScheduleId) return;
    fetchSchedule();
    projectApi.getProjectMembers(tenantId, projectId)
      .then(res => {
        const raw: ProjectMemberWeb[] = res.data ?? [];
        setMembers(raw.map(m => ({
          userId: m.userId,
          email: m.email,
          firstName: m.firstName,
          lastName: m.lastName,
        })));
      })
      .catch(() => { /* Błąd pobierania uczestników nie blokuje renderowania */ });
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [tenantId, projectId, workScheduleId]);

  // Gdy zmienią się dane preloadedSchedule z zewnątrz — aktualizujemy stan
  useEffect(() => {
    if (!isPreloaded || !preloadedSchedule) return;
    set(preloadedSchedule);
    setExpandedStages(new Set(collectStageIds(preloadedSchedule.stages ?? [])));
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [preloadedSchedule]);

  // Ciche odświeżenie w tle — nie ustawia isLoading, nie powoduje wyświetlenia szkieletu ani resetu scrolla.
  // W trybie preloaded jest no-op — dane zarządza komponent zewnętrzny.
  const silentRefresh = useCallback(async () => {
    if (isPreloaded) return;
    try {
      const res = await workScheduleApi.getDetails(tenantId, projectId, workScheduleId);
      set(res.data);
    } catch {
      // Ignorujemy błąd w tle — UI już pokazuje poprawny stan z optimistic update
    }
  }, [isPreloaded, tenantId, projectId, workScheduleId]);

  const refreshSchedule = useCallback((data: WorkScheduleDetailsWeb) => {
    set(data);
  }, []);

  // ─── UI akcje ───────────────────────────────────────────────────────────────
  const setMode = useCallback((m: GanttMode) => setModeState(m), []);

  const toggleStage = useCallback((stageId: string) => {
    setExpandedStages(prev => {
      const s = new Set(prev);
      s.has(stageId) ? s.delete(stageId) : s.add(stageId);
      return s;
    });
  }, []);

  const expandAll = useCallback((stages: WorkScheduleStageWeb[]) => {
    setExpandedStages(new Set(collectStageIds(stages)));
  }, []);

  const collapseAll = useCallback(() => setExpandedStages(new Set()), []);

  const expandStages = useCallback((stageIds: Array<string | null | undefined>) => {
    const ids = stageIds.filter((id): id is string => Boolean(id));
    if (ids.length === 0) {
      return;
    }
    setExpandedStages((prev) => new Set([...prev, ...ids]));
  }, []);

  useEffect(() => {
    if (!searchQuery.trim() || !schedule?.stages) {
      return;
    }
    const idsToExpand = collectExpandableStageIdsForSearch(schedule.stages, searchQuery);
    if (idsToExpand.length === 0) {
      return;
    }
    setExpandedStages((prev) => new Set([...prev, ...idsToExpand]));
  }, [searchQuery, schedule?.stages]);

  const toggleWorkPeriods = useCallback((workId: string) => {
    setCollapsedWorks(prev => {
      const s = new Set(prev);
      s.has(workId) ? s.delete(workId) : s.add(workId);
      return s;
    });
  }, []);

  const openMobileModal = useCallback((modal: MobileModal) => setMobileModal(modal), []);
  const closeMobileModal = useCallback(() => setMobileModal(null), []);

  // ─── Mutacje ─────────────────────────────────────────────────────────────────
  // Mutacje wywoływane wyłącznie gdy schedule !== null — alias dla czytelności bez zmiany logiki
  const set = setSchedule as React.Dispatch<React.SetStateAction<WorkScheduleDetailsWeb>>;

  const renameSchedule = useCallback(async (name: string) => {
    set(s => ({ ...s!, name }));
    runDebounced("renameSchedule", 600,
      () => workScheduleApi.renameSchedule(tenantId, projectId, workScheduleId, name),
      scheduleRef.current!);
  }, [tenantId, projectId, workScheduleId, runDebounced]);

  const addStage = useCallback(async (name: string, parentStageId?: string | null) => {
    const prev = scheduleRef.current!;
    const tempId = `temp-${Date.now()}`;
    const order = parentStageId
      ? (findStageInTree(scheduleRef.current!.stages, parentStageId)?.childStages ?? []).length
      : scheduleRef.current!.stages.length;
    const newStage: WorkScheduleStageWeb = { id: tempId, name, order, works: [], childStages: [] };
    set(s => ({
      ...s,
      stages: parentStageId
        ? updateStageInTree(s.stages, parentStageId, st => ({ ...st, childStages: [...(st.childStages ?? []), newStage] }))
        : [...s.stages, newStage],
    }));
    expandStages([tempId, parentStageId]);
    runDebounced(`addStage-${tempId}`, 50,
      async () => {
        const res = await workScheduleApi.addStage(tenantId, projectId, workScheduleId, { name, order, parentStageId: parentStageId ?? null });
        setExpandedStages((prev) => {
          const next = new Set(prev);
          next.delete(tempId);
          next.add(res.data);
          return next;
        });
        return res;
      },
      prev,
      { onSuccess: silentRefresh });
  }, [tenantId, projectId, workScheduleId, silentRefresh, runDebounced, expandStages]);

  const deleteStage = useCallback(async (stageId: string) => {
    const prev = scheduleRef.current!;
    set(s => ({ ...s!, stages: removeStageFromTree(s.stages, stageId) }));
    runDebounced(`deleteStage-${stageId}`, 200,
      () => workScheduleApi.deleteStage(tenantId, projectId, workScheduleId, stageId),
      prev);
  }, [tenantId, projectId, workScheduleId, runDebounced]);

  const renameStage = useCallback(async (stageId: string, name: string) => {
    set(s => ({ ...s!, stages: updateStageInTree(s.stages, stageId, st => ({ ...st, name })) }));
    runDebounced(`renameStage-${stageId}`, 600,
      () => workScheduleApi.renameStage(tenantId, projectId, workScheduleId, stageId, name),
      scheduleRef.current!);
  }, [tenantId, projectId, workScheduleId, runDebounced]);

  const reorderStages = useCallback(async (orderedStageIds: string[]) => {
    const prev = scheduleRef.current!;
    set(s => ({ ...s!, stages: reorderStagesInTree(s.stages, orderedStageIds) }));
    runDebounced("reorderStages", 300,
      () => workScheduleApi.reorderStages(tenantId, projectId, workScheduleId, orderedStageIds),
      prev);
  }, [tenantId, projectId, workScheduleId, runDebounced]);

  const moveStage = useCallback(async (stageId: string, parentStageId: string | null) => {
    const prev = scheduleRef.current!;
    runDebounced(`moveStage-${stageId}`, 50,
      () => workScheduleApi.moveStage(tenantId, projectId, workScheduleId, stageId, parentStageId),
      prev,
      { onSuccess: silentRefresh });
  }, [tenantId, projectId, workScheduleId, silentRefresh, runDebounced]);

  const addWork = useCallback(async (stageId: string, name: string, colorRgb: string) => {
    const prev = scheduleRef.current!;
    const stage = findStageInTree(scheduleRef.current!.stages, stageId);
    const order = stage ? (stage.works ?? []).length : 0;
    const tempWork: WorkScheduleStageWorkWeb = {
      id: `temp-${Date.now()}`, name, order, colorRgb,
      isClosed: false, periods: [], assignees: [], comments: [],
    };
    set(s => ({
      ...s,
      stages: updateStageInTree(s.stages, stageId, st => ({ ...st, works: [...st.works, tempWork] })),
    }));
    expandStages([stageId]);
    runDebounced(`addWork-${tempWork.id}`, 50,
      () => workScheduleApi.addWork(tenantId, projectId, workScheduleId, stageId, { name, order, colorRgb }),
      prev,
      { onSuccess: silentRefresh });
  }, [tenantId, projectId, workScheduleId, silentRefresh, runDebounced, expandStages]);

  const deleteWork = useCallback(async (stageId: string, workId: string) => {
    const prev = scheduleRef.current!;
    set(s => ({
      ...s,
      stages: updateStageInTree(s.stages, stageId, st => ({ ...st, works: st.works.filter(w => w.id !== workId) })),
    }));
    runDebounced(`deleteWork-${workId}`, 200,
      () => workScheduleApi.deleteWork(tenantId, projectId, workScheduleId, stageId, workId),
      prev);
  }, [tenantId, projectId, workScheduleId, runDebounced]);

  const renameWork = useCallback(async (stageId: string, workId: string, name: string) => {
    set(s => ({ ...s!, stages: updateWorkInTree(s.stages, workId, w => ({ ...w, name })) }));
    runDebounced(`renameWork-${workId}`, 600,
      () => workScheduleApi.renameWork(tenantId, projectId, workScheduleId, stageId, workId, name),
      scheduleRef.current!);
  }, [tenantId, projectId, workScheduleId, runDebounced]);

  const reorderWorks = useCallback(async (stageId: string, orderedWorkIds: string[]) => {
    const prev = scheduleRef.current!;
    set(s => ({
      ...s,
      stages: updateStageInTree(s.stages, stageId, st => {
        const workMap = new Map(st.works.map(w => [w.id, w]));
        return {
          ...st,
          works: orderedWorkIds
            .map((id, i) => workMap.has(id) ? { ...workMap.get(id)!, order: i } : null)
            .filter((w): w is WorkScheduleStageWorkWeb => w !== null),
        };
      }),
    }));
    runDebounced(`reorderWorks-${stageId}`, 300,
      () => workScheduleApi.reorderWorks(tenantId, projectId, workScheduleId, stageId, orderedWorkIds),
      prev);
  }, [tenantId, projectId, workScheduleId, runDebounced]);

  const moveWork = useCallback(async (stageId: string, workId: string, targetStageId: string, targetOrder: number) => {
    const prev = scheduleRef.current!;
    const movingWork = findStageInTree(scheduleRef.current!.stages, stageId)?.works.find(w => w.id === workId);
    if (movingWork) {
      set(s => ({ ...s!, stages: moveWorkInTree(s.stages, workId, stageId, targetStageId, targetOrder, movingWork) }));
    }
    runDebounced(`moveWork-${workId}`, 50,
      () => workScheduleApi.moveWork(tenantId, projectId, workScheduleId, stageId, workId, targetStageId, targetOrder),
      prev,
      { onSuccess: silentRefresh });
  }, [tenantId, projectId, workScheduleId, silentRefresh, runDebounced]);

  const setPeriods = useCallback(async (stageId: string, workId: string, periods: Array<{ startDate: string; endDate: string; isClosed: boolean }>) => {
    const prev = scheduleRef.current!;
    const sanitized = periods.map(p => ({ startDate: p.startDate, endDate: p.endDate, isClosed: p.isClosed }));
    set(s => ({
      ...s,
      stages: updateWorkInTree(s.stages, workId, w => ({ ...w, periods: periods as any })),
    }));
    runDebounced(`setPeriods-${workId}`, 300,
      () => workScheduleApi.setPeriods(tenantId, projectId, workScheduleId, stageId, workId, sanitized),
      prev,
      { onSuccess: silentRefresh });
  }, [tenantId, projectId, workScheduleId, silentRefresh, runDebounced]);

  const setWorkColor = useCallback(async (stageId: string, workId: string, colorRgb: string) => {
    set(s => ({ ...s!, stages: updateWorkInTree(s.stages, workId, w => ({ ...w, colorRgb })) }));
    runDebounced(`setWorkColor-${workId}`, 500,
      () => workScheduleApi.setWorkColor(tenantId, projectId, workScheduleId, stageId, workId, colorRgb),
      scheduleRef.current!);
  }, [tenantId, projectId, workScheduleId, runDebounced]);

  const setWorkIsClosed = useCallback(async (stageId: string, workId: string, isClosed: boolean) => {
    const prev = scheduleRef.current!;
    set(s => ({
      ...s,
      stages: updateWorkInTree(s.stages, workId, w => ({
        ...w,
        isClosed,
        periods: w.periods.map(p => ({ ...p, isClosed })),
      })),
    }));
    runDebounced(`setWorkIsClosed-${workId}`, 200,
      () => workScheduleApi.setWorkIsClosed(tenantId, projectId, workScheduleId, stageId, workId, isClosed),
      prev);
  }, [tenantId, projectId, workScheduleId, runDebounced]);

  const setPeriodIsClosed = useCallback(async (stageId: string, workId: string, periodId: string, isClosed: boolean) => {
    const prev = scheduleRef.current!;
    set(s => ({
      ...s,
      stages: updateWorkInTree(s.stages, workId, w => {
        const newPeriods = w.periods.map(p => p.id === periodId ? { ...p, isClosed } : p);
        const allClosed = newPeriods.every(p => p.isClosed);
        return { ...w, isClosed: allClosed, periods: newPeriods };
      }),
    }));
    runDebounced(`setPeriodIsClosed-${periodId}`, 200,
      () => workScheduleApi.setPeriodIsClosed(tenantId, projectId, workScheduleId, stageId, workId, periodId, isClosed),
      prev);
  }, [tenantId, projectId, workScheduleId, runDebounced]);

  const setAssignments = useCallback(async (stageId: string, workId: string, userIds: string[]) => {
    const prev = scheduleRef.current!;
    const memberMap = new Map(members.map(m => [m.userId, m]));
    set(s => ({
      ...s,
      stages: updateWorkInTree(s.stages, workId, w => ({
        ...w,
        assignees: userIds.map(uid => ({
          userId: uid,
          userName: (() => { const m = memberMap.get(uid); return m ? [m.firstName, m.lastName].filter(Boolean).join(' ') || m.email : uid; })(),
        })),
      })),
    }));
    runDebounced(`setAssignments-${workId}`, 300,
      () => workScheduleApi.setAssignments(tenantId, projectId, workScheduleId, stageId, workId, userIds),
      prev);
  }, [tenantId, projectId, workScheduleId, members, runDebounced]);

  const addComment = useCallback(async (stageId: string, workId: string, content: string) => {
    const tempId = `temp-${Date.now()}`;
    const optimisticComment = {
      id: tempId,
      content,
      createdAt: new Date().toISOString(),
      createdByUserId: user?.id ?? "",
      createdByUserName: [user?.firstName, user?.lastName].filter(Boolean).join(" ") || (user as any)?.email || "",
    };
    set(s => ({
      ...s,
      stages: updateWorkInTree(s.stages, workId, w => ({
        ...w,
        comments: [...(w.comments ?? []), optimisticComment],
      })),
    }));
    try {
      const res = await workScheduleApi.addComment(tenantId, projectId, workScheduleId, stageId, workId, content);
      const realId = res.data;
      set(s => ({
        ...s,
        stages: updateWorkInTree(s.stages, workId, w => ({
          ...w,
          comments: w.comments.map(c => c.id === tempId ? { ...c, id: realId } : c),
        })),
      }));
    } catch (err) {
      // Rollback temp comment
      set(s => ({
        ...s,
        stages: updateWorkInTree(s.stages, workId, w => ({
          ...w,
          comments: w.comments.filter(c => c.id !== tempId),
        })),
      }));
      const { title, description } = handleApiError(err);
      showError(title, description);
    }
  }, [user, tenantId, projectId, workScheduleId, showError]);

  const updateComment = useCallback(async (stageId: string, workId: string, commentId: string, content: string) => {
    set(s => ({
      ...s,
      stages: updateWorkInTree(s.stages, workId, w => ({
        ...w,
        comments: w.comments.map(c => c.id === commentId ? { ...c, content } : c),
      })),
    }));
    runDebounced(`updateComment-${commentId}`, 400,
      () => workScheduleApi.updateComment(tenantId, projectId, workScheduleId, stageId, workId, commentId, content),
      scheduleRef.current!);
  }, [tenantId, projectId, workScheduleId, runDebounced]);

  const deleteComment = useCallback(async (stageId: string, workId: string, commentId: string) => {
    const prev = scheduleRef.current!;
    set(s => ({
      ...s,
      stages: updateWorkInTree(s.stages, workId, w => ({
        ...w,
        comments: w.comments.filter(c => c.id !== commentId),
      })),
    }));
    runDebounced(`deleteComment-${commentId}`, 200,
      () => workScheduleApi.deleteComment(tenantId, projectId, workScheduleId, stageId, workId, commentId),
      prev);
  }, [tenantId, projectId, workScheduleId, runDebounced]);

  const setDependencies = useCallback(async (dependencies: WorkScheduleWorkDependencyWeb[]) => {
    const key = "setDependencies";
    startMutation(key);
    const payload = dependencies.map(d => ({
      predecessorWorkId: d.predecessorWorkId,
      successorWorkId: d.successorWorkId,
      dependencyType: d.dependencyType,
      lagDays: d.lagDays,
    }));
    try {
      const res = await workScheduleApi.setDependencies(tenantId, projectId, workScheduleId, payload);
      set(res.data);
      return res.data;
    } catch (err) {
      const { title, description } = handleApiError(err);
      showError(title, description);
      throw err;
    } finally {
      endMutation(key);
    }
  }, [tenantId, projectId, workScheduleId, showError]);

  const syncWithEstimate = useCallback(async () => {
    const key = "syncWithEstimate";
    startMutation(key);
    try {
      await workScheduleApi.syncWithEstimate(tenantId, projectId, workScheduleId);
      await fetchSchedule();
    } catch (err) {
      const { title, description } = handleApiError(err);
      showError(title, description);
      throw err;
    } finally {
      endMutation(key);
    }
  }, [tenantId, projectId, workScheduleId, fetchSchedule, showError]);

  // ─── Wartość kontekstu ────────────────────────────────────────────────────────
  const value: GanttContextValue = useMemo(() => ({
    schedule,
    members,
    isLoading,
    isMutating,
    mode,
    canEdit,
    permissions,
    ganttPermissions: resolvedGanttPermissions,
    tenantId,
    projectId,
    workScheduleId,
    expandedStages,
    collapsedWorks,
    showComments,
    showDependencies,
    mobileModal,
    searchQuery,
    setSearchQuery,
    fetchSchedule,
    refreshSchedule,
    setMode,
    toggleStage,
    expandAll,
    collapseAll,
    toggleWorkPeriods,
    setShowComments,
    setShowDependencies,
    openMobileModal,
    closeMobileModal,
    renameSchedule,
    addStage,
    deleteStage,
    renameStage,
    reorderStages,
    moveStage,
    addWork,
    deleteWork,
    renameWork,
    reorderWorks,
    moveWork,
    setPeriods,
    setWorkColor,
    setWorkIsClosed,
    setPeriodIsClosed,
    setAssignments,
    addComment,
    updateComment,
    deleteComment,
    setDependencies,
    syncWithEstimate,
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }), [schedule, members, isLoading, isMutating, mode, canEdit, permissions, resolvedGanttPermissions, tenantId, projectId, workScheduleId,
    expandedStages, collapsedWorks, showComments, showDependencies, mobileModal, searchQuery, setSearchQuery,
    fetchSchedule, refreshSchedule, setMode, toggleStage, expandAll, collapseAll, toggleWorkPeriods, setShowComments, setShowDependencies,
    openMobileModal, closeMobileModal, renameSchedule, addStage, deleteStage, renameStage, reorderStages, moveStage,
    addWork, deleteWork, renameWork, reorderWorks, moveWork, setPeriods, setWorkColor, setWorkIsClosed,
    setPeriodIsClosed, setAssignments, addComment, updateComment, deleteComment, setDependencies, syncWithEstimate]);

  return (
    <GanttContext.Provider value={value}>
      {isLoading && !schedule && (
        <Center py={20}>
          <VStack spacing={4}>
            <Spinner size="xl" color="primary.500" />
            <Text color="gray.500">Ładowanie harmonogramu...</Text>
          </VStack>
        </Center>
      )}
      {!isLoading && (initialError || !schedule) && (
        <Box p={6}>
          <Alert status="error" borderRadius="md">
            <AlertIcon />
            {initialError ?? "Nie udało się załadować harmonogramu."}
          </Alert>
          <Button mt={4} onClick={fetchSchedule}>Spróbuj ponownie</Button>
        </Box>
      )}
      {schedule && children}
    </GanttContext.Provider>
  );
}

// ─── Pomocnicze funkcje aktualizacji drzewa (czyste, zwracają nowe tablice) ───

function updateStageInTree(
  stages: WorkScheduleStageWeb[],
  stageId: string,
  update: (s: WorkScheduleStageWeb) => WorkScheduleStageWeb
): WorkScheduleStageWeb[] {
  return stages.map(s => {
    if (s.id === stageId) return update(s);
    return { ...s, childStages: updateStageInTree(s.childStages ?? [], stageId, update) };
  });
}

function updateWorkInTree(
  stages: WorkScheduleStageWeb[],
  workId: string,
  update: (w: WorkScheduleStageWorkWeb) => WorkScheduleStageWorkWeb
): WorkScheduleStageWeb[] {
  return stages.map(s => ({
    ...s,
    works: s.works.map(w => w.id === workId ? update(w) : w),
    childStages: updateWorkInTree(s.childStages ?? [], workId, update),
  }));
}

function findStageInTree(stages: WorkScheduleStageWeb[], stageId: string): WorkScheduleStageWeb | null {
  for (const s of stages) {
    if (s.id === stageId) return s;
    const found = findStageInTree(s.childStages ?? [], stageId);
    if (found) return found;
  }
  return null;
}

function removeStageFromTree(stages: WorkScheduleStageWeb[], stageId: string): WorkScheduleStageWeb[] {
  return stages
    .filter(s => s.id !== stageId)
    .map(s => ({ ...s, childStages: removeStageFromTree(s.childStages ?? [], stageId) }));
}

function reorderStagesInTree(stages: WorkScheduleStageWeb[], orderedIds: string[]): WorkScheduleStageWeb[] {
  const idsSet = new Set(orderedIds);
  if (stages.some(s => idsSet.has(s.id))) {
    const map = new Map(stages.map(s => [s.id, s]));
    return orderedIds
      .map(id => map.get(id))
      .filter((s): s is WorkScheduleStageWeb => s !== undefined)
      .map(s => ({ ...s, childStages: reorderStagesInTree(s.childStages ?? [], orderedIds) }));
  }
  return stages.map(s => ({ ...s, childStages: reorderStagesInTree(s.childStages ?? [], orderedIds) }));
}

function moveWorkInTree(
  stages: WorkScheduleStageWeb[],
  workId: string,
  sourceStageId: string,
  targetStageId: string,
  targetOrder: number,
  work: WorkScheduleStageWorkWeb,
): WorkScheduleStageWeb[] {
  return stages.map(s => {
    let updated = { ...s };
    if (s.id === sourceStageId) {
      updated.works = s.works.filter(w => w.id !== workId);
    }
    if (s.id === targetStageId) {
      const without = updated.works.filter(w => w.id !== workId);
      without.splice(targetOrder, 0, { ...work, order: targetOrder });
      updated.works = without.map((w, i) => ({ ...w, order: i }));
    }
    updated.childStages = moveWorkInTree(updated.childStages ?? [], workId, sourceStageId, targetStageId, targetOrder, work);
    return updated;
  });
}

export { findStageInTree, findStageForWork };
