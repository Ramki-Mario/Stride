import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { NotificationDto } from '../models/notification.models';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly http = inject(HttpClient);
  private readonly base = '/bff/notifications';

  // ── Server state ────────────────────────────────────────────────────────
  readonly notifications  = signal<NotificationDto[]>([]);
  readonly isLoading      = signal(false);
  readonly error          = signal<string | null>(null);

  // ── Derived ─────────────────────────────────────────────────────────────
  readonly unreadCount = computed(
    () => this.notifications().filter(n => !n.isRead).length
  );

  readonly recentNotifications = computed(
    () => [...this.notifications()].slice(0, 8)
  );

  // ── API calls ────────────────────────────────────────────────────────────

  loadNotifications(): void {
    this.isLoading.set(true);
    this.error.set(null);

    this.http.get<NotificationDto[]>(this.base).subscribe({
      next:  data => { this.notifications.set(data); this.isLoading.set(false); },
      error: ()   => { this.error.set('Failed to load notifications.'); this.isLoading.set(false); },
    });
  }

  getUnreadCount(): Observable<number> {
    return this.http.get<number>(`${this.base}/unread-count`);
  }

  markAsRead(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/read`, {}).pipe(
      tap(() => {
        this.notifications.update(list =>
          list.map(n => n.id === id ? { ...n, isRead: true } : n)
        );
      })
    );
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`).pipe(
      tap(() => {
        this.notifications.update(list => list.filter(n => n.id !== id));
      })
    );
  }

  markAllRead(): void {
    const unread = this.notifications().filter(n => !n.isRead);
    unread.forEach(n => this.markAsRead(n.id).subscribe());
  }
}
