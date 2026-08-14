import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CurrencyPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { Subject } from 'rxjs';
import { debounceTime } from 'rxjs/operators';
import { Cart } from '../../../shared/models/cart.model';
import { CartService } from '../../../core/services/cart.service';
import { CartLineItemComponent } from '../cart-line-item/cart-line-item.component';

interface QuantityUpdate {
  productId: string;
  quantity: number;
}

@Component({
  selector: 'app-cart-page',
  standalone: true,
  imports: [
    MatProgressBarModule,
    MatButtonModule,
    MatCardModule,
    RouterLink,
    CurrencyPipe,
    CartLineItemComponent,
  ],
  templateUrl: './cart-page.component.html',
  styleUrl: './cart-page.component.scss',
})
export class CartPageComponent implements OnInit {
  private cartService = inject(CartService);
  private router = inject(Router);

  cart = signal<Cart | null>(null);
  isLoading = signal<boolean>(false);
  hasError = signal<boolean>(false);
  isUnauthorized = signal<boolean>(false);

  itemCountLabel = computed(() => {
    const count = this.cart()?.itemCount ?? 0;
    return count === 1 ? '1 item' : `${count} items`;
  });

  private quantityUpdate$ = new Subject<QuantityUpdate>();

  constructor() {
    this.quantityUpdate$.pipe(debounceTime(500)).subscribe(({ productId, quantity }) => {
      this.cartService.updateQuantity(productId, quantity).subscribe({
        next: (cart) => this.cart.set(cart),
        error: (err: HttpErrorResponse) => this.handleError(err),
      });
    });
  }

  ngOnInit(): void {
    this.loadCart();
  }

  loadCart(): void {
    this.isLoading.set(true);
    this.hasError.set(false);
    this.isUnauthorized.set(false);

    this.cartService.getCart().subscribe({
      next: (cart) => {
        this.cart.set(cart);
        this.isLoading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.isLoading.set(false);
        this.handleError(err);
      },
    });
  }

  retry(): void {
    this.loadCart();
  }

  onQuantityChange(productId: string, quantity: number): void {
    const current = this.cart();
    if (!current) return;

    // Optimistic local update of the affected line's quantity/lineTotal only.
    // itemCount and grandTotal stay pinned to the last server-confirmed
    // response until the debounced PATCH settles (T-03-14) — the summary
    // panel never derives its total from a purely local recomputation.
    const items = current.items.map((item) =>
      item.productId === productId
        ? { ...item, quantity, lineTotal: item.unitPrice * quantity }
        : item,
    );
    this.cart.set({ ...current, items });

    this.quantityUpdate$.next({ productId, quantity });
  }

  onRemove(productId: string): void {
    this.cartService.removeItem(productId).subscribe({
      next: (cart) => this.cart.set(cart),
      error: (err: HttpErrorResponse) => this.handleError(err),
    });
  }

  private handleError(err: HttpErrorResponse): void {
    if (err.status === 401) {
      this.isUnauthorized.set(true);
      this.router.navigate(['/login']);
    } else {
      this.hasError.set(true);
    }
  }
}
