import { axiosClient } from "./axiosClient";
import type { WorkScheduleDetailsWeb } from "../types/workSchedule.types";

const base = (tenantId: string, projectId: string, wsId: string) =>
  `/tenants/${tenantId}/project/${projectId}/work-schedule/${wsId}`;

const stagesBase = (tenantId: string, projectId: string, wsId: string) =>
  `${base(tenantId, projectId, wsId)}/stages`;

const worksBase = (tenantId: string, projectId: string, wsId: string, stageId: string, workId: string) =>
  `${stagesBase(tenantId, projectId, wsId)}/${stageId}/works/${workId}`;

export const workScheduleApi = {
  // ──────────────────────────────────────────────────────────────────
  // Harmonogram
  // ──────────────────────────────────────────────────────────────────

  /** PUT /{workScheduleId} — zmienia tylko nazwę 204 No Content */
  renameSchedule: (tenantId: string, projectId: string, wsId: string, name: string) =>
    axiosClient.put(base(tenantId, projectId, wsId), { tenantId, projectId, workScheduleId: wsId, name }),

  /** GET /details/{workScheduleId} */
  getDetails: (tenantId: string, projectId: string, wsId: string) =>
    axiosClient.get<WorkScheduleDetailsWeb>(
      `/tenants/${tenantId}/project/${projectId}/work-schedule/details/${wsId}`
    ),

  // ──────────────────────────────────────────────────────────────────
  // Etapy (Stages)
  // ──────────────────────────────────────────────────────────────────

  /** POST /{workScheduleId}/stages — 201 + Guid */
  addStage: (
    tenantId: string,
    projectId: string,
    wsId: string,
    payload: { name: string; order: number; parentStageId?: string | null; costEstimateGroupId?: string | null }
  ) => axiosClient.post<string>(stagesBase(tenantId, projectId, wsId), {
    tenantId, projectId, workScheduleId: wsId, ...payload,
  }),

  /** DELETE /{workScheduleId}/stages/{stageId} — 204 */
  deleteStage: (tenantId: string, projectId: string, wsId: string, stageId: string) =>
    axiosClient.delete(`${stagesBase(tenantId, projectId, wsId)}/${stageId}`),

  /** PATCH /{workScheduleId}/stages/{stageId}/name — 204 */
  renameStage: (tenantId: string, projectId: string, wsId: string, stageId: string, name: string) =>
    axiosClient.patch(`${stagesBase(tenantId, projectId, wsId)}/${stageId}/name`, {
      tenantId, projectId, workScheduleId: wsId, stageId, name,
    }),

  /** PUT /{workScheduleId}/stages/order — 204 */
  reorderStages: (tenantId: string, projectId: string, wsId: string, orderedStageIds: string[]) =>
    axiosClient.put(`${stagesBase(tenantId, projectId, wsId)}/order`, {
      tenantId, projectId, workScheduleId: wsId, orderedStageIds,
    }),

  /** PATCH /{workScheduleId}/stages/{stageId}/parent — 204 */
  moveStage: (tenantId: string, projectId: string, wsId: string, stageId: string, parentStageId: string | null) =>
    axiosClient.patch(`${stagesBase(tenantId, projectId, wsId)}/${stageId}/parent`, {
      tenantId, projectId, workScheduleId: wsId, stageId, parentStageId,
    }),

  // ──────────────────────────────────────────────────────────────────
  // Zakresy prac (Works)
  // ──────────────────────────────────────────────────────────────────

  /** POST /{workScheduleId}/stages/{stageId}/works — 201 + Guid */
  addWork: (
    tenantId: string,
    projectId: string,
    wsId: string,
    stageId: string,
    payload: { name: string; order: number; colorRgb: string; costEstimateItemId?: string | null }
  ) => axiosClient.post<string>(`${stagesBase(tenantId, projectId, wsId)}/${stageId}/works`, {
    tenantId, projectId, workScheduleId: wsId, workScheduleStageId: stageId, ...payload,
  }),

  /** DELETE /{workScheduleId}/stages/{stageId}/works/{workId} — 204 */
  deleteWork: (tenantId: string, projectId: string, wsId: string, stageId: string, workId: string) =>
    axiosClient.delete(`${stagesBase(tenantId, projectId, wsId)}/${stageId}/works/${workId}`),

  /** PATCH /{workScheduleId}/stages/{stageId}/works/{workId}/name — 204 */
  renameWork: (tenantId: string, projectId: string, wsId: string, stageId: string, workId: string, name: string) =>
    axiosClient.patch(`${worksBase(tenantId, projectId, wsId, stageId, workId)}/name`, {
      tenantId, projectId, workScheduleId: wsId, workScheduleStageId: stageId, workScheduleStageWorkId: workId, name,
    }),

  /** PUT /{workScheduleId}/stages/{stageId}/works/order — 204 */
  reorderWorks: (tenantId: string, projectId: string, wsId: string, stageId: string, orderedWorkIds: string[]) =>
    axiosClient.put(`${stagesBase(tenantId, projectId, wsId)}/${stageId}/works/order`, {
      tenantId, projectId, workScheduleId: wsId, workScheduleStageId: stageId, orderedWorkIds,
    }),

  /** PATCH /{workScheduleId}/stages/{stageId}/works/{workId}/stage — 204 */
  moveWork: (
    tenantId: string,
    projectId: string,
    wsId: string,
    stageId: string,
    workId: string,
    targetStageId: string,
    targetOrder: number
  ) => axiosClient.patch(`${worksBase(tenantId, projectId, wsId, stageId, workId)}/stage`, {
    tenantId, projectId, workScheduleId: wsId, workScheduleStageWorkId: workId,
    targetStageId, targetOrder,
  }),

  // ──────────────────────────────────────────────────────────────────
  // Okresy (Periods)
  // ──────────────────────────────────────────────────────────────────

  /** PUT /{workScheduleId}/stages/{stageId}/works/{workId}/periods — 204 */
  setPeriods: (
    tenantId: string,
    projectId: string,
    wsId: string,
    stageId: string,
    workId: string,
    periods: Array<{ startDate: string; endDate: string; isClosed: boolean }>
  ) => axiosClient.put(`${worksBase(tenantId, projectId, wsId, stageId, workId)}/periods`, {
    tenantId, projectId, workScheduleId: wsId, workScheduleStageWorkId: workId, periods,
  }),

  /** PATCH /{workScheduleId}/stages/{stageId}/works/{workId}/color-rgb — 204 */
  setWorkColor: (
    tenantId: string,
    projectId: string,
    wsId: string,
    stageId: string,
    workId: string,
    colorRgb: string
  ) => axiosClient.patch(`${worksBase(tenantId, projectId, wsId, stageId, workId)}/color-rgb`, {
    tenantId, projectId, workScheduleId: wsId, workScheduleStageId: stageId, workScheduleStageWorkId: workId, colorRgb,
  }),

  /** PATCH /{workScheduleId}/stages/{stageId}/works/{workId}/is-closed — 204 */
  setWorkIsClosed: (
    tenantId: string,
    projectId: string,
    wsId: string,
    stageId: string,
    workId: string,
    isClosed: boolean
  ) => axiosClient.patch(`${worksBase(tenantId, projectId, wsId, stageId, workId)}/is-closed`, {
    tenantId, projectId, workScheduleId: wsId, workScheduleStageId: stageId, workScheduleStageWorkId: workId, isClosed,
  }),

  /** PATCH /{workScheduleId}/stages/{stageId}/works/{workId}/periods/{periodId}/is-closed — 204 */
  setPeriodIsClosed: (
    tenantId: string,
    projectId: string,
    wsId: string,
    stageId: string,
    workId: string,
    periodId: string,
    isClosed: boolean
  ) => axiosClient.patch(
    `${worksBase(tenantId, projectId, wsId, stageId, workId)}/periods/${periodId}/is-closed`,
    { tenantId, projectId, workScheduleId: wsId, workScheduleStageWorkId: workId, periodId, isClosed }
  ),

  // ──────────────────────────────────────────────────────────────────
  // Przypisania (Assignments)
  // ──────────────────────────────────────────────────────────────────

  /** PUT /{workScheduleId}/stages/{stageId}/works/{workId}/assignments — 204 */
  setAssignments: (
    tenantId: string,
    projectId: string,
    wsId: string,
    stageId: string,
    workId: string,
    assignedUserIds: string[]
  ) => axiosClient.put(`${worksBase(tenantId, projectId, wsId, stageId, workId)}/assignments`, {
    tenantId, projectId, workScheduleId: wsId, workScheduleStageWorkId: workId, userIds: assignedUserIds,
  }),

  // ──────────────────────────────────────────────────────────────────
  // Komentarze (Comments)
  // ──────────────────────────────────────────────────────────────────

  /** POST /{workScheduleId}/stages/{stageId}/works/{workId}/comments — 201 + Guid */
  addComment: (
    tenantId: string, projectId: string, wsId: string, stageId: string, workId: string, content: string
  ) => axiosClient.post<string>(`${worksBase(tenantId, projectId, wsId, stageId, workId)}/comments`, {
    tenantId, projectId, workScheduleId: wsId, workScheduleStageWorkId: workId, content,
  }),

  /** PUT /{workScheduleId}/stages/{stageId}/works/{workId}/comments/{commentId} — 204 */
  updateComment: (
    tenantId: string, projectId: string, wsId: string, stageId: string, workId: string, commentId: string, content: string
  ) => axiosClient.put(
    `${worksBase(tenantId, projectId, wsId, stageId, workId)}/comments/${commentId}`,
    { tenantId, projectId, workScheduleId: wsId, workScheduleStageWorkId: workId, commentId, content }
  ),

  /** DELETE /{workScheduleId}/stages/{stageId}/works/{workId}/comments/{commentId} — 204 */
  deleteComment: (
    tenantId: string, projectId: string, wsId: string, stageId: string, workId: string, commentId: string
  ) => axiosClient.delete(
    `${worksBase(tenantId, projectId, wsId, stageId, workId)}/comments/${commentId}`
  ),

  // ──────────────────────────────────────────────────────────────────
  // Zależności (Dependencies)
  // ──────────────────────────────────────────────────────────────────

  /** PUT /{workScheduleId}/dependencies — 200 + WorkScheduleDetailsWeb */
  setDependencies: (
    tenantId: string,
    projectId: string,
    wsId: string,
    dependencies: Array<{
      predecessorWorkId: string;
      successorWorkId: string;
      dependencyType: number;
      lagDays: number;
    }>
  ) => axiosClient.put<WorkScheduleDetailsWeb>(`${base(tenantId, projectId, wsId)}/dependencies`, {
    tenantId, projectId, workScheduleId: wsId, dependencies,
  }),

  /** POST /{workScheduleId}/sync-with-estimate — 204 */
  syncWithEstimate: (tenantId: string, projectId: string, wsId: string) =>
    axiosClient.post(`${base(tenantId, projectId, wsId)}/sync-with-estimate`),
};
