import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CurrencyPipe } from '@angular/common';
import { MatChipsModule } from '@angular/material/chips';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { HttpErrorResponse } from '@angular/common/http';
import { Product } from '../../../shared/models/product.model';
import { CatalogService } from '../../../core/services/catalog.service';
import { CartService } from '../../../core/services/cart.service';

@Component({
  selector: 'app-product-detail',
  standalone: true,
  imports: [
    MatChipsModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatIconModule,
    RouterLink,
    CurrencyPipe,
  ],
  templateUrl: './product-detail.component.html',
})
export class ProductDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private catalogService = inject(CatalogService);
  private cartService = inject(CartService);

  product = signal<Product | null>(null);
  isLoading = signal<boolean>(false);
  notFound = signal<boolean>(false);
  isAdding = signal<boolean>(false);
  addError = signal<boolean>(false);

  stockLabel = computed(() => {
    const p = this.product();
    if (!p) return '';
    if (p.stockQuantity > 10) return 'In Stock';
    if (p.stockQuantity >= 1) return 'Low Stock';
    return 'Out of Stock';
  });

  stockColor = computed((): string => {
    const p = this.product();
    if (!p) return '';
    return p.stockQuantity > 10 ? 'primary' : '';
  });

  ngOnInit(): void {
    const id = this.route.snapshot.params['id'];
    if (!id) {
      this.notFound.set(true);
      return;
    }
    this.isLoading.set(true);
    this.catalogService.getProduct(id).subscribe({
      next: (product) => {
        this.product.set(product);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.isLoading.set(false);
        this.notFound.set(true);
      },
    });
  }

  onAddToCart(): void {
    const p = this.product();
    if (!p) return;

    this.isAdding.set(true);
    this.addError.set(false);

    this.cartService.addItem(p.id, 1).subscribe({
      next: () => {
        this.isAdding.set(false);
        this.router.navigate(['/cart']);
      },
      error: (err: HttpErrorResponse) => {
        this.isAdding.set(false);
        this.addError.set(true);
      },
    });
  }
}
