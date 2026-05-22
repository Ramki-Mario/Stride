import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { NgClass } from '@angular/common';
import { NotificationService } from '../../services/notification.service';
import { NOTIFICATION_TYPE_CONFIG, NotificationDto } from '../../models/notification.models';

type FilterTab = 'all' | 'unread';

@Component({
  selector: 'app-notifications-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgClass],
  template: `
    <div class="notif-page">

      <!-- ── Page header ────────────────────────────────────── -->
      <div class="notif-header">
        <div>
          <h1 class="notif-title">Notifications</h1>
          <p class="notif-subtitle">
            @if (service.unreadCount() > 0) {
              {{ service.unreadCount() }} unread notification{{ service.unreadCount() === 1 ? '' : 's' }}
            } @else {
              You're all caught up
            }
          </p>
        </div>

        @if (service.unreadCount() > 0) {
          <button class="stride-btn stride-btn-secondary" (click)="markAllRead()">
            Mark all as read
          </button>
        }
      </div>

      <!-- ── Filter tabs ─────────────────────────────────────── -->
      <div class="notif-tabs" role="tablist">
        <button role="tab"
                class="notif-tab"
                [class.notif-tab--active]="activeTab() === 'all'"
                (click)="activeTab.set('all')">
          All
          <span class="notif-tab-count">{{ service.notifications().length }}</span>
        </button>
        <button role="tab"
                class="notif-tab"
                [class.notif-tab--active]="activeTab() === 'unread'"
                (click)="activeTab.set('unread')">
          Unread
          @if (service.unreadCount() > 0) {
            <span class="notif-tab-count notif-tab-count--badge">
              {{ service.unreadCount() }}
            </span>
          }
        </button>
      </div>

      <!-- ── Loading skeleton ───────────────────────────────── -->
      @if (service.isLoading()) {
        <div class="notif-list">
          @for (i of skeletons; track i) {
            <div class="notif-skeleton">
              <div class="sk-dot"></div>
              <div class="sk-body">
                <div class="sk-line sk-line--title"></div>
                <div class="sk-line sk-line--body"></div>
                <div class="sk-line sk-line--time"></div>
              </div>
            </div>
          }
        </div>

      <!-- ── Error state ─────────────────────────────────────── -->
      } @else if (service.error()) {
        <div class="notif-empty">
          <svg width="40" height="40" viewBox="0 0 24 24" fill="none" aria-hidden="true">
            <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="1.5"/>
            <line x1="12" y1="8" x2="12" y2="12" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
            <line x1="12" y1="16" x2="12.01" y2="16" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
          </svg>
          <p>{{ service.error() }}</p>
          <button class="stride-btn stride-btn-secondary" (click)="service.loadNotifications()">
            Retry
          </button>
        </div>

      <!-- ── Empty state ─────────────────────────────────────── -->
      } @else if (filteredNotifications().length === 0) {
        <div class="notif-empty">
          <svg width="40" height="40" viewBox="0 0 24 24" fill="none" aria-hidden="true">
            <path d="M18 8A6 6 0 006 8c0 7-3 9-3 9h18s-3-2-3-9M13.73 21a2 2 0 01-3.46 0"
                  stroke="currentColor" stroke-width="1.5"
                  stroke-linecap="round" stroke-linejoin="round"/>
          </svg>
          <p>
            @if (activeTab() === 'unread') { No unread notifications }
            @else { No notifications yet }
          </p>
        </div>

      <!-- ── Notification list ───────────────────────────────── -->
      } @else {
        <div class="notif-list" role="list">
          @for (n of filteredNotifications(); track n.id) {
            <div class="notif-item"
                 [class.notif-item--unread]="!n.isRead"
                 role="listitem">

              <!-- Type indicator dot -->
              <div class="notif-dot"
                   [ngClass]="typeConfig(n).colorClass"
                   [title]="typeConfig(n).label"
                   aria-hidden="true">
              </div>

              <!-- Content -->
              <div class="notif-content">
                <div class="notif-content-top">
                  <span class="notif-type-badge" [ngClass]="typeConfig(n).colorClass + '-bg'">
                    {{ typeConfig(n).label }}
                  </span>
                  @if (!n.isRead) {
                    <span class="notif-unread-dot" aria-label="Unread"></span>
                  }
                </div>
                <p class="notif-item-title">{{ n.title }}</p>
                <p class="notif-item-body">{{ n.body }}</p>
                <time class="notif-item-time">{{ relativeTime(n.createdAt) }}</time>
              </div>

              <!-- Actions -->
              <div class="notif-actions">
                @if (!n.isRead) {
                  <button class="notif-action-btn"
                          title="Mark as read"
                          (click)="markRead(n)">
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                      <polyline points="20 6 9 17 4 12"
                                stroke="currentColor" stroke-width="2.2"
                                stroke-linecap="round" stroke-linejoin="round"/>
                    </svg>
                  </button>
                }
                <button class="notif-action-btn notif-action-btn--danger"
                        title="Dismiss"
                        (click)="dismiss(n)">
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                    <line x1="18" y1="6" x2="6" y2="18" stroke="currentColor" stroke-width="2"
                          stroke-linecap="round"/>
                    <line x1="6" y1="6" x2="18" y2="18" stroke="currentColor" stroke-width="2"
                          stroke-linecap="round"/>
                  </svg>
                </button>
              </div>
            </div>
          }
        </div>
      }

    </div>
  `,
  styles: [`
    .notif-page {
      max-width: 52rem;
      margin: 0 auto;
      padding: 1.5rem 1rem 3rem;
    }

    /* Header */
    .notif-header {
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      gap: 1rem;
      margin-bottom: 1.5rem;
    }

    .notif-title {
      font-size: 1.375rem;
      font-weight: 700;
      color: var(--stride-text-primary);
      margin: 0 0 0.25rem;
    }

    .notif-subtitle {
      font-size: 0.875rem;
      color: var(--stride-text-muted);
      margin: 0;
    }

    /* Tabs */
    .notif-tabs {
      display: flex;
      gap: 0.25rem;
      border-bottom: 1px solid var(--stride-border-soft);
      margin-bottom: 1.25rem;
    }

    .notif-tab {
      display: flex;
      align-items: center;
      gap: 0.375rem;
      padding: 0.5rem 0.75rem;
      font-size: 0.875rem;
      font-weight: 500;
      color: var(--stride-text-muted);
      background: none;
      border: none;
      border-bottom: 2px solid transparent;
      margin-bottom: -1px;
      cursor: pointer;
      transition: color 120ms, border-color 120ms;
      font-family: inherit;
    }

    .notif-tab:hover { color: var(--stride-text-primary); }

    .notif-tab--active {
      color: var(--stride-primary);
      border-bottom-color: var(--stride-primary);
    }

    .notif-tab-count {
      font-size: 0.75rem;
      padding: 0.1rem 0.4rem;
      border-radius: 99px;
      background: var(--stride-surface-secondary);
      color: var(--stride-text-muted);
    }

    .notif-tab-count--badge {
      background: var(--stride-primary);
      color: #fff;
    }

    /* Notification list */
    .notif-list {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }

    .notif-item {
      display: flex;
      align-items: flex-start;
      gap: 0.875rem;
      padding: 1rem 1.25rem;
      background: var(--stride-surface);
      border: 1px solid var(--stride-border-soft);
      border-radius: var(--stride-radius-lg);
      transition: border-color 150ms, box-shadow 150ms;
    }

    .notif-item:hover {
      border-color: var(--stride-border);
      box-shadow: var(--stride-shadow-sm);
    }

    .notif-item--unread {
      background: color-mix(in srgb, var(--stride-primary) 3%, var(--stride-surface));
      border-color: color-mix(in srgb, var(--stride-primary) 20%, var(--stride-border-soft));
    }

    /* Dot */
    .notif-dot {
      width: 0.625rem;
      height: 0.625rem;
      border-radius: 50%;
      margin-top: 0.35rem;
      flex-shrink: 0;
    }

    .notif-type--started   { background: var(--stride-primary); }
    .notif-type--completed { background: #10B981; }
    .notif-type--failed    { background: #EF4444; }
    .notif-type--assigned  { background: #F59E0B; }
    .notif-type--alert     { background: #F59E0B; }

    /* Content */
    .notif-content { flex: 1; min-width: 0; }

    .notif-content-top {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      margin-bottom: 0.25rem;
    }

    .notif-type-badge {
      font-size: 0.6875rem;
      font-weight: 600;
      padding: 0.125rem 0.5rem;
      border-radius: 99px;
      text-transform: uppercase;
      letter-spacing: 0.04em;
    }

    .notif-type--started-bg   { background: color-mix(in srgb, var(--stride-primary) 12%, transparent); color: var(--stride-primary); }
    .notif-type--completed-bg { background: color-mix(in srgb, #10B981 12%, transparent); color: #059669; }
    .notif-type--failed-bg    { background: color-mix(in srgb, #EF4444 12%, transparent); color: #DC2626; }
    .notif-type--assigned-bg  { background: color-mix(in srgb, #F59E0B 12%, transparent); color: #D97706; }
    .notif-type--alert-bg     { background: color-mix(in srgb, #F59E0B 12%, transparent); color: #D97706; }

    .notif-unread-dot {
      width: 0.4375rem;
      height: 0.4375rem;
      border-radius: 50%;
      background: var(--stride-primary);
      flex-shrink: 0;
    }

    .notif-item-title {
      font-size: 0.875rem;
      font-weight: 600;
      color: var(--stride-text-primary);
      margin: 0 0 0.25rem;
    }

    .notif-item-body {
      font-size: 0.8125rem;
      color: var(--stride-text-secondary);
      margin: 0 0 0.375rem;
      line-height: 1.5;
    }

    .notif-item-time {
      font-size: 0.75rem;
      color: var(--stride-text-muted);
    }

    /* Actions */
    .notif-actions {
      display: flex;
      align-items: center;
      gap: 0.375rem;
      flex-shrink: 0;
    }

    .notif-action-btn {
      width: 2rem;
      height: 2rem;
      border-radius: var(--stride-radius-md);
      display: flex;
      align-items: center;
      justify-content: center;
      border: 1px solid var(--stride-border-soft);
      background: var(--stride-surface);
      color: var(--stride-text-muted);
      cursor: pointer;
      transition: all 120ms;
    }

    .notif-action-btn:hover {
      background: var(--stride-surface-hover);
      color: var(--stride-primary);
      border-color: var(--stride-border);
    }

    .notif-action-btn--danger:hover {
      color: var(--stride-error, #EF4444);
      background: color-mix(in srgb, #EF4444 8%, transparent);
      border-color: color-mix(in srgb, #EF4444 25%, transparent);
    }

    /* Empty / error state */
    .notif-empty {
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
    .notif-skeleton {
      display: flex;
      gap: 0.875rem;
      padding: 1rem 1.25rem;
      background: var(--stride-surface);
      border: 1px solid var(--stride-border-soft);
      border-radius: var(--stride-radius-lg);
      animation: pulse 1.5s ease-in-out infinite;
    }

    .sk-dot {
      width: 0.625rem;
      height: 0.625rem;
      border-radius: 50%;
      background: var(--stride-border);
      flex-shrink: 0;
      margin-top: 0.35rem;
    }

    .sk-body { flex: 1; display: flex; flex-direction: column; gap: 0.375rem; }

    .sk-line {
      border-radius: 4px;
      background: var(--stride-border);
    }

    .sk-line--title { height: 0.875rem; width: 55%; }
    .sk-line--body  { height: 0.75rem;  width: 80%; }
    .sk-line--time  { height: 0.625rem; width: 25%; }

    @keyframes pulse {
      0%, 100% { opacity: 1; }
      50%       { opacity: 0.5; }
    }
  `],
})
export class NotificationsPageComponent implements OnInit {
  readonly service    = inject(NotificationService);
  readonly activeTab  = signal<FilterTab>('all');
  readonly skeletons  = [1, 2, 3, 4, 5];

  readonly filteredNotifications = computed<NotificationDto[]>(() => {
    const all = this.service.notifications();
    return this.activeTab() === 'unread' ? all.filter(n => !n.isRead) : all;
  });

  ngOnInit(): void {
    this.service.loadNotifications();
  }

  markRead(n: NotificationDto): void {
    this.service.markAsRead(n.id).subscribe();
  }

  dismiss(n: NotificationDto): void {
    this.service.delete(n.id).subscribe();
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
    return new Date(iso).toLocaleDateString('en-GB', {
      day: 'numeric', month: 'short', year: 'numeric',
    });
  }
}
