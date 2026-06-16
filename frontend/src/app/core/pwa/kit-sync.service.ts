import { Injectable, inject, signal } from '@angular/core';
import { KitOfflineQueueService } from './kit-offline-queue.service';

/**
 * Drains the KitOps offline action queue whenever connectivity is restored.
 * Uses raw `fetch` with `credentials: 'include'` so the HttpOnly session
 * cookie is forwarded — identical to the workflow SyncService pattern.
 *
 * Conflict safety: actions are replayed in FIFO order; individual failures
 * leave the action in the queue for the next reconnect rather than aborting
 * the whole drain so later independent actions still succeed.
 */
@Injectable({ providedIn: 'root' })
export class KitSyncService {
  private readonly queue = inject(KitOfflineQueueService);

  readonly pendingCount  = signal(0);
  readonly isSyncing     = signal(false);
  readonly lastSyncError = signal<string | null>(null);

  constructor() {
    this.refreshCount();
    window.addEventListener('online', () => this.drain());
  }

  async refreshCount(): Promise<void> {
    this.pendingCount.set(await this.queue.count());
  }

  /** Replays all queued kit actions against the BFF.  FIFO, failure-tolerant. */
  async drain(): Promise<void> {
    if (this.isSyncing()) return;

    const items = await this.queue.getAll();
    if (!items.length) return;

    this.isSyncing.set(true);
    this.lastSyncError.set(null);
    let errors = 0;

    for (const item of items) {
      try {
        let url: string;
        let method: string;

        if (item.type === 'checkout') {
          url    = '/bff/kit-ops/checkouts';
          method = 'POST';
        } else {
          url    = `/bff/kit-ops/checkouts/${item.checkoutId}/return`;
          method = 'PUT';
        }

        const res = await fetch(url, {
          method,
          headers:     { 'Content-Type': 'application/json' },
          body:        item.payload,
          credentials: 'include',
        });

        // 2xx = success, 409 = already applied (idempotent conflict) — both safe to dequeue
        if (res.ok || res.status === 409) {
          await this.queue.dequeue(item.id);
        } else {
          errors++;
        }
      } catch {
        errors++;
      }
    }

    this.isSyncing.set(false);
    await this.refreshCount();

    if (errors > 0) {
      this.lastSyncError.set(
        `${errors} action(s) could not be synced — will retry on next reconnect.`,
      );
    }
  }
}
