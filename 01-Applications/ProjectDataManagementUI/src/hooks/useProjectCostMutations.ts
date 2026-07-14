import { useState, useCallback } from 'react';
import { projectApi } from '../api/projectApi';
import type { ProjectCostListItemWeb } from '../types/project.types';

export interface CreateProjectCostRequest {
  name: string;
  number?: string | null;
  contractorId?: string | null;
  categoryId?: string | null;
  date: Date;
  description?: string;
  net?: number | null;
  gross?: number | null;
  document?: File;
}

export interface UpdateProjectCostRequest {
  name: string;
  number?: string | null;
  contractorId?: string | null;
  categoryId?: string | null;
  date: Date;
  description?: string;
  net?: number | null;
  gross?: number | null;
  document?: File;
  updatedDocument?: File;
  removeDocument: boolean;
}

export interface UseProjectCostMutationsResult {
  createCost: (data: CreateProjectCostRequest) => Promise<ProjectCostListItemWeb>;
  updateCost: (costId: string, data: UpdateProjectCostRequest) => Promise<ProjectCostListItemWeb>;
  deleteCost: (costId: string) => Promise<void>;
  submitCostForApproval: (costId: string) => Promise<ProjectCostListItemWeb>;
  withdrawCostFromApproval: (costId: string) => Promise<ProjectCostListItemWeb>;
  approveCost: (costId: string) => Promise<ProjectCostListItemWeb>;
  rejectCost: (costId: string) => Promise<ProjectCostListItemWeb>;
  isCreating: boolean;
  isUpdating: boolean;
  isDeleting: boolean;
  isSubmitting: boolean;
  isWithdrawing: boolean;
  isApproving: boolean;
  isRejecting: boolean;
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
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isWithdrawing, setIsWithdrawing] = useState(false);
  const [isApproving, setIsApproving] = useState(false);
  const [isRejecting, setIsRejecting] = useState(false);

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

  const submitCostForApproval = useCallback(
    async (costId: string): Promise<ProjectCostListItemWeb> => {
      setIsSubmitting(true);
      try {
        const result = await projectApi.submitProjectCostForApproval(tenantId, projectId, costId);
        onSuccess?.();
        return result;
      } finally {
        setIsSubmitting(false);
      }
    },
    [tenantId, projectId, onSuccess]
  );

  const withdrawCostFromApproval = useCallback(
    async (costId: string): Promise<ProjectCostListItemWeb> => {
      setIsWithdrawing(true);
      try {
        const result = await projectApi.withdrawProjectCostFromApproval(tenantId, projectId, costId);
        onSuccess?.();
        return result;
      } finally {
        setIsWithdrawing(false);
      }
    },
    [tenantId, projectId, onSuccess]
  );

  const approveCost = useCallback(
    async (costId: string): Promise<ProjectCostListItemWeb> => {
      setIsApproving(true);
      try {
        const result = await projectApi.approveProjectCost(tenantId, projectId, costId);
        onSuccess?.();
        return result;
      } finally {
        setIsApproving(false);
      }
    },
    [tenantId, projectId, onSuccess]
  );

  const rejectCost = useCallback(
    async (costId: string): Promise<ProjectCostListItemWeb> => {
      setIsRejecting(true);
      try {
        const result = await projectApi.rejectProjectCost(tenantId, projectId, costId);
        onSuccess?.();
        return result;
      } finally {
        setIsRejecting(false);
      }
    },
    [tenantId, projectId, onSuccess]
  );

  return {
    createCost,
    updateCost,
    deleteCost,
    submitCostForApproval,
    withdrawCostFromApproval,
    approveCost,
    rejectCost,
    isCreating,
    isUpdating,
    isDeleting,
    isSubmitting,
    isWithdrawing,
    isApproving,
    isRejecting,
  };
}

export default useProjectCostMutations;
