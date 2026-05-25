import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { adminApi } from "../../api/adminApi";
import type {
  AdminUserDetailsWeb,
  AdminUserListItemWeb,
  AdminTenantListItemWeb,
  AdminTenantDetailsWeb,
  SubscriptionPlanDefinitionWeb,
  UpdateSubscriptionPlanRequest,
} from "../../types/admin.types";
import type {
  AddedSubscriptionOverride,
  AddSubscriptionOverrideRequest,
  GrantFullAccessResult,
  PlanDefinition,
  SubscriptionPaymentInfo,
  TenantSubscription,
  TenantSubscriptionSummary,
  UpdatePlanDefinitionRequest,
} from "../../types/subscription";
import { SubscriptionPlan } from "../../types/subscription";

export const adminKeys = {
  users: ["admin", "users"] as const,
  userDetails: (userId: string) => ["admin", "users", userId] as const,
  tenants: ["admin", "tenants"] as const,
  tenantDetails: (tenantId: string) => ["admin", "tenants", tenantId] as const,
  subscriptionPlans: ["admin", "subscription-plans"] as const,
};

export function useAdminUsers() {
  return useQuery<AdminUserListItemWeb[]>({
    queryKey: adminKeys.users,
    queryFn: async () => {
      const response = await adminApi.getUsers();
      return response.data;
    },
  });
}

export function useAdminUserDetails(userId: string | null) {
  return useQuery<AdminUserDetailsWeb>({
    queryKey: adminKeys.userDetails(userId ?? ""),
    queryFn: async () => {
      const response = await adminApi.getUserDetails(userId!);
      return response.data;
    },
    enabled: !!userId,
  });
}

export function useAdminTenants() {
  return useQuery<AdminTenantListItemWeb[]>({
    queryKey: adminKeys.tenants,
    queryFn: async () => {
      const response = await adminApi.getTenants();
      return response.data;
    },
  });
}

export function useAdminTenantDetails(tenantId: string | null) {
  return useQuery<AdminTenantDetailsWeb>({
    queryKey: adminKeys.tenantDetails(tenantId ?? ""),
    queryFn: async () => {
      const response = await adminApi.getTenantDetails(tenantId!);
      return response.data;
    },
    enabled: !!tenantId,
  });
}

export function useAdminSubscriptionPlans() {
  return useQuery<SubscriptionPlanDefinitionWeb[]>({
    queryKey: adminKeys.subscriptionPlans,
    queryFn: async () => {
      const response = await adminApi.getSubscriptionPlans();
      return response.data;
    },
  });
}

export function useUpdateSubscriptionPlan() {
  const queryClient = useQueryClient();
  return useMutation<
    SubscriptionPlanDefinitionWeb,
    Error,
    { planId: string; data: UpdateSubscriptionPlanRequest }
  >({
    mutationFn: async ({ planId, data }) => {
      const response = await adminApi.updateSubscriptionPlan(planId, data);
      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: adminKeys.subscriptionPlans });
    },
  });
}

// ── Admin subscription management hooks ─────────────────────────────────────

export const subscriptionAdminKeys = {
  allPlans: ["admin", "subscriptions", "plans"] as const,
  tenantSubscription: (tenantId: string) =>
    ["admin", "subscriptions", "tenants", tenantId] as const,
  tenantPayments: (tenantId: string) =>
    ["admin", "subscriptions", "tenants", tenantId, "payments"] as const,
};

export function useAdminSubscriptionPlansList() {
  return useQuery<PlanDefinition[]>({
    queryKey: subscriptionAdminKeys.allPlans,
    queryFn: async () => {
      const response = await adminApi.getAdminSubscriptionPlans();
      return response.data;
    },
  });
}

export function useUpdatePlanDefinition() {
  const queryClient = useQueryClient();
  return useMutation<
    PlanDefinition,
    Error,
    { plan: SubscriptionPlan; data: UpdatePlanDefinitionRequest }
  >({
    mutationFn: async ({ plan, data }) => {
      const response = await adminApi.updateAdminPlanDefinition(plan, data);
      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: subscriptionAdminKeys.allPlans });
    },
  });
}

export function useTenantSubscription(tenantId: string | null) {
  return useQuery<TenantSubscription>({
    queryKey: subscriptionAdminKeys.tenantSubscription(tenantId ?? ""),
    queryFn: async () => {
      const response = await adminApi.getTenantSubscription(tenantId!);
      return response.data;
    },
    enabled: !!tenantId,
  });
}

export function useChangeTenantPlan() {
  const queryClient = useQueryClient();
  return useMutation<
    TenantSubscriptionSummary,
    Error,
    { tenantId: string; plan: SubscriptionPlan }
  >({
    mutationFn: async ({ tenantId, plan }) => {
      const response = await adminApi.changeTenantPlan(tenantId, { plan });
      return response.data;
    },
    onSuccess: (_data, { tenantId }) => {
      queryClient.invalidateQueries({
        queryKey: subscriptionAdminKeys.tenantSubscription(tenantId),
      });
    },
  });
}

export function useGrantFullAccess() {
  const queryClient = useQueryClient();
  return useMutation<GrantFullAccessResult, Error, string>({
    mutationFn: async (tenantId) => {
      const response = await adminApi.grantFullAccess(tenantId);
      return response.data;
    },
    onSuccess: (_data, tenantId) => {
      queryClient.invalidateQueries({
        queryKey: subscriptionAdminKeys.tenantSubscription(tenantId),
      });
    },
  });
}

export function useRevokeFullAccess() {
  const queryClient = useQueryClient();
  return useMutation<void, Error, string>({
    mutationFn: async (tenantId) => {
      await adminApi.revokeFullAccess(tenantId);
    },
    onSuccess: (_data, tenantId) => {
      queryClient.invalidateQueries({
        queryKey: subscriptionAdminKeys.tenantSubscription(tenantId),
      });
    },
  });
}

export function useAddSubscriptionOverride() {
  const queryClient = useQueryClient();
  return useMutation<
    AddedSubscriptionOverride,
    Error,
    { tenantId: string; data: AddSubscriptionOverrideRequest }
  >({
    mutationFn: async ({ tenantId, data }) => {
      const response = await adminApi.addSubscriptionOverride(tenantId, data);
      return response.data;
    },
    onSuccess: (_data, { tenantId }) => {
      queryClient.invalidateQueries({
        queryKey: subscriptionAdminKeys.tenantSubscription(tenantId),
      });
    },
  });
}

export function useDeactivateSubscriptionOverride() {
  const queryClient = useQueryClient();
  return useMutation<void, Error, { tenantId: string; overrideId: string }>({
    mutationFn: async ({ tenantId, overrideId }) => {
      await adminApi.deactivateSubscriptionOverride(tenantId, overrideId);
    },
    onSuccess: (_data, { tenantId }) => {
      queryClient.invalidateQueries({
        queryKey: subscriptionAdminKeys.tenantSubscription(tenantId),
      });
    },
  });
}

export function useAdminPaymentHistory(tenantId: string | null) {
  return useQuery<SubscriptionPaymentInfo[]>({
    queryKey: subscriptionAdminKeys.tenantPayments(tenantId ?? ""),
    queryFn: async () => {
      const response = await adminApi.getAdminPaymentHistory(tenantId!);
      return response.data;
    },
    enabled: !!tenantId,
  });
}
