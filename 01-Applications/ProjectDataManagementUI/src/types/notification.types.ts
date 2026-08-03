export const NotificationType = {
  Info: 0,
  Success: 1,
  Warning: 2,
  Error: 3,
} as const;

export type NotificationType = (typeof NotificationType)[keyof typeof NotificationType];

export interface NotificationMetadata {
  route?: string;
  batchId?: string;
  pendingCount?: number;
  errorCount?: number;
  duplicateCount?: number;
  FileId?: string;
  PackageId?: string;
  VersionId?: string;
  CommentId?: string;
  EntityType?: string;
}

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
  metadata?: NotificationMetadata | null;
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
