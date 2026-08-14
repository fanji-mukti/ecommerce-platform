import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatChipsModule } from '@angular/material/chips';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatButtonModule } from '@angular/material/button';
import { Product } from '../../../shared/models/product.model';
import { ProductCardComponent } from '../product-card/product-card.component';
import { CatalogService } from '../../../core/services/catalog.service';

@Component({
  selector: 'app-catalog-list',
  standalone: true,
  imports: [
    MatProgressBarModule,
    MatChipsModule,
    MatPaginatorModule,
    MatButtonModule,
    ProductCardComponent,
  ],
  templateUrl: './catalog-list.component.html',
  styleUrl: './catalog-list.component.scss',
})
export class CatalogListComponent implements OnInit {
  private catalogService = inject(CatalogService);

  products = signal<Product[]>([]);
  isLoading = signal<boolean>(false);
  hasError = signal<boolean>(false);
  selectedCategory = signal<string | null>(null);
  currentPage = signal<number>(0);
  totalCount = signal<number>(0);

  readonly pageSize = 12;
  readonly pageSizeOptions = [12, 24, 48];

  categories = computed(() => {
    const cats = [...new Set(this.products().map((p) => p.category))].sort();
    return ['All', ...cats];
  });

  ngOnInit(): void {
    this.loadProducts();
  }

  onCategoryChange(category: string): void {
    this.selectedCategory.set(category === 'All' ? null : category);
    this.currentPage.set(0);
    this.loadProducts();
  }

  onPageChange(event: PageEvent): void {
    this.currentPage.set(event.pageIndex);
    this.loadProducts();
  }

  retry(): void {
    this.hasError.set(false);
    this.loadProducts();
  }

  loadProducts(): void {
    this.isLoading.set(true);
    this.hasError.set(false);

    const page = this.currentPage() + 1;
    const category = this.selectedCategory();

    this.catalogService.getProducts(page, this.pageSize, category).subscribe({
      next: (result) => {
        this.products.set(result.items);
        this.totalCount.set(result.totalCount);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.hasError.set(true);
      },
    });
  }
}
