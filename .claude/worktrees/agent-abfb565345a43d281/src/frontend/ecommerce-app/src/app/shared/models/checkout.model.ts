export type CheckoutStatusValue =
  | 'Started'
  | 'AwaitingPayment'
  | 'Paid'
  | 'Cancelled'
  | 'Failed'
  | 'Fulfilled';

export interface CheckoutStatus {
  checkoutId: string;
  status: CheckoutStatusValue;
  failureReason: string | null;
}
