import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { NgClass } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { interval } from 'rxjs';
import { HealthService } from '../../services/health.service';
import { HealthEntry, HealthStatus } from '../../models/health.models';

/** Friendly display names for each health-check entry name. */
const ENTRY_LABELS: Record<string, { label: string; icon: string }> = {
  'sql-server': { label: 'SQL Server',  icon: 'db'    },
  'redis':      { label: 'Redis Cache', icon: 'cache' },
};

const AUTO_REFRESH_MS = 30_000; // 30 seconds

@Component({
  selector: 'app-health-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgClass],
  template: `
    <div class="hp-page">

      <!-- ── Header ──────────────────────────────────────────────────── -->
      <div class="hp-header">
        <div>
          <h1 class="hp-title">System Health</h1>
          <p class="hp-subtitle">
            @if (service.lastChecked()) {
              Last checked {{ formatTime(service.lastChecked()!) }}
              <span class="hp-auto-badge">next refresh in {{ countdown() }}s</span>
            } @else {
              Checking status…
            }
          </p>
        </div>

        <button class="stride-btn stride-btn-secondary"
                (click)="refresh()"
                [disabled]="service.isLoading()">
          @if (service.isLoading()) { Refreshing… } @else { Refresh now }
        </button>
      </div>

      <!-- ── Overall status banner ─────────────────────────────────── -->
      @if (service.report(); as report) {
        <div class="hp-banner" [ngClass]="bannerClass(report.status)">
          <span class="hp-banner-dot" [ngClass]="dotClass(report.status)"></span>
          <span class="hp-banner-text">
            Overall: <strong>{{ report.status }}</strong>
          </span>
          <span class="hp-banner-dur">{{ formatDuration(report.totalDuration) }}</span>
        </div>
      }

      <!-- ── Loading skeleton ───────────────────────────────────────── -->
      @if (service.isLoading() && !service.report()) {
        <div class="hp-cards">
          @for (i of [1,2]; track i) {
            <div class="hp-card hp-card--skeleton">
              <div class="sk-icon"></div>
              <div class="sk-body">
                <div class="sk-line sk-line--title"></div>
                <div class="sk-line sk-line--status"></div>
              </div>
            </div>
          }
        </div>
      }

      <!-- ── Error state ─────────────────────────────────────────────── -->
      @else if (service.error()) {
        <div class="hp-empty">
          <svg width="40" height="40" viewBox="0 0 24 24" fill="none" aria-hidden="true">
            <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="1.5"/>
            <line x1="12" y1="8" x2="12" y2="12" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
            <line x1="12" y1="16" x2="12.01" y2="16" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
          </svg>
          <p>{{ service.error() }}</p>
          <button class="stride-btn stride-btn-secondary" (click)="service.loadHealth()">Retry</button>
        </div>
      }

      <!-- ── Health cards ────────────────────────────────────────────── -->
      @else if (service.report(); as report) {
        <div class="hp-cards">
          @for (entry of reportEntries(); track entry.name) {
            <div class="hp-card" [ngClass]="cardClass(entry.status)">

              <!-- Icon -->
              <div class="hp-card-icon" [ngClass]="iconClass(entry.status)">
                @if (entry.icon === 'db') {
                  <!-- Database icon -->
                  <svg width="20" height="20" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                    <ellipse cx="12" cy="5" rx="9" ry="3" stroke="currentColor" stroke-width="1.8"/>
                    <path d="M3 5v6c0 1.66 4.03 3 9 3s9-1.34 9-3V5"
                          stroke="currentColor" stroke-width="1.8" stroke-linecap="round"/>
                    <path d="M3 11v6c0 1.66 4.03 3 9 3s9-1.34 9-3v-6"
                          stroke="currentColor" stroke-width="1.8" stroke-linecap="round"/>
                  </svg>
                } @else {
                  <!-- Cache / lightning icon -->
                  <svg width="20" height="20" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                    <path d="M13 2L3 14h9l-1 8 10-12h-9l1-8z"
                          stroke="currentColor" stroke-width="1.8"
                          stroke-linecap="round" stroke-linejoin="round"/>
                  </svg>
                }
              </div>

              <!-- Body -->
              <div class="hp-card-body">
                <div class="hp-card-name">{{ entry.label }}</div>
                <div class="hp-card-status" [ngClass]="statusTextClass(entry.status)">
                  <span class="hp-status-dot" [ngClass]="dotClass(entry.status)"></span>
                  {{ entry.status }}
                </div>
                @if (entry.description) {
                  <p class="hp-card-desc">{{ entry.description }}</p>
                }
                @if (entry.exception) {
                  <p class="hp-card-exc">{{ entry.exception }}</p>
                }
              </div>

              <!-- Duration -->
              <div class="hp-card-dur">{{ formatDuration(entry.duration) }}</div>

              <!-- Tags -->
              @if (entry.tags.length) {
                <div class="hp-card-tags">
                  @for (tag of entry.tags; track tag) {
                    <span class="hp-tag">{{ tag }}</span>
                  }
                </div>
              }
            </div>
          }
        </div>
      }

    </div>
  `,
  styles: [`
    .hp-page {
      max-width: 52rem;
      margin: 0 auto;
      padding: 1.5rem 1rem 3rem;
    }

    /* Header */
    .hp-header {
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      gap: 1rem;
      margin-bottom: 1.5rem;
    }

    .hp-title {
      font-size: 1.375rem;
      font-weight: 700;
      color: var(--stride-text-primary);
      margin: 0 0 0.25rem;
    }

    .hp-subtitle {
      font-size: 0.875rem;
      color: var(--stride-text-muted);
      margin: 0;
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }

    .hp-auto-badge {
      font-size: 0.75rem;
      font-weight: 500;
      padding: 0.1rem 0.5rem;
      border-radius: 99px;
      background: var(--stride-surface-secondary);
      color: var(--stride-text-muted);
    }

    /* Banner */
    .hp-banner {
      display: flex;
      align-items: center;
      gap: 0.625rem;
      padding: 0.75rem 1.25rem;
      border-radius: var(--stride-radius-lg);
      margin-bottom: 1.5rem;
      font-size: 0.9375rem;
    }

    .hp-banner--healthy   { background: color-mix(in srgb, #10B981 8%, transparent); border: 1px solid color-mix(in srgb, #10B981 25%, transparent); }
    .hp-banner--degraded  { background: color-mix(in srgb, #F59E0B 8%, transparent); border: 1px solid color-mix(in srgb, #F59E0B 25%, transparent); }
    .hp-banner--unhealthy { background: color-mix(in srgb, #EF4444 8%, transparent); border: 1px solid color-mix(in srgb, #EF4444 25%, transparent); }

    .hp-banner-text { flex: 1; color: var(--stride-text-primary); }
    .hp-banner-dur  { font-size: 0.8125rem; color: var(--stride-text-muted); }

    /* Status dot */
    .hp-banner-dot, .hp-status-dot {
      width: 0.5rem;
      height: 0.5rem;
      border-radius: 50%;
      flex-shrink: 0;
    }

    .hp-dot--healthy   { background: #10B981; }
    .hp-dot--degraded  { background: #F59E0B; }
    .hp-dot--unhealthy { background: #EF4444; }

    /* Cards grid */
    .hp-cards {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(22rem, 1fr));
      gap: 0.75rem;
    }

    .hp-card {
      display: grid;
      grid-template-columns: 3rem 1fr auto;
      grid-template-rows: auto auto;
      align-items: start;
      gap: 0 0.75rem;
      padding: 1.25rem;
      background: var(--stride-surface);
      border: 1px solid var(--stride-border-soft);
      border-radius: var(--stride-radius-lg);
      transition: border-color 150ms, box-shadow 150ms;
    }

    .hp-card:hover {
      border-color: var(--stride-border);
      box-shadow: var(--stride-shadow-sm);
    }

    .hp-card--healthy   { border-left: 3px solid #10B981; }
    .hp-card--degraded  { border-left: 3px solid #F59E0B; }
    .hp-card--unhealthy { border-left: 3px solid #EF4444; }

    /* Card icon */
    .hp-card-icon {
      grid-row: 1 / 3;
      width: 2.75rem;
      height: 2.75rem;
      border-radius: var(--stride-radius-md);
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
    }

    .hp-icon--healthy   { background: color-mix(in srgb, #10B981 12%, transparent); color: #059669; }
    .hp-icon--degraded  { background: color-mix(in srgb, #F59E0B 12%, transparent); color: #D97706; }
    .hp-icon--unhealthy { background: color-mix(in srgb, #EF4444 12%, transparent); color: #DC2626; }

    /* Card body */
    .hp-card-body { min-width: 0; }

    .hp-card-name {
      font-size: 0.9375rem;
      font-weight: 600;
      color: var(--stride-text-primary);
      margin-bottom: 0.25rem;
    }

    .hp-card-status {
      display: flex;
      align-items: center;
      gap: 0.375rem;
      font-size: 0.8125rem;
      font-weight: 500;
    }

    .hp-text--healthy   { color: #059669; }
    .hp-text--degraded  { color: #D97706; }
    .hp-text--unhealthy { color: #DC2626; }

    .hp-card-desc {
      font-size: 0.75rem;
      color: var(--stride-text-secondary);
      margin: 0.25rem 0 0;
    }

    .hp-card-exc {
      font-size: 0.75rem;
      color: var(--stride-error, #EF4444);
      margin: 0.25rem 0 0;
      font-family: monospace;
      white-space: pre-wrap;
      word-break: break-all;
    }

    /* Card duration */
    .hp-card-dur {
      font-size: 0.75rem;
      color: var(--stride-text-muted);
      white-space: nowrap;
      padding-top: 0.125rem;
    }

    /* Tags */
    .hp-card-tags {
      grid-column: 2 / 4;
      display: flex;
      gap: 0.375rem;
      flex-wrap: wrap;
      margin-top: 0.625rem;
    }

    .hp-tag {
      font-size: 0.6875rem;
      font-weight: 500;
      padding: 0.1rem 0.45rem;
      border-radius: 99px;
      background: var(--stride-surface-secondary);
      color: var(--stride-text-muted);
      text-transform: lowercase;
    }

    /* Empty / error */
    .hp-empty {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 0.75rem;
      padding: 4rem 1rem;
      color: var(--stride-text-muted);
      font-size: 0.9375rem;
      text-align: center;
    }

    /* Skeleton */
    .hp-card--skeleton {
      animation: pulse 1.5s ease-in-out infinite;
    }

    .sk-icon {
      grid-row: 1 / 3;
      width: 2.75rem;
      height: 2.75rem;
      border-radius: var(--stride-radius-md);
      background: var(--stride-border);
    }

    .sk-body { display: flex; flex-direction: column; gap: 0.375rem; }

    .sk-line {
      border-radius: 4px;
      background: var(--stride-border);
    }

    .sk-line--title  { height: 0.875rem; width: 55%; }
    .sk-line--status { height: 0.75rem;  width: 30%; }

    @keyframes pulse {
      0%, 100% { opacity: 1; }
      50%       { opacity: 0.5; }
    }
  `],
})
export class HealthPageComponent implements OnInit {
  readonly service    = inject(HealthService);
  private readonly destroyRef = inject(DestroyRef);

