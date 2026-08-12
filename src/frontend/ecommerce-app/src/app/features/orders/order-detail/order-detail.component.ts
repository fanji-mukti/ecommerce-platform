import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CurrencyPipe } from '@angular/common';
import { MatChipsModule } from '@angular/material/chips';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { OrderDetail } from '../../../shared/models/order.model';
import { OrdersService } from '../../../core/services/orders.service';

const FAILURE_STATUSES = ['Cancelled', 'Failed'];

@Component({
  selector: 'app-order-detail',
  standalone: true,
  imports: [MatChipsModule, MatButtonModule, MatIconModule, MatCardModule, RouterLink, CurrencyPipe],
  templateUrl: './order-detail.component.html',
  styleUrl: './order-detail.component.scss',
})
export class OrderDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private ordersService = inject(OrdersService);

  order = signal<OrderDetail | null>(null);
  isLoading = signal<boolean>(false);
  notFound = signal<boolean>(false);

  statusColor = computed((): 'warn' | 'primary' =>
    FAILURE_STATUSES.includes(this.order()?.status ?? '') ? 'warn' : 'primary',
  );

  showFailureReason = computed(
    (): boolean =>
      FAILURE_STATUSES.includes(this.order()?.status ?? '') && !!this.order()?.failureReason,
  );

  ngOnInit(): void {
    const id = this.route.snapshot.params['id'];
    if (!id) {
      this.notFound.set(true);
      return;
    }
    this.isLoading.set(true);
    this.ordersService.getOrder(id).subscribe({
      next: (order) => {
        this.order.set(order);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.notFound.set(true);
      },
    });
  }
}
