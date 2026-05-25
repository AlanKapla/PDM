import { axiosClient } from "./axiosClient";
import type {
  AdminUserDetailsWeb,
  AdminUserListItemWeb,
  AdminTenantListItemWeb,
  AdminTenantDetailsWeb,
  SubscriptionPlanDefinitionWeb,
  UpdateSubscriptionPlanRequest,
} from "../types/admin.types";
import type {
  AddedSubscriptionOverride,
  AddSubscriptionOverrideRequest,
  GrantFullAccessResult,
  PlanDefinition,
  SubscriptionPaymentInfo,
  TenantSubscription,
  TenantSubscriptionSummary,
  UpdatePlanDefinitionRequest,
} from "../types/subscription";
import type { SubscriptionPlan } from "../types/subscription";

export const adminApi = {
  getUsers: (): Promise<{ data: AdminUserListItemWeb[] }> =>
    axiosClient.get<AdminUserListItemWeb[]>("/admin/users"),

  getUserDetails: (userId: string): Promise<{ data: AdminUserDetailsWeb }> =>
    axiosClient.get<AdminUserDetailsWeb>(`/admin/users/${userId}`),

  getTenants: (): Promise<{ data: AdminTenantListItemWeb[] }> =>
    axiosClient.get<AdminTenantListItemWeb[]>("/admin/tenants"),

  getTenantDetails: (tenantId: string): Promise<{ data: AdminTenantDetailsWeb }> =>
    axiosClient.get<AdminTenantDetailsWeb>(`/admin/tenants/${tenantId}`),

  getSubscriptionPlans: (): Promise<{ data: SubscriptionPlanDefinitionWeb[] }> =>
    axiosClient.get<SubscriptionPlanDefinitionWeb[]>("/admin/subscription-plans"),

  updateSubscriptionPlan: (
    planId: string,
    data: UpdateSubscriptionPlanRequest,
  ): Promise<{ data: SubscriptionPlanDefinitionWeb }> =>
    axiosClient.put<SubscriptionPlanDefinitionWeb>(`/admin/subscription-plans/${planId}`, data),

  // ── Admin subscription management (/admin/subscriptions/…) ──────────────────

  getAdminSubscriptionPlans: (): Promise<{ data: PlanDefinition[] }> =>
    axiosClient.get<PlanDefinition[]>("/admin/subscriptions/plans"),

  updateAdminPlanDefinition: (
    plan: SubscriptionPlan,
    data: UpdatePlanDefinitionRequest,
  ): Promise<{ data: PlanDefinition }> =>
    axiosClient.put<PlanDefinition>(`/admin/subscriptions/plans/${plan}`, data),

  getTenantSubscription: (tenantId: string): Promise<{ data: TenantSubscription }> =>
    axiosClient.get<TenantSubscription>(`/admin/subscriptions/tenants/${tenantId}`),

  changeTenantPlan: (
    tenantId: string,
    data: { plan: SubscriptionPlan },
  ): Promise<{ data: TenantSubscriptionSummary }> =>
    axiosClient.put<TenantSubscriptionSummary>(
      `/admin/subscriptions/tenants/${tenantId}/plan`,
      data,
    ),

  grantFullAccess: (tenantId: string): Promise<{ data: GrantFullAccessResult }> =>
    axiosClient.post<GrantFullAccessResult>(
      `/admin/subscriptions/tenants/${tenantId}/full-access`,
    ),

  revokeFullAccess: (tenantId: string): Promise<void> =>
    axiosClient
      .delete(`/admin/subscriptions/tenants/${tenantId}/full-access`)
      .then(() => undefined),

  addSubscriptionOverride: (
    tenantId: string,
    data: AddSubscriptionOverrideRequest,
  ): Promise<{ data: AddedSubscriptionOverride }> =>
    axiosClient.post<AddedSubscriptionOverride>(
      `/admin/subscriptions/tenants/${tenantId}/overrides`,
      data,
    ),

  deactivateSubscriptionOverride: (
    tenantId: string,
    overrideId: string,
  ): Promise<void> =>
    axiosClient
      .delete(`/admin/subscriptions/tenants/${tenantId}/overrides/${overrideId}`)
      .then(() => undefined),

  getAdminPaymentHistory: (tenantId: string): Promise<{ data: SubscriptionPaymentInfo[] }> =>
    axiosClient.get<SubscriptionPaymentInfo[]>(`/admin/subscriptions/tenants/${tenantId}/payments`),
};
