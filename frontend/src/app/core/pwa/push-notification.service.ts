import { Injectable, inject, signal } from '@angular/core';
import { SwPush } from '@angular/service-worker';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class PushNotificationService {
  private readonly swPush = inject(SwPush);
  private readonly http   = inject(HttpClient);

  readonly permissionState = signal<NotificationPermission | 'unsupported'>('default');
  readonly isSubscribed    = signal(false);

  constructor() {
    if (!this.swPush.isEnabled) {
      this.permissionState.set('unsupported');
      return;
    }
    if ('Notification' in window) {
      this.permissionState.set(Notification.permission);
    }
    // Detect existing subscription on startup
    this.swPush.subscription.subscribe(sub => {
      this.isSubscribed.set(sub !== null);
    });
  }

  get isSupported(): boolean {
    return this.swPush.isEnabled && 'Notification' in window;
  }

  /**
   * Requests notification permission, subscribes to Web Push via the SW,
   * and registers the subscription endpoint with the backend.
   */
  async subscribe(vapidPublicKey: string): Promise<void> {
    if (!this.isSupported) return;

    try {
      const sub = await this.swPush.requestSubscription({ serverPublicKey: vapidPublicKey });
      this.permissionState.set(Notification.permission);

      const json = sub.toJSON();
      await firstValueFrom(
        this.http.post('/bff/notifications/push/subscribe', {
          endpoint: json.endpoint,
          p256dh:   json.keys?.['p256dh'],
          auth:     json.keys?.['auth'],
        }),
      );
      this.isSubscribed.set(true);
    } catch {
      this.permissionState.set(Notification.permission);
    }
  }

  /** Unsubscribes from push and tells the backend to remove the subscription. */
  async unsubscribe(): Promise<void> {
    if (!this.isSupported) return;
    try {
      const sub = await firstValueFrom(this.swPush.subscription);
      if (!sub) return;
      await this.swPush.unsubscribe();
      await firstValueFrom(
        this.http.post('/bff/notifications/push/unsubscribe', {
          endpoint: sub.endpoint,
        }),
      );
      this.isSubscribed.set(false);
    } catch { /* silent */ }
  }
}
