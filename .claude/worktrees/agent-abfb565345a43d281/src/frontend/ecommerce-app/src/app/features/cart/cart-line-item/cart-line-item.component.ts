import { Component, input, output } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatIconButton } from '@angular/material/button';
import { CartLineItem } from '../../../shared/models/cart.model';

@Component({
  selector: 'app-cart-line-item',
  standalone: true,
  imports: [MatCardModule, MatIconModule, MatIconButton, CurrencyPipe],
  templateUrl: './cart-line-item.component.html',
})
export class CartLineItemComponent {
  item = input.required<CartLineItem>();

  quantityChange = output<number>();
  remove = output<void>();

  onIncrement(): void {
    this.quantityChange.emit(this.item().quantity + 1);
  }

  onDecrement(): void {
    const next = this.item().quantity - 1;
    if (next >= 1) {
      this.quantityChange.emit(next);
    }
  }

  onRemove(): void {
    this.remove.emit();
  }
}
