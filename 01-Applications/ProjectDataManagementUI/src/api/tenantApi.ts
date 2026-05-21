import { axiosClient } from "./axiosClient";

export const tenantApi = {
  getUserTenants: async () => {
    return axiosClient.get("/tenants/my-tenants");
  },

  getAdminTenants: async () => {
    return axiosClient.get("/tenants/admin-tenants");
  },

  getTenantDetails: async (tenantId: string) => {
    return axiosClient.get(`/tenants/${tenantId}/details`);
  },

  changeActiveTenant: async (tenantId: string) => {
    return axiosClient.put("/tenants/active", { tenantId });
  },

  createTenant: async (name: string) => {
    return axiosClient.post("/tenants/create", { name });
  },

  updateTenant: async (tenantId: string, name: string) => {
    return axiosClient.put(`/tenants/${tenantId}`, { name });
  },

  toggleTenantStatus: async (tenantId: string, isActive: boolean) => {
    return axiosClient.patch(`/tenants/${tenantId}/status?isActive=${isActive}`);
  },

  inviteMember: async (tenantId: string, email: string) => {
    return axiosClient.post(`/tenants/${tenantId}/invitations`, { email });
  },

  acceptInvitation: async (token: string) => {
    return axiosClient.post("/tenants/invitations/accept", { token });
  },

  removeInvitation: async (tenantId: string, invitationId: string) => {
    return axiosClient.delete(`/tenants/${tenantId}/invitations/${invitationId}`);
  },

  removeMember: async (tenantId: string, userId: string) => {
    return axiosClient.delete(`/tenants/${tenantId}/members/${userId}`);
  },

  getActiveInvitations: async () => {
    return axiosClient.get("/tenants/invitations");
  },

  getTenantMembers: async (tenantId: string) => {
    return axiosClient.get(`/tenants/${tenantId}/members`);
  },

  updateTenantMemberRole: async (tenantId: string, userId: string, roleId: string) => {
    return axiosClient.patch(`/tenants/${tenantId}/members/${userId}/role`, { roleId });
  }
};