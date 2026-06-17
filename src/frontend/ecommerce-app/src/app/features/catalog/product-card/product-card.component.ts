import { Component, computed, input } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatButtonModule } from '@angular/material/button';
import { Product } from '../../../shared/models/product.model';

@Component({
  selector: 'app-product-card',
  standalone: true,
  imports: [MatCardModule, MatChipsModule, MatButtonModule, RouterLink, CurrencyPipe],
  templateUrl: './product-card.component.html',
})
export class ProductCardComponent {
  product = input.required<Product>();

  stockLabel = computed(() => {
    const qty = this.product().stockQuantity;
    if (qty > 10) return 'In Stock';
    if (qty >= 1) return 'Low Stock';
    return 'Out of Stock';
  });

  stockColor = computed((): string => {
    const qty = this.product().stockQuantity;
    if (qty > 10) return 'primary';
    return '';
  });

  isOutOfStock = computed(() => this.product().stockQuantity === 0);
}
