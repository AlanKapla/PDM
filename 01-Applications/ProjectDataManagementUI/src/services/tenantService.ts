import { tenantApi } from "../api/tenantApi";
import type { TenantDetails, ActiveTenant } from "../types/auth.types";

/**
 * Service layer for tenant operations
 * Converted to axios - returns response.data instead of response.ok/json()
 */

export const getUserTenants = async (): Promise<TenantDetails[]> => {
  try {
    const response = await tenantApi.getUserTenants();
    return response.data;
  } catch (error) {
    console.error("Error fetching user tenants:", error);
    return [];
  }
};

export const getActiveTenant = async (): Promise<ActiveTenant | null> => {
  try {
    const response = await tenantApi.getActiveTenant();
    return response.data;
  } catch (error) {
    console.error("Error fetching active tenant:", error);
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

export const createTenant = async (name: string): Promise<TenantDetails | null> => {
  try {
    const response = await tenantApi.createTenant(name);
    return response.data;
  } catch (error) {
    console.error("Error creating tenant:", error);
    return null;
  }
};

export const updateTenant = async (tenantId: string, name: string): Promise<TenantDetails | null> => {
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

export const getActiveInvitations = async () => {
  try {
    const response = await tenantApi.getActiveInvitations();
    return response.data;
  } catch (error) {
    console.error("Error fetching active invitations:", error);
    return [];
  }
};
