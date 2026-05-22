import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  HostListener,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { NgClass } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { interval } from 'rxjs';
import { NotificationService } from '../../services/notification.service';
import { NOTIFICATION_TYPE_CONFIG, NotificationDto } from '../../models/notification.models';

@Component({
  selector: 'app-notification-bell',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgClass, RouterLink],
  template: `
    <div class="bell-wrap">
      <!-- ── Bell trigger ──────────────────────────────────── -->
      <button class="tb-action bell-btn"
              (click)="toggle()"
              [attr.aria-label]="unreadCount() > 0
                ? unreadCount() + ' unread notifications'
                : 'Notifications'"
              [attr.aria-expanded]="open()"
              aria-haspopup="true"
              title="Notifications">
        <svg width="17" height="17" viewBox="0 0 24 24" fill="none" aria-hidden="true">
          <path d="M18 8A6 6 0 006 8c0 7-3 9-3 9h18s-3-2-3-9M13.73 21a2 2 0 01-3.46 0"
                stroke="currentColor" stroke-width="1.8"
                stroke-linecap="round" stroke-linejoin="round"/>
        </svg>

        @if (unreadCount() > 0) {
          <span class="bell-badge" aria-hidden="true">
            {{ unreadCount() > 99 ? '99+' : unreadCount() }}
          </span>
        }
      </button>

      <!-- ── Dropdown panel ────────────────────────────────── -->
      @if (open()) {
        <div class="bell-panel" role="dialog" aria-label="Notifications">

          <!-- Header -->
          <div class="bell-panel-header">
            <span class="bell-panel-title">Notifications</span>
            @if (unreadCount() > 0) {
              <button class="bell-mark-all" (click)="markAllRead()">
                Mark all read
              </button>
            }
          </div>

          <!-- List -->
          @if (service.isLoading()) {
            <div class="bell-empty">Loading…</div>
          } @else if (recentNotifications().length === 0) {
            <div class="bell-empty">
              <svg width="32" height="32" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                <path d="M18 8A6 6 0 006 8c0 7-3 9-3 9h18s-3-2-3-9M13.73 21a2 2 0 01-3.46 0"
                      stroke="currentColor" stroke-width="1.8"
                      stroke-linecap="round" stroke-linejoin="round"/>
              </svg>
              <span>You're all caught up!</span>
            </div>
          } @else {
            <ul class="bell-list" role="list">
              @for (n of recentNotifications(); track n.id) {
                <li class="bell-item" [class.bell-item--unread]="!n.isRead" role="listitem">
                  <!-- Type dot -->
                  <span class="bell-dot"
                        [ngClass]="typeConfig(n).colorClass"
                        aria-hidden="true">
                  </span>

                  <!-- Content -->
                  <div class="bell-item-body">
                    <p class="bell-item-title">{{ n.title }}</p>
                    <p class="bell-item-body-text">{{ n.body }}</p>
                    <time class="bell-item-time">{{ relativeTime(n.createdAt) }}</time>
                  </div>

                  <!-- Actions -->
                  @if (!n.isRead) {
                    <button class="bell-item-read"
                            title="Mark as read"
                            (click)="markRead(n, $event)">
                      <svg width="12" height="12" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                        <polyline points="20 6 9 17 4 12"
                                  stroke="currentColor" stroke-width="2.2"
                                  stroke-linecap="round" stroke-linejoin="round"/>
                      </svg>
                    </button>
                  }
                </li>
              }
            </ul>
          }

          <!-- Footer -->
          <div class="bell-panel-footer">
            <a routerLink="/notifications"
               class="bell-view-all"
               (click)="open.set(false)">
              View all notifications
            </a>
          </div>

        </div>
      }
    </div>
  `,
  styles: [`
    .bell-wrap { position: relative; }

    /* Re-use .tb-action from topbar — bell button */
    .bell-btn { position: relative; }

    /* Unread badge */
    .bell-badge {
      position: absolute;
      top: 0.2rem;
      right: 0.2rem;
      min-width: 1rem;
      height: 1rem;
      padding: 0 0.2rem;
      border-radius: 99px;
      background: var(--stride-error, #EF4444);
      color: #fff;
      font-size: 0.6rem;
      font-weight: 700;
      line-height: 1rem;
      text-align: center;
      pointer-events: none;
    }

    /* Panel */
    .bell-panel {
      position: absolute;
      top: calc(100% + 0.5rem);
      right: 0;
      width: 22rem;
      background: var(--stride-surface);
      border: 1px solid var(--stride-border);
      border-radius: var(--stride-radius-xl);
      box-shadow: var(--stride-shadow-lg);
      z-index: 300;
      overflow: hidden;
      animation: dd-in 120ms cubic-bezier(0.4,0,0.2,1) forwards;
    }

    @keyframes dd-in {
      from { opacity: 0; transform: translateY(-6px); }
      to   { opacity: 1; transform: translateY(0); }
    }

    /* Header */
    .bell-panel-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 0.875rem 1rem 0.625rem;
      border-bottom: 1px solid var(--stride-border-soft);
    }

    .bell-panel-title {
      font-size: 0.875rem;
      font-weight: 600;
      color: var(--stride-text-primary);
    }

    .bell-mark-all {
      font-size: 0.75rem;
      color: var(--stride-primary);
      background: none;
      border: none;
      cursor: pointer;
      padding: 0;
      font-family: inherit;
    }

    .bell-mark-all:hover { text-decoration: underline; }

    /* Empty state */
    .bell-empty {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 0.5rem;
      padding: 2rem 1rem;
      color: var(--stride-text-muted);
      font-size: 0.875rem;
    }

    /* List */
    .bell-list {
      list-style: none;
      margin: 0;
      padding: 0.25rem 0;
      max-height: 20rem;
      overflow-y: auto;
    }

    .bell-item {
      display: flex;
      align-items: flex-start;
      gap: 0.625rem;
      padding: 0.625rem 1rem;
      transition: background 120ms;
      position: relative;
    }

    .bell-item:hover { background: var(--stride-surface-hover); }

    .bell-item--unread {
      background: color-mix(in srgb, var(--stride-primary) 4%, transparent);
    }

    .bell-item--unread:hover {
      background: color-mix(in srgb, var(--stride-primary) 8%, transparent);
    }

    /* Type dot */
    .bell-dot {
      width: 0.5rem;
      height: 0.5rem;
      border-radius: 50%;
      margin-top: 0.3rem;
      flex-shrink: 0;
    }

    .notif-type--started   { background: var(--stride-primary); }
    .notif-type--completed { background: var(--stride-success, #10B981); }
    .notif-type--failed    { background: var(--stride-error, #EF4444); }
    .notif-type--assigned  { background: #F59E0B; }
    .notif-type--alert     { background: #F59E0B; }

    /* Item body */
    .bell-item-body { flex: 1; min-width: 0; }

    .bell-item-title {
      font-size: 0.8125rem;
      font-weight: 500;
      color: var(--stride-text-primary);
      margin: 0 0 0.125rem;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .bell-item-body-text {
      font-size: 0.75rem;
      color: var(--stride-text-secondary);
      margin: 0 0 0.25rem;
      display: -webkit-box;
      -webkit-line-clamp: 2;
      -webkit-box-orient: vertical;
      overflow: hidden;
    }

    .bell-item-time {
      font-size: 0.6875rem;
      color: var(--stride-text-muted);
    }

    /* Per-item mark-read button */
    .bell-item-read {
      opacity: 0;
      transition: opacity 120ms;
      padding: 0.25rem;
      border-radius: var(--stride-radius-sm);
      border: 1px solid var(--stride-border);
      background: var(--stride-surface);
      cursor: pointer;
      color: var(--stride-text-muted);
      flex-shrink: 0;
    }

    .bell-item:hover .bell-item-read { opacity: 1; }
    .bell-item-read:hover {
      background: var(--stride-surface-hover);
      color: var(--stride-primary);
    }

    /* Footer */
    .bell-panel-footer {
      padding: 0.625rem 1rem;
      border-top: 1px solid var(--stride-border-soft);
      text-align: center;
    }

    .bell-view-all {
      font-size: 0.8125rem;
      color: var(--stride-primary);
      text-decoration: none;
      font-weight: 500;
    }

    .bell-view-all:hover { text-decoration: underline; }
  `],
})
export class NotificationBellComponent implements OnInit {
  readonly service      = inject(NotificationService);
  private  readonly router      = inject(Router);
  private  readonly el          = inject(ElementRef);
  private  readonly destroyRef  = inject(DestroyRef);

