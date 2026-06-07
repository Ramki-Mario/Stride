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
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { Subject } from 'rxjs';
import { SelectModule } from 'primeng/select';

import { ClientService } from '../../services/client.service';
import { ClientSummaryDto, ClientStatus } from '../../models/client.models';

@Component({
  selector: 'app-clients-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgClass, FormsModule, ReactiveFormsModule, SelectModule],
  template: `
    <div class="cl-page">

      <!-- ── Header ─────────────────────────────────────────────────────── -->
      <div class="cl-header">
        <div>
          <h1 class="cl-title">Clients</h1>
          <p class="cl-subtitle">
            @if (!svc.isLoading()) {
              {{ svc.totalCount() }} client{{ svc.totalCount() === 1 ? '' : 's' }} total
            } @else { Loading… }
          </p>
        </div>
        <button class="stride-btn stride-btn-primary" (click)="openCreateModal()">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" aria-hidden="true">
            <line x1="12" y1="5" x2="12" y2="19" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
            <line x1="5" y1="12" x2="19" y2="12" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
          </svg>
          New Client
        </button>
      </div>

      <!-- ── KPI cards ──────────────────────────────────────────────────── -->
      <div class="cl-kpis">
        <div class="cl-kpi cl-kpi--total">
          <span class="cl-kpi-label">Total Clients</span>
          <span class="cl-kpi-value">{{ svc.totalCount() }}</span>
        </div>
        <div class="cl-kpi cl-kpi--active">
          <span class="cl-kpi-label">Active</span>
          <span class="cl-kpi-value">{{ svc.activeCount() }}</span>
        </div>
        <div class="cl-kpi cl-kpi--inactive">
          <span class="cl-kpi-label">Inactive</span>
          <span class="cl-kpi-value">{{ svc.inactiveCount() }}</span>
        </div>
      </div>

      <!-- ── Notification ────────────────────────────────────────────────── -->
      @if (notification()) {
        <div class="cl-notification" [class.cl-notification--error]="notification()!.type === 'error'">
          <span>{{ notification()!.message }}</span>
          <button class="cl-notif-close" (click)="notification.set(null)" aria-label="Dismiss">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none">
              <line x1="18" y1="6" x2="6" y2="18" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
              <line x1="6" y1="6" x2="18" y2="18" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
            </svg>
          </button>
        </div>
      }

      <!-- ── Toolbar ─────────────────────────────────────────────────────── -->
      <div class="cl-toolbar">
        <div class="cl-search-wrap">
          <svg class="cl-search-icon" width="16" height="16" viewBox="0 0 24 24" fill="none" aria-hidden="true">
            <circle cx="11" cy="11" r="8" stroke="currentColor" stroke-width="1.75"/>
            <line x1="21" y1="21" x2="16.65" y2="16.65" stroke="currentColor" stroke-width="1.75" stroke-linecap="round"/>
          </svg>
          <input class="cl-search" type="search" placeholder="Search by name, email, contact…"
                 [value]="searchTerm()"
                 (input)="onSearchInput($event)"
                 aria-label="Search clients"/>
        </div>
        <p-select [options]="statusFilterOptions" [(ngModel)]="selectedStatus"
                  (onChange)="onFilterChange()"
                  optionLabel="label" optionValue="value"
                  styleClass="cl-filter-select" placeholder="All Statuses"/>
        @if (hasActiveFilters()) {
          <button class="cl-clear-btn" (click)="clearFilters()">Clear</button>
        }
      </div>

      <!-- ── Loading skeleton ────────────────────────────────────────────── -->
      @if (svc.isLoading()) {
        <div class="cl-table-wrap">
          <table class="cl-table" aria-label="Loading clients">
            <thead><tr>
              <th>Name</th><th>Contact</th><th>Email</th><th>Phone</th><th>Status</th><th>Created</th><th></th>
            </tr></thead>
            <tbody>
              @for (i of skeletons; track i) {
                <tr class="sk-row">
                  <td><div class="sk-line sk-line--name"></div></td>
                  <td><div class="sk-line sk-line--contact"></div></td>
                  <td><div class="sk-line sk-line--email"></div></td>
                  <td><div class="sk-line sk-line--phone"></div></td>
                  <td><div class="sk-line sk-line--badge"></div></td>
                  <td><div class="sk-line sk-line--date"></div></td>
                  <td></td>
                </tr>
              }
            </tbody>
          </table>
        </div>

      <!-- ── Error ──────────────────────────────────────────────────────── -->
      } @else if (svc.error()) {
        <div class="cl-empty">
          <svg width="40" height="40" viewBox="0 0 24 24" fill="none" aria-hidden="true">
            <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="1.5"/>
            <line x1="12" y1="8" x2="12" y2="12" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
            <line x1="12" y1="16" x2="12.01" y2="16" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
          </svg>
          <p>{{ svc.error() }}</p>
          <button class="stride-btn stride-btn-secondary" (click)="reload()">Retry</button>
        </div>

      <!-- ── Empty ──────────────────────────────────────────────────────── -->
      } @else if (svc.isEmpty()) {
        <div class="cl-empty">
          <svg width="40" height="40" viewBox="0 0 24 24" fill="none" aria-hidden="true">
            <circle cx="12" cy="12" r="9" stroke="currentColor" stroke-width="1.5"/>
            <path d="M12 7v5l3 3" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/>
          </svg>
          <p>
            @if (hasActiveFilters()) { No clients match the current filters. }
            @else { No clients yet. Add your first client to get started. }
          </p>
          @if (!hasActiveFilters()) {
            <button class="stride-btn stride-btn-primary" (click)="openCreateModal()">New Client</button>
          }
        </div>

      <!-- ── Table ──────────────────────────────────────────────────────── -->
      } @else {
        <div class="cl-table-wrap">
          <table class="cl-table" aria-label="Clients">
            <thead>
              <tr>
                <th>Name</th>
                <th>Contact Person</th>
                <th>Email</th>
                <th>Phone</th>
                <th>Status</th>
                <th>Created</th>
                <th class="cl-th-actions"><span class="sr-only">Actions</span></th>
              </tr>
            </thead>
            <tbody>
              @for (client of svc.clients(); track client.id) {
                <tr [class.cl-row--inactive]="client.status === 1">
                  <td class="cl-td-name">{{ client.name }}</td>
                  <td class="cl-td-muted">{{ client.contactPerson || '—' }}</td>
                  <td class="cl-td-muted">{{ client.email || '—' }}</td>
                  <td class="cl-td-muted">{{ client.phone || '—' }}</td>
                  <td>
                    <span class="cl-badge" [ngClass]="statusBadgeClass(client.status)">
                      {{ client.statusLabel }}
                    </span>
                  </td>
                  <td class="cl-td-date">{{ formatDate(client.createdAt) }}</td>
                  <td class="cl-td-actions">
                    <div class="cl-menu-wrap">
                      <button class="cl-menu-trigger"
                              [class.cl-menu-trigger--open]="openMenuId() === client.id"
                              (click)="toggleMenu(client.id)"
                              aria-label="Client actions">
                        <svg width="16" height="16" viewBox="0 0 24 24" fill="none">
                          <circle cx="12" cy="5"  r="1.2" fill="currentColor"/>
                          <circle cx="12" cy="12" r="1.2" fill="currentColor"/>
                          <circle cx="12" cy="19" r="1.2" fill="currentColor"/>
                        </svg>
                      </button>
                      @if (openMenuId() === client.id) {
                        <div class="cl-menu" role="menu">
                          <button class="cl-menu-item" role="menuitem" (click)="openEditModal(client)">
                            Edit
                          </button>
                          @if (client.status === 0) {
                            <button class="cl-menu-item cl-menu-item--danger" role="menuitem"
                                    (click)="deactivateClient(client)">
                              Deactivate
                            </button>
                          }
                          @if (client.status === 1) {
                            <button class="cl-menu-item" role="menuitem"
                                    (click)="reactivateClient(client)">
                              Reactivate
                            </button>
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

        <!-- ── Pagination ──────────────────────────────────────────────── -->
        @if (svc.totalPages() > 1) {
          <div class="cl-pagination">
            <span class="cl-page-info">
              Page {{ svc.page() }} of {{ svc.totalPages() }} · {{ svc.totalCount() }} total
            </span>
            <div class="cl-page-controls">
              <button class="cl-page-btn" [disabled]="!svc.hasPrevious()"
                      (click)="goToPage(svc.page() - 1)">
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none">
                  <polyline points="15 18 9 12 15 6" stroke="currentColor" stroke-width="2"
                            stroke-linecap="round" stroke-linejoin="round"/>
                </svg>
              </button>
              @for (p of pageRange(); track p) {
                <button class="cl-page-btn" [class.cl-page-btn--active]="p === svc.page()"
                        (click)="goToPage(p)">{{ p }}</button>
              }
              <button class="cl-page-btn" [disabled]="!svc.hasNext()"
                      (click)="goToPage(svc.page() + 1)">
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none">
                  <polyline points="9 18 15 12 9 6" stroke="currentColor" stroke-width="2"
                            stroke-linecap="round" stroke-linejoin="round"/>
                </svg>
              </button>
            </div>
          </div>
        }
      }
    </div>

    <!-- ═══════════════════════════════════════════════════════════════════ -->
    <!-- Create / Edit Client Modal                                          -->
    <!-- ═══════════════════════════════════════════════════════════════════ -->
    @if (showModal()) {
      <div class="modal-backdrop" (click)="closeModal()" role="presentation">
        <div class="modal" role="dialog" aria-modal="true"
             [attr.aria-labelledby]="editingClient() ? 'edit-modal-title' : 'create-modal-title'"
             (click)="$event.stopPropagation()">

          <div class="modal-header">
            @if (editingClient()) {
              <h2 class="modal-title" id="edit-modal-title">Edit Client</h2>
            } @else {
              <h2 class="modal-title" id="create-modal-title">New Client</h2>
            }
            <button class="modal-close" (click)="closeModal()" aria-label="Close">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none">
                <line x1="18" y1="6" x2="6"  y2="18" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
                <line x1="6"  y1="6" x2="18" y2="18" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
              </svg>
            </button>
          </div>

          <form class="modal-body" [formGroup]="clientForm" (ngSubmit)="submitModal()">

            <!-- Name -->
            <div class="form-field">
              <label class="form-label" for="cl-name">
                Name <span class="form-required">*</span>
              </label>
              <input id="cl-name" class="form-input" formControlName="name"
                     placeholder="Acme Corporation"
                     [class.form-input--error]="clientForm.controls.name.invalid && clientForm.controls.name.touched"/>
              @if (clientForm.controls.name.invalid && clientForm.controls.name.touched) {
                <p class="form-error">Name is required.</p>
              }
            </div>

            <!-- Contact Person + Email -->
            <div class="form-row">
              <div class="form-field">
                <label class="form-label" for="cl-contact">Contact Person</label>
                <input id="cl-contact" class="form-input" formControlName="contactPerson"
                       placeholder="Jane Smith"/>
              </div>
              <div class="form-field">
                <label class="form-label" for="cl-email">Email</label>
                <input id="cl-email" class="form-input" type="email" formControlName="email"
                       placeholder="jane@acme.com"
                       [class.form-input--error]="clientForm.controls.email.invalid && clientForm.controls.email.touched"/>
                @if (clientForm.controls.email.invalid && clientForm.controls.email.touched) {
                  <p class="form-error">Enter a valid email address.</p>
                }
              </div>
            </div>

            <!-- Phone + Address -->
            <div class="form-row">
              <div class="form-field">
                <label class="form-label" for="cl-phone">Phone</label>
                <input id="cl-phone" class="form-input" formControlName="phone"
                       placeholder="+44 20 1234 5678"/>
              </div>
              <div class="form-field">
                <label class="form-label" for="cl-address">Address</label>
                <input id="cl-address" class="form-input" formControlName="address"
                       placeholder="1 High Street, London"/>
              </div>
            </div>

            <!-- Notes -->
            <div class="form-field">
              <label class="form-label" for="cl-notes">Notes</label>
              <textarea id="cl-notes" class="form-input form-textarea" formControlName="notes"
                        rows="3" placeholder="Optional notes…"></textarea>
            </div>

            @if (modalError()) {
              <p class="form-error">{{ modalError() }}</p>
            }

            <div class="modal-footer">
              <button type="button" class="stride-btn stride-btn-secondary"
                      (click)="closeModal()" [disabled]="saving()">Cancel</button>
              <button type="submit" class="stride-btn stride-btn-primary"
                      [disabled]="clientForm.invalid || saving()">
                @if (saving()) {
                  @if (editingClient()) { Saving… } @else { Creating… }
                } @else {
                  @if (editingClient()) { Save Changes } @else { Create Client }
                }
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
    .cl-page {
      max-width: 72rem;
      margin: 0 auto;
      padding: 1.5rem 1rem 3rem;
    }

    /* ── Header ─────────────────────────────────────────────────────────── */
    .cl-header {
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      gap: 1rem;
      margin-bottom: 1.25rem;
    }
    .cl-title {
      font-size: 1.375rem;
      font-weight: 700;
      color: var(--stride-text-primary);
      margin: 0 0 0.25rem;
    }
    .cl-subtitle {
      font-size: 0.875rem;
      color: var(--stride-text-muted);
      margin: 0;
    }

    /* ── KPI cards ──────────────────────────────────────────────────────── */
    .cl-kpis {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(10rem, 1fr));
      gap: 0.875rem;
      margin-bottom: 1.25rem;
    }
    .cl-kpi {
      background: var(--stride-surface);
      border: 1px solid var(--stride-border-soft);
      border-radius: var(--stride-radius-lg);
      padding: 1rem 1.25rem;
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
    }
    .cl-kpi-label {
      font-size: 0.75rem;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      color: var(--stride-text-muted);
    }
    .cl-kpi-value {
      font-size: 1.5rem;
      font-weight: 700;
      color: var(--stride-text-primary);
    }
    .cl-kpi--total    { border-left: 3px solid var(--stride-primary); }
    .cl-kpi--active   { border-left: 3px solid #10B981; }
    .cl-kpi--inactive { border-left: 3px solid #6B7280; }

    /* ── Notification ────────────────────────────────────────────────────── */
    .cl-notification {
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
    .cl-notification--error {
      background: color-mix(in srgb, #EF4444 10%, var(--stride-surface));
      border-color: color-mix(in srgb, #EF4444 30%, transparent);
      color: #DC2626;
    }
    .cl-notif-close {
      background: none; border: none; cursor: pointer; color: inherit;
      padding: 0.125rem; display: flex; align-items: center; opacity: 0.7;
    }
    .cl-notif-close:hover { opacity: 1; }

    /* ── Toolbar ─────────────────────────────────────────────────────────── */
    .cl-toolbar {
      display: flex;
      align-items: center;
      flex-wrap: wrap;
      gap: 0.625rem;
      margin-bottom: 1.25rem;
    }
    .cl-search-wrap {
      position: relative;
      flex: 1;
      min-width: 14rem;
    }
    .cl-search-icon {
      position: absolute;
      left: 0.75rem;
      top: 50%;
      transform: translateY(-50%);
      color: var(--stride-text-muted);
      pointer-events: none;
    }
    .cl-search {
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
    .cl-search:focus {
      outline: none;
      border-color: var(--stride-primary);
      box-shadow: 0 0 0 3px color-mix(in srgb, var(--stride-primary) 15%, transparent);
    }
    .cl-search::placeholder { color: var(--stride-text-muted); }
    :host ::ng-deep .cl-filter-select .p-select { min-width: 10rem; font-size: 0.875rem; }
    .cl-clear-btn {
      font-size: 0.8125rem; color: var(--stride-text-muted);
      background: none; border: none; cursor: pointer;
      padding: 0.25rem 0.5rem; font-family: inherit;
    }
    .cl-clear-btn:hover { color: var(--stride-primary); }

    /* ── Table ───────────────────────────────────────────────────────────── */
    .cl-table-wrap {
      border: 1px solid var(--stride-border-soft);
      border-radius: var(--stride-radius-lg);
      /* overflow: visible so action menus escape the wrapper */
      overflow: visible;
      background: var(--stride-surface);
    }
    .cl-table {
      width: 100%;
      border-collapse: collapse;
      font-size: 0.875rem;
    }
    .cl-table thead tr {
      background: var(--stride-surface-secondary);
      border-bottom: 1px solid var(--stride-border-soft);
    }
    .cl-table thead tr th:first-child { border-top-left-radius: var(--stride-radius-lg); }
    .cl-table thead tr th:last-child  { border-top-right-radius: var(--stride-radius-lg); }
    .cl-table tbody tr:last-child td:first-child { border-bottom-left-radius: var(--stride-radius-lg); }
    .cl-table tbody tr:last-child td:last-child  { border-bottom-right-radius: var(--stride-radius-lg); }
    .cl-table th {
      padding: 0.625rem 1rem;
      text-align: left;
      font-size: 0.75rem;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      color: var(--stride-text-muted);
      white-space: nowrap;
    }
    .cl-th-actions { width: 3rem; }
    .cl-table tbody tr {
      border-bottom: 1px solid var(--stride-border-soft);
      transition: background 120ms;
    }
    .cl-table tbody tr:last-child { border-bottom: none; }
    .cl-table tbody tr:hover { background: var(--stride-surface-hover); }
    .cl-row--inactive { opacity: 0.6; }
    .cl-table td { padding: 0.875rem 1rem; vertical-align: middle; color: var(--stride-text-primary); }

    .cl-td-name    { font-weight: 600; }
    .cl-td-muted   { color: var(--stride-text-secondary); font-size: 0.875rem; }
    .cl-td-date    { color: var(--stride-text-muted); font-size: 0.8125rem; white-space: nowrap; }

    /* ── Badges ──────────────────────────────────────────────────────────── */
    .cl-badge {
      display: inline-block;
      font-size: 0.6875rem;
      font-weight: 600;
      padding: 0.2rem 0.55rem;
      border-radius: 99px;
      text-transform: uppercase;
      letter-spacing: 0.04em;
      white-space: nowrap;
    }
    .badge--active   { background: color-mix(in srgb, #10B981 12%, transparent); color: #059669; }
    .badge--inactive { background: color-mix(in srgb, #6B7280 12%, transparent); color: #6B7280; }

    /* ── Action menu ──────────────────────────────────────────────────────── */
    .cl-td-actions { width: 3rem; text-align: center; }
    .cl-menu-wrap  { position: relative; display: inline-block; }
    .cl-menu-trigger {
      width: 2rem; height: 2rem;
      border-radius: var(--stride-radius-md);
      display: flex; align-items: center; justify-content: center;
      border: 1px solid transparent;
      background: transparent;
      color: var(--stride-text-muted);
      cursor: pointer;
      transition: all 120ms;
    }
    .cl-menu-trigger:hover, .cl-menu-trigger--open {
      background: var(--stride-surface-secondary);
      border-color: var(--stride-border-soft);
      color: var(--stride-text-primary);
    }
    .cl-menu {
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
    .cl-menu-item {
      display: block; width: 100%; text-align: left;
      padding: 0.5rem 0.875rem;
      font-size: 0.875rem; font-family: inherit;
      background: none; border: none;
      color: var(--stride-text-primary);
      cursor: pointer; transition: background 100ms;
    }
    .cl-menu-item:hover { background: var(--stride-surface-hover); }
    .cl-menu-item--danger { color: #EF4444; }
    .cl-menu-item--danger:hover { background: color-mix(in srgb, #EF4444 8%, transparent); }

    /* ── Pagination ───────────────────────────────────────────────────────── */
    .cl-pagination {
      display: flex; align-items: center; justify-content: space-between;
      flex-wrap: wrap; gap: 0.75rem; margin-top: 1rem;
    }
    .cl-page-info { font-size: 0.8125rem; color: var(--stride-text-muted); }
    .cl-page-controls { display: flex; align-items: center; gap: 0.25rem; }
    .cl-page-btn {
      min-width: 2rem; height: 2rem; padding: 0 0.375rem;
      border-radius: var(--stride-radius-md);
      border: 1px solid var(--stride-border-soft);
      background: var(--stride-surface);
      color: var(--stride-text-secondary);
      font-size: 0.8125rem; font-family: inherit;
      cursor: pointer; display: flex; align-items: center; justify-content: center;
      transition: all 120ms;
    }
    .cl-page-btn:hover:not(:disabled) { border-color: var(--stride-primary); color: var(--stride-primary); }
    .cl-page-btn--active { background: var(--stride-primary); border-color: var(--stride-primary); color: #fff; font-weight: 600; }
    .cl-page-btn:disabled { opacity: 0.4; cursor: not-allowed; }

    /* ── Empty / error ─────────────────────────────────────────────────────── */
    .cl-empty {
      display: flex; flex-direction: column; align-items: center;
      gap: 0.75rem; padding: 4rem 1rem;
      color: var(--stride-text-muted); font-size: 0.9375rem; text-align: center;
    }

    /* ── Skeleton ──────────────────────────────────────────────────────────── */
    .sk-row { animation: pulse 1.5s ease-in-out infinite; }
    .sk-line { border-radius: 4px; background: var(--stride-border); height: 0.75rem; }
    .sk-line--name    { width: 9rem; }
    .sk-line--contact { width: 7rem; }
    .sk-line--email   { width: 10rem; }
    .sk-line--phone   { width: 6rem; }
    .sk-line--badge   { width: 4rem; }
    .sk-line--date    { width: 5rem; }
    @keyframes pulse { 0%, 100% { opacity: 1; } 50% { opacity: 0.5; } }

    /* ── Modal ─────────────────────────────────────────────────────────────── */
    .modal-backdrop {
      position: fixed; inset: 0; background: rgba(0,0,0,0.45);
      display: flex; align-items: center; justify-content: center;
      z-index: 100; padding: 1rem;
    }
    .modal {
      background: var(--stride-surface);
      border-radius: var(--stride-radius-xl);
      box-shadow: var(--stride-shadow-xl);
      width: 100%; max-width: 36rem; overflow: hidden;
    }
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

    /* ── Form ───────────────────────────────────────────────────────────────── */
    .form-row { display: grid; grid-template-columns: 1fr 1fr; gap: 0.875rem; }
    .form-field { display: flex; flex-direction: column; gap: 0.375rem; }
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
    .form-textarea { resize: vertical; min-height: 5rem; }
    .form-error { font-size: 0.8125rem; color: #EF4444; margin: 0; }

    /* ── Click-away ─────────────────────────────────────────────────────────── */
    .click-away { position: fixed; inset: 0; z-index: 40; }

    /* ── A11y ───────────────────────────────────────────────────────────────── */
    .sr-only {
      position: absolute; width: 1px; height: 1px;
      padding: 0; margin: -1px; overflow: hidden;
      clip: rect(0,0,0,0); white-space: nowrap; border: 0;
    }
  `],
})
export class ClientsPageComponent implements OnInit {
  readonly svc        = inject(ClientService);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  // ── Filter state ──────────────────────────────────────────────────────────
  readonly searchTerm  = signal('');
  selectedStatus: ClientStatus | null = null;
  private readonly search$ = new Subject<string>();

