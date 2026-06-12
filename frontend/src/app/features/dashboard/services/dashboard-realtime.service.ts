import { Injectable, signal } from '@angular/core';
import { Observable, Subject } from 'rxjs';
import {
  HubConnection,
  HubConnectionBuilder,
  LogLevel,
} from '@microsoft/signalr';

/**
 * Connection state of the dashboard hub.
 *  - 'connecting'   → initial connect or automatic reconnect in flight
 *  - 'connected'    → live pushes flowing; polling fallback OFF
 *  - 'disconnected' → no socket; polling fallback ON
 */
export type RealtimeState = 'connecting' | 'connected' | 'disconnected';

/**
 * DashboardRealtimeService (US-176)
 *
 * Maintains a SignalR connection to the BFF dashboard hub. Authentication rides
 * on the same HttpOnly session cookie as every other BFF call — no tokens in JS.
 *
 * The hub is push-only: the server sends 'dashboardUpdate' with an event type
 * string ('stepCompleted', 'stepAssigned', 'stepOverdue', 'workflowCompleted',
 * 'invoiceCreated') and the dashboard refetches the affected panels over HTTP.
 *
 * Reconnect policy never gives up (capped 30s backoff), so a long network blip
 * eventually returns to live mode and the consumer's polling fallback switches off.
 */
@Injectable({ providedIn: 'root' })
export class DashboardRealtimeService {
  private connection: HubConnection | null = null;
  private retryTimer: ReturnType<typeof setTimeout> | null = null;
  private stopping = false;

  readonly state = signal<RealtimeState>('disconnected');

  private readonly updatesSubject = new Subject<string>();
  /** Emits the event type each time the server pushes a dashboard update. */
  readonly updates$: Observable<string> = this.updatesSubject.asObservable();

  /** Opens the hub connection. Safe to call when already started. */
  start(): void {
    if (this.connection) return;
    this.stopping = false;

    this.connection = new HubConnectionBuilder()
      .withUrl('/bff/hubs/dashboard')
      .withAutomaticReconnect({
        // Never give up: 0s, 2s, 10s, then every 30s — a manager keeps the
        // dashboard open all day and must return to live mode after any outage.
        nextRetryDelayInMilliseconds: ctx =>
          [0, 2_000, 10_000][ctx.previousRetryCount] ?? 30_000,
      })
      .configureLogging(LogLevel.Warning)
      .build();

    this.connection.on('dashboardUpdate', (eventType: string) =>
      this.updatesSubject.next(eventType));

    this.connection.onreconnecting(() => this.state.set('connecting'));
    this.connection.onreconnected(()  => this.state.set('connected'));
    this.connection.onclose(()        => {
      if (!this.stopping) this.state.set('disconnected');
    });

    void this.connect();
  }

  /** Closes the connection and cancels any pending retry. */
  stop(): void {
    this.stopping = true;
    if (this.retryTimer !== null) {
      clearTimeout(this.retryTimer);
      this.retryTimer = null;
    }

    const conn = this.connection;
    this.connection = null;
    this.state.set('disconnected');

    void conn?.stop();
  }

  private async connect(): Promise<void> {
    if (!this.connection || this.stopping) return;

    this.state.set('connecting');
    try {
      await this.connection.start();
      if (!this.stopping) this.state.set('connected');
    } catch {
      if (this.stopping) return;
      // Initial connect failed (BFF down / network) — flip to disconnected so the
      // polling fallback engages, and retry on the same cadence as reconnects.
      this.state.set('disconnected');
      this.retryTimer = setTimeout(() => void this.connect(), 30_000);
    }
  }
}
