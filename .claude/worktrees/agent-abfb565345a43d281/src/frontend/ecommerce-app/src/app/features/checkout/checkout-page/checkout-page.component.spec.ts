import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient, withFetch } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { provideZonelessChangeDetection } from '@angular/core';
import { CheckoutPageComponent } from './checkout-page.component';
import { Cart } from '../../../shared/models/cart.model';

const nonEmptyCart: Cart = {
  items: [{ productId: 'p1', productName: 'Widget', unitPrice: 10, quantity: 1, lineTotal: 10 }],
  itemCount: 1,
  grandTotal: 10,
};

const emptyCart: Cart = { items: [], itemCount: 0, grandTotal: 0 };

describe('CheckoutPageComponent', () => {
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [CheckoutPageComponent],
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
    vi.useRealTimers();
  });

  it('redirects to /cart when the cart is empty', async () => {
    await TestBed.compileComponents();
    httpMock = TestBed.inject(HttpTestingController);
    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate');

    const fixture = TestBed.createComponent(CheckoutPageComponent);
    fixture.detectChanges();

    httpMock.expectOne('/api/cart').flush(emptyCart);
    fixture.detectChanges();

    expect(navigateSpy).toHaveBeenCalledWith(['/cart']);
  });

  it('shows the hint text and demo toggle before Place Order is clicked', async () => {
    await TestBed.compileComponents();
    httpMock = TestBed.inject(HttpTestingController);

    const fixture = TestBed.createComponent(CheckoutPageComponent);
    fixture.detectChanges();

    httpMock.expectOne('/api/cart').flush(nonEmptyCart);
    fixture.detectChanges();

    const compiled: HTMLElement = fixture.nativeElement;
    expect(compiled.querySelector('.hint-text')?.textContent?.trim()).toBe(
      'Tip: cart totals ending in .99 simulate a payment failure.',
    );
    expect(compiled.textContent).toContain('Simulate payment failure');
    expect(compiled.querySelector('mat-stepper')).toBeTruthy();
  });

  it('calls startCheckout and disables the button while placing the order', async () => {
    await TestBed.compileComponents();
    httpMock = TestBed.inject(HttpTestingController);

    const fixture = TestBed.createComponent(CheckoutPageComponent);
    fixture.detectChanges();

    httpMock.expectOne('/api/cart').flush(nonEmptyCart);
    fixture.detectChanges();

    const compiled: HTMLElement = fixture.nativeElement;
    const placeOrderButton = Array.from(compiled.querySelectorAll('button')).find((b) =>
      b.textContent?.includes('Place Order'),
    ) as HTMLButtonElement;
    expect(placeOrderButton).toBeTruthy();

    placeOrderButton.dispatchEvent(new Event('click', { bubbles: true }));
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/checkout');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ simulatePaymentFailure: false });

    expect(placeOrderButton.hasAttribute('disabled')).toBe(true);

    req.flush({ checkoutId: 'chk-1' });
  });

  it('polls status every 1500ms and navigates to /orders/:id on a terminal state', async () => {
    vi.useFakeTimers();
    await TestBed.compileComponents();
    httpMock = TestBed.inject(HttpTestingController);
    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate');

    const fixture = TestBed.createComponent(CheckoutPageComponent);
    fixture.detectChanges();

    httpMock.expectOne('/api/cart').flush(nonEmptyCart);
    fixture.detectChanges();

    const compiled: HTMLElement = fixture.nativeElement;
    const placeOrderButton = Array.from(compiled.querySelectorAll('button')).find((b) =>
      b.textContent?.includes('Place Order'),
    ) as HTMLButtonElement;
    placeOrderButton.dispatchEvent(new Event('click', { bubbles: true }));
    fixture.detectChanges();

    httpMock.expectOne('/api/checkout').flush({ checkoutId: 'chk-1' });
    fixture.detectChanges();

    await vi.advanceTimersByTimeAsync(1500);
    httpMock.expectOne('/api/checkout/chk-1').flush({
      checkoutId: 'chk-1',
      status: 'Started',
      failureReason: null,
    });
    fixture.detectChanges();
    expect(navigateSpy).not.toHaveBeenCalledWith(['/orders', 'chk-1']);

    await vi.advanceTimersByTimeAsync(1500);
    httpMock.expectOne('/api/checkout/chk-1').flush({
      checkoutId: 'chk-1',
      status: 'AwaitingPayment',
      failureReason: null,
    });
    fixture.detectChanges();
    expect(navigateSpy).not.toHaveBeenCalledWith(['/orders', 'chk-1']);

    await vi.advanceTimersByTimeAsync(1500);
    httpMock.expectOne('/api/checkout/chk-1').flush({
      checkoutId: 'chk-1',
      status: 'Paid',
      failureReason: null,
    });
    fixture.detectChanges();

    expect(navigateSpy).toHaveBeenCalledWith(['/orders', 'chk-1']);

    // Polling must have stopped — no further request pending.
    await vi.advanceTimersByTimeAsync(3000);
    httpMock.verify();
  });
});
