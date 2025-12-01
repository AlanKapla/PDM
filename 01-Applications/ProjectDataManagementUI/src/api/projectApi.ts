import { fetchWithAuth } from "./authApi";

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;

export const projectApi = {
  // Pobierz szczegóły projektu
  getProjectDetails: async (tenantId: string, projectId: string): Promise<Response> => {
    return fetchWithAuth(`${API_BASE_URL}/api/tenants/${tenantId}/Project/${projectId}`, {
      method: "GET",
      credentials: "include",
    });
  },

  // Pobierz projekt (alias)
  getProject: async (tenantId: string, projectId: string): Promise<Response> => {
    return fetchWithAuth(`${API_BASE_URL}/api/tenants/${tenantId}/Project/${projectId}`, {
      method: "GET",
      credentials: "include",
    });
  },

  // Pobierz członków projektu
  getProjectMembers: async (tenantId: string, projectId: string): Promise<Response> => {
    return fetchWithAuth(`${API_BASE_URL}/api/tenants/${tenantId}/Project/${projectId}/members`, {
      method: "GET",
      credentials: "include",
    });
  },

  // Dodaj członka do projektu
  addProjectMember: async (tenantId: string, projectId: string, userId: string): Promise<Response> => {
    return fetchWithAuth(`${API_BASE_URL}/api/tenants/${tenantId}/Project/${projectId}/members`, {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ tenantId, projectId, userId }),
    });
  },

  // Usuń członka z projektu
  removeProjectMember: async (tenantId: string, projectId: string, userId: string): Promise<Response> => {
    return fetchWithAuth(`${API_BASE_URL}/api/tenants/${tenantId}/Project/${projectId}/members/${userId}`, {
      method: "DELETE",
      credentials: "include",
    });
  },
};
