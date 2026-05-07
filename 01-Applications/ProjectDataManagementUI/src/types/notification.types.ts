export const NotificationType = {
  Info: 0,
  Success: 1,
  Warning: 2,
  Error: 3,
} as const;

export type NotificationType = (typeof NotificationType)[keyof typeof NotificationType];

export interface NotificationWeb {
  id: string;
  tenantId: string;
  projectId?: string | null;
  tenantName: string;
  projectName?: string | null;
  userId: string;
  type: NotificationType;
  title: string;
  message: string;
  createdAt: string;
  isRead: boolean;
  metadata?: Record<string, any> | null;
}

export interface NotificationPayloadDto {
  notification: NotificationWeb;
  unreadNotificationCounter: number;
}

export interface NotificationMarkAsReadDto {
  notificationId: string;
  userId: string;
  readAt: string; // DateTimeOffset from backend
}
