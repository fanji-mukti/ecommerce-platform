import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { NotificationEntry } from '../../shared/models/notification.model';

@Injectable({ providedIn: 'root' })
export class NotificationsService {
  private http = inject(HttpClient);

  getNotifications(): Observable<NotificationEntry[]> {
    return this.http.get<NotificationEntry[]>('/api/notifications');
  }
}
