import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { NgClass, CurrencyPipe } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormArray, Validators } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { Subject } from 'rxjs';
import { SelectModule } from 'primeng/select';

import { InvoiceService } from '../../services/invoice.service';
import { InvoiceSummaryDto, InvoiceStatus, INVOICE_STATUS_LABELS } from '../../models/invoice.models';

@Component({
  selector: 'app-invoicing-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgClass, FormsModule, ReactiveFormsModule, SelectModule, CurrencyPipe],
  template: `
    <div class="inv-page">

      <!-- ── Header ───────────────────────────────────────────────────── -->
      <div class="inv-header">
        <div>
          <h1 class="inv-title">Invoices</h1>
          <p class="inv-subtitle">
            @if (!svc.isLoading()) {
              {{ svc.totalCount() }} invoice{{ svc.totalCount() === 1 ? '' : 's' }} total
            } @else { Loading… }
          </p>
        </div>
        <button class="stride-btn stride-btn-primary" (click)="openCreateModal()">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" aria-hidden="true">
            <line x1="12" y1="5" x2="12" y2="19" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
            <line x1="5" y1="12" x2="19" y2="12" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
          </svg>
          New Invoice
        </button>
      </div>

      <!-- ── KPI cards ─────────────────────────────────────────────────── -->
      <div class="inv-kpis">
        <div class="inv-kpi inv-kpi--total">
          <span class="inv-kpi-label">Total Invoices</span>
          <span class="inv-kpi-value">{{ svc.totalCount() }}</span>
        </div>
        <div class="inv-kpi inv-kpi--pending">
          <span class="inv-kpi-label">Awaiting Payment</span>
          <span class="inv-kpi-value">{{ svc.pendingCount() }}</span>
        </div>
        <div class="inv-kpi inv-kpi--draft">
          <span class="inv-kpi-label">Drafts</span>
          <span class="inv-kpi-value">{{ svc.draftCount() }}</span>
        </div>
        <div class="inv-kpi inv-kpi--revenue">
          <span class="inv-kpi-label">Revenue Collected</span>
          <span class="inv-kpi-value">{{ svc.totalRevenue() | currency:'USD':'symbol':'1.2-2' }}</span>
        </div>
      </div>

      <!-- ── Notification ──────────────────────────────────────────────── -->
      @if (notification()) {
        <div class="inv-notification" [class.inv-notification--error]="notification()!.type === 'error'">
          <span>{{ notification()!.message }}</span>
          <button class="inv-notif-close" (click)="notification.set(null)" aria-label="Dismiss">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none">
              <line x1="18" y1="6" x2="6" y2="18" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
              <line x1="6" y1="6" x2="18" y2="18" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
            </svg>
          </button>
        </div>
      }

      <!-- ── Toolbar ───────────────────────────────────────────────────── -->
      <div class="inv-toolbar">
        <div class="inv-search-wrap">
          <svg class="inv-search-icon" width="16" height="16" viewBox="0 0 24 24" fill="none" aria-hidden="true">
            <circle cx="11" cy="11" r="8" stroke="currentColor" stroke-width="1.75"/>
            <line x1="21" y1="21" x2="16.65" y2="16.65" stroke="currentColor" stroke-width="1.75" stroke-linecap="round"/>
          </svg>
          <input class="inv-search" type="search" placeholder="Search by number, client…"
                 [value]="searchTerm()"
                 (input)="onSearchInput($event)" aria-label="Search invoices"/>
        </div>
        <p-select [options]="statusFilterOptions" [(ngModel)]="selectedStatus"
                  (onChange)="onFilterChange()"
                  optionLabel="label" optionValue="value"
                  styleClass="inv-filter-select" placeholder="All Statuses"/>
        @if (hasActiveFilters()) {
          <button class="inv-clear-btn" (click)="clearFilters()">Clear</button>
        }
      </div>

      <!-- ── Loading skeleton ──────────────────────────────────────────── -->
      @if (svc.isLoading()) {
        <div class="inv-table-wrap">
          <table class="inv-table" aria-label="Loading invoices">
            <thead><tr><th>Invoice</th><th>Client</th><th>Status</th><th>Due</th><th>Amount</th><th></th></tr></thead>
            <tbody>
              @for (i of skeletons; track i) {
                <tr class="sk-row">
                  <td><div class="sk-line sk-line--num"></div></td>
                  <td><div class="sk-text"><div class="sk-line sk-line--name"></div><div class="sk-line sk-line--email"></div></div></td>
                  <td><div class="sk-line sk-line--badge"></div></td>
                  <td><div class="sk-line sk-line--date"></div></td>
                  <td><div class="sk-line sk-line--amount"></div></td>
                  <td></td>
                </tr>
              }
            </tbody>
          </table>
        </div>

      <!-- ── Error ─────────────────────────────────────────────────────── -->
      } @else if (svc.error()) {
        <div class="inv-empty">
          <svg width="40" height="40" viewBox="0 0 24 24" fill="none" aria-hidden="true">
            <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="1.5"/>
            <line x1="12" y1="8" x2="12" y2="12" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
            <line x1="12" y1="16" x2="12.01" y2="16" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
          </svg>
          <p>{{ svc.error() }}</p>
          <button class="stride-btn stride-btn-secondary" (click)="reload()">Retry</button>
        </div>

      <!-- ── Empty ─────────────────────────────────────────────────────── -->
      } @else if (svc.isEmpty()) {
        <div class="inv-empty">
          <svg width="40" height="40" viewBox="0 0 24 24" fill="none" aria-hidden="true">
            <path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z"
                  stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/>
            <polyline points="14 2 14 8 20 8" stroke="currentColor" stroke-width="1.5"
                      stroke-linecap="round" stroke-linejoin="round"/>
          </svg>
          <p>
            @if (hasActiveFilters()) { No invoices match the current filters. }
            @else { No invoices yet. Create your first invoice to get started. }
          </p>
          @if (!hasActiveFilters()) {
            <button class="stride-btn stride-btn-primary" (click)="openCreateModal()">New Invoice</button>
          }
        </div>

      <!-- ── Table ─────────────────────────────────────────────────────── -->
      } @else {
        <div class="inv-table-wrap">
          <table class="inv-table" aria-label="Invoices">
            <thead>
              <tr>
                <th>Invoice #</th>
                <th>Client</th>
                <th>Status</th>
                <th>Due Date</th>
                <th class="inv-th-amount">Amount</th>
                <th class="inv-th-actions"><span class="sr-only">Actions</span></th>
              </tr>
            </thead>
            <tbody>
              @for (inv of svc.invoices(); track inv.id) {
                <tr [class.inv-row--void]="inv.status === 3">
                  <td class="inv-td-number">{{ inv.invoiceNumber }}</td>
                  <td>
                    <div class="inv-client-name">{{ inv.clientName }}</div>
                    <div class="inv-client-email">{{ inv.clientEmail }}</div>
                  </td>
                  <td>
                    <span class="inv-badge" [ngClass]="statusBadgeClass(inv.status)">
                      {{ inv.statusLabel }}
                    </span>
                  </td>
                  <td class="inv-td-date">{{ formatDate(inv.dueDate) }}</td>
                  <td class="inv-td-amount">
                    {{ inv.totalAmount | currency:inv.currency:'symbol':'1.2-2' }}
                  </td>
                  <td class="inv-td-actions">
                    <div class="inv-menu-wrap">
                      <button class="inv-menu-trigger"
                              [class.inv-menu-trigger--open]="openMenuId() === inv.id"
                              (click)="toggleMenu(inv.id)"
                              aria-label="Invoice actions">
                        <svg width="16" height="16" viewBox="0 0 24 24" fill="none">
                          <circle cx="12" cy="5"  r="1.2" fill="currentColor"/>
                          <circle cx="12" cy="12" r="1.2" fill="currentColor"/>
                          <circle cx="12" cy="19" r="1.2" fill="currentColor"/>
                        </svg>
                      </button>
                      @if (openMenuId() === inv.id) {
                        <div class="inv-menu" role="menu">
                          @if (inv.status === 0) {
                            <button class="inv-menu-item" role="menuitem" (click)="sendInvoice(inv)">Send</button>
                            <button class="inv-menu-item inv-menu-item--danger" role="menuitem" (click)="voidInvoice(inv)">Void</button>
                          }
                          @if (inv.status === 1) {
                            <button class="inv-menu-item" role="menuitem" (click)="markPaid(inv)">Mark Paid</button>
                            <button class="inv-menu-item inv-menu-item--danger" role="menuitem" (click)="voidInvoice(inv)">Void</button>
                          }
                          @if (inv.status === 2 || inv.status === 3) {
                            <span class="inv-menu-empty">No actions available</span>
                          }
                        </div>
                      }
                    </div>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>

        <!-- ── Pagination ────────────────────────────────────────────── -->
        @if (svc.totalPages() > 1) {
          <div class="inv-pagination">
            <span class="inv-page-info">
              Page {{ svc.page() }} of {{ svc.totalPages() }} · {{ svc.totalCount() }} total
            </span>
            <div class="inv-page-controls">
              <button class="inv-page-btn" [disabled]="!svc.hasPrevious()"
                      (click)="goToPage(svc.page() - 1)">
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none">
                  <polyline points="15 18 9 12 15 6" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                </svg>
              </button>
              @for (p of pageRange(); track p) {
                <button class="inv-page-btn" [class.inv-page-btn--active]="p === svc.page()"
                        (click)="goToPage(p)">{{ p }}</button>
              }
              <button class="inv-page-btn" [disabled]="!svc.hasNext()"
                      (click)="goToPage(svc.page() + 1)">
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none">
                  <polyline points="9 18 15 12 9 6" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                </svg>
              </button>
            </div>
          </div>
        }
      }
    </div>

    <!-- ═══════════════════════════════════════════════════════════════════ -->
    <!-- Create Invoice Modal                                                -->
    <!-- ═══════════════════════════════════════════════════════════════════ -->
    @if (showCreateModal()) {
      <div class="modal-backdrop" (click)="closeCreateModal()" role="presentation">
        <div class="modal modal--lg" role="dialog" aria-modal="true" aria-labelledby="create-modal-title"
             (click)="$event.stopPropagation()">
          <div class="modal-header">
            <h2 class="modal-title" id="create-modal-title">New Invoice</h2>
            <button class="modal-close" (click)="closeCreateModal()" aria-label="Close">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none">
                <line x1="18" y1="6" x2="6"  y2="18" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
                <line x1="6"  y1="6" x2="18" y2="18" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
              </svg>
            </button>
          </div>

          <form class="modal-body" [formGroup]="createForm" (ngSubmit)="submitCreate()">

            <!-- Row 1: number + currency -->
            <div class="form-row">
              <div class="form-field">
                <label class="form-label">Invoice # <span class="form-required">*</span></label>
                <input class="form-input" formControlName="invoiceNumber" placeholder="INV-001"
                       [class.form-input--error]="createForm.controls.invoiceNumber.invalid && createForm.controls.invoiceNumber.touched"/>
                @if (createForm.controls.invoiceNumber.invalid && createForm.controls.invoiceNumber.touched) {
                  <p class="form-error">Required.</p>
                }
              </div>
              <div class="form-field form-field--sm">
                <label class="form-label">Currency <span class="form-required">*</span></label>
                <input class="form-input" formControlName="currency" placeholder="USD" maxlength="3"/>
              </div>
            </div>

            <!-- Row 2: client name + email -->
            <div class="form-row">
              <div class="form-field">
                <label class="form-label">Client Name <span class="form-required">*</span></label>
                <input class="form-input" formControlName="clientName" placeholder="Acme Corp"
                       [class.form-input--error]="createForm.controls.clientName.invalid && createForm.controls.clientName.touched"/>
              </div>
              <div class="form-field">
                <label class="form-label">Client Email <span class="form-required">*</span></label>
                <input class="form-input" type="email" formControlName="clientEmail" placeholder="billing@acme.com"
                       [class.form-input--error]="createForm.controls.clientEmail.invalid && createForm.controls.clientEmail.touched"/>
              </div>
            </div>

            <!-- Row 3: due date + notes -->
            <div class="form-row">
              <div class="form-field form-field--sm">
                <label class="form-label">Due Date <span class="form-required">*</span></label>
                <input class="form-input" type="date" formControlName="dueDate"/>
              </div>
              <div class="form-field">
                <label class="form-label">Notes</label>
                <input class="form-input" formControlName="notes" placeholder="Optional notes…"/>
              </div>
            </div>

            <!-- Line items -->
            <div class="form-section">
              <div class="form-section-header">
                <span class="form-section-title">Line Items</span>
                <button type="button" class="form-add-btn" (click)="addLineItem()">+ Add</button>
              </div>

              <div formArrayName="lineItems" class="line-items">
                @for (item of lineItemsArray.controls; track $index) {
                  <div class="line-item" [formGroupName]="$index">
                    <input class="form-input line-item-desc" formControlName="description" placeholder="Description"/>
                    <input class="form-input line-item-price" type="number" formControlName="unitPrice" placeholder="0.00" min="0" step="0.01"/>
                    <input class="form-input line-item-qty" type="number" formControlName="quantity" placeholder="1" min="1"/>
                    <span class="line-item-subtotal">
                      {{ lineSubtotal($index) | currency:'USD':'symbol':'1.2-2' }}
                    </span>
                    @if (lineItemsArray.length > 1) {
                      <button type="button" class="line-item-remove" (click)="removeLineItem($index)" aria-label="Remove">
                        <svg width="14" height="14" viewBox="0 0 24 24" fill="none">
                          <line x1="18" y1="6" x2="6" y2="18" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
                          <line x1="6" y1="6" x2="18" y2="18" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
                        </svg>
                      </button>
                    }
                  </div>
                }
              </div>

              <div class="line-items-total">
                Total: <strong>{{ invoiceTotal() | currency:'USD':'symbol':'1.2-2' }}</strong>
              </div>
            </div>

            @if (createError()) {
              <p class="form-error">{{ createError() }}</p>
            }

            <div class="modal-footer">
              <button type="button" class="stride-btn stride-btn-secondary"
                      (click)="closeCreateModal()" [disabled]="creating()">Cancel</button>
              <button type="submit" class="stride-btn stride-btn-primary"
                      [disabled]="createForm.invalid || creating()">
                @if (creating()) { Creating… } @else { Create Invoice }
              </button>
            </div>
          </form>
        </div>
      </div>
    }

    <!-- Click-away for dropdown menus -->
    @if (openMenuId()) {
      <div class="click-away" (click)="openMenuId.set(null)" aria-hidden="true"></div>
    }
  `,
  styles: [`
    .inv-page {
      max-width: 72rem;
      margin: 0 auto;
      padding: 1.5rem 1rem 3rem;
    }

    /* Header */
    .inv-header {
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      gap: 1rem;
      margin-bottom: 1.25rem;
    }
    .inv-title {
      font-size: 1.375rem;
      font-weight: 700;
      color: var(--stride-text-primary);
      margin: 0 0 0.25rem;
    }
    .inv-subtitle {
      font-size: 0.875rem;
      color: var(--stride-text-muted);
      margin: 0;
    }

    /* KPI cards */
    .inv-kpis {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(10rem, 1fr));
      gap: 0.875rem;
      margin-bottom: 1.25rem;
    }
    .inv-kpi {
      background: var(--stride-surface);
      border: 1px solid var(--stride-border-soft);
      border-radius: var(--stride-radius-lg);
      padding: 1rem 1.25rem;
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
    }
    .inv-kpi-label {
      font-size: 0.75rem;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      color: var(--stride-text-muted);
    }
    .inv-kpi-value {
      font-size: 1.5rem;
      font-weight: 700;
      color: var(--stride-text-primary);
    }
    .inv-kpi--pending { border-left: 3px solid #F59E0B; }
    .inv-kpi--revenue { border-left: 3px solid #10B981; }
    .inv-kpi--total   { border-left: 3px solid var(--stride-primary); }
    .inv-kpi--draft   { border-left: 3px solid #6B7280; }

    /* Notification */
    .inv-notification {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 0.75rem;
      padding: 0.75rem 1rem;
      margin-bottom: 1rem;
      border-radius: var(--stride-radius-md);
      background: color-mix(in srgb, #10B981 10%, var(--stride-surface));
      border: 1px solid color-mix(in srgb, #10B981 30%, transparent);
      color: #059669;
      font-size: 0.875rem;
    }
    .inv-notification--error {
      background: color-mix(in srgb, #EF4444 10%, var(--stride-surface));
      border-color: color-mix(in srgb, #EF4444 30%, transparent);
      color: #DC2626;
    }
    .inv-notif-close {
      background: none; border: none; cursor: pointer; color: inherit;
      padding: 0.125rem; display: flex; align-items: center; opacity: 0.7;
    }
    .inv-notif-close:hover { opacity: 1; }

    /* Toolbar */
    .inv-toolbar {
      display: flex;
      align-items: center;
      flex-wrap: wrap;
      gap: 0.625rem;
      margin-bottom: 1.25rem;
    }
    .inv-search-wrap {
      position: relative;
      flex: 1;
      min-width: 14rem;
    }
    .inv-search-icon {
      position: absolute;
      left: 0.75rem;
      top: 50%;
      transform: translateY(-50%);
      color: var(--stride-text-muted);
      pointer-events: none;
    }
    .inv-search {
      width: 100%;
      padding: 0.5rem 0.75rem 0.5rem 2.25rem;
      border: 1px solid var(--stride-border-soft);
      border-radius: var(--stride-radius-md);
      background: var(--stride-surface);
      color: var(--stride-text-primary);
      font-size: 0.875rem;
      font-family: inherit;
      box-sizing: border-box;
    }
    .inv-search:focus {
      outline: none;
      border-color: var(--stride-primary);
      box-shadow: 0 0 0 3px color-mix(in srgb, var(--stride-primary) 15%, transparent);
    }
    .inv-search::placeholder { color: var(--stride-text-muted); }
    :host ::ng-deep .inv-filter-select .p-select { min-width: 10rem; font-size: 0.875rem; }
    .inv-clear-btn {
      font-size: 0.8125rem; color: var(--stride-text-muted);
      background: none; border: none; cursor: pointer;
      padding: 0.25rem 0.5rem; font-family: inherit;
    }
    .inv-clear-btn:hover { color: var(--stride-primary); }

    /* Table */
    .inv-table-wrap {
      border: 1px solid var(--stride-border-soft);
      border-radius: var(--stride-radius-lg);
      overflow: hidden;
      background: var(--stride-surface);
    }
    .inv-table {
      width: 100%;
      border-collapse: collapse;
      font-size: 0.875rem;
    }
    .inv-table thead tr {
      background: var(--stride-surface-secondary);
      border-bottom: 1px solid var(--stride-border-soft);
    }
    .inv-table th {
      padding: 0.625rem 1rem;
      text-align: left;
      font-size: 0.75rem;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      color: var(--stride-text-muted);
      white-space: nowrap;
    }
    .inv-th-amount, .inv-td-amount { text-align: right; }
    .inv-th-actions { width: 3rem; }
    .inv-table tbody tr {
      border-bottom: 1px solid var(--stride-border-soft);
      transition: background 120ms;
    }
    .inv-table tbody tr:last-child { border-bottom: none; }
    .inv-table tbody tr:hover { background: var(--stride-surface-hover); }
    .inv-row--void { opacity: 0.5; }
    .inv-table td { padding: 0.875rem 1rem; vertical-align: middle; color: var(--stride-text-primary); }

    .inv-td-number { font-weight: 600; font-family: monospace; font-size: 0.875rem; }
    .inv-client-name { font-weight: 600; }
    .inv-client-email { font-size: 0.8125rem; color: var(--stride-text-muted); margin-top: 0.125rem; }
    .inv-td-date { color: var(--stride-text-muted); font-size: 0.8125rem; white-space: nowrap; }
    .inv-td-amount { font-weight: 600; white-space: nowrap; }

    /* Badges */
    .inv-badge {
      display: inline-block;
      font-size: 0.6875rem;
      font-weight: 600;
      padding: 0.2rem 0.55rem;
      border-radius: 99px;
      text-transform: uppercase;
      letter-spacing: 0.04em;
      white-space: nowrap;
    }
    .badge--draft    { background: color-mix(in srgb, #6B7280 12%, transparent); color: #6B7280; }
    .badge--sent     { background: color-mix(in srgb, #F59E0B 12%, transparent); color: #D97706; }
    .badge--paid     { background: color-mix(in srgb, #10B981 12%, transparent); color: #059669; }
    .badge--void     { background: color-mix(in srgb, #EF4444 12%, transparent); color: #DC2626; }

    /* Action menu */
    .inv-td-actions { width: 3rem; text-align: center; }
    .inv-menu-wrap { position: relative; display: inline-block; }
    .inv-menu-trigger {
      width: 2rem; height: 2rem;
      border-radius: var(--stride-radius-md);
      display: flex; align-items: center; justify-content: center;
      border: 1px solid transparent;
      background: transparent;
      color: var(--stride-text-muted);
      cursor: pointer;
      transition: all 120ms;
    }
    .inv-menu-trigger:hover, .inv-menu-trigger--open {
      background: var(--stride-surface-secondary);
      border-color: var(--stride-border-soft);
      color: var(--stride-text-primary);
    }
    .inv-menu {
      position: absolute;
      right: 0;
      top: calc(100% + 0.25rem);
      background: var(--stride-surface);
      border: 1px solid var(--stride-border-soft);
      border-radius: var(--stride-radius-md);
      box-shadow: var(--stride-shadow-lg);
      min-width: 9rem;
      z-index: 50;
      padding: 0.25rem 0;
    }
    .inv-menu-item {
      display: block; width: 100%; text-align: left;
      padding: 0.5rem 0.875rem;
      font-size: 0.875rem; font-family: inherit;
      background: none; border: none;
      color: var(--stride-text-primary);
      cursor: pointer; transition: background 100ms;
    }
    .inv-menu-item:hover { background: var(--stride-surface-hover); }
    .inv-menu-item--danger { color: #EF4444; }
    .inv-menu-item--danger:hover { background: color-mix(in srgb, #EF4444 8%, transparent); }
    .inv-menu-empty {
      display: block; padding: 0.5rem 0.875rem;
      font-size: 0.8125rem; color: var(--stride-text-muted);
    }

    /* Pagination */
    .inv-pagination {
      display: flex; align-items: center; justify-content: space-between;
      flex-wrap: wrap; gap: 0.75rem; margin-top: 1rem;
    }
    .inv-page-info { font-size: 0.8125rem; color: var(--stride-text-muted); }
    .inv-page-controls { display: flex; align-items: center; gap: 0.25rem; }
    .inv-page-btn {
      min-width: 2rem; height: 2rem; padding: 0 0.375rem;
      border-radius: var(--stride-radius-md);
      border: 1px solid var(--stride-border-soft);
      background: var(--stride-surface);
      color: var(--stride-text-secondary);
      font-size: 0.8125rem; font-family: inherit;
      cursor: pointer; display: flex; align-items: center; justify-content: center;
      transition: all 120ms;
    }
    .inv-page-btn:hover:not(:disabled) { border-color: var(--stride-primary); color: var(--stride-primary); }
    .inv-page-btn--active { background: var(--stride-primary); border-color: var(--stride-primary); color: #fff; font-weight: 600; }
    .inv-page-btn:disabled { opacity: 0.4; cursor: not-allowed; }

    /* Empty / error */
    .inv-empty {
      display: flex; flex-direction: column; align-items: center;
      gap: 0.75rem; padding: 4rem 1rem;
      color: var(--stride-text-muted); font-size: 0.9375rem; text-align: center;
    }

    /* Skeleton */
    .sk-row { animation: pulse 1.5s ease-in-out infinite; }
    .sk-text { display: flex; flex-direction: column; gap: 0.375rem; }
    .sk-line { border-radius: 4px; background: var(--stride-border); height: 0.75rem; }
    .sk-line--num    { width: 6rem; }
    .sk-line--name   { width: 8rem; }
    .sk-line--email  { width: 11rem; height: 0.625rem; }
    .sk-line--badge  { width: 4rem; }
    .sk-line--date   { width: 5rem; }
    .sk-line--amount { width: 5rem; }
    @keyframes pulse { 0%, 100% { opacity: 1; } 50% { opacity: 0.5; } }

    /* Modal */
    .modal-backdrop {
      position: fixed; inset: 0; background: rgba(0,0,0,0.45);
      display: flex; align-items: center; justify-content: center;
      z-index: 100; padding: 1rem;
    }
    .modal {
      background: var(--stride-surface);
      border-radius: var(--stride-radius-xl);
      box-shadow: var(--stride-shadow-xl);
      width: 100%; max-width: 28rem; overflow: hidden;
    }
    .modal--lg { max-width: 42rem; }
    .modal-header {
      display: flex; align-items: center; justify-content: space-between;
      padding: 1.25rem 1.5rem 1rem;
      border-bottom: 1px solid var(--stride-border-soft);
    }
    .modal-title { font-size: 1.0625rem; font-weight: 700; color: var(--stride-text-primary); margin: 0; }
    .modal-close {
      background: none; border: none; cursor: pointer; color: var(--stride-text-muted);
      padding: 0.25rem; border-radius: var(--stride-radius-sm);
      display: flex; align-items: center; transition: color 120ms;
    }
    .modal-close:hover { color: var(--stride-text-primary); }
    .modal-body {
      padding: 1.25rem 1.5rem;
      display: flex; flex-direction: column; gap: 0.875rem;
      max-height: 75vh; overflow-y: auto;
    }
    .modal-footer {
      display: flex; justify-content: flex-end; gap: 0.625rem; padding-top: 0.5rem;
    }

    /* Form */
    .form-row { display: grid; grid-template-columns: 1fr 1fr; gap: 0.875rem; }
    .form-field { display: flex; flex-direction: column; gap: 0.375rem; }
    .form-field--sm { max-width: 8rem; }
    .form-label { font-size: 0.875rem; font-weight: 500; color: var(--stride-text-secondary); }
    .form-required { color: #EF4444; }
    .form-input {
      padding: 0.5rem 0.75rem;
      border: 1px solid var(--stride-border-soft);
      border-radius: var(--stride-radius-md);
      background: var(--stride-surface);
      color: var(--stride-text-primary);
      font-size: 0.875rem; font-family: inherit;
    }
    .form-input:focus {
      outline: none; border-color: var(--stride-primary);
      box-shadow: 0 0 0 3px color-mix(in srgb, var(--stride-primary) 15%, transparent);
    }
    .form-input--error { border-color: #EF4444; }
    .form-error { font-size: 0.8125rem; color: #EF4444; margin: 0; }

    /* Line items */
    .form-section { display: flex; flex-direction: column; gap: 0.625rem; }
    .form-section-header {
      display: flex; align-items: center; justify-content: space-between;
    }
    .form-section-title { font-size: 0.875rem; font-weight: 600; color: var(--stride-text-secondary); }
    .form-add-btn {
      font-size: 0.8125rem; color: var(--stride-primary);
      background: none; border: none; cursor: pointer; font-family: inherit;
    }
    .form-add-btn:hover { text-decoration: underline; }
    .line-items { display: flex; flex-direction: column; gap: 0.5rem; }
    .line-item {
      display: grid;
      grid-template-columns: 1fr 6rem 4rem 5rem auto;
      align-items: center;
      gap: 0.5rem;
    }
    .line-item-desc  { grid-column: 1; }
    .line-item-price { grid-column: 2; text-align: right; }
    .line-item-qty   { grid-column: 3; text-align: center; }
    .line-item-subtotal {
      grid-column: 4; text-align: right;
      font-size: 0.875rem; font-weight: 600; color: var(--stride-text-primary);
    }
    .line-item-remove {
      grid-column: 5;
      background: none; border: none; cursor: pointer;
      color: var(--stride-text-muted); padding: 0.25rem;
      display: flex; align-items: center;
    }
    .line-item-remove:hover { color: #EF4444; }
    .line-items-total {
      text-align: right; font-size: 0.875rem; color: var(--stride-text-secondary);
      padding-top: 0.375rem; border-top: 1px solid var(--stride-border-soft);
    }

    /* Click-away */
    .click-away { position: fixed; inset: 0; z-index: 40; }

    /* A11y */
    .sr-only {
      position: absolute; width: 1px; height: 1px;
      padding: 0; margin: -1px; overflow: hidden;
      clip: rect(0,0,0,0); white-space: nowrap; border: 0;
    }
  `],
})
export class InvoicingPageComponent implements OnInit {
  readonly svc        = inject(InvoiceService);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  // ── Filter state ─────────────────────────────────────────────────────────
  readonly searchTerm = signal('');
  selectedStatus: InvoiceStatus | null = null;
  private readonly search$ = new Subject<string>();

