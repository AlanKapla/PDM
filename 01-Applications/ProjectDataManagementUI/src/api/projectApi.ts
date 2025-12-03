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

  // Upload plików do projektu
  uploadFiles: async (
    tenantId: string, 
    projectId: string, 
    packageName: string, 
    files: Array<{ file: File; displayName?: string }>
  ): Promise<Response> => {
    const formData = new FormData();
    formData.append('TenantId', tenantId);
    formData.append('ProjectId', projectId);
    formData.append('PackageName', packageName);
    
    files.forEach((item, index) => {
      formData.append(`Files[${index}].File`, item.file);
      if (item.displayName) {
        formData.append(`Files[${index}].DisplayName`, item.displayName);
      }
    });

    return fetchWithAuth(`${API_BASE_URL}/api/tenants/${tenantId}/projects/${projectId}/File`, {
      method: "POST",
      credentials: "include",
      body: formData,
    });
  },

  // Pobierz pliki użytkownika w projekcie
  getMyFiles: async (tenantId: string, projectId: string): Promise<Response> => {
    return fetchWithAuth(`${API_BASE_URL}/api/tenants/${tenantId}/projects/${projectId}/File/my`, {
      method: "GET",
      credentials: "include",
    });
  },

  // Udostępnij pliki członkowi projektu
  shareFiles: async (
    tenantId: string,
    projectId: string,
    fileIds: string[],
    sharedWithUserId: string
  ): Promise<Response> => {
    return fetchWithAuth(`${API_BASE_URL}/api/tenants/${tenantId}/projects/${projectId}/File/share`, {
      method: "POST",
      credentials: "include",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        tenantId,
        projectId,
        projectFileIds: fileIds,
        sharedWithUserId,
      }),
    });
  },

  // Pobierz pliki udostępnione dla użytkownika
  getSharedFiles: async (tenantId: string, projectId: string): Promise<Response> => {
    return fetchWithAuth(`${API_BASE_URL}/api/tenants/${tenantId}/projects/${projectId}/File/shared`, {
      method: "GET",
      credentials: "include",
    });
  },
};
