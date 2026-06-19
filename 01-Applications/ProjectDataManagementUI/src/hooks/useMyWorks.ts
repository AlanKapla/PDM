import { useState, useCallback, useEffect } from "react";
import { projectApi } from "../api/projectApi";
import { workScheduleApi } from "../api/workScheduleApi";
import { useToastNotification } from "./useToastNotification";
import { getApiErrorMessage } from "../utils/apiErrorUtils";
import { updateWorkInTree } from "../utils/myWorksTree";
import type { UserAssignedWorksByTenantWeb } from "../types/workSchedule.types";

export const useMyWorks = () => {
  const [data, setData] = useState<UserAssignedWorksByTenantWeb[]>([]);
  const [loading, setLoading] = useState(true);
  const [mutating, setMutating] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const { showApiError } = useToastNotification();

  const reload = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const res = await projectApi.getMyAssignedWorks();
      setData((res.data as UserAssignedWorksByTenantWeb[]) ?? []);
    } catch (err) {
      setError(getApiErrorMessage(err));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { reload(); }, [reload]);

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
    } catch (err) {
      showApiError(err);
      reload();
    } finally {
      setMutating(false);
    }
  }, [reload, showApiError]);

  const setPeriodIsClosed = useCallback(async (
    tenantId: string,
    projectId: string,
    workScheduleId: string,
    stageId: string,
    workId: string,
    periodId: string,
    isClosed: boolean
  ) => {
    setData(prev => updateWorkInTree(prev, workId, work => {
      const updatedPeriods = work.periods.map(p => p.id === periodId ? { ...p, isClosed } : p);
      const allPeriodsClosed = updatedPeriods.length > 0 && updatedPeriods.every(p => p.isClosed);
      return {
        ...work,
        periods: updatedPeriods,
        isClosed: allPeriodsClosed,
      };
    }));

    try {
      setMutating(true);
      await workScheduleApi.setPeriodIsClosed(tenantId, projectId, workScheduleId, stageId, workId, periodId, isClosed);
    } catch (err) {
      showApiError(err);
      reload();
    } finally {
      setMutating(false);
    }
  }, [reload, showApiError]);

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
    } catch (err) {
      showApiError(err);
    } finally {
      setMutating(false);
    }
  }, [reload, showApiError]);

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
