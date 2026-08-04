import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient, withFetch } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, provideRouter, Router } from '@angular/router';
import { provideZonelessChangeDetection } from '@angular/core';
import { ProductDetailComponent } from './product-detail.component';
import { Product } from '../../../shared/models/product.model';
import { Cart } from '../../../shared/models/cart.model';

const testProduct: Product = {
  id: 'p1',
  name: 'Widget',
  sku: 'SKU-1',
  description: 'A widget',
  price: 9.99,
  stockQuantity: 20,
  category: 'Gadgets',
  imageUrl: null,
};

describe('ProductDetailComponent', () => {
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ProductDetailComponent],
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(withFetch()),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { snapshot: { params: { id: 'p1' } } } },
      ],
    });
  });

  afterEach(() => {
    httpMock?.verify();
  });

  it('renders an enabled "Add to Cart" button (no "Coming Soon" placeholder)', async () => {
    await TestBed.compileComponents();
    httpMock = TestBed.inject(HttpTestingController);

    const fixture = TestBed.createComponent(ProductDetailComponent);
    fixture.detectChanges();

    httpMock.expectOne('/api/catalog/products/p1').flush(testProduct);
    fixture.detectChanges();

    const compiled: HTMLElement = fixture.nativeElement;
    const button = compiled.querySelector('button');
    expect(button?.textContent?.trim()).toBe('Add to Cart');
    expect(button?.hasAttribute('disabled')).toBe(false);
  });

  it('calls CartService.addItem and navigates to /cart on click', async () => {
    await TestBed.compileComponents();
    httpMock = TestBed.inject(HttpTestingController);

    const fixture = TestBed.createComponent(ProductDetailComponent);
    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate');

    fixture.detectChanges();
    httpMock.expectOne('/api/catalog/products/p1').flush(testProduct);
    fixture.detectChanges();

    const compiled: HTMLElement = fixture.nativeElement;
    compiled.querySelector('button')?.dispatchEvent(new Event('click', { bubbles: true }));
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/cart/items');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ productId: 'p1', quantity: 1 });
    req.flush({ items: [], itemCount: 1, grandTotal: 9.99 } satisfies Cart);

    expect(navigateSpy).toHaveBeenCalledWith(['/cart']);
  });
});