  readonly hasActiveFilters = computed(() => !!this.searchTerm() || this.selectedStatus !== null);

  readonly statusFilterOptions = [
    { label: 'All Statuses', value: null },
    { label: 'Active',       value: 0 as ClientStatus },
    { label: 'Inactive',     value: 1 as ClientStatus },
  ];

  // ── UI state ───────────────────────────────────────────────────────────────
  readonly notification = signal<{ type: 'success' | 'error'; message: string } | null>(null);
  readonly openMenuId   = signal<string | null>(null);
  readonly skeletons    = [1, 2, 3, 4, 5];

  // ── Modal ─────────────────────────────────────────────────────────────────
  readonly showModal     = signal(false);
  readonly editingClient = signal<ClientSummaryDto | null>(null);
  readonly saving        = signal(false);
  readonly modalError    = signal<string | null>(null);

  readonly clientForm = this.fb.group({
    name:          ['', Validators.required],
    contactPerson: [''],
    email:         ['', Validators.email],
    phone:         [''],
    address:       [''],
    notes:         [''],
  });

  // ── Derived ────────────────────────────────────────────────────────────────
  readonly pageRange = computed(() => {
    const total = this.svc.totalPages(), current = this.svc.page(), delta = 2;
    const range: number[] = [];
    for (let i = Math.max(1, current - delta); i <= Math.min(total, current + delta); i++) range.push(i);
    return range;
  });

