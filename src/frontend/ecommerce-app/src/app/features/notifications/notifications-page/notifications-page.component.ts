import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatButtonModule } from '@angular/material/button';
import { NotificationEntry, NotificationEventType } from '../../../shared/models/notification.model';
import { NotificationsService } from '../../../core/services/notifications.service';

@Component({
  selector: 'app-notifications-page',
  standalone: true,
  imports: [
    DatePipe,
    RouterLink,
    MatListModule,
    MatIconModule,
    MatProgressBarModule,
    MatButtonModule,
  ],
  templateUrl: './notifications-page.component.html',
  styleUrl: './notifications-page.component.scss',
})
export class NotificationsPageComponent implements OnInit {
  private notificationsService = inject(NotificationsService);

  notifications = signal<NotificationEntry[]>([]);
  isLoading = signal(false);
  hasError = signal(false);

  ngOnInit(): void {
    this.loadNotifications();
  }

  retry(): void {
    this.loadNotifications();
  }

  iconFor(eventType: NotificationEventType): string {
    switch (eventType) {
      case 'OrderPaid':
        return 'payment';
      case 'OrderShipped':
        return 'local_shipping';
      case 'PaymentFailed':
        return 'error';
    }
  }

  isDestructive(eventType: NotificationEventType): boolean {
    return eventType === 'PaymentFailed';
  }

  private loadNotifications(): void {
    this.isLoading.set(true);
    this.hasError.set(false);

    this.notificationsService.getNotifications().subscribe({
      next: (notifications) => {
        this.notifications.set(notifications);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.hasError.set(true);
      },
    });
  }
}
