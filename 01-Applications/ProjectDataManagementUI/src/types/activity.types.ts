export const UserActivityEventType = {
  Login: "Login",
  DemoEnter: "DemoEnter",
} as const;

export type UserActivityEventType =
  (typeof UserActivityEventType)[keyof typeof UserActivityEventType];

export interface RecordActivityRequest {
  route?: string;
}

export interface UserActivityLogWeb {
  id: string;
  eventType: UserActivityEventType | string;
  ipAddress: string;
  occurredAtUtc: string;
  route: string | null;
  userId: string | null;
  azureAdB2CObjectId: string | null;
}
