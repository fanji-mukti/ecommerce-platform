import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { CurrencyPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { MatStepperModule } from '@angular/material/stepper';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule, MatCheckboxChange } from '@angular/material/checkbox';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { interval } from 'rxjs';
import { switchMap, takeWhile } from 'rxjs/operators';
import { Cart } from '../../../shared/models/cart.model';
import { CheckoutStatusValue } from '../../../shared/models/checkout.model';
import { CartService } from '../../../core/services/cart.service';
import { CheckoutService } from '../../../core/services/checkout.service';

const TERMINAL_STATUSES: CheckoutStatusValue[] = ['Paid', 'Cancelled', 'Failed', 'Fulfilled'];

@Component({
  selector: 'app-checkout-page',
  standalone: true,
  imports: [
    MatStepperModule,
    MatCardModule,
    MatCheckboxModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatProgressBarModule,
    CurrencyPipe,
  ],
  templateUrl: './checkout-page.component.html',
  styleUrl: './checkout-page.component.scss',
})
export class CheckoutPageComponent implements OnInit {
  private cartService = inject(CartService);
  private checkoutService = inject(CheckoutService);
  private router = inject(Router);

  cart = signal<Cart | null>(null);
  isLoading = signal<boolean>(false);
  hasError = signal<boolean>(false);
  simulatePaymentFailure = signal<boolean>(false);
  checkoutId = signal<string | null>(null);
  currentStatus = signal<CheckoutStatusValue | null>(null);
  isPlacingOrder = signal<boolean>(false);

  currentStepIndex = computed((): 0 | 1 | 2 => {
    const status = this.currentStatus();
    if (status === 'AwaitingPayment') return 1;
    if (status && this.isTerminal(status)) return 2;
    return 0;
  });

  ngOnInit(): void {
    this.loadCart();
  }

  loadCart(): void {
    this.isLoading.set(true);
    this.hasError.set(false);

    this.cartService.getCart().subscribe({
      next: (cart) => {
        this.isLoading.set(false);
        if (cart.items.length === 0) {
          this.router.navigate(['/cart']);
          return;
        }
        this.cart.set(cart);
      },
      error: () => {
        this.isLoading.set(false);
        this.hasError.set(true);
      },
    });
  }

  retry(): void {
    this.loadCart();
  }

  onCheckboxChange(event: MatCheckboxChange): void {
    this.simulatePaymentFailure.set(event.checked);
  }

  onPlaceOrder(): void {
    this.isPlacingOrder.set(true);
    this.hasError.set(false);

    this.checkoutService.startCheckout(this.simulatePaymentFailure()).subscribe({
      next: ({ checkoutId }) => {
        this.checkoutId.set(checkoutId);
        this.startPolling(checkoutId);
      },
      error: (_err: HttpErrorResponse) => {
        this.isPlacingOrder.set(false);
        this.hasError.set(true);
      },
    });
  }

  private startPolling(id: string): void {
    interval(1500)
      .pipe(
        switchMap(() => this.checkoutService.getStatus(id)),
        takeWhile((status) => {
          this.currentStatus.set(status.status);
          return !this.isTerminal(status.status);
        }, true),
      )
      .subscribe({
        next: (status) => {
          if (this.isTerminal(status.status)) {
            this.router.navigate(['/orders', id]);
          }
        },
        error: () => {
          this.hasError.set(true);
        },
      });
  }

  private isTerminal(status: CheckoutStatusValue): boolean {
    return TERMINAL_STATUSES.includes(status);
  }
}
