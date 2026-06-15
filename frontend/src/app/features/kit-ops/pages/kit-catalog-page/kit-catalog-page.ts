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
import {
  FormBuilder,
  FormsModule,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';

import { KitOpsService } from '../../services/kit-ops.service';
import { KitItemSummaryDto } from '../../models/kit-ops.models';

@Component({
  selector: 'app-kit-catalog-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgClass, FormsModule, ReactiveFormsModule],
  template: `
    <div class="kc-page">

      <!-- ── Header ────────────────────────────────────────────────────── -->
      <div class="kc-header">
        <div>
          <h1 class="kc-title">Kit Catalog</h1>
          <p class="kc-subtitle">
            @if (!svc.isLoading()) {
              {{ svc.totalCount() }} item{{ svc.totalCount() === 1 ? '' : 's' }} total
            } @else { Loading… }
          </p>
        </div>
        <button class="stride-btn stride-btn-primary" (click)="openCreateModal()">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" aria-hidden="true">
            <line x1="12" y1="5" x2="12" y2="19" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
            <line x1="5" y1="12" x2="19" y2="12" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
          </svg>
          New Kit Item
        </button>
      </div>

      <!-- ── KPI cards ──────────────────────────────────────────────── -->
      <div class="kc-kpis">
        <div class="kc-kpi kc-kpi--total">
          <span class="kc-kpi-label">Total Items</span>
          <span class="kc-kpi-value">{{ svc.totalCount() }}</span>
        </div>
        <div class="kc-kpi kc-kpi--active">
          <span class="kc-kpi-label">Active</span>
          <span class="kc-kpi-value">{{ svc.activeCount() }}</span>
        </div>
        <div class="kc-kpi kc-kpi--inactive">
          <span class="kc-kpi-label">Inactive</span>
          <span class="kc-kpi-value">{{ svc.inactiveCount() }}</span>
        </div>
      </div>

      <!-- ── Notification ───────────────────────────────────────────── -->
      @if (notification()) {
        <div class="kc-notification"
             [class.kc-notification--error]="notification()!.type === 'error'">
          <span>{{ notification()!.message }}</span>
          <button class="kc-notif-close" (click)="notification.set(null)" aria-label="Dismiss">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none">
              <line x1="18" y1="6" x2="6" y2="18" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
              <line x1="6" y1="6" x2="18" y2="18" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
            </svg>
          </button>
        </div>
      }

      <!-- ── Toolbar ────────────────────────────────────────────────── -->
      <div class="kc-toolbar">
        <div class="kc-search-wrap">
          <svg class="kc-search-icon" width="16" height="16" viewBox="0 0 24 24" fill="none" aria-hidden="true">
            <circle cx="11" cy="11" r="8" stroke="currentColor" stroke-width="1.75"/>
            <line x1="21" y1="21" x2="16.65" y2="16.65" stroke="currentColor" stroke-width="1.75" stroke-linecap="round"/>
          </svg>
          <input class="kc-search" type="search" placeholder="Search by name…"
                 [value]="searchTerm()"
                 (input)="onSearchInput($event)"
                 aria-label="Search kit items"/>
        </div>
        <input class="kc-category-filter" type="text" placeholder="Filter by category…"
               [value]="categoryFilter()"
               (input)="onCategoryInput($event)"
               aria-label="Filter by category"/>
        <select class="kc-status-filter" [(ngModel)]="selectedStatus" (change)="onFilterChange()">
          <option [ngValue]="null">All Statuses</option>
          <option [ngValue]="true">Active</option>
          <option [ngValue]="false">Inactive</option>
        </select>
        @if (hasActiveFilters()) {
          <button class="kc-clear-btn" (click)="clearFilters()">Clear</button>
        }
      </div>

      <!-- ── Loading skeleton ───────────────────────────────────────── -->
      @if (svc.isLoading()) {
        <div class="kc-table-wrap">
          <table class="kc-table" aria-label="Loading kit items">
            <thead><tr>
              <th>Name</th><th>Category</th><th>Description</th>
              <th>Qty</th><th>Status</th><th>Created</th><th></th>
            </tr></thead>
            <tbody>
              @for (i of skeletons; track i) {
                <tr class="sk-row">
                  <td><div class="sk-line sk-line--name"></div></td>
                  <td><div class="sk-line sk-line--cat"></div></td>
                  <td><div class="sk-line sk-line--desc"></div></td>
                  <td><div class="sk-line sk-line--qty"></div></td>
                  <td><div class="sk-line sk-line--badge"></div></td>
                  <td><div class="sk-line sk-line--date"></div></td>
                  <td></td>
                </tr>
              }
            </tbody>
          </table>
        </div>

      <!-- ── Error ─────────────────────────────────────────────────── -->
      } @else if (svc.error()) {
        <div class="kc-empty">
          <svg width="40" height="40" viewBox="0 0 24 24" fill="none" aria-hidden="true">
            <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="1.5"/>
            <line x1="12" y1="8" x2="12" y2="12" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
            <line x1="12" y1="16" x2="12.01" y2="16" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
          </svg>
          <p>{{ svc.error() }}</p>
          <button class="stride-btn stride-btn-secondary" (click)="reload()">Retry</button>
        </div>

      <!-- ── Empty ──────────────────────────────────────────────────── -->
      } @else if (svc.isEmpty()) {
        <div class="kc-empty">
          <svg width="40" height="40" viewBox="0 0 24 24" fill="none" aria-hidden="true">
            <rect x="3" y="3" width="18" height="18" rx="2" stroke="currentColor" stroke-width="1.5"/>
            <path d="M9 12l2 2 4-4" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/>
          </svg>
          <p>
            @if (hasActiveFilters()) { No kit items match the current filters. }
            @else { No kit items yet. Add your first item to get started. }
          </p>
          @if (!hasActiveFilters()) {
            <button class="stride-btn stride-btn-primary" (click)="openCreateModal()">New Kit Item</button>
          }
        </div>

      <!-- ── Table ──────────────────────────────────────────────────── -->
      } @else {
        <div class="kc-table-wrap">
          <table class="kc-table" aria-label="Kit catalog">
            <thead>
              <tr>
                <th>Name</th>
                <th>Category</th>
                <th>Description</th>
                <th>Qty</th>
                <th>Status</th>
                <th>Created</th>
                <th class="kc-th-actions"><span class="sr-only">Actions</span></th>
              </tr>
            </thead>
            <tbody>
              @for (item of svc.items(); track item.id) {
                <tr [class.kc-row--inactive]="!item.isActive">
                  <td class="kc-td-name">{{ item.name }}</td>
                  <td>
                    <span class="kc-category-pill">{{ item.category }}</span>
                  </td>
                  <td class="kc-td-muted">{{ item.description || '—' }}</td>
                  <td class="kc-td-qty">{{ item.totalQuantity }}</td>
                  <td>
                    <span class="kc-badge" [ngClass]="item.isActive ? 'badge--active' : 'badge--inactive'">
                      {{ item.isActive ? 'Active' : 'Inactive' }}
                    </span>
                  </td>
                  <td class="kc-td-date">{{ formatDate(item.createdAt) }}</td>
                  <td class="kc-td-actions">
                    <div class="kc-menu-wrap">
                      <button class="kc-menu-trigger"
                              [class.kc-menu-trigger--open]="openMenuId() === item.id"
                              (click)="toggleMenu(item.id)"
                              aria-label="Kit item actions">
                        <svg width="16" height="16" viewBox="0 0 24 24" fill="none">
                          <circle cx="12" cy="5"  r="1.2" fill="currentColor"/>
                          <circle cx="12" cy="12" r="1.2" fill="currentColor"/>
                          <circle cx="12" cy="19" r="1.2" fill="currentColor"/>
                        </svg>
                      </button>
                      @if (openMenuId() === item.id) {
                        <div class="kc-menu" role="menu">
                          <button class="kc-menu-item" role="menuitem" (click)="openEditModal(item)">
                            Edit
                          </button>
                          @if (item.isActive) {
                            <button class="kc-menu-item kc-menu-item--danger" role="menuitem"
                                    (click)="deactivateItem(item)">
                              Deactivate
                            </button>
                          } @else {
                            <button class="kc-menu-item" role="menuitem"
                                    (click)="reactivateItem(item)">
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

        <!-- ── Pagination ──────────────────────────────────────────── -->
        @if (svc.totalPages() > 1) {
          <div class="kc-pagination">
            <span class="kc-page-info">
              Page {{ svc.page() }} of {{ svc.totalPages() }} · {{ svc.totalCount() }} total
            </span>
            <div class="kc-page-controls">
              <button class="kc-page-btn" [disabled]="!svc.hasPrevious()"
                      (click)="goToPage(svc.page() - 1)">
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none">
                  <polyline points="15 18 9 12 15 6" stroke="currentColor" stroke-width="2"
                            stroke-linecap="round" stroke-linejoin="round"/>
                </svg>
              </button>
              @for (p of pageRange(); track p) {
                <button class="kc-page-btn" [class.kc-page-btn--active]="p === svc.page()"
                        (click)="goToPage(p)">{{ p }}</button>
              }
              <button class="kc-page-btn" [disabled]="!svc.hasNext()"
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

    <!-- ═══════════════════════════════════════════════════════════════ -->
    <!-- Create / Edit Modal                                             -->
    <!-- ═══════════════════════════════════════════════════════════════ -->
    @if (showModal()) {
      <div class="modal-backdrop" (click)="closeModal()" role="presentation">
        <div class="modal" role="dialog" aria-modal="true"
             [attr.aria-labelledby]="editingItem() ? 'edit-modal-title' : 'create-modal-title'"
             (click)="$event.stopPropagation()">

          <div class="modal-header">
            @if (editingItem()) {
              <h2 class="modal-title" id="edit-modal-title">Edit Kit Item</h2>
            } @else {
              <h2 class="modal-title" id="create-modal-title">New Kit Item</h2>
            }
            <button class="modal-close" (click)="closeModal()" aria-label="Close">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none">
                <line x1="18" y1="6" x2="6" y2="18" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
                <line x1="6" y1="6" x2="18" y2="18" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
              </svg>
            </button>
          </div>

          <form class="modal-body" [formGroup]="itemForm" (ngSubmit)="submitModal()">

            <!-- Name -->
            <div class="form-field">
              <label class="form-label" for="kc-name">
                Name <span class="form-required">*</span>
              </label>
              <input id="kc-name" class="form-input" formControlName="name"
                     placeholder="e.g. Safety Helmet, High-Vis Vest"
                     [class.form-input--error]="itemForm.controls.name.invalid && itemForm.controls.name.touched"/>
              @if (itemForm.controls.name.invalid && itemForm.controls.name.touched) {
                <p class="form-error">Name is required (max 200 characters).</p>
              }
            </div>

            <!-- Category -->
            <div class="form-field">
              <label class="form-label" for="kc-category">
                Category <span class="form-required">*</span>
              </label>
              <input id="kc-category" class="form-input" formControlName="category"
                     placeholder="e.g. PPE, Tools, Electronics"
                     [class.form-input--error]="itemForm.controls.category.invalid && itemForm.controls.category.touched"/>
              @if (itemForm.controls.category.invalid && itemForm.controls.category.touched) {
                <p class="form-error">Category is required (max 100 characters).</p>
              }
            </div>

            <!-- Total Quantity -->
            <div class="form-field">
              <label class="form-label" for="kc-qty">
                Total Quantity <span class="form-required">*</span>
              </label>
              <input id="kc-qty" class="form-input" type="number" min="1" formControlName="totalQuantity"
                     placeholder="e.g. 10"
                     [class.form-input--error]="itemForm.controls.totalQuantity.invalid && itemForm.controls.totalQuantity.touched"/>
              @if (itemForm.controls.totalQuantity.invalid && itemForm.controls.totalQuantity.touched) {
                <p class="form-error">Total quantity must be at least 1.</p>
              }
            </div>

            <!-- Description -->
            <div class="form-field">
              <label class="form-label" for="kc-desc">Description</label>
              <textarea id="kc-desc" class="form-input form-textarea" formControlName="description"
                        rows="2" placeholder="Optional — describe the item"></textarea>
            </div>

            @if (modalError()) {
              <p class="form-error">{{ modalError() }}</p>
            }

            <div class="modal-footer">
              <button type="button" class="stride-btn stride-btn-secondary"
                      (click)="closeModal()" [disabled]="saving()">Cancel</button>
              <button type="submit" class="stride-btn stride-btn-primary"
                      [disabled]="itemForm.invalid || saving()">
                @if (saving()) {
                  @if (editingItem()) { Saving… } @else { Creating… }
                } @else {
                  @if (editingItem()) { Save Changes } @else { Create Item }
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
    .kc-page {
      max-width: 72rem;
      margin: 0 auto;
      padding: 1.5rem 1rem 3rem;
    }

    /* ── Header ──────────────────────────────────────────────────── */
    .kc-header {
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      gap: 1rem;
      margin-bottom: 1.25rem;
    }
    .kc-title {
      font-size: 1.375rem;
      font-weight: 700;
      color: var(--stride-text-primary);
      margin: 0 0 0.25rem;
    }
    .kc-subtitle {
      font-size: 0.875rem;
      color: var(--stride-text-muted);
      margin: 0;
    }

    /* ── KPI cards ───────────────────────────────────────────────── */
    .kc-kpis {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(10rem, 1fr));
      gap: 0.875rem;
      margin-bottom: 1.25rem;
    }
    .kc-kpi {
      background: var(--stride-surface);
      border: 1px solid var(--stride-border-soft);
      border-radius: var(--stride-radius-lg);
      padding: 1rem 1.25rem;
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
    }
    .kc-kpi-label {
      font-size: 0.75rem;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      color: var(--stride-text-muted);
    }
    .kc-kpi-value {
      font-size: 1.5rem;
      font-weight: 700;
      color: var(--stride-text-primary);
    }
    .kc-kpi--total    { border-left: 3px solid var(--stride-primary); }
    .kc-kpi--active   { border-left: 3px solid #10B981; }
    .kc-kpi--inactive { border-left: 3px solid #6B7280; }

    /* ── Notification ─────────────────────────────────────────────── */
    .kc-notification {
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
    .kc-notification--error {
      background: color-mix(in srgb, #EF4444 10%, var(--stride-surface));
      border-color: color-mix(in srgb, #EF4444 30%, transparent);
      color: #DC2626;
    }
    .kc-notif-close {
      background: none; border: none; cursor: pointer; color: inherit;
      padding: 0.125rem; display: flex; align-items: center; opacity: 0.7;
    }
    .kc-notif-close:hover { opacity: 1; }

    /* ── Toolbar ──────────────────────────────────────────────────── */
    .kc-toolbar {
      display: flex;
      align-items: center;
      flex-wrap: wrap;
      gap: 0.625rem;
      margin-bottom: 1.25rem;
    }
    .kc-search-wrap {
      position: relative;
      flex: 1;
      min-width: 14rem;
    }
    .kc-search-icon {
      position: absolute;
      left: 0.75rem;
      top: 50%;
      transform: translateY(-50%);
      color: var(--stride-text-muted);
      pointer-events: none;
    }
    .kc-search {
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
    .kc-search:focus {
      outline: none;
      border-color: var(--stride-primary);
      box-shadow: 0 0 0 3px color-mix(in srgb, var(--stride-primary) 15%, transparent);
    }
    .kc-search::placeholder { color: var(--stride-text-muted); }
    .kc-category-filter,
    .kc-status-filter {
      padding: 0.5rem 0.75rem;
      border: 1px solid var(--stride-border-soft);
      border-radius: var(--stride-radius-md);
      background: var(--stride-surface);
      color: var(--stride-text-primary);
      font-size: 0.875rem;
      font-family: inherit;
      min-width: 10rem;
    }
    .kc-category-filter:focus,
    .kc-status-filter:focus {
      outline: none;
      border-color: var(--stride-primary);
      box-shadow: 0 0 0 3px color-mix(in srgb, var(--stride-primary) 15%, transparent);
    }
    .kc-clear-btn {
      font-size: 0.8125rem; color: var(--stride-text-muted);
      background: none; border: none; cursor: pointer;
      padding: 0.25rem 0.5rem; font-family: inherit;
    }
    .kc-clear-btn:hover { color: var(--stride-primary); }

    /* ── Table ────────────────────────────────────────────────────── */
    .kc-table-wrap {
      border: 1px solid var(--stride-border-soft);
      border-radius: var(--stride-radius-lg);
      overflow: visible;
      background: var(--stride-surface);
    }
    .kc-table {
      width: 100%;
      border-collapse: collapse;
      font-size: 0.875rem;
    }
    .kc-table thead tr {
      background: var(--stride-surface-secondary);
      border-bottom: 1px solid var(--stride-border-soft);
    }
    .kc-table thead tr th:first-child { border-top-left-radius: var(--stride-radius-lg); }
    .kc-table thead tr th:last-child  { border-top-right-radius: var(--stride-radius-lg); }
    .kc-table tbody tr:last-child td:first-child { border-bottom-left-radius: var(--stride-radius-lg); }
    .kc-table tbody tr:last-child td:last-child  { border-bottom-right-radius: var(--stride-radius-lg); }
    .kc-table th {
      padding: 0.625rem 1rem;
      text-align: left;
      font-size: 0.75rem;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      color: var(--stride-text-muted);
      white-space: nowrap;
    }
    .kc-th-actions { width: 3rem; }
    .kc-table tbody tr {
      border-bottom: 1px solid var(--stride-border-soft);
      transition: background 120ms;
    }
    .kc-table tbody tr:last-child { border-bottom: none; }
    .kc-table tbody tr:hover { background: var(--stride-surface-hover); }
    .kc-row--inactive { opacity: 0.6; }
    .kc-table td { padding: 0.875rem 1rem; vertical-align: middle; color: var(--stride-text-primary); }

    .kc-td-name  { font-weight: 600; }
    .kc-td-muted { color: var(--stride-text-secondary); font-size: 0.875rem; max-width: 18rem; }
    .kc-td-qty   { font-weight: 600; color: var(--stride-primary); text-align: center; }
    .kc-td-date  { color: var(--stride-text-muted); font-size: 0.8125rem; white-space: nowrap; }

    /* Category pill */
    .kc-category-pill {
      display: inline-flex;
      align-items: center;
      padding: 0.15rem 0.55rem;
      background: color-mix(in srgb, var(--stride-primary) 10%, transparent);
      color: var(--stride-primary);
      border-radius: 99px;
      font-size: 0.75rem;
      font-weight: 500;
    }

    /* ── Status badges ────────────────────────────────────────────── */
    .kc-badge {
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

    /* ── Action menu ─────────────────────────────────────────────── */
    .kc-td-actions { width: 3rem; text-align: center; }
    .kc-menu-wrap  { position: relative; display: inline-block; }
    .kc-menu-trigger {
      width: 2rem; height: 2rem;
      border-radius: var(--stride-radius-md);
      display: flex; align-items: center; justify-content: center;
      border: 1px solid transparent;
      background: transparent;
      color: var(--stride-text-muted);
      cursor: pointer;
      transition: all 120ms;
    }
    .kc-menu-trigger:hover,
    .kc-menu-trigger--open {
      background: var(--stride-surface-secondary);
      border-color: var(--stride-border-soft);
      color: var(--stride-text-primary);
    }
    .kc-menu {
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
    .kc-menu-item {
      display: block; width: 100%; text-align: left;
      padding: 0.5rem 0.875rem;
      font-size: 0.875rem; font-family: inherit;
      background: none; border: none;
      color: var(--stride-text-primary);
      cursor: pointer; transition: background 100ms;
    }
    .kc-menu-item:hover { background: var(--stride-surface-hover); }
    .kc-menu-item--danger { color: #EF4444; }
    .kc-menu-item--danger:hover { background: color-mix(in srgb, #EF4444 8%, transparent); }

    /* ── Pagination ──────────────────────────────────────────────── */
    .kc-pagination {
      display: flex; align-items: center; justify-content: space-between;
      flex-wrap: wrap; gap: 0.75rem; margin-top: 1rem;
    }
    .kc-page-info { font-size: 0.8125rem; color: var(--stride-text-muted); }
    .kc-page-controls { display: flex; align-items: center; gap: 0.25rem; }
    .kc-page-btn {
      min-width: 2rem; height: 2rem; padding: 0 0.375rem;
      border-radius: var(--stride-radius-md);
      border: 1px solid var(--stride-border-soft);
      background: var(--stride-surface);
      color: var(--stride-text-secondary);
      font-size: 0.8125rem; font-family: inherit;
      cursor: pointer; display: flex; align-items: center; justify-content: center;
      transition: all 120ms;
    }
    .kc-page-btn:hover:not(:disabled) { border-color: var(--stride-primary); color: var(--stride-primary); }
    .kc-page-btn--active { background: var(--stride-primary); border-color: var(--stride-primary); color: #fff; font-weight: 600; }
    .kc-page-btn:disabled { opacity: 0.4; cursor: not-allowed; }

    /* ── Empty / error ────────────────────────────────────────────── */
    .kc-empty {
      display: flex; flex-direction: column; align-items: center;
      gap: 0.75rem; padding: 4rem 1rem;
      color: var(--stride-text-muted); font-size: 0.9375rem; text-align: center;
    }

    /* ── Skeleton ────────────────────────────────────────────────── */
    .sk-row { animation: pulse 1.5s ease-in-out infinite; }
    .sk-line { border-radius: 4px; background: var(--stride-border); height: 0.75rem; }
    .sk-line--name  { width: 8rem; }
    .sk-line--cat   { width: 5rem; }
    .sk-line--desc  { width: 12rem; }
    .sk-line--qty   { width: 2.5rem; }
    .sk-line--badge { width: 4rem; }
    .sk-line--date  { width: 5rem; }
    @keyframes pulse { 0%, 100% { opacity: 1; } 50% { opacity: 0.5; } }

    /* ── Modal ───────────────────────────────────────────────────── */
    .modal-backdrop {
      position: fixed; inset: 0; background: rgba(0,0,0,0.45);
      display: flex; align-items: center; justify-content: center;
      z-index: 100; padding: 1rem;
    }
    .modal {
      background: var(--stride-surface);
      border-radius: var(--stride-radius-xl);
      box-shadow: var(--stride-shadow-xl);
      width: 100%; max-width: 34rem; overflow: hidden;
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

    /* ── Form ────────────────────────────────────────────────────── */
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
    .form-textarea { resize: vertical; min-height: 4rem; }
    .form-error { font-size: 0.8125rem; color: #EF4444; margin: 0; }

    /* ── Click-away ──────────────────────────────────────────────── */
    .click-away { position: fixed; inset: 0; z-index: 40; }

    /* ── A11y ────────────────────────────────────────────────────── */
    .sr-only {
      position: absolute; width: 1px; height: 1px;
      padding: 0; margin: -1px; overflow: hidden;
      clip: rect(0,0,0,0); white-space: nowrap; border: 0;
    }
  `],
})
export class KitCatalogPageComponent implements OnInit {
  readonly svc        = inject(KitOpsService);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  // ── Filter state ──────────────────────────────────────────────────────────
  readonly searchTerm     = signal('');
  readonly categoryFilter = signal('');
  selectedStatus: boolean | null = null;

