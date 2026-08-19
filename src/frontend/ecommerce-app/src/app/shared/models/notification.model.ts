export type NotificationEventType = 'OrderPaid' | 'OrderShipped' | 'PaymentFailed';

export interface NotificationEntry {
  id: string;
  orderId: string;
  message: string;
  eventType: NotificationEventType;
  occurredAt: string;
}
