import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient, withFetch } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, provideRouter, Router } from '@angular/router';
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
    vi.useRealTimers();
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

  it('shows the "Preparing your shipment…" indicator while the order is Paid', async () => {
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
    expect(fixture.componentInstance.isShipping()).toBe(true);
    expect(compiled.querySelector('.shipping-indicator-text')?.textContent?.trim()).toBe(
      'Preparing your shipment…',
    );
    // No fake timers in this test — interval(1500) has not ticked yet, so no second
    // request is pending; httpMock.verify() in afterEach confirms exactly one request fired.
  });

  it('does not show the shipping indicator for a cancelled order', async () => {
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
    expect(fixture.componentInstance.isShipping()).toBe(false);
    expect(compiled.querySelector('.shipping-indicator-text')).toBeNull();
    // Cancelled is terminal — polling must not have started a second request.
    httpMock.verify();
  });

  it('polls every 1500ms and stops once the order reaches Fulfilled, updating the displayed order', async () => {
    vi.useFakeTimers();
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
    expect(fixture.componentInstance.isShipping()).toBe(true);

    await vi.advanceTimersByTimeAsync(1500);
    httpMock.expectOne('/api/orders/o1').flush(paidOrder);
    fixture.detectChanges();
    expect(fixture.componentInstance.isShipping()).toBe(true);

    const fulfilledOrder: OrderDetail = { ...paidOrder, status: 'Fulfilled' };
    await vi.advanceTimersByTimeAsync(1500);
    httpMock.expectOne('/api/orders/o1').flush(fulfilledOrder);
    fixture.detectChanges();

    expect(fixture.componentInstance.order()?.status).toBe('Fulfilled');
    expect(fixture.componentInstance.isShipping()).toBe(false);

    // Polling must have stopped — no further request pending.
    await vi.advanceTimersByTimeAsync(3000);
    httpMock.verify();
  });

  it('recovers from a transient poll error and still reaches a terminal status on the next tick', async () => {
    vi.useFakeTimers();
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
    expect(fixture.componentInstance.isShipping()).toBe(true);

    // Transient error on the first poll tick — polling loop must survive it.
    await vi.advanceTimersByTimeAsync(1500);
    httpMock.expectOne('/api/orders/o1').flush(null, { status: 500, statusText: 'Server Error' });
    fixture.detectChanges();
    expect(fixture.componentInstance.order()?.status).toBe('Paid');
    expect(fixture.componentInstance.isShipping()).toBe(true);

    // Next tick succeeds and reaches a terminal status — proving the loop kept ticking.
    const fulfilledOrder: OrderDetail = { ...paidOrder, status: 'Fulfilled' };
    await vi.advanceTimersByTimeAsync(1500);
    httpMock.expectOne('/api/orders/o1').flush(fulfilledOrder);
    fixture.detectChanges();

    expect(fixture.componentInstance.order()?.status).toBe('Fulfilled');
    expect(fixture.componentInstance.isShipping()).toBe(false);

    // Polling must have stopped at the terminal status — no further request pending.
    await vi.advanceTimersByTimeAsync(3000);
    httpMock.verify();
  });

  it('stops polling on component destroy', async () => {
    vi.useFakeTimers();
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

    fixture.destroy();

    await vi.advanceTimersByTimeAsync(3000);
    httpMock.verify();
  });

  it('does not navigate on terminal status — stays on /orders/:id', async () => {
    vi.useFakeTimers();
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
    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate');

    const fixture = TestBed.createComponent(OrderDetailComponent);
    fixture.detectChanges();

    httpMock.expectOne('/api/orders/o1').flush(paidOrder);
    fixture.detectChanges();

    const fulfilledOrder: OrderDetail = { ...paidOrder, status: 'Fulfilled' };
    await vi.advanceTimersByTimeAsync(1500);
    httpMock.expectOne('/api/orders/o1').flush(fulfilledOrder);
    fixture.detectChanges();

    expect(navigateSpy).not.toHaveBeenCalled();
  });
});
