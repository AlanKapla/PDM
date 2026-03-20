import { tenantApi } from "../api/tenantApi";
import type { UserTenant, TenantBasic, TenantDetails, ActiveTenant } from "../types/auth.types";

/**
 * Service layer for tenant operations
 * Converted to axios - returns response.data instead of response.ok/json()
 */

export const getUserTenants = async (): Promise<UserTenant[]> => {
  try {
    const response = await tenantApi.getUserTenants();
    return response.data;
  } catch (error) {
    return [];
  }
};

export const getAdminTenants = async (): Promise<TenantBasic[]> => {
  try {
    const response = await tenantApi.getAdminTenants();
    return response.data;
  } catch (error) {
    return [];
  }
};

export const getTenantDetails = async (tenantId: string): Promise<TenantDetails | null> => {
  try {
    const response = await tenantApi.getTenantDetails(tenantId);
    return response.data;
  } catch (error) {
    return null;
  }
};

export const changeActiveTenant = async (tenantId: string): Promise<void> => {
  await tenantApi.changeActiveTenant(tenantId);
};

export const createTenant = async (name: string): Promise<UserTenant | null> => {
  try {
    const response = await tenantApi.createTenant(name);
    return response.data;
  } catch (error) {
    return null;
  }
};

export const updateTenant = async (tenantId: string, name: string): Promise<UserTenant | null> => {
  try {
    const response = await tenantApi.updateTenant(tenantId, name);
    return response.data;
  } catch (error) {
    return null;
  }
};

export const inviteTenantMember = async (tenantId: string, email: string): Promise<boolean> => {
  try {
    await tenantApi.inviteMember(tenantId, email);
    return true;
  } catch (error) {
    return false;
  }
};

export const acceptTenantInvitation = async (token: string): Promise<boolean> => {
  try {
    await tenantApi.acceptInvitation(token);
    return true;
  } catch (error) {
    return false;
  }
};

export const removeTenantMember = async (tenantId: string, userId: string): Promise<boolean> => {
  try {
    await tenantApi.removeMember(tenantId, userId);
    return true;
  } catch (error) {
    return false;
  }
};

export const removeTenantInvitation = async (tenantId: string, invitationId: string): Promise<boolean> => {
  try {
    await tenantApi.removeInvitation(tenantId, invitationId);
    return true;
  } catch (error) {
    return false;
  }
};

export const getActiveInvitations = async () => {
  try {
    const response = await tenantApi.getActiveInvitations();
    return response.data;
  } catch (error) {
    return [];
  }
};

export const updateTenantMemberRole = async (tenantId: string, userId: string, roleId: string): Promise<boolean> => {
  try {
    await tenantApi.updateTenantMemberRole(tenantId, userId, roleId);
    return true;
  } catch (error) {
    return false;
  }
};
