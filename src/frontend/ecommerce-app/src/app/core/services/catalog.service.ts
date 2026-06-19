import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Product, PagedResult } from '../../shared/models/product.model';

@Injectable({ providedIn: 'root' })
export class CatalogService {
  private http = inject(HttpClient);

  getProducts(page: number, pageSize: number, category?: string | null): Observable<PagedResult<Product>> {
    let url = `/api/catalog/products?page=${page}&pageSize=${pageSize}`;
    if (category) {
      url += `&category=${encodeURIComponent(category)}`;
    }
    return this.http.get<PagedResult<Product>>(url);
  }

  getProduct(id: string): Observable<Product> {
    return this.http.get<Product>(`/api/catalog/products/${id}`);
  }
}
