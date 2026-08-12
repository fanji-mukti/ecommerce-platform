import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CheckoutStatus } from '../../shared/models/checkout.model';

@Injectable({ providedIn: 'root' })
export class CheckoutService {
  private http = inject(HttpClient);

  startCheckout(simulatePaymentFailure: boolean): Observable<{ checkoutId: string }> {
    return this.http.post<{ checkoutId: string }>('/api/checkout', { simulatePaymentFailure });
  }

  getStatus(checkoutId: string): Observable<CheckoutStatus> {
    return this.http.get<CheckoutStatus>(`/api/checkout/${checkoutId}`);
  }
}
