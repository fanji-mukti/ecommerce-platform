import { describe, it, expect } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient, withFetch } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { provideZonelessChangeDetection } from '@angular/core';
import { NotificationsPageComponent } from './notifications-page.component';
import { NotificationEntry } from '../../../shared/models/notification.model';

const entries: NotificationEntry[] = [
  {
    id: 'n1',
    orderId: 'o1',
    message: 'Your order has been paid.',
    eventType: 'OrderPaid',
    occurredAt: '2026-08-12T00:00:00Z',
  },
  {
    id: 'n2',
    orderId: 'o2',
    message: 'Your order has shipped.',
    eventType: 'OrderShipped',
    occurredAt: '2026-08-12T01:00:00Z',
  },
  {
    id: 'n3',
    orderId: 'o3',
    message: 'Payment failed for your order.',
    eventType: 'PaymentFailed',
    occurredAt: '2026-08-12T02:00:00Z',
  },
];

function setup() {
  TestBed.configureTestingModule({
    imports: [NotificationsPageComponent],
    providers: [
      provideZonelessChangeDetection(),
      provideHttpClient(withFetch()),
      provideHttpClientTesting(),
      provideRouter([]),
    ],
  });
  return TestBed.inject(HttpTestingController);
}

describe('NotificationsPageComponent', () => {
  it('shows a loading indicator while the request is in flight', async () => {
    const httpMock = setup();
    await TestBed.compileComponents();

    const fixture = TestBed.createComponent(NotificationsPageComponent);
    fixture.detectChanges();

    const compiled: HTMLElement = fixture.nativeElement;
    expect(compiled.querySelector('mat-progress-bar')).not.toBeNull();

    httpMock.expectOne('/api/notifications').flush([]);
    httpMock.verify();
  });

  it('shows the empty state with no action button when there are zero entries', async () => {
    const httpMock = setup();
    await TestBed.compileComponents();

    const fixture = TestBed.createComponent(NotificationsPageComponent);
    fixture.detectChanges();

    httpMock.expectOne('/api/notifications').flush([]);
    fixture.detectChanges();

    const compiled: HTMLElement = fixture.nativeElement;
    expect(compiled.querySelector('.empty-state h2')?.textContent?.trim()).toBe(
      'No notifications yet',
    );
    expect(compiled.querySelector('.empty-state p')?.textContent?.trim()).toBe(
      'Updates about your orders will show up here.',
    );
    expect(compiled.querySelector('.empty-state button')).toBeNull();
    httpMock.verify();
  });

  it('shows the error state with a Retry button that re-fetches on click', async () => {
    const httpMock = setup();
    await TestBed.compileComponents();

    const fixture = TestBed.createComponent(NotificationsPageComponent);
    fixture.detectChanges();

    httpMock.expectOne('/api/notifications').flush(null, { status: 500, statusText: 'Error' });
    fixture.detectChanges();

    const compiled: HTMLElement = fixture.nativeElement;
    expect(compiled.querySelector('.error-state h2')?.textContent?.trim()).toBe(
      'Failed to load notifications',
    );
    const retryButton = compiled.querySelector<HTMLButtonElement>('.error-state button');
    expect(retryButton?.textContent?.trim()).toBe('Retry');

    retryButton?.click();
    fixture.detectChanges();

    httpMock.expectOne('/api/notifications').flush(entries);
    fixture.detectChanges();

    expect(compiled.querySelector('.error-state')).toBeNull();
    expect(compiled.querySelectorAll('mat-list-item, a[mat-list-item]').length).toBe(3);
    httpMock.verify();
  });

  it('renders a mat-list row per entry with the correct icon, destructive styling only for PaymentFailed, and an /orders/:id link', async () => {
    const httpMock = setup();
    await TestBed.compileComponents();

    const fixture = TestBed.createComponent(NotificationsPageComponent);
    fixture.detectChanges();

    httpMock.expectOne('/api/notifications').flush(entries);
    fixture.detectChanges();

    const compiled: HTMLElement = fixture.nativeElement;
    const rows = compiled.querySelectorAll('a[mat-list-item]');
    expect(rows.length).toBe(3);

    const icons = compiled.querySelectorAll('mat-icon');
    expect(icons[0].textContent?.trim()).toBe('payment');
    expect(icons[0].classList.contains('destructive-icon')).toBe(false);
    expect(icons[1].textContent?.trim()).toBe('local_shipping');
    expect(icons[1].classList.contains('destructive-icon')).toBe(false);
    expect(icons[2].textContent?.trim()).toBe('error');
    expect(icons[2].classList.contains('destructive-icon')).toBe(true);

    expect(rows[0].getAttribute('href')).toBe('/orders/o1');
    expect(rows[0].textContent).toContain('Your order has been paid.');

    httpMock.verify();
  });

  it('maps eventType to the correct icon via iconFor()', () => {
    setup();
    // Component instance created without detectChanges() -> ngOnInit does not
    // fire and no HTTP request is issued; safe to inspect pure methods only.
    const fixture = TestBed.createComponent(NotificationsPageComponent);
    const cmp = fixture.componentInstance;
    expect(cmp.iconFor('OrderPaid')).toBe('payment');
    expect(cmp.iconFor('OrderShipped')).toBe('local_shipping');
    expect(cmp.iconFor('PaymentFailed')).toBe('error');
    expect(cmp.isDestructive('PaymentFailed')).toBe(true);
    expect(cmp.isDestructive('OrderPaid')).toBe(false);
    expect(cmp.isDestructive('OrderShipped')).toBe(false);
  });
});
