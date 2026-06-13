import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { WebhookService } from '../../services/webhook.service';
import {
  WebhookDeliveryDto,
  WebhookSubscriptionDto,
  WebhookTestResult,
} from '../../models/webhook.models';

interface EditorState {
  mode:       'create' | 'edit';
  id:         string | null;
  url:        string;
  selected:   Set<string>;
  isActive:   boolean;
}

@Component({
  selector: 'app-webhooks-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, FormsModule],
  templateUrl: './webhooks-page.html',
  styleUrl: './webhooks-page.scss',
})
export class WebhooksPageComponent implements OnInit {
  readonly svc = inject(WebhookService);

  // ── Editor panel state ──────────────────────────────────────────────────
  readonly editor      = signal<EditorState | null>(null);
  readonly saving      = signal(false);
  readonly formError   = signal<string | null>(null);

  // ── Secret-shown-once dialog ──────────────────────────────────────────────
  readonly revealedSecret = signal<string | null>(null);
  readonly secretCopied   = signal(false);

  // ── Per-row test feedback ─────────────────────────────────────────────────
  readonly testingId   = signal<string | null>(null);
  readonly testResults = signal<Record<string, WebhookTestResult>>({});

  // ── Delivery log ──────────────────────────────────────────────────────────
  readonly deliverySubId   = signal<string | null>(null);
  readonly deliveries      = signal<WebhookDeliveryDto[]>([]);
  readonly deliveriesLoading = signal(false);
  readonly retryingId      = signal<string | null>(null);

  ngOnInit(): void {
    this.svc.loadEventTypes();
    this.svc.loadSubscriptions();
  }

  // ── Editor ────────────────────────────────────────────────────────────────

  openCreate(): void {
    this.formError.set(null);
    this.editor.set({ mode: 'create', id: null, url: '', selected: new Set(), isActive: true });
  }

  openEdit(sub: WebhookSubscriptionDto): void {
    this.formError.set(null);
    this.editor.set({
      mode: 'edit',
      id: sub.id,
      url: sub.url,
      selected: new Set(sub.eventTypes),
      isActive: sub.isActive,
    });
  }

  closeEditor(): void {
    this.editor.set(null);
    this.formError.set(null);
  }

  toggleEvent(key: string): void {
    const state = this.editor();
    if (!state) return;
    const next = new Set(state.selected);
    next.has(key) ? next.delete(key) : next.add(key);
    this.editor.set({ ...state, selected: next });
  }

  isSelected(key: string): boolean {
    return this.editor()?.selected.has(key) ?? false;
  }

  setUrl(url: string): void {
    const state = this.editor();
    if (state) this.editor.set({ ...state, url });
  }

  setActive(isActive: boolean): void {
    const state = this.editor();
    if (state) this.editor.set({ ...state, isActive });
  }

  save(): void {
    const state = this.editor();
    if (!state) return;

    const url = state.url.trim();
    if (!url) { this.formError.set('URL is required.'); return; }
    if (!/^https:\/\//i.test(url)) { this.formError.set('URL must start with https://'); return; }
    if (state.selected.size === 0) { this.formError.set('Select at least one event type.'); return; }

    this.saving.set(true);
    this.formError.set(null);
    const eventTypes = [...state.selected];

    if (state.mode === 'create') {
      this.svc.create({ url, eventTypes }).subscribe({
        next: result => {
          this.saving.set(false);
          this.closeEditor();
          this.revealSecret(result.signingSecret);
          this.svc.loadSubscriptions();
        },
        error: err => this.onSaveError(err),
      });
    } else {
      this.svc.update(state.id!, { url, eventTypes, isActive: state.isActive }).subscribe({
        next: () => {
          this.saving.set(false);
          this.closeEditor();
          this.svc.loadSubscriptions();
        },
        error: err => this.onSaveError(err),
      });
    }
  }

  private onSaveError(err: unknown): void {
    this.saving.set(false);
    const detail = (err as { error?: { detail?: string } })?.error?.detail;
    this.formError.set(detail || 'Could not save the webhook. Check the URL and try again.');
  }

  // ── Secret dialog ─────────────────────────────────────────────────────────

  private revealSecret(secret: string): void {
    this.secretCopied.set(false);
    this.revealedSecret.set(secret);
  }

  copySecret(): void {
    const secret = this.revealedSecret();
    if (!secret) return;
    navigator.clipboard?.writeText(secret).then(() => this.secretCopied.set(true));
  }

  dismissSecret(): void {
    this.revealedSecret.set(null);
    this.secretCopied.set(false);
  }

  // ── Row actions ─────────────────────────────────────────────────────────

  test(sub: WebhookSubscriptionDto): void {
    this.testingId.set(sub.id);
    this.svc.test(sub.id).subscribe({
      next: result => {
        this.testResults.set({ ...this.testResults(), [sub.id]: result });
        this.testingId.set(null);
      },
      error: () => {
        this.testResults.set({
          ...this.testResults(),
          [sub.id]: { success: false, statusCode: null, error: 'Request failed.', elapsedMs: 0 },
        });
        this.testingId.set(null);
      },
    });
  }

  regenerate(sub: WebhookSubscriptionDto): void {
    if (!confirm('Generate a new signing secret? The current secret will stop working immediately.')) return;
    this.svc.regenerateSecret(sub.id).subscribe({
      next: result => this.revealSecret(result.signingSecret),
    });
  }

  remove(sub: WebhookSubscriptionDto): void {
    if (!confirm(`Delete the webhook for ${sub.url}? This cannot be undone.`)) return;
    this.svc.delete(sub.id).subscribe({
      next: () => this.svc.loadSubscriptions(),
    });
  }

  labelFor(key: string): string {
    return this.svc.eventTypes().find(t => t.key === key)?.label ?? key;
  }

  resultFor(id: string): WebhookTestResult | undefined {
    return this.testResults()[id];
  }

  // ── Delivery log ──────────────────────────────────────────────────────────

  toggleDeliveries(sub: WebhookSubscriptionDto): void {
    if (this.deliverySubId() === sub.id) {
      this.deliverySubId.set(null);
      this.deliveries.set([]);
      return;
    }

    this.deliverySubId.set(sub.id);
    this.deliveries.set([]);
    this.deliveriesLoading.set(true);

    this.svc.getDeliveries(sub.id).subscribe({
      next: list => {
        this.deliveries.set(list);
        this.deliveriesLoading.set(false);
      },
      error: () => this.deliveriesLoading.set(false),
    });
  }

  retryDelivery(sub: WebhookSubscriptionDto, delivery: WebhookDeliveryDto): void {
    this.retryingId.set(delivery.id);
    this.svc.retryDelivery(sub.id, delivery.id).subscribe({
      next: () => {
        this.retryingId.set(null);
        // Reload the delivery list to reflect the new Pending status.
        this.svc.getDeliveries(sub.id).subscribe({
          next: list => this.deliveries.set(list),
        });
      },
      error: () => this.retryingId.set(null),
    });
  }

  statusLabel(status: string): string {
    switch (status) {
      case 'Success':   return '✓ Success';
      case 'Failed':    return '↻ Failed';
      case 'Exhausted': return '✗ Exhausted';
      default:          return '⋯ Pending';
    }
  }
}
