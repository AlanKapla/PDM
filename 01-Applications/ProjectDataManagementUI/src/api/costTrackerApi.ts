import { axiosClient } from './axiosClient';
import type {
  CostTrackerDetailsWeb,
  CostEstimateSummaryWeb,
  TrackedCostWeb,
  CreateCostRequest,
  UpdateCostRequest,
} from '../types/costTracker.types';

export interface UpdateTrackerBudgetRequest {
  budgetNet?: number | null;
  budgetGross?: number | null;
}

const buildCostFormData = (data: CreateCostRequest | UpdateCostRequest): FormData => {
  const form = new FormData();

  form.append('name', data.name);
  if (data.description) form.append('description', data.description);
  if (data.net !== undefined && data.net !== null) form.append('net', String(data.net));
  if (data.number) form.append('number', data.number);
  if (data.contractor) form.append('contractor', data.contractor);
  if (data.date) form.append('date', data.date);
  if (data.costEstimateId) form.append('costEstimateId', data.costEstimateId);
  if (data.costEstimateItemId) form.append('costEstimateItemId', data.costEstimateItemId);
  if (data.newFiles) {
    data.newFiles.forEach((file) => form.append('newFiles', file));
  }

  const updateData = data as UpdateCostRequest;
  if (updateData.existingAttachmentIds) {
    updateData.existingAttachmentIds.forEach((id) =>
      form.append('existingAttachmentIds', id)
    );
  }

  return form;
};

export const costTrackerApi = {
  /** GET /api/tenants/{tenantId}/projects/{projectId}/cost-trackers/by-project */
  getByProject: async (
    tenantId: string,
    projectId: string
  ): Promise<CostTrackerDetailsWeb> => {
    const res = await axiosClient.get<CostTrackerDetailsWeb>(
      `/tenants/${tenantId}/projects/${projectId}/cost-trackers/by-project`
    );
    return res.data;
  },

  /** GET /api/tenants/{tenantId}/projects/{projectId}/cost-trackers/by-estimate/{costEstimateId} */
  getByEstimate: async (
    tenantId: string,
    projectId: string,
    costEstimateId: string
  ): Promise<CostEstimateSummaryWeb> => {
    const res = await axiosClient.get<CostEstimateSummaryWeb>(
      `/tenants/${tenantId}/projects/${projectId}/cost-trackers/by-estimate/${costEstimateId}`
    );
    return res.data;
  },

  /** GET /api/.../cost-trackers/costs */
  getCosts: async (
    tenantId: string,
    projectId: string
  ): Promise<TrackedCostWeb[]> => {
    const res = await axiosClient.get<TrackedCostWeb[]>(
      `/tenants/${tenantId}/projects/${projectId}/cost-trackers/costs`
    );
    return res.data;
  },

  /** GET /api/.../cost-trackers/costs/{costId} */
  getCostDetails: async (
    tenantId: string,
    projectId: string,
    costId: string
  ): Promise<TrackedCostWeb> => {
    const res = await axiosClient.get<TrackedCostWeb>(
      `/tenants/${tenantId}/projects/${projectId}/cost-trackers/costs/${costId}`
    );
    return res.data;
  },

  /** GET /api/.../by-estimate/{costEstimateId}/items/{costEstimateItemId}/costs */
  getItemCosts: async (
    tenantId: string,
    projectId: string,
    costEstimateId: string,
    costEstimateItemId: string
  ): Promise<TrackedCostWeb[]> => {
    const res = await axiosClient.get<TrackedCostWeb[]>(
      `/tenants/${tenantId}/projects/${projectId}/cost-trackers/by-estimate/${costEstimateId}/items/${costEstimateItemId}/costs`
    );
    return res.data;
  },

  /** POST /api/.../cost-trackers/costs — multipart/form-data */
  createCost: async (
    tenantId: string,
    projectId: string,
    data: CreateCostRequest
  ): Promise<TrackedCostWeb> => {
    const res = await axiosClient.post<TrackedCostWeb>(
      `/tenants/${tenantId}/projects/${projectId}/cost-trackers/costs`,
      buildCostFormData(data),
      { headers: { 'Content-Type': 'multipart/form-data' } }
    );
    return res.data;
  },

  /** PUT /api/.../cost-trackers/costs/{costId} — multipart/form-data */
  updateCost: async (
    tenantId: string,
    projectId: string,
    costId: string,
    data: UpdateCostRequest
  ): Promise<TrackedCostWeb> => {
    const res = await axiosClient.put<TrackedCostWeb>(
      `/tenants/${tenantId}/projects/${projectId}/cost-trackers/costs/${costId}`,
      buildCostFormData(data),
      { headers: { 'Content-Type': 'multipart/form-data' } }
    );
    return res.data;
  },

  /** DELETE /api/.../cost-trackers/costs/{costId} */
  deleteCost: async (
    tenantId: string,
    projectId: string,
    costId: string
  ): Promise<void> => {
    await axiosClient.delete(
      `/tenants/${tenantId}/projects/${projectId}/cost-trackers/costs/${costId}`
    );
  },

  /** PUT /api/.../cost-trackers/{costTrackerId}/budget */
  updateBudget: async (
    tenantId: string,
    projectId: string,
    costTrackerId: string,
    data: UpdateTrackerBudgetRequest
  ): Promise<void> => {
    await axiosClient.put(
      `/tenants/${tenantId}/projects/${projectId}/cost-trackers/${costTrackerId}/budget`,
      data
    );
  },
};
