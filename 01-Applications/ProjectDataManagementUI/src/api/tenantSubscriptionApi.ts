import { axiosClient } from "./axiosClient";
import type {
  TenantSubscriptionInfo,
  SubscriptionPlanInfo,
  RequestPlanChangeRequest,
  MockPaymentResult,
  SubscriptionStatusInfo,
  SubscriptionPaymentInfo,
} from "../types/subscription";

export const tenantSubscriptionApi = {
  getMySubscription: (tenantId: string): Promise<{ data: TenantSubscriptionInfo }> =>
    axiosClient.get<TenantSubscriptionInfo>(`/tenants/${tenantId}/subscription`),

  getAvailablePlans: (tenantId: string): Promise<{ data: SubscriptionPlanInfo[] }> =>
    axiosClient.get<SubscriptionPlanInfo[]>(`/tenants/${tenantId}/subscription/plans`),

  requestPlanChange: (
    tenantId: string,
    data: RequestPlanChangeRequest,
  ): Promise<{ data: TenantSubscriptionInfo }> =>
    axiosClient.put<TenantSubscriptionInfo>(
      `/tenants/${tenantId}/subscription/plan`,
      data,
    ),

  processPayment: (tenantId: string): Promise<{ data: MockPaymentResult }> =>
    axiosClient.post<MockPaymentResult>(`/tenants/${tenantId}/subscription/pay`),

  getSubscriptionStatus: (tenantId: string): Promise<{ data: SubscriptionStatusInfo }> =>
    axiosClient.get<SubscriptionStatusInfo>(`/tenants/${tenantId}/subscription/status`),

  getPaymentHistory: (tenantId: string): Promise<{ data: SubscriptionPaymentInfo[] }> =>
    axiosClient.get<SubscriptionPaymentInfo[]>(`/tenants/${tenantId}/subscription/payments`),
};
