const API_BASE = import.meta.env.VITE_API_BASE_URL || "";
const API_URL = `${API_BASE}/api/Tenant`;

export const tenantApi = {
  getUserTenants: async () => {
    return fetch(`${API_URL}/user-tenants`, {
      method: "GET",
      credentials: "include",
    });
  },

  getActiveTenant: async () => {
    return fetch(`${API_URL}/active`, {
      method: "GET",
      credentials: "include",
    });
  },

  changeActiveTenant: async (tenantId: string) => {
    return fetch(`${API_URL}/active`, {
      method: "PUT",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ tenantId }),
    });
  },

  createTenant: async (name: string) => {
    return fetch(`${API_URL}/create`, {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ name }),
    });
  },

  updateTenant: async (tenantId: string, name: string) => {
    return fetch(`${API_URL}/${tenantId}`, {
      method: "PUT",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ tenantId, name }),
    });
  },

  inviteMember: async (tenantId: string, email: string) => {
    return fetch(`${API_URL}/${tenantId}/invitations`, {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ tenantId, email }),
    });
  },

  acceptInvitation: async (token: string) => {
    const url = `${API_URL}/invitations/accept`;
    const body = { token };
    console.log("[API] acceptInvitation - URL:", url);
    console.log("[API] acceptInvitation - Token:", token);
    console.log("[API] acceptInvitation - Body:", JSON.stringify(body));
    
    const response = await fetch(url, {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    });
    
    console.log("[API] acceptInvitation - Response status:", response.status);
    console.log("[API] acceptInvitation - Response OK:", response.ok);
    
    return response;
  },

  removeMember: async (tenantId: string, userId: string) => {
    return fetch(`${API_URL}/${tenantId}/members/${userId}`, {
      method: "DELETE",
      credentials: "include",
    });
  },

  getActiveInvitations: async () => {
    return fetch(`${API_URL}/invitations`, {
      method: "GET",
      credentials: "include",
    });
  },

  getTenantProjects: async (tenantId: string) => {
    return fetch(`${API_BASE}/api/tenants/${tenantId}/Project`, {
      method: "GET",
      credentials: "include",
    });
  },

  createProject: async (tenantId: string, name: string) => {
    return fetch(`${API_BASE}/api/tenants/${tenantId}/Project`, {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ name }),
    });
  },
};
