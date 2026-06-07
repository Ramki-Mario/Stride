import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { NgClass, CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs/operators';

import { InvoiceService }    from '../../services/invoice.service';
import { InvoiceDetailDto, INVOICE_STATUS_CSS, INVOICE_STATUS_LABELS, InvoiceStatus } from '../../models/invoice.models';

@Component({
  selector: 'app-invoice-detail-page',
  standalone: true,
  imports: [NgClass, CurrencyPipe, DatePipe, DecimalPipe, RouterLink],
  templateUrl: './invoice-detail-page.html',
  styleUrl:    './invoice-detail-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InvoiceDetailPageComponent implements OnInit {
  private readonly route      = inject(ActivatedRoute);
  private readonly router     = inject(Router);
  private readonly svc        = inject(InvoiceService);
  private readonly destroyRef = inject(DestroyRef);

  readonly invoice      = signal<InvoiceDetailDto | null>(null);
  readonly isLoading    = signal(true);
  readonly error        = signal<string | null>(null);
  readonly actionPending = signal<string | null>(null);
  readonly actionError  = signal<string | null>(null);

  // ── Computed ───────────────────────────────────────────────────────────────

  readonly statusCss = computed(() => {
    const inv = this.invoice();
    return inv ? (INVOICE_STATUS_CSS[inv.status as InvoiceStatus] ?? 'inv-badge-draft') : '';
  });

  readonly statusLabel = computed(() => {
    const inv = this.invoice();
    return inv ? (INVOICE_STATUS_LABELS[inv.status as InvoiceStatus] ?? inv.statusLabel) : '';
  });

  readonly canSend = computed(() => this.invoice()?.status === 0 && !!this.invoice()?.clientEmail);
  readonly canMarkPaid = computed(() => this.invoice()?.status === 1);
  readonly canVoid = computed(() => {
    const s = this.invoice()?.status;
    return s === 0 || s === 1;
  });

  readonly sourceWorkflowLink = computed(() => {
    const wfId = this.invoice()?.sourceWorkflowInstanceId;
    // We don't have the workflow definition ID here — link to instances list is not ideal.
    // Instead, we store it and let the template decide.
    return wfId ?? null;
  });

  // ── Lifecycle ──────────────────────────────────────────────────────────────

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) { this.router.navigate(['/invoicing']); return; }
    this.loadInvoice(id);
  }

  // ── Load ───────────────────────────────────────────────────────────────────

  loadInvoice(id: string): void {
    this.isLoading.set(true);
    this.error.set(null);

    this.svc.getInvoiceById(id)
      .pipe(finalize(() => this.isLoading.set(false)), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next:  inv  => this.invoice.set(inv),
        error: ()   => this.error.set('Failed to load invoice. Please try again.'),
      });
  }

  // ── Actions ────────────────────────────────────────────────────────────────

  send(): void {
    const inv = this.invoice();
    if (!inv) return;
    this.actionPending.set('Sending…');
    this.actionError.set(null);

    this.svc.sendInvoice(inv.id)
      .pipe(finalize(() => this.actionPending.set(null)), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next:  () => this.loadInvoice(inv.id),
        error: () => this.actionError.set('Could not send invoice. Ensure a client email is set.'),
      });
  }

  markPaid(): void {
    const inv = this.invoice();
    if (!inv) return;
    this.actionPending.set('Marking paid…');
    this.actionError.set(null);

    this.svc.markPaid(inv.id)
      .pipe(finalize(() => this.actionPending.set(null)), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next:  () => this.loadInvoice(inv.id),
        error: () => this.actionError.set('Could not mark invoice as paid.'),
      });
  }

  voidInvoice(): void {
    const inv = this.invoice();
    if (!inv || !confirm(`Void invoice ${inv.invoiceNumber}? This cannot be undone.`)) return;
    this.actionPending.set('Voiding…');
    this.actionError.set(null);

    this.svc.voidInvoice(inv.id)
      .pipe(finalize(() => this.actionPending.set(null)), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next:  () => this.loadInvoice(inv.id),
        error: () => this.actionError.set('Could not void invoice.'),
      });
  }

  // ── Helpers ────────────────────────────────────────────────────────────────

  formatDueDate(iso: string): string {
    return new Date(`${iso}T00:00:00`).toLocaleDateString('en-GB', {
      day: 'numeric', month: 'short', year: 'numeric',
    });
  }

  formatDateTime(iso: string): string {
    return new Date(iso).toLocaleString('en-GB', {
      day: 'numeric', month: 'short', year: 'numeric',
      hour: '2-digit', minute: '2-digit',
    });
  }
}
