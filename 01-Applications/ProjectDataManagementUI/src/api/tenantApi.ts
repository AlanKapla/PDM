import { axiosClient } from "./axiosClient";

export const tenantApi = {
  getUserTenants: async () => {
    return axiosClient.get("/tenant/user-tenants");
  },

  getActiveTenant: async () => {
    return axiosClient.get("/tenant/active");
  },

  changeActiveTenant: async (tenantId: string) => {
    return axiosClient.put("/tenant/active", { tenantId });
  },

  createTenant: async (name: string) => {
    return axiosClient.post("/tenant/create", { name });
  },

  updateTenant: async (tenantId: string, name: string) => {
    return axiosClient.put(`/tenant/${tenantId}`, { tenantId, name });
  },

  toggleTenantStatus: async (tenantId: string, isActive: boolean) => {
    return axiosClient.patch(`/tenant/${tenantId}/status?isActive=${isActive}`);
  },

  inviteMember: async (tenantId: string, email: string) => {
    return axiosClient.post(`/tenant/${tenantId}/invitations`, { tenantId, email });
  },

  acceptInvitation: async (token: string) => {
    console.log("[API] acceptInvitation - Token:", token);
    return axiosClient.post("/tenant/invitations/accept", { token });
  },

  removeInvitation: async (tenantId: string, invitationId: string) => {
    return axiosClient.delete(`/tenant/${tenantId}/invitations/${invitationId}`);
  },

  removeMember: async (tenantId: string, userId: string) => {
    return axiosClient.delete(`/tenant/${tenantId}/members/${userId}`);
  },

  getActiveInvitations: async () => {
    return axiosClient.get("/tenant/invitations");
  },

  getTenantProjects: async (tenantId: string) => {
    return axiosClient.get(`/tenants/${tenantId}/Project`);
  },

  createProject: async (tenantId: string, name: string) => {
    return axiosClient.post(`/tenants/${tenantId}/Project`, { name });
  },

  getTenantMembers: async (tenantId: string) => {
    return axiosClient.get(`/tenant/${tenantId}/members`);
  },


  updateTenantMemberRole: async (tenantId: string, userId: string, roleId: string) => {
    return axiosClient.patch(`/tenant/${tenantId}/members/${userId}/role`, { roleId });
  }
};