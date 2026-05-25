export enum SubscriptionPlan {
  Free       = 0,
  Standard   = 1,
  Premium    = 2,
  Enterprise = 3,
}

export enum SubscriptionStatus {
  Active      = 0,
  Trialing    = 1,
  PastDue     = 2,
  Canceled    = 3,
  GracePeriod = 4,
}

export const PlanLabels: Record<SubscriptionPlan, string> = {
  [SubscriptionPlan.Free]:       'Free',
  [SubscriptionPlan.Standard]:   'Standard',
  [SubscriptionPlan.Premium]:    'Premium',
  [SubscriptionPlan.Enterprise]: 'Enterprise',
};

export const StatusLabels: Record<SubscriptionStatus, string> = {
  [SubscriptionStatus.Active]:      'Aktywna',
  [SubscriptionStatus.Trialing]:    'Trial',
  [SubscriptionStatus.PastDue]:     'Przeterminowana',
  [SubscriptionStatus.Canceled]:    'Anulowana',
  [SubscriptionStatus.GracePeriod]: 'Grace period',
};

export interface PlanDefinition {
  id: string;
  plan: SubscriptionPlan;
  name: string;
  maxProjects: number;
  maxUsers: number;
  price: number;
  currency: string;
  isActive: boolean;
  updatedAt: string | null;
}

export interface SubscriptionOverride {
  id: string;
  key: string;
  value: string;
  reason: string;
  setByAdminId: string;
  expiresAt: string | null;
  isActive: boolean;
  isValid: boolean;
}

export interface TenantSubscription {
  tenantId: string;
  plan: SubscriptionPlan;
  status: SubscriptionStatus;
  maxProjects: number;
  maxUsers: number;
  isFullAccess: boolean;
  fullAccessGrantedByAdminId: string | null;
  fullAccessGrantedAt: string | null;
  currentPeriodStart: string;
  currentPeriodEnd: string | null;
  trialEndsAt: string | null;
  canceledAt: string | null;
  overrides: SubscriptionOverride[];
}

export interface TenantSubscriptionSummary {
  tenantId: string;
  plan: SubscriptionPlan;
  status: SubscriptionStatus;
  maxProjects: number;
  maxUsers: number;
  isFullAccess: boolean;
  fullAccessGrantedByAdminId: string | null;
  fullAccessGrantedAt: string | null;
  currentPeriodStart: string;
  currentPeriodEnd: string | null;
  trialEndsAt: string | null;
  canceledAt: string | null;
}

export interface GrantFullAccessResult {
  grantedAt: string;
  grantedByAdminId: string;
}

export interface UpdatePlanDefinitionRequest {
  name: string;
  maxProjects: number;
  maxUsers: number;
  price: number;
  currency: string;
  isActive: boolean;
}

export interface AddSubscriptionOverrideRequest {
  key: string;
  value: string;
  reason: string;
  expiresAt: string | null;
}

export interface AddedSubscriptionOverride {
  id: string;
  key: string;
  value: string;
  reason: string;
  expiresAt: string | null;
  createdAt: string;
}

export const formatLimit = (value: number): string =>
  value === -1 ? '∞' : value.toString();

// ── Tenant admin subscription (bez pól admina) ──────────────────────────────

export interface TenantSubscriptionInfo {
  tenantId: string;
  plan: SubscriptionPlan;
  status: SubscriptionStatus;
  maxProjects: number;
  maxUsers: number;
  isFullAccess: boolean;
  currentPeriodStart: string;
  currentPeriodEnd: string | null;
  trialEndsAt: string | null;
  canceledAt: string | null;
}

export interface SubscriptionPlanInfo {
  plan: SubscriptionPlan;
  name: string;
  maxProjects: number;
  maxUsers: number;
  price: number;
  currency: string;
}

export interface RequestPlanChangeRequest {
  plan: SubscriptionPlan;
}

// ── Billing ────────────────────────────────────────────────────────────────

export interface MockPaymentResult {
  paymentId: string;
  amount: number;
  currency: string;
  status: string;
  paidAt: string;
  periodEnd: string;
  nextPaymentDue: string;
}

export interface SubscriptionStatusInfo {
  plan: SubscriptionPlan;
  planName: string;
  status: SubscriptionStatus;
  statusLabel: string;
  nextPaymentDue: string | null;
  lastPaidAt: string | null;
  lastPaidAmount: number | null;
  currency: string;
  gracePeriodEndsAt: string | null;
  currentPeriodEnd: string | null;
  price: number;
  isCurrentPeriodPaid: boolean;
}

export interface SubscriptionPaymentInfo {
  id: string;
  plan: SubscriptionPlan;
  planName: string;
  amount: number;
  currency: string;
  status: number;
  statusLabel: string;
  periodStart: string;
  periodEnd: string;
  paidAt: string | null;
  failureReason: string | null;
  createdAt: string;
}
