export interface NotificationModel {
    notificationID?: number;
  userID: number;
  title: string;
  message: string;
  notificationType: string;
  targetURL?: string;
  isRead: boolean;
  createdAt: string;
  readAt?: string;
}
