import { axiosClient } from '../../../api/axiosClient';
import type {
  ProjectDashboardWeb,
  TrackedCostWeb,
  CreateTrackedCostRequest,
  UpdateTrackedCostRequest,
  UpdateTrackerBudgetRequest,
} from '../types/projectDashboard.types';

const BASE = (tenantId: string, projectId: string) =>
  `/tenants/${tenantId}/projects/${projectId}`;

/**
 * Pobiera dane dashboardu projektu.
 * GET api/tenants/{tenantId}/projects/{projectId}/dashboard
 */
export async function getProjectDashboard(
  tenantId: string,
  projectId: string,
  signal?: AbortSignal
): Promise<ProjectDashboardWeb> {
  const response = await axiosClient.get<ProjectDashboardWeb>(
    `${BASE(tenantId, projectId)}/dashboard`,
    { signal }
  );
  return response.data;
}

/**
 * Tworzy nowy koszt śledzony.
 * POST api/tenants/{tenantId}/projects/{projectId}/cost-trackers/costs
 */
export async function createTrackedCost(
  tenantId: string,
  projectId: string,
  data: CreateTrackedCostRequest
): Promise<TrackedCostWeb> {
  const formData = new FormData();
  if (data.costEstimateItemId != null) formData.append('costEstimateItemId', data.costEstimateItemId);
  if (data.workScheduleStageWorkId != null) formData.append('workScheduleStageWorkId', data.workScheduleStageWorkId);
  formData.append('name', data.name);
  if (data.description != null) formData.append('description', data.description);
  if (data.net != null) formData.append('net', String(data.net));
  if (data.gross != null) formData.append('gross', String(data.gross));
  if (data.number != null) formData.append('number', data.number);
  if (data.contractorId != null) formData.append('contractorId', data.contractorId);
  if (data.categoryId != null) formData.append('categoryId', data.categoryId);
  if (data.date != null) formData.append('date', data.date);
  if (data.newFiles) {
    data.newFiles.forEach((file) => formData.append('newFiles', file));
  }

  const response = await axiosClient.post<TrackedCostWeb>(
    `${BASE(tenantId, projectId)}/cost-trackers/costs`,
    formData,
    { headers: { 'Content-Type': 'multipart/form-data' } }
  );
  return response.data;
}

/**
 * Aktualizuje koszt śledzony.
 * PUT api/tenants/{tenantId}/projects/{projectId}/cost-trackers/costs/{costId}
 */
export async function updateTrackedCost(
  tenantId: string,
  projectId: string,
  costId: string,
  data: UpdateTrackedCostRequest
): Promise<TrackedCostWeb> {
  const formData = new FormData();
  formData.append('name', data.name);
  if (data.description != null) formData.append('description', data.description);
  if (data.net != null) formData.append('net', String(data.net));
  if (data.gross != null) formData.append('gross', String(data.gross));
  if (data.number != null) formData.append('number', data.number);
  if (data.contractorId != null) formData.append('contractorId', data.contractorId);
  if (data.categoryId != null) formData.append('categoryId', data.categoryId);
  if (data.date != null) formData.append('date', data.date);
  if (data.costEstimateItemId != null) formData.append('costEstimateItemId', data.costEstimateItemId);
  if (data.workScheduleStageWorkId != null) formData.append('workScheduleStageWorkId', data.workScheduleStageWorkId);
  if (data.newFiles) {
    data.newFiles.forEach((file) => formData.append('newFiles', file));
  }
  if (data.existingAttachmentIds !== undefined) {
    if (data.existingAttachmentIds.length === 0) {
      formData.append('clearAllAttachments', 'true');
    } else {
      data.existingAttachmentIds.forEach((id) =>
        formData.append('existingAttachmentIds', id)
      );
    }
  }

  const response = await axiosClient.put<TrackedCostWeb>(
    `${BASE(tenantId, projectId)}/cost-trackers/costs/${costId}`,
    formData,
    { headers: { 'Content-Type': 'multipart/form-data' } }
  );
  return response.data;
}

/**
 * Usuwa koszt śledzony.
 * DELETE api/tenants/{tenantId}/projects/{projectId}/cost-trackers/costs/{costId}
 */
export async function deleteTrackedCost(
  tenantId: string,
  projectId: string,
  costId: string
): Promise<void> {
  await axiosClient.delete(
    `${BASE(tenantId, projectId)}/cost-trackers/costs/${costId}`
  );
}

/**
 * Aktualizuje budżet rezerwowy projektu.
 * PUT api/tenants/{tenantId}/projects/{projectId}/cost-trackers/budget
 */
export async function updateTrackerBudget(
  tenantId: string,
  projectId: string,
  data: UpdateTrackerBudgetRequest
): Promise<void> {
  await axiosClient.put(
    `${BASE(tenantId, projectId)}/cost-trackers/budget`,
    data
  );
}
