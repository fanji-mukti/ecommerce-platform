import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Cart } from '../../shared/models/cart.model';

@Injectable({ providedIn: 'root' })
export class CartService {
  private http = inject(HttpClient);

  getCart(): Observable<Cart> {
    return this.http.get<Cart>('/api/cart');
  }

  addItem(productId: string, quantity: number): Observable<Cart> {
    return this.http.post<Cart>('/api/cart/items', { productId, quantity });
  }

  updateQuantity(productId: string, quantity: number): Observable<Cart> {
    return this.http.patch<Cart>(`/api/cart/items/${productId}`, { quantity });
  }

  removeItem(productId: string): Observable<Cart> {
    return this.http.delete<Cart>(`/api/cart/items/${productId}`);
  }
}
