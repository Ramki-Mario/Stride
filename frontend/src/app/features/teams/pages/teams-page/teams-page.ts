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

import { TeamsService } from '../../services/teams.service';
import { TeamSummaryDto, TeamStatus } from '../../models/team.models';

@Component({
  selector: 'app-teams-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgClass, FormsModule, ReactiveFormsModule, SelectModule],
  template: `
    <div class="tm-page">

      <!-- ── Header ────────────────────────────────────────────────────── -->
      <div class="tm-header">
        <div>
          <h1 class="tm-title">Teams</h1>
          <p class="tm-subtitle">
            @if (!svc.isLoading()) {
              {{ svc.totalCount() }} team{{ svc.totalCount() === 1 ? '' : 's' }} total
            } @else { Loading… }
          </p>
        </div>
        <button class="stride-btn stride-btn-primary" (click)="openCreateModal()">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" aria-hidden="true">
            <line x1="12" y1="5" x2="12" y2="19" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
            <line x1="5"  y1="12" x2="19" y2="12" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
          </svg>
          New Team
        </button>
      </div>

      <!-- ── KPI cards ──────────────────────────────────────────────── -->
      <div class="tm-kpis">
        <div class="tm-kpi tm-kpi--total">
          <span class="tm-kpi-label">Total Teams</span>
          <span class="tm-kpi-value">{{ svc.totalCount() }}</span>
        </div>
        <div class="tm-kpi tm-kpi--active">
          <span class="tm-kpi-label">Active</span>
          <span class="tm-kpi-value">{{ svc.activeCount() }}</span>
        </div>
        <div class="tm-kpi tm-kpi--inactive">
          <span class="tm-kpi-label">Inactive</span>
          <span class="tm-kpi-value">{{ svc.inactiveCount() }}</span>
        </div>
      </div>

      <!-- ── Notification ───────────────────────────────────────────── -->
      @if (notification()) {
        <div class="tm-notification"
             [class.tm-notification--error]="notification()!.type === 'error'">
          <span>{{ notification()!.message }}</span>
          <button class="tm-notif-close" (click)="notification.set(null)" aria-label="Dismiss">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none">
              <line x1="18" y1="6"  x2="6"  y2="18" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
              <line x1="6"  y1="6"  x2="18" y2="18" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
            </svg>
          </button>
        </div>
      }

      <!-- ── Toolbar ────────────────────────────────────────────────── -->
      <div class="tm-toolbar">
        <div class="tm-search-wrap">
          <svg class="tm-search-icon" width="16" height="16" viewBox="0 0 24 24" fill="none" aria-hidden="true">
            <circle cx="11" cy="11" r="8" stroke="currentColor" stroke-width="1.75"/>
            <line x1="21" y1="21" x2="16.65" y2="16.65" stroke="currentColor" stroke-width="1.75" stroke-linecap="round"/>
          </svg>
          <input class="tm-search" type="search" placeholder="Search by team name…"
                 [value]="searchTerm()"
                 (input)="onSearchInput($event)"
                 aria-label="Search teams"/>
        </div>
        <p-select [options]="statusFilterOptions" [(ngModel)]="selectedStatus"
                  (onChange)="onFilterChange()"
                  optionLabel="label" optionValue="value"
                  styleClass="tm-filter-select" placeholder="All Statuses"/>
        @if (hasActiveFilters()) {
          <button class="tm-clear-btn" (click)="clearFilters()">Clear</button>
        }
      </div>

      <!-- ── Loading skeleton ───────────────────────────────────────── -->
      @if (svc.isLoading()) {
        <div class="tm-table-wrap">
          <table class="tm-table" aria-label="Loading teams">
            <thead><tr>
              <th>Name</th><th>Description</th><th>Parent Team</th><th>Status</th><th>Created</th><th></th>
            </tr></thead>
            <tbody>
              @for (i of skeletons; track i) {
                <tr class="sk-row">
                  <td><div class="sk-line sk-line--name"></div></td>
                  <td><div class="sk-line sk-line--desc"></div></td>
                  <td><div class="sk-line sk-line--parent"></div></td>
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
        <div class="tm-empty">
          <svg width="40" height="40" viewBox="0 0 24 24" fill="none" aria-hidden="true">
            <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="1.5"/>
            <line x1="12" y1="8"  x2="12" y2="12" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
            <line x1="12" y1="16" x2="12.01" y2="16" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
          </svg>
          <p>{{ svc.error() }}</p>
          <button class="stride-btn stride-btn-secondary" (click)="reload()">Retry</button>
        </div>

      <!-- ── Empty ─────────────────────────────────────────────────── -->
      } @else if (svc.isEmpty()) {
        <div class="tm-empty">
          <svg width="40" height="40" viewBox="0 0 24 24" fill="none" aria-hidden="true">
            <circle cx="12" cy="12" r="9" stroke="currentColor" stroke-width="1.5"/>
            <path d="M9 12l2 2 4-4" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/>
          </svg>
          <p>
            @if (hasActiveFilters()) { No teams match the current filters. }
            @else { No teams yet. Create your first team to get started. }
          </p>
          @if (!hasActiveFilters()) {
            <button class="stride-btn stride-btn-primary" (click)="openCreateModal()">New Team</button>
          }
        </div>

      <!-- ── Table ─────────────────────────────────────────────────── -->
      } @else {
        <div class="tm-table-wrap">
          <table class="tm-table" aria-label="Teams">
            <thead>
              <tr>
                <th>Name</th>
                <th>Description</th>
                <th>Parent Team</th>
                <th>Status</th>
                <th>Created</th>
                <th class="tm-th-actions"><span class="sr-only">Actions</span></th>
              </tr>
            </thead>
            <tbody>
              @for (team of svc.teams(); track team.id) {
                <tr [class.tm-row--inactive]="team.status === 1">
                  <td class="tm-td-name">{{ team.name }}</td>
                  <td class="tm-td-muted">{{ team.description || '—' }}</td>
                  <td class="tm-td-muted">
                    @if (team.parentTeamName) {
                      <span class="tm-parent-badge">{{ team.parentTeamName }}</span>
                    } @else {
                      <span class="tm-td-none">—</span>
                    }
                  </td>
                  <td>
                    <span class="tm-badge" [ngClass]="statusBadgeClass(team.status)">
                      {{ team.statusLabel }}
                    </span>
                  </td>
                  <td class="tm-td-date">{{ formatDate(team.createdAt) }}</td>
                  <td class="tm-td-actions">
                    <div class="tm-menu-wrap">
                      <button class="tm-menu-trigger"
                              [class.tm-menu-trigger--open]="openMenuId() === team.id"
                              (click)="toggleMenu(team.id)"
                              aria-label="Team actions">
                        <svg width="16" height="16" viewBox="0 0 24 24" fill="none">
                          <circle cx="12" cy="5"  r="1.2" fill="currentColor"/>
                          <circle cx="12" cy="12" r="1.2" fill="currentColor"/>
                          <circle cx="12" cy="19" r="1.2" fill="currentColor"/>
                        </svg>
                      </button>
                      @if (openMenuId() === team.id) {
                        <div class="tm-menu" role="menu">
                          <button class="tm-menu-item" role="menuitem" (click)="openEditModal(team)">
                            Edit
                          </button>
                          @if (team.status === 0) {
                            <button class="tm-menu-item tm-menu-item--danger" role="menuitem"
                                    (click)="deactivateTeam(team)">
                              Deactivate
                            </button>
                          }
                          @if (team.status === 1) {
                            <button class="tm-menu-item" role="menuitem"
                                    (click)="reactivateTeam(team)">
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
          <div class="tm-pagination">
            <span class="tm-page-info">
              Page {{ svc.page() }} of {{ svc.totalPages() }} · {{ svc.totalCount() }} total
            </span>
            <div class="tm-page-controls">
              <button class="tm-page-btn" [disabled]="!svc.hasPrevious()"
                      (click)="goToPage(svc.page() - 1)">
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none">
                  <polyline points="15 18 9 12 15 6" stroke="currentColor" stroke-width="2"
                            stroke-linecap="round" stroke-linejoin="round"/>
                </svg>
              </button>
              @for (p of pageRange(); track p) {
                <button class="tm-page-btn" [class.tm-page-btn--active]="p === svc.page()"
                        (click)="goToPage(p)">{{ p }}</button>
              }
              <button class="tm-page-btn" [disabled]="!svc.hasNext()"
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
    <!-- Create / Edit Team Modal                                        -->
    <!-- ═══════════════════════════════════════════════════════════════ -->
    @if (showModal()) {
      <div class="modal-backdrop" (click)="closeModal()" role="presentation">
        <div class="modal" role="dialog" aria-modal="true"
             [attr.aria-labelledby]="editingTeam() ? 'edit-modal-title' : 'create-modal-title'"
             (click)="$event.stopPropagation()">

          <div class="modal-header">
            @if (editingTeam()) {
              <h2 class="modal-title" id="edit-modal-title">Edit Team</h2>
            } @else {
              <h2 class="modal-title" id="create-modal-title">New Team</h2>
            }
            <button class="modal-close" (click)="closeModal()" aria-label="Close">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none">
                <line x1="18" y1="6"  x2="6"  y2="18" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
                <line x1="6"  y1="6"  x2="18" y2="18" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
              </svg>
            </button>
          </div>

          <form class="modal-body" [formGroup]="teamForm" (ngSubmit)="submitModal()">

            <!-- Name -->
            <div class="form-field">
              <label class="form-label" for="tm-name">
                Name <span class="form-required">*</span>
              </label>
              <input id="tm-name" class="form-input" formControlName="name"
                     placeholder="e.g. Engineering, Operations"
                     [class.form-input--error]="teamForm.controls.name.invalid && teamForm.controls.name.touched"/>
              @if (teamForm.controls.name.invalid && teamForm.controls.name.touched) {
                <p class="form-error">Name is required (max 100 characters).</p>
              }
            </div>

            <!-- Description -->
            <div class="form-field">
              <label class="form-label" for="tm-desc">Description</label>
              <textarea id="tm-desc" class="form-input form-textarea" formControlName="description"
                        rows="2" placeholder="Optional — describe the team's purpose"></textarea>
            </div>

            <!-- Parent Team -->
            <div class="form-field">
              <label class="form-label" for="tm-parent">Parent Team</label>
              <select id="tm-parent" class="form-input form-select" formControlName="parentTeamId">
                <option [value]="null">— No parent (top-level) —</option>
                @for (t of parentTeamOptions(); track t.id) {
                  <option [value]="t.id">{{ t.name }}</option>
                }
              </select>
              <p class="form-hint">Assign a parent to create a team hierarchy.</p>
            </div>

            @if (modalError()) {
              <p class="form-error">{{ modalError() }}</p>
            }

            <div class="modal-footer">
              <button type="button" class="stride-btn stride-btn-secondary"
                      (click)="closeModal()" [disabled]="saving()">Cancel</button>
              <button type="submit" class="stride-btn stride-btn-primary"
                      [disabled]="teamForm.invalid || saving()">
                @if (saving()) {
                  @if (editingTeam()) { Saving… } @else { Creating… }
                } @else {
                  @if (editingTeam()) { Save Changes } @else { Create Team }
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
    .tm-page {
      max-width: 72rem;
      margin: 0 auto;
      padding: 1.5rem 1rem 3rem;
    }

    /* ── Header ──────────────────────────────────────────────────── */
    .tm-header {
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      gap: 1rem;
      margin-bottom: 1.25rem;
    }
    .tm-title {
      font-size: 1.375rem;
      font-weight: 700;
      color: var(--stride-text-primary);
      margin: 0 0 0.25rem;
    }
    .tm-subtitle {
      font-size: 0.875rem;
      color: var(--stride-text-muted);
      margin: 0;
    }

    /* ── KPI cards ───────────────────────────────────────────────── */
    .tm-kpis {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(10rem, 1fr));
      gap: 0.875rem;
      margin-bottom: 1.25rem;
    }
    .tm-kpi {
      background: var(--stride-surface);
      border: 1px solid var(--stride-border-soft);
      border-radius: var(--stride-radius-lg);
      padding: 1rem 1.25rem;
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
    }
    .tm-kpi-label {
      font-size: 0.75rem;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      color: var(--stride-text-muted);
    }
    .tm-kpi-value {
      font-size: 1.5rem;
      font-weight: 700;
      color: var(--stride-text-primary);
    }
    .tm-kpi--total    { border-left: 3px solid var(--stride-primary); }
    .tm-kpi--active   { border-left: 3px solid #10B981; }
    .tm-kpi--inactive { border-left: 3px solid #6B7280; }

    /* ── Notification ─────────────────────────────────────────────── */
    .tm-notification {
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
    .tm-notification--error {
      background: color-mix(in srgb, #EF4444 10%, var(--stride-surface));
      border-color: color-mix(in srgb, #EF4444 30%, transparent);
      color: #DC2626;
    }
    .tm-notif-close {
      background: none; border: none; cursor: pointer; color: inherit;
      padding: 0.125rem; display: flex; align-items: center; opacity: 0.7;
    }
    .tm-notif-close:hover { opacity: 1; }

    /* ── Toolbar ──────────────────────────────────────────────────── */
    .tm-toolbar {
      display: flex;
      align-items: center;
      flex-wrap: wrap;
      gap: 0.625rem;
      margin-bottom: 1.25rem;
    }
    .tm-search-wrap {
      position: relative;
      flex: 1;
      min-width: 14rem;
    }
    .tm-search-icon {
      position: absolute;
      left: 0.75rem;
      top: 50%;
      transform: translateY(-50%);
      color: var(--stride-text-muted);
      pointer-events: none;
    }
    .tm-search {
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
    .tm-search:focus {
      outline: none;
      border-color: var(--stride-primary);
      box-shadow: 0 0 0 3px color-mix(in srgb, var(--stride-primary) 15%, transparent);
    }
    .tm-search::placeholder { color: var(--stride-text-muted); }
    :host ::ng-deep .tm-filter-select .p-select { min-width: 10rem; font-size: 0.875rem; }
    .tm-clear-btn {
      font-size: 0.8125rem; color: var(--stride-text-muted);
      background: none; border: none; cursor: pointer;
      padding: 0.25rem 0.5rem; font-family: inherit;
    }
    .tm-clear-btn:hover { color: var(--stride-primary); }

    /* ── Table ────────────────────────────────────────────────────── */
    .tm-table-wrap {
      border: 1px solid var(--stride-border-soft);
      border-radius: var(--stride-radius-lg);
      overflow: visible;   /* allow action menus to escape */
      background: var(--stride-surface);
    }
    .tm-table {
      width: 100%;
      border-collapse: collapse;
      font-size: 0.875rem;
    }
    .tm-table thead tr {
      background: var(--stride-surface-secondary);
      border-bottom: 1px solid var(--stride-border-soft);
    }
    .tm-table thead tr th:first-child { border-top-left-radius: var(--stride-radius-lg); }
    .tm-table thead tr th:last-child  { border-top-right-radius: var(--stride-radius-lg); }
    .tm-table tbody tr:last-child td:first-child { border-bottom-left-radius: var(--stride-radius-lg); }
    .tm-table tbody tr:last-child td:last-child  { border-bottom-right-radius: var(--stride-radius-lg); }
    .tm-table th {
      padding: 0.625rem 1rem;
      text-align: left;
      font-size: 0.75rem;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      color: var(--stride-text-muted);
      white-space: nowrap;
    }
    .tm-th-actions { width: 3rem; }
    .tm-table tbody tr {
      border-bottom: 1px solid var(--stride-border-soft);
      transition: background 120ms;
    }
    .tm-table tbody tr:last-child { border-bottom: none; }
    .tm-table tbody tr:hover { background: var(--stride-surface-hover); }
    .tm-row--inactive { opacity: 0.6; }
    .tm-table td { padding: 0.875rem 1rem; vertical-align: middle; color: var(--stride-text-primary); }

    .tm-td-name   { font-weight: 600; }
    .tm-td-muted  { color: var(--stride-text-secondary); font-size: 0.875rem; max-width: 16rem; }
    .tm-td-date   { color: var(--stride-text-muted); font-size: 0.8125rem; white-space: nowrap; }
    .tm-td-none   { color: var(--stride-text-muted); }

    /* Parent team pill */
    .tm-parent-badge {
      display: inline-flex;
      align-items: center;
      gap: 0.25rem;
      padding: 0.15rem 0.5rem;
      background: color-mix(in srgb, var(--stride-primary) 10%, transparent);
      color: var(--stride-primary);
      border-radius: 99px;
      font-size: 0.75rem;
      font-weight: 500;
    }

    /* ── Status badges ────────────────────────────────────────────── */
    .tm-badge {
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
    .tm-td-actions { width: 3rem; text-align: center; }
    .tm-menu-wrap  { position: relative; display: inline-block; }
    .tm-menu-trigger {
      width: 2rem; height: 2rem;
      border-radius: var(--stride-radius-md);
      display: flex; align-items: center; justify-content: center;
      border: 1px solid transparent;
      background: transparent;
      color: var(--stride-text-muted);
      cursor: pointer;
      transition: all 120ms;
    }
    .tm-menu-trigger:hover,
    .tm-menu-trigger--open {
      background: var(--stride-surface-secondary);
      border-color: var(--stride-border-soft);
      color: var(--stride-text-primary);
    }
    .tm-menu {
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
    .tm-menu-item {
      display: block; width: 100%; text-align: left;
      padding: 0.5rem 0.875rem;
      font-size: 0.875rem; font-family: inherit;
      background: none; border: none;
      color: var(--stride-text-primary);
      cursor: pointer; transition: background 100ms;
    }
    .tm-menu-item:hover { background: var(--stride-surface-hover); }
    .tm-menu-item--danger { color: #EF4444; }
    .tm-menu-item--danger:hover { background: color-mix(in srgb, #EF4444 8%, transparent); }

    /* ── Pagination ──────────────────────────────────────────────── */
    .tm-pagination {
      display: flex; align-items: center; justify-content: space-between;
      flex-wrap: wrap; gap: 0.75rem; margin-top: 1rem;
    }
    .tm-page-info { font-size: 0.8125rem; color: var(--stride-text-muted); }
    .tm-page-controls { display: flex; align-items: center; gap: 0.25rem; }
    .tm-page-btn {
      min-width: 2rem; height: 2rem; padding: 0 0.375rem;
      border-radius: var(--stride-radius-md);
      border: 1px solid var(--stride-border-soft);
      background: var(--stride-surface);
      color: var(--stride-text-secondary);
      font-size: 0.8125rem; font-family: inherit;
      cursor: pointer; display: flex; align-items: center; justify-content: center;
      transition: all 120ms;
    }
    .tm-page-btn:hover:not(:disabled) { border-color: var(--stride-primary); color: var(--stride-primary); }
    .tm-page-btn--active { background: var(--stride-primary); border-color: var(--stride-primary); color: #fff; font-weight: 600; }
    .tm-page-btn:disabled { opacity: 0.4; cursor: not-allowed; }

    /* ── Empty / error ────────────────────────────────────────────── */
    .tm-empty {
      display: flex; flex-direction: column; align-items: center;
      gap: 0.75rem; padding: 4rem 1rem;
      color: var(--stride-text-muted); font-size: 0.9375rem; text-align: center;
    }

    /* ── Skeleton ────────────────────────────────────────────────── */
    .sk-row { animation: pulse 1.5s ease-in-out infinite; }
    .sk-line { border-radius: 4px; background: var(--stride-border); height: 0.75rem; }
    .sk-line--name   { width: 8rem; }
    .sk-line--desc   { width: 12rem; }
    .sk-line--parent { width: 7rem; }
    .sk-line--badge  { width: 4rem; }
    .sk-line--date   { width: 5rem; }
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
    .form-hint { font-size: 0.75rem; color: var(--stride-text-muted); margin: 0; }
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
    .form-select { appearance: auto; cursor: pointer; }
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
export class TeamsPageComponent implements OnInit {
  readonly svc        = inject(TeamsService);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  // ── Filter state ──────────────────────────────────────────────────────────
  readonly searchTerm  = signal('');
  selectedStatus: TeamStatus | null = null;
  private readonly search$ = new Subject<string>();

  readonly hasActiveFilters = computed(() => !!this.searchTerm() || this.selectedStatus !== null);

  readonly statusFilterOptions = [
    { label: 'All Statuses', value: null        },
    { label: 'Active',       value: 0 as TeamStatus },
    { label: 'Inactive',     value: 1 as TeamStatus },
  ];

  // ── UI state ─────────────────────────────────────────────────────────────
  readonly notification = signal<{ type: 'success' | 'error'; message: string } | null>(null);
  readonly openMenuId   = signal<string | null>(null);
  readonly skeletons    = [1, 2, 3, 4, 5];

  // ── Modal ─────────────────────────────────────────────────────────────────
  readonly showModal   = signal(false);
  readonly editingTeam = signal<TeamSummaryDto | null>(null);
  readonly saving      = signal(false);
  readonly modalError  = signal<string | null>(null);

  /** All active teams available as parent options, populated when modal opens. */
  private readonly allActiveTeams = signal<TeamSummaryDto[]>([]);

  /**
   * Parent team options = all active teams minus the team being edited
   * (to prevent a team from being its own parent).
   */
  readonly parentTeamOptions = computed(() => {
    const editingId = this.editingTeam()?.id;
    return this.allActiveTeams().filter(t => t.id !== editingId);
  });

  readonly teamForm = this.fb.group({
    name:         ['', [Validators.required, Validators.maxLength(100)]],
    description:  [''],
    parentTeamId: [null as string | null],
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
    this.editingTeam.set(null);
    this.teamForm.reset({ name: '', description: '', parentTeamId: null });
    this.modalError.set(null);
    this.openMenuId.set(null);
    this.loadParentOptions();
    this.showModal.set(true);
  }

  // ── Modal: edit ───────────────────────────────────────────────────────────
  openEditModal(team: TeamSummaryDto): void {
    this.openMenuId.set(null);
    this.editingTeam.set(team);
    this.modalError.set(null);

    // Load full detail to get description and parentTeamId
    this.svc.getTeamById(team.id).subscribe({
      next: detail => {
        this.teamForm.setValue({
          name:         detail.name,
          description:  detail.description ?? '',
          parentTeamId: detail.parentTeamId ?? null,
        });
        this.loadParentOptions();
        this.showModal.set(true);
      },
      error: () => this.notify('error', 'Failed to load team details.'),
    });
  }

  closeModal(): void {
    if (this.saving()) return;
    this.showModal.set(false);
    this.editingTeam.set(null);
  }

  submitModal(): void {
    if (this.teamForm.invalid) { this.teamForm.markAllAsTouched(); return; }

    const v       = this.teamForm.getRawValue();
    const payload = {
      name:         v.name!.trim(),
      description:  v.description?.trim() || null,
      parentTeamId: v.parentTeamId || null,
    };

    this.saving.set(true);
    this.modalError.set(null);

    const editing = this.editingTeam();

    if (editing) {
      this.svc.updateTeam(editing.id, payload).subscribe({
        next: () => {
          this.saving.set(false);
          this.showModal.set(false);
          this.editingTeam.set(null);
          this.notify('success', `"${payload.name}" updated successfully.`);
          this.reload();
        },
        error: err => {
          this.saving.set(false);
          this.modalError.set(err?.error?.detail ?? 'Failed to update team.');
        },
      });
    } else {
      this.svc.createTeam(payload).subscribe({
        next: () => {
          this.saving.set(false);
          this.showModal.set(false);
          this.notify('success', `"${payload.name}" created successfully.`);
          this.reload(1);
        },
        error: err => {
          this.saving.set(false);
          this.modalError.set(err?.error?.detail ?? 'Failed to create team.');
        },
      });
    }
  }

  // ── Row actions ───────────────────────────────────────────────────────────
  deactivateTeam(team: TeamSummaryDto): void {
    this.openMenuId.set(null);
    this.svc.deactivateTeam(team.id).subscribe({
      next: () => {
        this.notify('success', `"${team.name}" deactivated.`);
        this.reload();
      },
      error: err => this.notify('error', err?.error?.detail ?? 'Failed to deactivate team.'),
    });
  }

  reactivateTeam(team: TeamSummaryDto): void {
    this.openMenuId.set(null);
    this.svc.reactivateTeam(team.id).subscribe({
      next: () => {
        this.notify('success', `"${team.name}" reactivated.`);
        this.reload();
      },
      error: err => this.notify('error', err?.error?.detail ?? 'Failed to reactivate team.'),
    });
  }

  // ── Menu toggle ───────────────────────────────────────────────────────────
  toggleMenu(id: string): void {
    this.openMenuId.update(cur => (cur === id ? null : id));
  }

  // ── Display helpers ───────────────────────────────────────────────────────
  statusBadgeClass(status: TeamStatus): string {
    return status === 0 ? 'badge--active' : 'badge--inactive';
  }

  formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString('en-GB', {
      day: 'numeric', month: 'short', year: 'numeric',
    });
  }

  // ── Private helpers ───────────────────────────────────────────────────────
  reload(page?: number): void {
    this.svc.loadTeams(
      page ?? this.svc.page(),
      this.svc.pageSize(),
      this.searchTerm() || undefined,
      this.selectedStatus ?? undefined,
    );
  }

  /**
   * Load all active teams for the parent picker.
   * We always fetch page-size 200 to show all teams in the dropdown without pagination.
   */
  private loadParentOptions(): void {
    this.svc.getTeams(undefined, 0).subscribe({
      next: teams => this.allActiveTeams.set(teams),
      error: () => this.allActiveTeams.set([]),
    });
  }

  private notify(type: 'success' | 'error', message: string): void {
    this.notification.set({ type, message });
  }
}
