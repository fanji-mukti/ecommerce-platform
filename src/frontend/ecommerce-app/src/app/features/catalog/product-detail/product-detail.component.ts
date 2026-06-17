import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { CurrencyPipe } from '@angular/common';
import { MatChipsModule } from '@angular/material/chips';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { Product } from '../../../shared/models/product.model';

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
  private http = inject(HttpClient);

  product = signal<Product | null>(null);
  isLoading = signal<boolean>(false);
  notFound = signal<boolean>(false);

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
    this.http.get<Product>(`/api/catalog/products/${id}`).subscribe({
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
}
