import { axiosClient } from "./axiosClient";
import type { SendWelcomeEmailsResultWeb } from "../types/user.types";

export const userApi = {
  sendWelcomeEmails: async (): Promise<SendWelcomeEmailsResultWeb> => {
    const response = await axiosClient.post<SendWelcomeEmailsResultWeb>(
      "/user/send-welcome-emails"
    );
    return response.data;
  },
};
