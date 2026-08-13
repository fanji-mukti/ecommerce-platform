import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient, withFetch } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { provideZonelessChangeDetection } from '@angular/core';
import { CartPageComponent } from './cart-page.component';
import { Cart } from '../../../shared/models/cart.model';

describe('CartPageComponent', () => {
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [CartPageComponent],
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(withFetch()),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    });
  });

  afterEach(() => {
    httpMock?.verify();
  });

  it('renders "Your Cart" as the h1 heading', async () => {
    await TestBed.compileComponents();
    httpMock = TestBed.inject(HttpTestingController);

    const fixture = TestBed.createComponent(CartPageComponent);
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/cart');
    req.flush({ items: [], itemCount: 0, grandTotal: 0 } satisfies Cart);
    fixture.detectChanges();

    const compiled: HTMLElement = fixture.nativeElement;
    expect(compiled.querySelector('h1')?.textContent?.trim()).toBe('Your Cart');
  });

  it('renders empty state with a Browse Catalog link when the cart has no items', async () => {
    await TestBed.compileComponents();
    httpMock = TestBed.inject(HttpTestingController);

    const fixture = TestBed.createComponent(CartPageComponent);
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/cart');
    req.flush({ items: [], itemCount: 0, grandTotal: 0 } satisfies Cart);
    fixture.detectChanges();

    const compiled: HTMLElement = fixture.nativeElement;
    expect(compiled.querySelector('.empty-state h2')?.textContent?.trim()).toBe(
      'Your cart is empty',
    );
    expect(compiled.querySelector('.empty-state a')?.textContent?.trim()).toBe('Browse Catalog');
  });

  it('pluralizes the item-count subtext: 1 item vs N items', async () => {
    await TestBed.compileComponents();
    httpMock = TestBed.inject(HttpTestingController);

    const fixture = TestBed.createComponent(CartPageComponent);
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/cart');
    req.flush({
      items: [
        {
          productId: 'p1',
          productName: 'Widget',
          unitPrice: 10,
          quantity: 1,
          lineTotal: 10,
        },
      ],
      itemCount: 1,
      grandTotal: 10,
    } satisfies Cart);
    fixture.detectChanges();

    let compiled: HTMLElement = fixture.nativeElement;
    expect(compiled.querySelector('.summary-count')?.textContent?.trim()).toBe('1 item');

    fixture.componentInstance.cart.set({
      items: [
        {
          productId: 'p1',
          productName: 'Widget',
          unitPrice: 10,
          quantity: 3,
          lineTotal: 30,
        },
      ],
      itemCount: 3,
      grandTotal: 30,
    });
    fixture.detectChanges();

    compiled = fixture.nativeElement;
    expect(compiled.querySelector('.summary-count')?.textContent?.trim()).toBe('3 items');
  });
});
