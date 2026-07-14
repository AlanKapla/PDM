import { tenantApi } from "../api/tenantApi";
import type { UserTenant, TenantBasic, TenantDetails } from "../types/auth.types";

/**
 * Service layer for tenant operations.
 * Błędy API propagują do wywołującego — obsługa UX w hooku/komponencie przez showApiError.
 */

export const getUserTenants = async (): Promise<UserTenant[]> => {
  const response = await tenantApi.getUserTenants();
  return response.data;
};

export const getAdminTenants = async (): Promise<TenantBasic[]> => {
  const response = await tenantApi.getAdminTenants();
  return response.data;
};

export const getTenantDetails = async (tenantId: string): Promise<TenantDetails> => {
  const response = await tenantApi.getTenantDetails(tenantId);
  return response.data;
};

export const changeActiveTenant = async (tenantId: string): Promise<void> => {
  await tenantApi.changeActiveTenant(tenantId);
};

export const createTenant = async (name: string): Promise<UserTenant> => {
  const response = await tenantApi.createTenant(name);
  return response.data;
};

export const updateTenant = async (tenantId: string, name: string): Promise<UserTenant> => {
  const response = await tenantApi.updateTenant(tenantId, name);
  return response.data;
};

export const inviteTenantMember = async (tenantId: string, email: string): Promise<void> => {
  await tenantApi.inviteMember(tenantId, email);
};

export const acceptTenantInvitation = async (token: string): Promise<void> => {
  await tenantApi.acceptInvitation(token);
};

export const removeTenantMember = async (tenantId: string, userId: string): Promise<void> => {
  await tenantApi.removeMember(tenantId, userId);
};

export const removeTenantInvitation = async (tenantId: string, invitationId: string): Promise<void> => {
  await tenantApi.removeInvitation(tenantId, invitationId);
};

export const getActiveInvitations = async () => {
  const response = await tenantApi.getActiveInvitations();
  return response.data;
};

export const updateTenantMemberAdmin = async (
  tenantId: string,
  userId: string,
  isAdmin: boolean,
): Promise<void> => {
  await tenantApi.updateTenantMemberAdmin(tenantId, userId, isAdmin);
};