  private readonly search$   = new Subject<string>();
  private readonly category$ = new Subject<string>();

  readonly hasActiveFilters = computed(
    () => !!this.searchTerm() || !!this.categoryFilter() || this.selectedStatus !== null
  );

  // ── UI state ─────────────────────────────────────────────────────────────
  readonly notification = signal<{ type: 'success' | 'error'; message: string } | null>(null);
  readonly openMenuId   = signal<string | null>(null);
  readonly skeletons    = [1, 2, 3, 4, 5];

  // ── Modal ─────────────────────────────────────────────────────────────────
  readonly showModal   = signal(false);
  readonly editingItem = signal<KitItemSummaryDto | null>(null);
  readonly saving      = signal(false);
  readonly modalError  = signal<string | null>(null);

  readonly itemForm = this.fb.group({
    name:          ['', [Validators.required, Validators.maxLength(200)]],
    category:      ['', [Validators.required, Validators.maxLength(100)]],
    totalQuantity: [1,  [Validators.required, Validators.min(1)]],
    description:   [''],
  });

  // ── Derived ───────────────────────────────────────────────────────────────
  readonly pageRange = computed(() => {
    const total = this.svc.totalPages(), current = this.svc.page(), delta = 2;
    const range: number[] = [];
    for (let i = Math.max(1, current - delta); i <= Math.min(total, current + delta); i++) {
      range.push(i);
    }
    return range;
  });

