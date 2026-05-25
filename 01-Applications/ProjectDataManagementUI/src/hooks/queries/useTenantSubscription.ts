import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { tenantSubscriptionApi } from "../../api/tenantSubscriptionApi";
import type {
  TenantSubscriptionInfo,
  SubscriptionPlanInfo,
  RequestPlanChangeRequest,
  MockPaymentResult,
  SubscriptionStatusInfo,
  SubscriptionPaymentInfo,
} from "../../types/subscription";

export const tenantSubscriptionKeys = {
  subscription: (tenantId: string) =>
    ["tenants", tenantId, "subscription"] as const,
  plans: (tenantId: string) =>
    ["tenants", tenantId, "subscription", "plans"] as const,
  status: (tenantId: string) =>
    ["tenants", tenantId, "subscription", "status"] as const,
  payments: (tenantId: string) =>
    ["tenants", tenantId, "subscription", "payments"] as const,
};

export function useMyTenantSubscription(tenantId: string | null) {
  return useQuery<TenantSubscriptionInfo>({
    queryKey: tenantSubscriptionKeys.subscription(tenantId ?? ""),
    queryFn: () =>
      tenantSubscriptionApi
        .getMySubscription(tenantId!)
        .then((r) => r.data),
    enabled: !!tenantId,
  });
}

export function useSubscriptionPlans(tenantId: string | null) {
  return useQuery<SubscriptionPlanInfo[]>({
    queryKey: tenantSubscriptionKeys.plans(tenantId ?? ""),
    queryFn: () =>
      tenantSubscriptionApi
        .getAvailablePlans(tenantId!)
        .then((r) => r.data),
    enabled: !!tenantId,
  });
}

export function useSubscriptionStatus(tenantId: string | null) {
  return useQuery<SubscriptionStatusInfo>({
    queryKey: tenantSubscriptionKeys.status(tenantId ?? ""),
    queryFn: () =>
      tenantSubscriptionApi
        .getSubscriptionStatus(tenantId!)
        .then((r) => r.data),
    enabled: !!tenantId,
  });
}

export function useRequestPlanChange() {
  const queryClient = useQueryClient();

  return useMutation<
    TenantSubscriptionInfo,
    Error,
    { tenantId: string; data: RequestPlanChangeRequest }
  >({
    mutationFn: ({ tenantId, data }) =>
      tenantSubscriptionApi
        .requestPlanChange(tenantId, data)
        .then((r) => r.data),
    onSuccess: (_result, variables) => {
      void queryClient.invalidateQueries({
        queryKey: tenantSubscriptionKeys.subscription(variables.tenantId),
      });
      void queryClient.invalidateQueries({
        queryKey: tenantSubscriptionKeys.status(variables.tenantId),
      });
    },
  });
}

export function useProcessMockPayment() {
  const queryClient = useQueryClient();

  return useMutation<MockPaymentResult, Error, string>({
    mutationFn: (tenantId) =>
      tenantSubscriptionApi.processPayment(tenantId).then((r) => r.data),
    onSuccess: (_result, tenantId) => {
      void queryClient.invalidateQueries({
        queryKey: tenantSubscriptionKeys.subscription(tenantId),
      });
      void queryClient.invalidateQueries({
        queryKey: tenantSubscriptionKeys.status(tenantId),
      });
      void queryClient.invalidateQueries({
        queryKey: tenantSubscriptionKeys.payments(tenantId),
      });
    },
  });
}

export function usePaymentHistory(tenantId: string | null) {
  return useQuery<SubscriptionPaymentInfo[]>({
    queryKey: tenantSubscriptionKeys.payments(tenantId ?? ""),
    queryFn: () =>
      tenantSubscriptionApi
        .getPaymentHistory(tenantId!)
        .then((r) => r.data),
    enabled: !!tenantId,
  });
}