  readonly hasActiveFilters = computed(() => !!this.searchTerm() || this.selectedStatus !== null);

  readonly statusFilterOptions = [
    { label: 'All Statuses', value: null },
    { label: 'Draft',  value: 0 as InvoiceStatus },
    { label: 'Sent',   value: 1 as InvoiceStatus },
    { label: 'Paid',   value: 2 as InvoiceStatus },
    { label: 'Void',   value: 3 as InvoiceStatus },
  ];

  // ── UI state ──────────────────────────────────────────────────────────────
  readonly notification = signal<{ type: 'success' | 'error'; message: string } | null>(null);
  readonly openMenuId   = signal<string | null>(null);
  readonly skeletons    = [1, 2, 3, 4, 5, 6];

  // ── Create modal ──────────────────────────────────────────────────────────
  readonly showCreateModal = signal(false);
  readonly creating        = signal(false);
  readonly createError     = signal<string | null>(null);

  readonly createForm = this.fb.group({
    invoiceNumber: ['', Validators.required],
    clientName:    ['', Validators.required],
    clientEmail:   ['', [Validators.required, Validators.email]],
    currency:      ['USD', [Validators.required, Validators.minLength(3), Validators.maxLength(3)]],
    dueDate:       ['', Validators.required],
    notes:         [''],
    lineItems: this.fb.array([this.buildLineItem()]),
  });

