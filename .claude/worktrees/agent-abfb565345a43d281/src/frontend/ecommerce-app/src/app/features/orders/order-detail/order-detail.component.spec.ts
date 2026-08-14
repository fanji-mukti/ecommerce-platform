import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient, withFetch } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { provideZonelessChangeDetection } from '@angular/core';
import { OrderDetailComponent } from './order-detail.component';
import { OrderDetail } from '../../../shared/models/order.model';

const paidOrder: OrderDetail = {
  id: 'o1',
  status: 'Paid',
  totalAmount: 19.99,
  lineItems: [{ productId: 'p1', productName: 'Widget', unitPrice: 19.99, quantity: 1 }],
  createdAt: '2026-08-12T00:00:00Z',
  failureReason: null,
};

const cancelledOrder: OrderDetail = {
  id: 'o2',
  status: 'Cancelled',
  totalAmount: 9.99,
  lineItems: [{ productId: 'p1', productName: 'Widget', unitPrice: 9.99, quantity: 1 }],
  createdAt: '2026-08-12T00:00:00Z',
  failureReason: 'Payment declined',
};

describe('OrderDetailComponent', () => {
  let httpMock: HttpTestingController;

  afterEach(() => {
    httpMock?.verify();
  });

  it('shows not-found state when the route id param is missing', async () => {
    TestBed.configureTestingModule({
      imports: [OrderDetailComponent],
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(withFetch()),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { snapshot: { params: {} } } },
      ],
    });
    await TestBed.compileComponents();
    httpMock = TestBed.inject(HttpTestingController);

    const fixture = TestBed.createComponent(OrderDetailComponent);
    fixture.detectChanges();

    const compiled: HTMLElement = fixture.nativeElement;
    expect(compiled.querySelector('.not-found-state h1, .not-found-state h2')?.textContent?.trim()).toBe(
      'Order not found',
    );
  });

  it('shows not-found state when the order fetch errors', async () => {
    TestBed.configureTestingModule({
      imports: [OrderDetailComponent],
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(withFetch()),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { snapshot: { params: { id: 'missing' } } } },
      ],
    });
    await TestBed.compileComponents();
    httpMock = TestBed.inject(HttpTestingController);

    const fixture = TestBed.createComponent(OrderDetailComponent);
    fixture.detectChanges();

    httpMock.expectOne('/api/orders/missing').flush(null, { status: 404, statusText: 'Not Found' });
    fixture.detectChanges();

    const compiled: HTMLElement = fixture.nativeElement;
    expect(compiled.querySelector('.not-found-state h1, .not-found-state h2')?.textContent?.trim()).toBe(
      'Order not found',
    );
  });

  it('renders the verbatim failure reason and a warn-colored chip for a cancelled order', async () => {
    TestBed.configureTestingModule({
      imports: [OrderDetailComponent],
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(withFetch()),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { snapshot: { params: { id: 'o2' } } } },
      ],
    });
    await TestBed.compileComponents();
    httpMock = TestBed.inject(HttpTestingController);

    const fixture = TestBed.createComponent(OrderDetailComponent);
    fixture.detectChanges();

    httpMock.expectOne('/api/orders/o2').flush(cancelledOrder);
    fixture.detectChanges();

    const compiled: HTMLElement = fixture.nativeElement;
    expect(compiled.querySelector('.failure-reason')?.textContent?.trim()).toBe('Payment declined');
    expect(fixture.componentInstance.statusColor()).toBe('warn');
  });

  it('renders no failure-reason paragraph and a primary chip for a paid order', async () => {
    TestBed.configureTestingModule({
      imports: [OrderDetailComponent],
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(withFetch()),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { snapshot: { params: { id: 'o1' } } } },
      ],
    });
    await TestBed.compileComponents();
    httpMock = TestBed.inject(HttpTestingController);

    const fixture = TestBed.createComponent(OrderDetailComponent);
    fixture.detectChanges();

    httpMock.expectOne('/api/orders/o1').flush(paidOrder);
    fixture.detectChanges();

    const compiled: HTMLElement = fixture.nativeElement;
    expect(compiled.querySelector('.failure-reason')).toBeNull();
    expect(fixture.componentInstance.statusColor()).toBe('primary');
  });
});