  // ── Lifecycle ─────────────────────────────────────────────────────────────
  ngOnInit(): void {
    this.search$
      .pipe(debounceTime(350), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe(term => { this.searchTerm.set(term); this.reload(1); });

    this.category$
      .pipe(debounceTime(350), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe(cat => { this.categoryFilter.set(cat); this.reload(1); });

    this.reload();
  }

  // ── Search / filter ───────────────────────────────────────────────────────
  onSearchInput(event: Event): void {
    this.search$.next((event.target as HTMLInputElement).value);
  }

  onCategoryInput(event: Event): void {
    this.category$.next((event.target as HTMLInputElement).value);
  }

  onFilterChange(): void { this.reload(1); }

  clearFilters(): void {
    this.searchTerm.set('');
    this.categoryFilter.set('');
    this.selectedStatus = null;
    this.search$.next('');
    this.category$.next('');
    this.reload(1);
  }

  // ── Pagination ────────────────────────────────────────────────────────────
  goToPage(page: number): void {
    if (page < 1 || page > this.svc.totalPages()) return;
    this.reload(page);
  }

  // ── Modal: create ─────────────────────────────────────────────────────────
  openCreateModal(): void {
    this.editingItem.set(null);
    this.itemForm.reset({ name: '', category: '', totalQuantity: 1, description: '' });
    this.modalError.set(null);
    this.openMenuId.set(null);
    this.showModal.set(true);
  }

  // ── Modal: edit ───────────────────────────────────────────────────────────
  openEditModal(item: KitItemSummaryDto): void {
    this.openMenuId.set(null);
    this.editingItem.set(item);
    this.modalError.set(null);

    this.svc.getKitItemById(item.id).subscribe({
      next: detail => {
        this.itemForm.setValue({
          name:          detail.name,
          category:      detail.category,
          totalQuantity: detail.totalQuantity,
          description:   detail.description ?? '',
        });
        this.showModal.set(true);
      },
      error: () => this.notify('error', 'Failed to load item details.'),
    });
  }

  closeModal(): void {
    if (this.saving()) return;
    this.showModal.set(false);
    this.editingItem.set(null);
  }

  submitModal(): void {
    if (this.itemForm.invalid) { this.itemForm.markAllAsTouched(); return; }

    const v = this.itemForm.getRawValue();
    const payload = {
      name:          v.name!.trim(),
      category:      v.category!.trim(),
      totalQuantity: v.totalQuantity!,
      description:   v.description?.trim() || null,
    };

    this.saving.set(true);
    this.modalError.set(null);

    const editing = this.editingItem();

    if (editing) {
      this.svc.updateKitItem(editing.id, payload).subscribe({
        next: () => {
          this.saving.set(false);
          this.showModal.set(false);
          this.editingItem.set(null);
          this.notify('success', `"${payload.name}" updated successfully.`);
          this.reload();
        },
        error: err => {
          this.saving.set(false);
          this.modalError.set(err?.error?.error ?? err?.error?.detail ?? 'Failed to update item.');
        },
      });
    } else {
      this.svc.createKitItem(payload).subscribe({
        next: () => {
          this.saving.set(false);
          this.showModal.set(false);
          this.notify('success', `"${payload.name}" created successfully.`);
          this.reload(1);
        },
        error: err => {
          this.saving.set(false);
          this.modalError.set(err?.error?.error ?? err?.error?.detail ?? 'Failed to create item.');
        },
      });
    }
  }

  // ── Row actions ───────────────────────────────────────────────────────────
  deactivateItem(item: KitItemSummaryDto): void {
    this.openMenuId.set(null);
    this.svc.deactivateKitItem(item.id).subscribe({
      next: () => {
        this.notify('success', `"${item.name}" deactivated.`);
        this.reload();
      },
      error: err => this.notify('error', err?.error?.error ?? 'Failed to deactivate item.'),
    });
  }

  reactivateItem(item: KitItemSummaryDto): void {
    this.openMenuId.set(null);
    this.svc.reactivateKitItem(item.id).subscribe({
      next: () => {
        this.notify('success', `"${item.name}" reactivated.`);
        this.reload();
      },
      error: err => this.notify('error', err?.error?.error ?? 'Failed to reactivate item.'),
    });
  }

  // ── Menu toggle ───────────────────────────────────────────────────────────
  toggleMenu(id: string): void {
    this.openMenuId.update(cur => (cur === id ? null : id));
  }

  // ── Display helpers ───────────────────────────────────────────────────────
  formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString('en-GB', {
      day: 'numeric', month: 'short', year: 'numeric',
    });
  }

  // ── Private helpers ───────────────────────────────────────────────────────
  reload(page?: number): void {
    this.svc.loadKitItems(
      page ?? this.svc.page(),
      this.svc.pageSize(),
      this.searchTerm() || undefined,
      this.categoryFilter() || undefined,
      this.selectedStatus ?? undefined,
    );
  }

  private notify(type: 'success' | 'error', message: string): void {
    this.notification.set({ type, message });
  }
}