  get lineItemsArray() { return this.createForm.get('lineItems') as FormArray; }

  // ── Derived ───────────────────────────────────────────────────────────────
  readonly pageRange = computed(() => {
    const total = this.svc.totalPages(), current = this.svc.page(), delta = 2;
    const range: number[] = [];
    for (let i = Math.max(1, current - delta); i <= Math.min(total, current + delta); i++) range.push(i);
    return range;
  });

  readonly lineItemSubtotals = signal<number[]>([0]);
  readonly invoiceTotal      = signal<number>(0);

  // ── Lifecycle ─────────────────────────────────────────────────────────────

  ngOnInit(): void {
    this.search$
      .pipe(debounceTime(350), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe(term => { this.searchTerm.set(term); this.reload(1); });

    // Keep invoiceTotal and lineItemSubtotals in sync with the FormArray.
    // computed() doesn't track FormArray value changes (they're RxJS, not signals).
    this.lineItemsArray.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((items: { unitPrice: unknown; quantity: unknown }[]) => {
        const subtotals = items.map(item =>
          Math.round(Number(item.unitPrice ?? 0) * Number(item.quantity ?? 0) * 100) / 100,
        );
        this.lineItemSubtotals.set(subtotals);
        this.invoiceTotal.set(subtotals.reduce((sum, s) => sum + s, 0));
      });

    this.reload();
  }

  // ── Search / filter ────────────────────────────────────────────────────────

  onSearchInput(event: Event): void {
    this.search$.next((event.target as HTMLInputElement).value);
  }

  onFilterChange(): void { this.reload(1); }

  clearFilters(): void {
    this.searchTerm.set('');
    this.selectedStatus = null;
    this.search$.next('');
    this.reload(1);
  }

  // ── Pagination ─────────────────────────────────────────────────────────────

  goToPage(page: number): void {
    if (page < 1 || page > this.svc.totalPages()) return;
    this.reload(page);
  }

  // ── Create modal ───────────────────────────────────────────────────────────

  openCreateModal(): void {
    this.createForm.reset({ invoiceNumber: '', clientName: '', clientEmail: '',
      currency: 'USD', dueDate: '', notes: '' });
    while (this.lineItemsArray.length > 1) this.lineItemsArray.removeAt(1);
    this.lineItemsArray.at(0).reset({ description: '', unitPrice: 0, quantity: 1 });
    this.createError.set(null);
    this.showCreateModal.set(true);
  }

  closeCreateModal(): void {
    if (this.creating()) return;
    this.showCreateModal.set(false);
  }

  addLineItem(): void {
    this.lineItemsArray.push(this.buildLineItem());
  }

  removeLineItem(index: number): void {
    this.lineItemsArray.removeAt(index);
  }

  lineSubtotal(index: number): number {
    return this.lineItemSubtotals()[index] ?? 0;
  }

  submitCreate(): void {
    if (this.createForm.invalid) { this.createForm.markAllAsTouched(); return; }

    const v = this.createForm.getRawValue();
    this.creating.set(true);
    this.createError.set(null);

    const lineItems = (v.lineItems as { description: string; unitPrice: number; quantity: number }[])
      .map(l => ({ description: l.description, unitPrice: l.unitPrice, quantity: l.quantity }));

    this.svc.generateInvoice({
      invoiceNumber: v.invoiceNumber!,
      clientName:    v.clientName!,
      clientEmail:   v.clientEmail!,
      currency:      v.currency!,
      dueDate:       v.dueDate!,
      notes:         v.notes || null,
      lineItems,
    }).subscribe({
      next: () => {
        this.creating.set(false);
        this.showCreateModal.set(false);
        this.notification.set({ type: 'success', message: `Invoice ${v.invoiceNumber} created.` });
        this.reload(1);
      },
      error: (err) => {
        this.creating.set(false);
        this.createError.set(err?.error?.error ?? 'Failed to create invoice.');
      },
    });
  }

  // ── Row actions ─────────────────────────────────────────────────────────────

  sendInvoice(inv: InvoiceSummaryDto): void {
    this.openMenuId.set(null);
    this.svc.sendInvoice(inv.id).subscribe({
      next: () => {
        this.notification.set({ type: 'success', message: `Invoice ${inv.invoiceNumber} sent.` });
        this.reload();
      },
      error: (err) => this.notification.set({ type: 'error',
        message: err?.error?.error ?? 'Failed to send invoice.' }),
    });
  }

  markPaid(inv: InvoiceSummaryDto): void {
    this.openMenuId.set(null);
    this.svc.markPaid(inv.id).subscribe({
      next: () => {
        this.notification.set({ type: 'success', message: `Invoice ${inv.invoiceNumber} marked as paid.` });
        this.reload();
      },
      error: (err) => this.notification.set({ type: 'error',
        message: err?.error?.error ?? 'Failed to mark invoice as paid.' }),
    });
  }

  voidInvoice(inv: InvoiceSummaryDto): void {
    this.openMenuId.set(null);
    this.svc.voidInvoice(inv.id).subscribe({
      next: () => {
        this.notification.set({ type: 'success', message: `Invoice ${inv.invoiceNumber} voided.` });
        this.reload();
      },
      error: (err) => this.notification.set({ type: 'error',
        message: err?.error?.error ?? 'Failed to void invoice.' }),
    });
  }

  // ── Menu toggle ─────────────────────────────────────────────────────────────

  toggleMenu(id: string): void {
    this.openMenuId.update(cur => (cur === id ? null : id));
  }

  // ── Display helpers ──────────────────────────────────────────────────────────

  statusBadgeClass(status: InvoiceStatus): string {
    const map: Record<InvoiceStatus, string> = {
      0: 'badge--draft',
      1: 'badge--sent',
      2: 'badge--paid',
      3: 'badge--void',
    };
    return map[status];
  }

  formatDate(iso: string): string {
    return new Date(`${iso}T00:00:00`).toLocaleDateString('en-GB', {
      day: 'numeric', month: 'short', year: 'numeric',
    });
  }

  // ── Private ──────────────────────────────────────────────────────────────────

  reload(page?: number): void {
    this.svc.loadInvoices(
      page ?? this.svc.page(),
      this.svc.pageSize(),
      this.searchTerm() || undefined,
      this.selectedStatus ?? undefined,
    );
  }

  private buildLineItem() {
    return this.fb.group({
      description: ['', Validators.required],
      unitPrice:   [0, [Validators.required, Validators.min(0)]],
      quantity:    [1, [Validators.required, Validators.min(1)]],
    });
  }
}