  // ── Lifecycle ─────────────────────────────────────────────────────────────
  ngOnInit(): void {
    this.search$
      .pipe(debounceTime(350), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe(term => { this.searchTerm.set(term); this.reload(1); });

    this.reload();
  }

  // ── Search / filter ───────────────────────────────────────────────────────
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

  // ── Pagination ────────────────────────────────────────────────────────────
  goToPage(page: number): void {
    if (page < 1 || page > this.svc.totalPages()) return;
    this.reload(page);
  }

  // ── Modal: create ─────────────────────────────────────────────────────────
  openCreateModal(): void {
    this.editingClient.set(null);
    this.clientForm.reset();
    this.modalError.set(null);
    this.openMenuId.set(null);
    this.showModal.set(true);
  }

  // ── Modal: edit ───────────────────────────────────────────────────────────
  openEditModal(client: ClientSummaryDto): void {
    this.openMenuId.set(null);
    this.editingClient.set(client);
    this.modalError.set(null);

    // Load full detail to pre-fill address + notes
    this.svc.getClientById(client.id).subscribe({
      next: detail => {
        this.clientForm.setValue({
          name:          detail.name,
          contactPerson: detail.contactPerson ?? '',
          email:         detail.email ?? '',
          phone:         detail.phone ?? '',
          address:       detail.address ?? '',
          notes:         detail.notes ?? '',
        });
        this.showModal.set(true);
      },
      error: () => {
        this.notify('error', 'Failed to load client details.');
      },
    });
  }

  closeModal(): void {
    if (this.saving()) return;
    this.showModal.set(false);
    this.editingClient.set(null);
  }

  submitModal(): void {
    if (this.clientForm.invalid) { this.clientForm.markAllAsTouched(); return; }

    const v = this.clientForm.getRawValue();
    const payload = {
      name:          v.name!.trim(),
      contactPerson: v.contactPerson?.trim() || null,
      email:         v.email?.trim() || null,
      phone:         v.phone?.trim() || null,
      address:       v.address?.trim() || null,
      notes:         v.notes?.trim() || null,
    };

    this.saving.set(true);
    this.modalError.set(null);

    const editing = this.editingClient();

    if (editing) {
      // ── Update ──────────────────────────────────────────────────────────
      this.svc.updateClient(editing.id, payload).subscribe({
        next: () => {
          this.saving.set(false);
          this.showModal.set(false);
          this.editingClient.set(null);
          this.notify('success', `"${payload.name}" updated successfully.`);
          this.reload();
        },
        error: err => {
          this.saving.set(false);
          this.modalError.set(err?.error?.error ?? 'Failed to update client.');
        },
      });
    } else {
      // ── Create ──────────────────────────────────────────────────────────
      this.svc.createClient(payload).subscribe({
        next: () => {
          this.saving.set(false);
          this.showModal.set(false);
          this.notify('success', `"${payload.name}" created successfully.`);
          this.reload(1);
        },
        error: err => {
          this.saving.set(false);
          this.modalError.set(err?.error?.error ?? 'Failed to create client.');
        },
      });
    }
  }

  // ── Row actions ────────────────────────────────────────────────────────────
  deactivateClient(client: ClientSummaryDto): void {
    this.openMenuId.set(null);
    this.svc.deactivateClient(client.id).subscribe({
      next: () => {
        this.notify('success', `"${client.name}" deactivated.`);
        this.reload();
      },
      error: err => this.notify('error', err?.error?.error ?? 'Failed to deactivate client.'),
    });
  }

  reactivateClient(client: ClientSummaryDto): void {
    this.openMenuId.set(null);
    this.svc.reactivateClient(client.id).subscribe({
      next: () => {
        this.notify('success', `"${client.name}" reactivated.`);
        this.reload();
      },
      error: err => this.notify('error', err?.error?.error ?? 'Failed to reactivate client.'),
    });
  }

  // ── Menu toggle ────────────────────────────────────────────────────────────
  toggleMenu(id: string): void {
    this.openMenuId.update(cur => (cur === id ? null : id));
  }

  // ── Display helpers ────────────────────────────────────────────────────────
  statusBadgeClass(status: ClientStatus): string {
    return status === 0 ? 'badge--active' : 'badge--inactive';
  }

  formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString('en-GB', {
      day: 'numeric', month: 'short', year: 'numeric',
    });
  }

  // ── Reload ─────────────────────────────────────────────────────────────────
  reload(page?: number): void {
    this.svc.loadClients(
      page ?? this.svc.page(),
      this.svc.pageSize(),
      this.searchTerm() || undefined,
      this.selectedStatus ?? undefined,
    );
  }

  private notify(type: 'success' | 'error', message: string): void {
    this.notification.set({ type, message });
  }
}
