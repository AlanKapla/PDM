import { useState, useCallback } from 'react';
import { projectApi } from '../api/projectApi';
import type { ProjectCostListItemWeb } from '../types/project.types';

export interface CreateProjectCostRequest {
  name: string;
  number?: string | null;
  contractorId?: string | null;
  date: Date;
  description?: string;
  net?: number | null;
  gross?: number | null;
  isAccepted?: boolean;
  document?: File;
}

export interface UpdateProjectCostRequest {
  name: string;
  number?: string | null;
  contractorId?: string | null;
  date: Date;
  description?: string;
  net?: number | null;
  gross?: number | null;
  isAccepted: boolean;
  document?: File;
  updatedDocument?: File;
  removeDocument: boolean;
}

export interface UseProjectCostMutationsResult {
  createCost: (data: CreateProjectCostRequest) => Promise<ProjectCostListItemWeb>;
  updateCost: (costId: string, data: UpdateProjectCostRequest) => Promise<ProjectCostListItemWeb>;
  deleteCost: (costId: string) => Promise<void>;
  isCreating: boolean;
  isUpdating: boolean;
  isDeleting: boolean;
}

/**
 * Mutacje dla kosztów projektowych (create/update/delete).
 * Błędy są rzucane — obsługa po stronie wywołującego komponentu.
 */
export function useProjectCostMutations(
  tenantId: string,
  projectId: string,
  onSuccess?: () => void
): UseProjectCostMutationsResult {
  const [isCreating, setIsCreating] = useState(false);
  const [isUpdating, setIsUpdating] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);

  const createCost = useCallback(
    async (data: CreateProjectCostRequest): Promise<ProjectCostListItemWeb> => {
      setIsCreating(true);
      try {
        const result = await projectApi.createProjectCost(tenantId, projectId, data);
        onSuccess?.();
        return result;
      } finally {
        setIsCreating(false);
      }
    },
    [tenantId, projectId, onSuccess]
  );

  const updateCost = useCallback(
    async (costId: string, data: UpdateProjectCostRequest): Promise<ProjectCostListItemWeb> => {
      setIsUpdating(true);
      try {
        const result = await projectApi.updateProjectCost(tenantId, projectId, costId, data);
        onSuccess?.();
        return result;
      } finally {
        setIsUpdating(false);
      }
    },
    [tenantId, projectId, onSuccess]
  );

  const deleteCost = useCallback(
    async (costId: string): Promise<void> => {
      setIsDeleting(true);
      try {
        await projectApi.deleteProjectCost(tenantId, projectId, costId);
        onSuccess?.();
      } finally {
        setIsDeleting(false);
      }
    },
    [tenantId, projectId, onSuccess]
  );

  return { createCost, updateCost, deleteCost, isCreating, isUpdating, isDeleting };
}

export default useProjectCostMutations;
