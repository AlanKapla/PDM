import { axiosClient } from "./axiosClient";
import type { RecordActivityRequest } from "../types/activity.types";

export const activityApi = {
  recordLogin: async (body?: RecordActivityRequest): Promise<void> => {
    await axiosClient.post("/activity/login", body ?? {});
  },

  recordDemo: async (body?: RecordActivityRequest): Promise<void> => {
    await axiosClient.post("/activity/demo", body ?? {});
  },
};
