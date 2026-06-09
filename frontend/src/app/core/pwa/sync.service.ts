import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ConnectivityService } from './connectivity.service';
import { OfflineQueueService } from './offline-queue.service';

@Injectable({ providedIn: 'root' })
export class SyncService {
  private readonly http         = inject(HttpClient);
  private readonly connectivity = inject(ConnectivityService);
  private readonly queue        = inject(OfflineQueueService);

  readonly pendingCount = signal(0);
  readonly isSyncing    = signal(false);
  readonly lastError    = signal<string | null>(null);

  constructor() {
    // Refresh count on startup
    this.refreshCount();

    // Auto-drain when we come back online
    window.addEventListener('online', () => this.drain());
  }

  async refreshCount(): Promise<void> {
    this.pendingCount.set(await this.queue.count());
  }

  /** Submits all queued step completions. Called automatically on reconnect. */
  async drain(): Promise<void> {
    if (this.isSyncing()) return;
    const items = await this.queue.getAll();
    if (!items.length) return;

    this.isSyncing.set(true);
    this.lastError.set(null);
    let errors = 0;

    for (const item of items) {
      try {
        await fetch(
          `/bff/workflows/instances/${item.instanceId}/steps/${item.stepId}/complete`,
          {
            method:      'POST',
            headers:     { 'Content-Type': 'application/json' },
            body:        item.payload,
            credentials: 'include',  // send session cookie
          },
        );
        await this.queue.dequeue(item.id);
      } catch {
        errors++;
      }
    }

    this.isSyncing.set(false);
    await this.refreshCount();

    if (errors > 0) {
      this.lastError.set(`${errors} completion(s) could not be synced — will retry on next reconnect.`);
    }
  }
}
