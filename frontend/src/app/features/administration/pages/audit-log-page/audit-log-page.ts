import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  inject,
  signal,
  computed,
} from '@angular/core';
import { NgClass, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { SelectModule } from 'primeng/select';

import { AuditLogService } from '../../services/audit-log.service';
import {
  AuditLogEntry,
  ACTION_LABELS,
  ACTION_BADGE_CLASS,
} from '../../models/audit-log.models';

@Component({
  selector: 'app-audit-log-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgClass, DatePipe, FormsModule, RouterLink, SelectModule],
  templateUrl: './audit-log-page.html',
  styleUrl: './audit-log-page.scss',
})
export class AuditLogPageComponent implements OnInit {
  readonly svc = inject(AuditLogService);

  // ── Filter state ──────────────────────────────────────────────────────────
  fromDate = signal('');
  toDate   = signal('');
  selectedAction: string | null = null;

  readonly actionOptions = [
    { label: 'All Actions',          value: null },
    { label: 'User Invited',         value: 'user.invited' },
    { label: 'Role Changed',         value: 'user.role_changed' },
    { label: 'User Deactivated',     value: 'user.deactivated' },
    { label: 'User Reactivated',     value: 'user.reactivated' },
    { label: 'Invoice Created',      value: 'invoice.generated' },
    { label: 'Invoice Sent',         value: 'invoice.sent' },
    { label: 'Invoice Paid',         value: 'invoice.paid' },
    { label: 'Invoice Voided',       value: 'invoice.voided' },
    { label: 'Settings Updated',     value: 'tenant_settings.updated' },
  ];

  readonly skeletons = [1, 2, 3, 4, 5, 6, 7, 8];

  // ── Expanded rows ──────────────────────────────────────────────────────────
  expandedId = signal<string | null>(null);

  readonly pageRange = computed(() => {
    const total = this.svc.totalPages(), current = this.svc.page(), delta = 2;
    const range: number[] = [];
    for (let i = Math.max(1, current - delta); i <= Math.min(total, current + delta); i++)
      range.push(i);
    return range;
  });

  ngOnInit(): void { this.load(); }

  load(page = 1): void {
    this.svc.load(
      page,
      this.svc.pageSize(),
      this.fromDate() || undefined,
      this.toDate()   || undefined,
      this.selectedAction ?? undefined,
    );
  }

  applyFilters(): void { this.load(1); }

  clearFilters(): void {
    this.fromDate.set('');
    this.toDate.set('');
    this.selectedAction = null;
    this.load(1);
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.svc.totalPages()) return;
    this.load(page);
  }

  toggleExpand(id: string): void {
    this.expandedId.update(cur => cur === id ? null : id);
  }

  actionLabel(action: string): string {
    return ACTION_LABELS[action] ?? action;
  }

  actionBadgeClass(action: string): string {
    return ACTION_BADGE_CLASS[action] ?? 'badge--default';
  }

  shortId(id: string | null): string {
    return id ? id.substring(0, 8).toUpperCase() : '—';
  }

  hasDetails(entry: AuditLogEntry): boolean {
    return !!(entry.oldValueJson || entry.newValueJson);
  }
}
