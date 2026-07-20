import { axiosClient } from "./axiosClient";
import type {
  AdminUserWeb,
  ColdMailHistoryWeb,
  ColdMailTemplateWeb,
  SendColdMailsRequest,
  SendColdMailsResultWeb,
  SendWelcomeEmailsResultWeb,
} from "../types/admin.types";

export const adminApi = {
  getUsers: async (): Promise<AdminUserWeb[]> => {
    const response = await axiosClient.get<AdminUserWeb[]>("/admin/users");
    return response.data;
  },

  sendWelcomeEmailToUser: async (userId: string): Promise<AdminUserWeb> => {
    const response = await axiosClient.post<AdminUserWeb>(
      `/admin/users/${userId}/welcome-email`
    );
    return response.data;
  },

  sendWelcomeEmails: async (): Promise<SendWelcomeEmailsResultWeb> => {
    const response = await axiosClient.post<SendWelcomeEmailsResultWeb>(
      "/admin/welcome-emails/send"
    );
    return response.data;
  },

  getColdMailTemplate: async (): Promise<ColdMailTemplateWeb> => {
    const response = await axiosClient.get<ColdMailTemplateWeb>(
      "/admin/cold-mails/template"
    );
    return response.data;
  },

  sendColdMails: async (
    request: SendColdMailsRequest
  ): Promise<SendColdMailsResultWeb> => {
    const response = await axiosClient.post<SendColdMailsResultWeb>(
      "/admin/cold-mails/send",
      request
    );
    return response.data;
  },

  getColdMails: async (email?: string): Promise<ColdMailHistoryWeb[]> => {
    const response = await axiosClient.get<ColdMailHistoryWeb[]>(
      "/admin/cold-mails",
      { params: email ? { email } : undefined }
    );
    return response.data;
  },
};
