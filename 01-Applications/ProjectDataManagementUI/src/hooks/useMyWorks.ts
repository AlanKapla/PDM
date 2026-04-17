import { useState, useCallback, useEffect } from "react";
import { projectApi } from "../api/projectApi";
import { workScheduleApi } from "../api/workScheduleApi";
import { useToastNotification } from "./useToastNotification";
import { updateWorkInTree } from "../utils/myWorksTree";
import type { UserAssignedWorksByTenantWeb } from "../types/workSchedule.types";

export const useMyWorks = () => {
  const [data, setData] = useState<UserAssignedWorksByTenantWeb[]>([]);
  const [loading, setLoading] = useState(true);
  const [mutating, setMutating] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const { showError } = useToastNotification();

  const reload = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const res = await projectApi.getMyAssignedWorks();
      setData((res.data as UserAssignedWorksByTenantWeb[]) ?? []);
    } catch (err: any) {
      setError(err?.message ?? "Błąd pobierania przypisanych prac");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { reload(); }, [reload]);

  // ─── Zamknij / otwórz zakres pracy ─────────────────────────────────────────
  // Optimistic update — zmiana widoczna natychmiast, cofnięcie przy błędzie.

  const setWorkIsClosed = useCallback(async (
    tenantId: string,
    projectId: string,
    workScheduleId: string,
    stageId: string,
    workId: string,
    isClosed: boolean
  ) => {
    setData(prev => updateWorkInTree(prev, workId, work => ({
      ...work,
      isClosed,
      periods: work.periods.map(p => ({ ...p, isClosed })),
    })));

    try {
      setMutating(true);
      await workScheduleApi.setWorkIsClosed(tenantId, projectId, workScheduleId, stageId, workId, isClosed);
    } catch (err: any) {
      showError("Błąd zapisu", "Nie udało się zmienić statusu zakresu pracy");
      reload();
    } finally {
      setMutating(false);
    }
  }, [reload, showError]);

  // ─── Zamknij / otwórz pojedynczy okres pracy ────────────────────────────────

  const setPeriodIsClosed = useCallback(async (
    tenantId: string,
    projectId: string,
    workScheduleId: string,
    stageId: string,
    workId: string,
    periodId: string,
    isClosed: boolean
  ) => {
    setData(prev => updateWorkInTree(prev, workId, work => ({
      ...work,
      periods: work.periods.map(p => p.id === periodId ? { ...p, isClosed } : p),
    })));

    try {
      setMutating(true);
      await workScheduleApi.setPeriodIsClosed(tenantId, projectId, workScheduleId, stageId, workId, periodId, isClosed);
    } catch (err: any) {
      showError("Błąd zapisu", "Nie udało się zmienić statusu okresu");
      reload();
    } finally {
      setMutating(false);
    }
  }, [reload, showError]);

  // ─── Dodaj komentarz ────────────────────────────────────────────────────────
  // Brak optimistic update dla komentarzy — odśwież po sukcesie.

  const addComment = useCallback(async (
    tenantId: string,
    projectId: string,
    workScheduleId: string,
    stageId: string,
    workId: string,
    content: string
  ) => {
    try {
      setMutating(true);
      await workScheduleApi.addComment(tenantId, projectId, workScheduleId, stageId, workId, content);
      await reload();
    } catch (err: any) {
      showError("Błąd", "Nie udało się dodać komentarza");
    } finally {
      setMutating(false);
    }
  }, [reload, showError]);

  return {
    data,
    loading,
    mutating,
    error,
    reload,
    setWorkIsClosed,
    setPeriodIsClosed,
    addComment,
  };
};
