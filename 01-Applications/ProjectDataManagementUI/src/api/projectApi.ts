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
    files: Array<{ file: File; displayName?: string; comment?: string }>
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
      if (item.comment) {
        formData.append(`Files[${index}].Comment`, item.comment);
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

  // Usuń plik z projektu
  deleteFile: async (tenantId: string, projectId: string, fileId: string): Promise<Response> => {
    return fetchWithAuth(`${API_BASE_URL}/api/tenants/${tenantId}/projects/${projectId}/File/${fileId}`, {
      method: "DELETE",
      credentials: "include",
    });
  },

  // Upload nowej wersji pliku
  uploadNewVersion: async (
    tenantId: string,
    projectId: string,
    fileId: string,
    file: File,
    comment?: string
  ): Promise<Response> => {
    const formData = new FormData();
    formData.append("File", file);
    if (comment) {
      formData.append("Comment", comment);
    }

    return fetchWithAuth(`${API_BASE_URL}/api/tenants/${tenantId}/projects/${projectId}/File/${fileId}/versions`, {
      method: "POST",
      credentials: "include",
      body: formData,
    });
  },

  // Dodaj komentarz do wersji pliku
  addFileVersionComment: async (
    tenantId: string,
    projectId: string,
    fileId: string,
    versionId: string,
    comment: string
  ): Promise<Response> => {
    return fetchWithAuth(`${API_BASE_URL}/api/tenants/${tenantId}/projects/${projectId}/File/${fileId}/versions/${versionId}/comments`, {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ comment }),
    });
  },

  // Zmień status projektu (aktywuj/dezaktywuj)
  toggleProjectStatus: async (tenantId: string, projectId: string, isActive: boolean): Promise<Response> => {
    return fetchWithAuth(`${API_BASE_URL}/api/tenants/${tenantId}/Project/${projectId}/toggle-status?isActive=${isActive}`, {
      method: "PATCH",
      credentials: "include",
    });
  },

  // Utwórz harmonogram prac
  createWorkSchedule: async (
    tenantId: string,
    projectId: string,
    command: {
      name: string;
      stages: Array<{
        name: string;
        order: number;
        works: Array<{
          name: string;
          order: number;
          colorRgb: string;
          periods: Array<{
            startDate: string;
            endDate: string;
          }>;
          assignedUserIds: string[];
        }>;
      }>;
    }
  ): Promise<Response> => {
    return fetchWithAuth(`${API_BASE_URL}/api/tenants/${tenantId}/projects/${projectId}/work-schedules`, {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        tenantId,
        projectId,
        name: command.name,
        stages: command.stages,
      }),
    });
  },

  // Pobierz moje harmonogramy prac (lista podsumowań)
  getMyWorkSchedules: async (tenantId: string, projectId: string): Promise<Response> => {
    return fetchWithAuth(`${API_BASE_URL}/api/tenants/${tenantId}/projects/${projectId}/work-schedules/my`, {
      method: "GET",
      credentials: "include",
    });
  },

  // Pobierz szczegóły pojedynczego harmonogramu prac
  getWorkSchedule: async (tenantId: string, projectId: string, workScheduleId: string): Promise<Response> => {
    return fetchWithAuth(`${API_BASE_URL}/api/tenants/${tenantId}/projects/${projectId}/work-schedules/${workScheduleId}`, {
      method: "GET",
      credentials: "include",
    });
  },

  // Aktualizuj harmonogram prac
  updateWorkSchedule: async (
    tenantId: string,
    projectId: string,
    workScheduleId: string,
    command: {
      name: string;
      stages: Array<{
        id?: string;
        name: string;
        order: number;
        works: Array<{
          id?: string;
          name: string;
          order: number;
          colorRgb: string;
          isClosed: boolean;
          periods: Array<{
            id?: string;
            startDate: string;
            endDate: string;
          }>;
          assignedUserIds: string[];
        }>;
      }>;
    }
  ): Promise<Response> => {
    return fetchWithAuth(`${API_BASE_URL}/api/tenants/${tenantId}/projects/${projectId}/work-schedules/${workScheduleId}`, {
      method: "PUT",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        tenantId,
        projectId,
        workScheduleId,
        name: command.name,
        stages: command.stages,
      }),
    });
  },

  // Pobierz prace przypisane do użytkownika
  getMyAssignedWorks: async (tenantId: string): Promise<Response> => {
    return fetchWithAuth(`${API_BASE_URL}/api/tenants/${tenantId}/my-assigned-works`, {
      method: "GET",
      credentials: "include",
    });
  },
};