  readonly open = signal(false);

  readonly unreadCount         = this.service.unreadCount;
  readonly recentNotifications = this.service.recentNotifications;

  ngOnInit(): void {
    // Initial load
    this.service.loadNotifications();

    // Refresh unread count every 60 seconds
    interval(60_000)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.service.loadNotifications());
  }

  toggle(): void {
    this.open.update(v => !v);
    if (this.open()) {
      this.service.loadNotifications();
    }
  }

  markRead(n: NotificationDto, event: MouseEvent): void {
    event.stopPropagation();
    this.service.markAsRead(n.id).subscribe();
  }

  markAllRead(): void {
    this.service.markAllRead();
  }

  typeConfig(n: NotificationDto) {
    return NOTIFICATION_TYPE_CONFIG[n.type] ?? NOTIFICATION_TYPE_CONFIG['SystemAlert'];
  }

  relativeTime(iso: string): string {
    const diff = Date.now() - new Date(iso).getTime();
    const mins = Math.floor(diff / 60_000);
    if (mins < 1)  return 'Just now';
    if (mins < 60) return `${mins}m ago`;
    const hrs = Math.floor(mins / 60);
    if (hrs < 24)  return `${hrs}h ago`;
    const days = Math.floor(hrs / 24);
    if (days < 7)  return `${days}d ago`;
    return new Date(iso).toLocaleDateString('en-GB', { day: 'numeric', month: 'short' });
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.el.nativeElement.contains(event.target as Node)) {
      this.open.set(false);
    }
  }
}
