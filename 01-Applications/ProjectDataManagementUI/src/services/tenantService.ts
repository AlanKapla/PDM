import { tenantApi } from "../api/tenantApi";
import type { TenantDetails, ActiveTenant } from "../types/auth.types";

export const getUserTenants = async (): Promise<TenantDetails[]> => {
  const res = await tenantApi.getUserTenants();

  if (!res.ok) return [];

  return res.json();
};

export const getActiveTenant = async (): Promise<ActiveTenant | null> => {
  const res = await tenantApi.getActiveTenant();

  if (!res.ok) return null;

  return res.json();
};

export const changeActiveTenant = async (tenantId: string): Promise<boolean> => {
  const res = await tenantApi.changeActiveTenant(tenantId);
  return res.ok;
};

export const createTenant = async (name: string): Promise<TenantDetails | null> => {
  const res = await tenantApi.createTenant(name);
  
  if (!res.ok) return null;
  
  return res.json();
};

export const updateTenant = async (tenantId: string, name: string): Promise<TenantDetails | null> => {
  const res = await tenantApi.updateTenant(tenantId, name);
  
  if (!res.ok) return null;
  
  return res.json();
};

export const inviteTenantMember = async (tenantId: string, email: string): Promise<boolean> => {
  const res = await tenantApi.inviteMember(tenantId, email);
  return res.ok;
};

export const acceptTenantInvitation = async (token: string): Promise<boolean> => {
  console.log("[Service] acceptTenantInvitation - Start");
  const res = await tenantApi.acceptInvitation(token);
  console.log("[Service] acceptTenantInvitation - Response received:", res.status, res.statusText);
  
  if (!res.ok) {
    try {
      const text = await res.text();
      console.error("[Service] Błąd akceptacji zaproszenia - Status:", res.status);
      console.error("[Service] Błąd akceptacji zaproszenia - Response:", text);
    } catch (e) {
      console.error("[Service] Nie można odczytać treści błędu");
    }
  } else {
    console.log("[Service] Zaproszenie zaakceptowane pomyślnie");
  }
  
  return res.ok;
};

export const removeTenantMember = async (tenantId: string, userId: string): Promise<boolean> => {
  const res = await tenantApi.removeMember(tenantId, userId);
  return res.ok;
};

export const getActiveInvitations = async () => {
  const res = await tenantApi.getActiveInvitations();
  
  if (!res.ok) return [];
  
  return res.json();
};
