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
    console.error("Error fetching user tenants:", error);
    return [];
  }
};

export const getAdminTenants = async (): Promise<TenantBasic[]> => {
  try {
    const response = await tenantApi.getAdminTenants();
    return response.data;
  } catch (error) {
    console.error("Error fetching admin tenants:", error);
    return [];
  }
};

export const getTenantDetails = async (tenantId: string): Promise<TenantDetails | null> => {
  try {
    const response = await tenantApi.getTenantDetails(tenantId);
    return response.data;
  } catch (error) {
    console.error("Error fetching tenant details:", error);
    return null;
  }
};

export const changeActiveTenant = async (tenantId: string): Promise<boolean> => {
  try {
    await tenantApi.changeActiveTenant(tenantId);
    return true;
  } catch (error) {
    console.error("Error changing active tenant:", error);
    return false;
  }
};

export const createTenant = async (name: string): Promise<UserTenant | null> => {
  try {
    const response = await tenantApi.createTenant(name);
    return response.data;
  } catch (error) {
    console.error("Error creating tenant:", error);
    return null;
  }
};

export const updateTenant = async (tenantId: string, name: string): Promise<UserTenant | null> => {
  try {
    const response = await tenantApi.updateTenant(tenantId, name);
    return response.data;
  } catch (error) {
    console.error("Error updating tenant:", error);
    return null;
  }
};

export const inviteTenantMember = async (tenantId: string, email: string): Promise<boolean> => {
  try {
    await tenantApi.inviteMember(tenantId, email);
    return true;
  } catch (error) {
    console.error("Error inviting tenant member:", error);
    return false;
  }
};

export const acceptTenantInvitation = async (token: string): Promise<boolean> => {
  try {
    console.log("[Service] acceptTenantInvitation - Start");
    await tenantApi.acceptInvitation(token);
    console.log("[Service] Zaproszenie zaakceptowane pomyślnie");
    return true;
  } catch (error) {
    console.error("[Service] Błąd akceptacji zaproszenia:", error);
    return false;
  }
};

export const removeTenantMember = async (tenantId: string, userId: string): Promise<boolean> => {
  try {
    await tenantApi.removeMember(tenantId, userId);
    return true;
  } catch (error) {
    console.error("Error removing tenant member:", error);
    return false;
  }
};

export const removeTenantInvitation = async (tenantId: string, invitationId: string): Promise<boolean> => {
  try {
    await tenantApi.removeInvitation(tenantId, invitationId);
    return true;
  } catch (error) {
    console.error("Error removing tenant invitation:", error);
    return false;
  }
};

export const getActiveInvitations = async () => {
  try {
    const response = await tenantApi.getActiveInvitations();
    return response.data;
  } catch (error) {
    console.error("Error fetching active invitations:", error);
    return [];
  }
};

export const updateTenantMemberRole = async (tenantId: string, userId: string, roleId: string): Promise<boolean> => {
  try {
    await tenantApi.updateTenantMemberRole(tenantId, userId, roleId);
    return true;
  } catch (error) {
    console.error("Error updating tenant member role:", error);
    return false;
  }
};