  /** Counts down from 30 → 1 between auto-refreshes. */
  readonly countdown = signal(AUTO_REFRESH_MS / 1000);
  private _tick = 0;

  /** Flattened entry list for *ngFor */
  readonly reportEntries = computed(() => {
    const report = this.service.report();
    if (!report) return [];
    return Object.entries(report.entries).map(([name, entry]) => ({
      name,
      ...entry,
      ...(ENTRY_LABELS[name] ?? { label: name, icon: 'db' }),
    }));
  });

  ngOnInit(): void {
    this.service.loadHealth();

    // Single 1-second interval drives both the visible countdown and the
    // auto-refresh trigger, keeping them perfectly in sync.
    interval(1000)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this._tick++;
        const period = AUTO_REFRESH_MS / 1000; // 30
        const mod    = this._tick % period;
        // After a refresh (mod === 0) reset to 30; otherwise count down.
        this.countdown.set(mod === 0 ? period : period - mod);
        if (mod === 0) {
          this.service.loadHealth();
        }
      });
  }

  /** Manual refresh — also resets the countdown so the timer restarts from 30. */
  refresh(): void {
    this._tick = 0;
    this.countdown.set(AUTO_REFRESH_MS / 1000);
    this.service.loadHealth();
  }

  // ── Style helpers ─────────────────────────────────────────────────────────

  bannerClass(status: string): string {
    return `hp-banner--${status.toLowerCase()}`;
  }

  dotClass(status: string): string {
    return `hp-dot--${status.toLowerCase()}`;
  }

  cardClass(status: string): string {
    return `hp-card--${status.toLowerCase()}`;
  }

  iconClass(status: string): string {
    return `hp-icon--${status.toLowerCase()}`;
  }

  statusTextClass(status: string): string {
    return `hp-text--${status.toLowerCase()}`;
  }

  // ── Formatters ────────────────────────────────────────────────────────────

  /**
   * Converts "00:00:00.1234567" → "123ms" or "1.23s"
   * Handles ISO 8601 duration strings produced by .NET TimeSpan.
   */
  formatDuration(iso: string): string {
    if (!iso) return '';
    const parts = iso.split(':');
    if (parts.length !== 3) return iso;
    const [, , secPart] = parts;
    const totalMs = parseFloat(secPart) * 1000;
    if (totalMs < 1000) return `${Math.round(totalMs)}ms`;
    return `${(totalMs / 1000).toFixed(2)}s`;
  }

  formatTime(d: Date): string {
    return d.toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit', second: '2-digit' });
  }
}
