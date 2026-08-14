export interface CartLineItem {
  productId: string;
  productName: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
}

export interface Cart {
  items: CartLineItem[];
  itemCount: number;
  grandTotal: number;
}
