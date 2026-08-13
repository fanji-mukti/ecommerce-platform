export interface OrderLineItem {
  productId: string;
  productName: string;
  unitPrice: number;
  quantity: number;
}

export interface OrderDetail {
  id: string;
  status: string;
  totalAmount: number;
  lineItems: OrderLineItem[];
  createdAt: string;
  failureReason: string | null;
}
