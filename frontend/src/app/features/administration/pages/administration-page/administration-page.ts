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

import { AdminService } from '../../services/admin.service';
import { RoleService } from '../../services/role.service';
import { AdminUserDto, UserStatus } from '../../models/admin-user.models';

@Component({
  selector: 'app-administration-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgClass, FormsModule, ReactiveFormsModule, SelectModule],
  template: `
    <div class="admin-page">

      <!-- ── Page header ──────────────────────────────────────────────── -->
      <div class="admin-header">
        <div>
          <h1 class="admin-title">User Management</h1>
          <p class="admin-subtitle">
            @if (!svc.isLoading()) {
              {{ svc.totalCount() }} user{{ svc.totalCount() === 1 ? '' : 's' }} in this tenant
            } @else {
              Loading users&hellip;
            }
          </p>
        </div>
        <button class="stride-btn stride-btn-primary" (click)="openInviteModal()">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" aria-hidden="true">
            <line x1="12" y1="5" x2="12" y2="19" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
            <line x1="5" y1="12" x2="19" y2="12" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
          </svg>
          Invite User
        </button>
      </div>

      <!-- ── Inline notification ───────────────────────────────────────── -->
      @if (notification()) {
        <div class="admin-notification" [class.admin-notification--error]="notification()!.type === 'error'">
          <span>{{ notification()!.message }}</span>
          <button class="admin-notification-close" (click)="notification.set(null)" aria-label="Dismiss">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none">
              <line x1="18" y1="6" x2="6" y2="18" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
              <line x1="6" y1="6" x2="18" y2="18" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
            </svg>
          </button>
        </div>
      }

      <!-- ── Toolbar: search + filters ────────────────────────────────── -->
      <div class="admin-toolbar">
        <div class="admin-search-wrap">
          <svg class="admin-search-icon" width="16" height="16" viewBox="0 0 24 24" fill="none" aria-hidden="true">
            <circle cx="11" cy="11" r="8" stroke="currentColor" stroke-width="1.75"/>
            <line x1="21" y1="21" x2="16.65" y2="16.65" stroke="currentColor" stroke-width="1.75" stroke-linecap="round"/>
          </svg>
          <input
            class="admin-search"
            type="search"
            placeholder="Search by name or email…"
            [value]="searchTerm()"
            (input)="onSearchInput($event)"
            aria-label="Search users"
          />
        </div>

        <p-select
          [options]="roleFilterOptions()"
          [(ngModel)]="selectedRole"
          (onChange)="onFilterChange()"
          optionLabel="label"
          optionValue="value"
          class="admin-filter-select"
          placeholder="All Roles"
        />

        <p-select
          [options]="statusOptions"
          [(ngModel)]="selectedStatus"
          (onChange)="onFilterChange()"
          optionLabel="label"
          optionValue="value"
          styleClass="admin-filter-select"
          placeholder="All Statuses"
        />

        @if (hasActiveFilters()) {
          <button class="admin-clear-btn" (click)="clearFilters()">Clear filters</button>
        }
      </div>

      <!-- ── Loading skeleton ──────────────────────────────────────────── -->
      @if (svc.isLoading()) {
        <div class="admin-table-wrap">
          <table class="admin-table" aria-label="Loading users">
            <thead>
              <tr>
                <th>User</th><th>Role</th><th>Status</th><th>Joined</th><th></th>
              </tr>
            </thead>
            <tbody>
              @for (i of skeletons; track i) {
                <tr class="sk-row">
                  <td>
                    <div class="sk-user">
                      <div class="sk-avatar"></div>
                      <div class="sk-text-block">
                        <div class="sk-line sk-line--name"></div>
                        <div class="sk-line sk-line--email"></div>
                      </div>
                    </div>
                  </td>
                  <td><div class="sk-line sk-line--badge"></div></td>
                  <td><div class="sk-line sk-line--badge"></div></td>
                  <td><div class="sk-line sk-line--date"></div></td>
                  <td></td>
                </tr>
              }
            </tbody>
          </table>
        </div>

      <!-- ── Error state ────────────────────────────────────────────────── -->
      } @else if (svc.error()) {
        <div class="admin-empty">
          <svg width="40" height="40" viewBox="0 0 24 24" fill="none" aria-hidden="true">
            <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="1.5"/>
            <line x1="12" y1="8" x2="12" y2="12" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
            <line x1="12" y1="16" x2="12.01" y2="16" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
          </svg>
          <p>{{ svc.error() }}</p>
          <button class="stride-btn stride-btn-secondary" (click)="reload()">Retry</button>
        </div>

      <!-- ── Empty state ────────────────────────────────────────────────── -->
      } @else if (svc.isEmpty()) {
        <div class="admin-empty">
          <svg width="40" height="40" viewBox="0 0 24 24" fill="none" aria-hidden="true">
            <path d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2" stroke="currentColor" stroke-width="1.5"
                  stroke-linecap="round" stroke-linejoin="round"/>
            <circle cx="9" cy="7" r="4" stroke="currentColor" stroke-width="1.5"/>
            <path d="M23 21v-2a4 4 0 00-3-3.87M16 3.13a4 4 0 010 7.75"
                  stroke="currentColor" stroke-width="1.5" stroke-linecap="round"/>
          </svg>
          <p>
            @if (hasActiveFilters()) { No users match the current filters. }
            @else { No users in this tenant yet. Invite someone to get started. }
          </p>
          @if (!hasActiveFilters()) {
            <button class="stride-btn stride-btn-primary" (click)="openInviteModal()">Invite User</button>
          }
        </div>

      <!-- ── User table ─────────────────────────────────────────────────── -->
      } @else {
        <div class="admin-table-wrap">
          <table class="admin-table" aria-label="Tenant users">
            <thead>
              <tr>
                <th>User</th>
                <th>Role</th>
                <th>Status</th>
                <th>Joined</th>
                <th class="admin-th-actions"><span class="sr-only">Actions</span></th>
              </tr>
            </thead>
            <tbody>
              @for (user of svc.users(); track user.id) {
                <tr [class.admin-row--inactive]="!user.isActive && !user.isPending">

                  <!-- User cell: avatar initial + name + email -->
                  <td>
                    <div class="admin-user-cell">
                      <div class="admin-avatar" [style]="avatarColor(user.role)">
                        {{ initials(user.displayName) }}
                      </div>
                      <div>
                        <div class="admin-user-name">{{ user.displayName }}</div>
                        <div class="admin-user-email">{{ user.email }}</div>
                      </div>
                    </div>
                  </td>

                  <!-- Role badge -->
                  <td>
                    <span class="admin-badge" [style]="roleBadgeStyle(user.role)">
                      {{ user.role }}
                    </span>
                  </td>

                  <!-- Status badge -->
                  <td>
                    <span class="admin-badge" [ngClass]="statusBadgeClass(user)">
                      {{ statusLabel(user) }}
                    </span>
                  </td>

                  <!-- Joined date -->
                  <td class="admin-date">{{ formatDate(user.createdAt) }}</td>

                  <!-- Row actions -->
                  <td class="admin-td-actions">
                    <div class="admin-action-menu-wrap">
                      <button
                        class="admin-action-trigger"
                        [class.admin-action-trigger--open]="openMenuId() === user.id"
                        (click)="toggleMenu(user.id)"
                        [attr.aria-expanded]="openMenuId() === user.id"
                        aria-label="User actions"
                      >
                        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                          <circle cx="12" cy="5"  r="1.2" fill="currentColor"/>
                          <circle cx="12" cy="12" r="1.2" fill="currentColor"/>
                          <circle cx="12" cy="19" r="1.2" fill="currentColor"/>
                        </svg>
                      </button>

                      @if (openMenuId() === user.id) {
                        <div class="admin-action-menu" role="menu">
                          <button class="admin-action-item" role="menuitem"
                                  (click)="openRoleModal(user)">
                            Change Role
                          </button>
                          @if (user.isActive || user.isPending) {
                            <button class="admin-action-item admin-action-item--danger" role="menuitem"
                                    (click)="deactivate(user)">
                              Deactivate
                            </button>
                          } @else {
                            <button class="admin-action-item" role="menuitem"
                                    (click)="reactivate(user)">
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
          <div class="admin-pagination">
            <span class="admin-pagination-info">
              Page {{ svc.page() }} of {{ svc.totalPages() }}
              &nbsp;·&nbsp;
              {{ svc.totalCount() }} total
            </span>
            <div class="admin-pagination-controls">
              <button class="admin-page-btn" [disabled]="!svc.hasPrevious()"
                      (click)="goToPage(svc.page() - 1)">
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none">
                  <polyline points="15 18 9 12 15 6" stroke="currentColor"
                            stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                </svg>
              </button>

              @for (p of pageRange(); track p) {
                <button class="admin-page-btn"
                        [class.admin-page-btn--active]="p === svc.page()"
                        (click)="goToPage(p)">
                  {{ p }}
                </button>
              }

              <button class="admin-page-btn" [disabled]="!svc.hasNext()"
                      (click)="goToPage(svc.page() + 1)">
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none">
                  <polyline points="9 18 15 12 9 6" stroke="currentColor"
                            stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                </svg>
              </button>
            </div>
          </div>
        }
      }

    </div>

    <!-- ═══════════════════════════════════════════════════════════════════ -->
    <!-- Invite User Modal                                                   -->
    <!-- ═══════════════════════════════════════════════════════════════════ -->
    @if (showInviteModal()) {
      <div class="modal-backdrop" (click)="closeInviteModal()" role="presentation">
        <div class="modal" role="dialog" aria-modal="true" aria-labelledby="invite-modal-title"
             (click)="$event.stopPropagation()">
          <div class="modal-header">
            <h2 class="modal-title" id="invite-modal-title">Invite User</h2>
            <button class="modal-close" (click)="closeInviteModal()" aria-label="Close">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none">
                <line x1="18" y1="6" x2="6"  y2="18" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
                <line x1="6"  y1="6" x2="18" y2="18" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
              </svg>
            </button>
          </div>

          <form class="modal-body" [formGroup]="inviteForm" (ngSubmit)="submitInvite()">

            <div class="form-field">
              <label class="form-label" for="invite-email">Email address <span class="form-required">*</span></label>
              <input id="invite-email" class="form-input" type="email"
                     formControlName="email" placeholder="user@example.com"
                     [class.form-input--error]="inviteForm.controls.email.invalid && inviteForm.controls.email.touched"/>
              @if (inviteForm.controls.email.invalid && inviteForm.controls.email.touched) {
                <p class="form-error">A valid email address is required.</p>
              }
            </div>

            <div class="form-field">
              <label class="form-label" for="invite-name">Display name <span class="form-required">*</span></label>
              <input id="invite-name" class="form-input" type="text"
                     formControlName="displayName" placeholder="Jane Smith"
                     [class.form-input--error]="inviteForm.controls.displayName.invalid && inviteForm.controls.displayName.touched"/>
              @if (inviteForm.controls.displayName.invalid && inviteForm.controls.displayName.touched) {
                <p class="form-error">Display name is required.</p>
              }
            </div>

            <div class="form-field">
              <label class="form-label" for="invite-role">Role</label>
              <p-select
                inputId="invite-role"
                [options]="rolePickerOptions()"
                formControlName="roleId"
                optionLabel="label"
                optionValue="value"
                styleClass="form-select"
                placeholder="Select a role (optional)"
              />
            </div>

            @if (inviteError()) {
              <p class="form-error">{{ inviteError() }}</p>
            }

            <div class="modal-footer">
              <button type="button" class="stride-btn stride-btn-secondary"
                      (click)="closeInviteModal()" [disabled]="inviting()">
                Cancel
              </button>
              <button type="submit" class="stride-btn stride-btn-primary"
                      [disabled]="inviteForm.invalid || inviting()">
                @if (inviting()) { Sending invite… } @else { Send Invite }
              </button>
            </div>
          </form>
        </div>
      </div>
    }

    <!-- ═══════════════════════════════════════════════════════════════════ -->
    <!-- Change Role Modal                                                   -->
    <!-- ═══════════════════════════════════════════════════════════════════ -->
    @if (showRoleModal()) {
      <div class="modal-backdrop" (click)="closeRoleModal()" role="presentation">
        <div class="modal modal--sm" role="dialog" aria-modal="true" aria-labelledby="role-modal-title"
             (click)="$event.stopPropagation()">
          <div class="modal-header">
            <h2 class="modal-title" id="role-modal-title">Change Role</h2>
            <button class="modal-close" (click)="closeRoleModal()" aria-label="Close">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none">
                <line x1="18" y1="6" x2="6"  y2="18" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
                <line x1="6"  y1="6" x2="18" y2="18" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
              </svg>
            </button>
          </div>

          <div class="modal-body">
            <p class="modal-description">
              Change role for <strong>{{ roleTargetUser()?.displayName }}</strong>
            </p>

            <div class="form-field">
              <label class="form-label" for="new-role">New Role <span class="form-required">*</span></label>
              <p-select
                inputId="new-role"
                [options]="rolePickerOptions()"
                [(ngModel)]="selectedNewRoleId"
                optionLabel="label"
                optionValue="value"
                styleClass="form-select"
                placeholder="Select a role"
              />
            </div>

            @if (roleError()) {
              <p class="form-error">{{ roleError() }}</p>
            }

            <div class="modal-footer">
              <button class="stride-btn stride-btn-secondary"
                      (click)="closeRoleModal()" [disabled]="changingRole()">
                Cancel
              </button>
              <button class="stride-btn stride-btn-primary"
                      (click)="submitRoleChange()" [disabled]="changingRole() || !selectedNewRoleId">
                @if (changingRole()) { Saving… } @else { Save }
              </button>
            </div>
          </div>
        </div>
      </div>
    }

    <!-- Click-away overlay to close dropdown menus -->
    @if (openMenuId()) {
      <div class="click-away" (click)="openMenuId.set(null)" aria-hidden="true"></div>
    }
  `,
  styles: [`
    /* ── Layout ──────────────────────────────────────────────────────────── */
    .admin-page {
      max-width: 72rem;
      margin: 0 auto;
      padding: 1.5rem 1rem 3rem;
    }

    /* ── Header ──────────────────────────────────────────────────────────── */
    .admin-header {
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      gap: 1rem;
      margin-bottom: 1.5rem;
    }

    .admin-title {
      font-size: 1.375rem;
      font-weight: 700;
      color: var(--stride-text-primary);
      margin: 0 0 0.25rem;
    }

    .admin-subtitle {
      font-size: 0.875rem;
      color: var(--stride-text-muted);
      margin: 0;
    }

    /* ── Notification banner ─────────────────────────────────────────────── */
    .admin-notification {
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

    .admin-notification--error {
      background: color-mix(in srgb, #EF4444 10%, var(--stride-surface));
      border-color: color-mix(in srgb, #EF4444 30%, transparent);
      color: #DC2626;
    }

    .admin-notification-close {
      background: none;
      border: none;
      cursor: pointer;
      color: inherit;
      padding: 0.125rem;
      display: flex;
      align-items: center;
      opacity: 0.7;
      flex-shrink: 0;
    }

    .admin-notification-close:hover { opacity: 1; }

    /* ── Toolbar ─────────────────────────────────────────────────────────── */
    .admin-toolbar {
      display: flex;
      align-items: center;
      flex-wrap: wrap;
      gap: 0.625rem;
      margin-bottom: 1.25rem;
    }

    .admin-search-wrap {
      position: relative;
      flex: 1;
      min-width: 14rem;
    }

    .admin-search-icon {
      position: absolute;
      left: 0.75rem;
      top: 50%;
      transform: translateY(-50%);
      color: var(--stride-text-muted);
      pointer-events: none;
    }

    .admin-search {
      width: 100%;
      padding: 0.5rem 0.75rem 0.5rem 2.25rem;
      border: 1px solid var(--stride-border-soft);
      border-radius: var(--stride-radius-md);
      background: var(--stride-surface);
      color: var(--stride-text-primary);
      font-size: 0.875rem;
      font-family: inherit;
      transition: border-color 150ms;
      box-sizing: border-box;
    }

    .admin-search:focus {
      outline: none;
      border-color: var(--stride-primary);
      box-shadow: 0 0 0 3px color-mix(in srgb, var(--stride-primary) 15%, transparent);
    }

    .admin-search::placeholder { color: var(--stride-text-muted); }

    :host ::ng-deep .admin-filter-select .p-select {
      min-width: 10rem;
      font-size: 0.875rem;
    }

    .admin-clear-btn {
      font-size: 0.8125rem;
      color: var(--stride-text-muted);
      background: none;
      border: none;
      cursor: pointer;
      padding: 0.25rem 0.5rem;
      border-radius: var(--stride-radius-sm);
      font-family: inherit;
      white-space: nowrap;
    }

    .admin-clear-btn:hover { color: var(--stride-primary); }

    /* ── Table wrapper ───────────────────────────────────────────────────── */
    .admin-table-wrap {
      border: 1px solid var(--stride-border-soft);
      border-radius: var(--stride-radius-lg);
      overflow: hidden;
      background: var(--stride-surface);
    }

    .admin-table {
      width: 100%;
      border-collapse: collapse;
      font-size: 0.875rem;
    }

    .admin-table thead tr {
      background: var(--stride-surface-secondary);
      border-bottom: 1px solid var(--stride-border-soft);
    }

    .admin-table th {
      padding: 0.625rem 1rem;
      text-align: left;
      font-size: 0.75rem;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      color: var(--stride-text-muted);
      white-space: nowrap;
    }

    .admin-th-actions { width: 3rem; }

    .admin-table tbody tr {
      border-bottom: 1px solid var(--stride-border-soft);
      transition: background 120ms;
    }

    .admin-table tbody tr:last-child { border-bottom: none; }

    .admin-table tbody tr:hover { background: var(--stride-surface-hover); }

    .admin-row--inactive { opacity: 0.55; }

    .admin-table td {
      padding: 0.875rem 1rem;
      vertical-align: middle;
      color: var(--stride-text-primary);
    }

    /* ── User cell ───────────────────────────────────────────────────────── */
    .admin-user-cell {
      display: flex;
      align-items: center;
      gap: 0.75rem;
    }

    .admin-avatar {
      width: 2.25rem;
      height: 2.25rem;
      border-radius: 50%;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 0.8125rem;
      font-weight: 700;
      flex-shrink: 0;
      text-transform: uppercase;
    }

    .admin-avatar--admin              { background: color-mix(in srgb, var(--stride-primary) 15%, transparent); color: var(--stride-primary); }
    .admin-avatar--operationsmanager  { background: color-mix(in srgb, #3B82F6 15%, transparent); color: #2563EB; }
    .admin-avatar--financeuser        { background: color-mix(in srgb, #10B981 15%, transparent); color: #059669; }
    .admin-avatar--fieldworker        { background: color-mix(in srgb, #0891B2 15%, transparent); color: #0E7490; }
    .admin-avatar--supervisor         { background: color-mix(in srgb, #F59E0B 15%, transparent); color: #D97706; }

    .admin-user-name  { font-weight: 600; color: var(--stride-text-primary); }
    .admin-user-email { font-size: 0.8125rem; color: var(--stride-text-muted); margin-top: 0.125rem; }

    /* ── Badges ──────────────────────────────────────────────────────────── */
    .admin-badge {
      display: inline-block;
      font-size: 0.6875rem;
      font-weight: 600;
      padding: 0.2rem 0.55rem;
      border-radius: 99px;
      text-transform: uppercase;
      letter-spacing: 0.04em;
      white-space: nowrap;
    }

    .badge-role--admin              { background: color-mix(in srgb, var(--stride-primary) 12%, transparent); color: var(--stride-primary); }
    .badge-role--operationsmanager  { background: color-mix(in srgb, #3B82F6 12%, transparent); color: #2563EB; }
    .badge-role--financeuser        { background: color-mix(in srgb, #10B981 12%, transparent); color: #059669; }
    .badge-role--fieldworker        { background: color-mix(in srgb, #0891B2 12%, transparent); color: #0E7490; }
    .badge-role--supervisor         { background: color-mix(in srgb, #F59E0B 12%, transparent); color: #D97706; }

    .badge-status--active   { background: color-mix(in srgb, #10B981 12%, transparent); color: #059669; }
    .badge-status--pending  { background: color-mix(in srgb, #F59E0B 12%, transparent); color: #D97706; }
    .badge-status--inactive { background: color-mix(in srgb, #6B7280 12%, transparent); color: #6B7280; }

    /* ── Date ────────────────────────────────────────────────────────────── */
    .admin-date {
      color: var(--stride-text-muted);
      font-size: 0.8125rem;
      white-space: nowrap;
    }

    /* ── Row action menu ─────────────────────────────────────────────────── */
    .admin-td-actions { width: 3rem; text-align: center; }

    .admin-action-menu-wrap {
      position: relative;
      display: inline-block;
    }

    .admin-action-trigger {
      width: 2rem;
      height: 2rem;
      border-radius: var(--stride-radius-md);
      display: flex;
      align-items: center;
      justify-content: center;
      border: 1px solid transparent;
      background: transparent;
      color: var(--stride-text-muted);
      cursor: pointer;
      transition: all 120ms;
    }

    .admin-action-trigger:hover,
    .admin-action-trigger--open {
      background: var(--stride-surface-secondary);
      border-color: var(--stride-border-soft);
      color: var(--stride-text-primary);
    }

    .admin-action-menu {
      position: absolute;
      right: 0;
      top: calc(100% + 0.25rem);
      background: var(--stride-surface);
      border: 1px solid var(--stride-border-soft);
      border-radius: var(--stride-radius-md);
      box-shadow: var(--stride-shadow-lg);
      min-width: 10rem;
      z-index: 50;
      padding: 0.25rem 0;
    }

    .admin-action-item {
      display: block;
      width: 100%;
      text-align: left;
      padding: 0.5rem 0.875rem;
      font-size: 0.875rem;
      font-family: inherit;
      background: none;
      border: none;
      color: var(--stride-text-primary);
      cursor: pointer;
      transition: background 100ms;
    }

    .admin-action-item:hover { background: var(--stride-surface-hover); }

    .admin-action-item--danger { color: #EF4444; }
    .admin-action-item--danger:hover { background: color-mix(in srgb, #EF4444 8%, transparent); }

    /* ── Pagination ──────────────────────────────────────────────────────── */
    .admin-pagination {
      display: flex;
      align-items: center;
      justify-content: space-between;
      flex-wrap: wrap;
      gap: 0.75rem;
      margin-top: 1rem;
    }

    .admin-pagination-info {
      font-size: 0.8125rem;
      color: var(--stride-text-muted);
    }

    .admin-pagination-controls {
      display: flex;
      align-items: center;
      gap: 0.25rem;
    }

    .admin-page-btn {
      min-width: 2rem;
      height: 2rem;
      padding: 0 0.375rem;
      border-radius: var(--stride-radius-md);
      border: 1px solid var(--stride-border-soft);
      background: var(--stride-surface);
      color: var(--stride-text-secondary);
      font-size: 0.8125rem;
      font-family: inherit;
      cursor: pointer;
      display: flex;
      align-items: center;
      justify-content: center;
      transition: all 120ms;
    }

    .admin-page-btn:hover:not(:disabled) {
      border-color: var(--stride-primary);
      color: var(--stride-primary);
    }

    .admin-page-btn--active {
      background: var(--stride-primary);
      border-color: var(--stride-primary);
      color: #fff;
      font-weight: 600;
    }

    .admin-page-btn:disabled {
      opacity: 0.4;
      cursor: not-allowed;
    }

    /* ── Empty / error state ─────────────────────────────────────────────── */
    .admin-empty {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 0.75rem;
      padding: 4rem 1rem;
      color: var(--stride-text-muted);
      font-size: 0.9375rem;
      text-align: center;
    }

    /* ── Skeleton ────────────────────────────────────────────────────────── */
    .sk-row { animation: pulse 1.5s ease-in-out infinite; }

    .sk-user {
      display: flex;
      align-items: center;
      gap: 0.75rem;
    }

    .sk-avatar {
      width: 2.25rem;
      height: 2.25rem;
      border-radius: 50%;
      background: var(--stride-border);
      flex-shrink: 0;
    }

    .sk-text-block { display: flex; flex-direction: column; gap: 0.375rem; }

    .sk-line {
      border-radius: 4px;
      background: var(--stride-border);
      height: 0.75rem;
    }

    .sk-line--name  { width: 8rem; }
    .sk-line--email { width: 11rem; height: 0.6875rem; }
    .sk-line--badge { width: 4rem; }
    .sk-line--date  { width: 5.5rem; }

    @keyframes pulse {
      0%, 100% { opacity: 1; }
      50%       { opacity: 0.5; }
    }

    /* ── Modal ───────────────────────────────────────────────────────────── */
    .modal-backdrop {
      position: fixed;
      inset: 0;
      background: rgba(0, 0, 0, 0.45);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 100;
      padding: 1rem;
    }

    .modal {
      background: var(--stride-surface);
      border-radius: var(--stride-radius-xl);
      box-shadow: var(--stride-shadow-xl);
      width: 100%;
      max-width: 28rem;
      overflow: hidden;
    }

    .modal--sm { max-width: 22rem; }

    .modal-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 1.25rem 1.5rem 1rem;
      border-bottom: 1px solid var(--stride-border-soft);
    }

    .modal-title {
      font-size: 1.0625rem;
      font-weight: 700;
      color: var(--stride-text-primary);
      margin: 0;
    }

    .modal-close {
      background: none;
      border: none;
      cursor: pointer;
      color: var(--stride-text-muted);
      padding: 0.25rem;
      border-radius: var(--stride-radius-sm);
      display: flex;
      align-items: center;
      transition: color 120ms;
    }

    .modal-close:hover { color: var(--stride-text-primary); }

    .modal-body {
      padding: 1.25rem 1.5rem;
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }

    .modal-description {
      font-size: 0.875rem;
      color: var(--stride-text-secondary);
      margin: 0;
    }

    .modal-footer {
      display: flex;
      justify-content: flex-end;
      gap: 0.625rem;
      padding-top: 0.5rem;
    }

    /* ── Form fields ─────────────────────────────────────────────────────── */
    .form-field { display: flex; flex-direction: column; gap: 0.375rem; }

    .form-label {
      font-size: 0.875rem;
      font-weight: 500;
      color: var(--stride-text-secondary);
    }

    .form-required { color: #EF4444; }

    .form-input {
      padding: 0.5rem 0.75rem;
      border: 1px solid var(--stride-border-soft);
      border-radius: var(--stride-radius-md);
      background: var(--stride-surface);
      color: var(--stride-text-primary);
      font-size: 0.875rem;
      font-family: inherit;
      transition: border-color 150ms;
    }

    .form-input:focus {
      outline: none;
      border-color: var(--stride-primary);
      box-shadow: 0 0 0 3px color-mix(in srgb, var(--stride-primary) 15%, transparent);
    }

    .form-input--error { border-color: #EF4444; }

    :host ::ng-deep .form-select .p-select { width: 100%; font-size: 0.875rem; }

    .form-error {
      font-size: 0.8125rem;
      color: #EF4444;
      margin: 0;
    }

    /* ── Click-away overlay ──────────────────────────────────────────────── */
    .click-away {
      position: fixed;
      inset: 0;
      z-index: 40;
    }

    /* ── Accessibility ───────────────────────────────────────────────────── */
    .sr-only {
      position: absolute;
      width: 1px; height: 1px;
      padding: 0; margin: -1px;
      overflow: hidden;
      clip: rect(0,0,0,0);
      white-space: nowrap;
      border: 0;
    }
  `],
})
export class AdministrationPageComponent implements OnInit {
  readonly svc     = inject(AdminService);
  readonly roleSvc = inject(RoleService);

  private readonly fb         = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  // ── Filter state ─────────────────────────────────────────────────────────
  readonly searchTerm = signal('');
  selectedRole:   string = '';
  selectedStatus: UserStatus | '' = '';

  private readonly search$ = new Subject<string>();

  readonly hasActiveFilters = computed(
    () => !!this.searchTerm() || !!this.selectedRole || !!this.selectedStatus
  );

  // ── Role filter options (dynamically derived from catalog) ─────────────────
  readonly roleFilterOptions = computed(() => [
    { label: 'All Roles', value: '' },
    ...this.roleSvc.roles().map(r => ({ label: r.name, value: r.name })),
  ]);

  readonly rolePickerOptions = computed(() =>
    this.roleSvc.roles().map(r => ({ label: r.name, value: r.id }))
  );

  readonly statusOptions = [
    { label: 'All Statuses', value: '' as UserStatus | '' },
    { label: 'Active',       value: 'Active'   as UserStatus },
    { label: 'Pending',      value: 'Pending'  as UserStatus },
    { label: 'Inactive',     value: 'Inactive' as UserStatus },
  ];

  // ── UI state ──────────────────────────────────────────────────────────────
  readonly notification = signal<{ type: 'success' | 'error'; message: string } | null>(null);
  readonly openMenuId   = signal<string | null>(null);
  readonly skeletons    = [1, 2, 3, 4, 5, 6, 7, 8];

  // ── Invite modal ──────────────────────────────────────────────────────────
  readonly showInviteModal = signal(false);
  readonly inviting        = signal(false);
  readonly inviteError     = signal<string | null>(null);

  readonly inviteForm = this.fb.group({
    email:       ['', [Validators.required, Validators.email]],
    displayName: ['', Validators.required],
    roleId:      ['' as string],
  });

  // ── Change Role modal ─────────────────────────────────────────────────────
  readonly showRoleModal   = signal(false);
  readonly changingRole    = signal(false);
  readonly roleError       = signal<string | null>(null);
  readonly roleTargetUser  = signal<AdminUserDto | null>(null);
  selectedNewRoleId = '';

  // ── Derived ───────────────────────────────────────────────────────────────
  readonly pageRange = computed(() => {
    const total   = this.svc.totalPages();
    const current = this.svc.page();
    const delta   = 2;
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
      .subscribe(term => {
        this.searchTerm.set(term);
        this.reload(1);
      });

    this.reload();
    this.roleSvc.loadRoles();
  }

  // ── Search / filter ────────────────────────────────────────────────────────

  onSearchInput(event: Event): void {
    this.search$.next((event.target as HTMLInputElement).value);
  }

  onFilterChange(): void {
    this.reload(1);
  }

  clearFilters(): void {
    this.searchTerm.set('');
    this.selectedRole   = '';
    this.selectedStatus = '';
    this.search$.next('');
    this.reload(1);
  }

  // ── Pagination ─────────────────────────────────────────────────────────────

  goToPage(page: number): void {
    if (page < 1 || page > this.svc.totalPages()) return;
    this.reload(page);
  }

  // ── Invite modal ───────────────────────────────────────────────────────────

  openInviteModal(): void {
    this.inviteForm.reset({ email: '', displayName: '', roleId: '' });
    this.inviteError.set(null);
    this.showInviteModal.set(true);
  }

  closeInviteModal(): void {
    if (this.inviting()) return;
    this.showInviteModal.set(false);
  }

  submitInvite(): void {
    if (this.inviteForm.invalid) { this.inviteForm.markAllAsTouched(); return; }

    const { email, displayName, roleId } = this.inviteForm.getRawValue();
    this.inviting.set(true);
    this.inviteError.set(null);

    const request = roleId
      ? { email: email!, displayName: displayName!, roleId }
      : { email: email!, displayName: displayName! };

    this.svc.inviteUser(request).subscribe({
      next: () => {
        this.inviting.set(false);
        this.showInviteModal.set(false);
        this.notification.set({ type: 'success', message: `Invite sent to ${email}.` });
        this.reload(1);
      },
      error: (err) => {
        this.inviting.set(false);
        this.inviteError.set(err?.error?.error ?? 'Failed to send invite. Please try again.');
      },
    });
  }

  // ── Change Role modal ──────────────────────────────────────────────────────

  openRoleModal(user: AdminUserDto): void {
    this.openMenuId.set(null);
    this.roleTargetUser.set(user);
    this.selectedNewRoleId = user.roleId ?? '';
    this.roleError.set(null);
    this.showRoleModal.set(true);
  }

  closeRoleModal(): void {
    if (this.changingRole()) return;
    this.showRoleModal.set(false);
  }

  submitRoleChange(): void {
    const user = this.roleTargetUser();
    if (!user || !this.selectedNewRoleId) return;

    this.changingRole.set(true);
    this.roleError.set(null);

    const assign = () =>
      this.svc.assignUserRole(user.id, { roleId: this.selectedNewRoleId }).subscribe({
        next: () => {
          this.changingRole.set(false);
          this.showRoleModal.set(false);
          const roleName = this.roleSvc.roles().find(r => r.id === this.selectedNewRoleId)?.name
            ?? this.selectedNewRoleId;
          this.notification.set({ type: 'success', message: `${user.displayName}'s role updated to ${roleName}.` });
          this.reload();
        },
        error: (err) => {
          this.changingRole.set(false);
          this.roleError.set(err?.error?.error ?? 'Failed to update role.');
        },
      });

    if (user.roleId && user.roleId !== this.selectedNewRoleId) {
      this.svc.revokeUserRole(user.id, user.roleId).subscribe({
        next:  () => assign(),
        error: () => assign(), // proceed with assign even if revoke fails (role may already be gone)
      });
    } else {
      assign();
    }
  }

  // ── Deactivate / Reactivate ─────────────────────────────────────────────────

  deactivate(user: AdminUserDto): void {
    this.openMenuId.set(null);
    this.svc.deactivateUser(user.id).subscribe({
      next: () => {
        this.notification.set({ type: 'success', message: `${user.displayName} has been deactivated.` });
        this.reload();
      },
      error: (err) => {
        this.notification.set({ type: 'error', message: err?.error?.error ?? 'Failed to deactivate user.' });
      },
    });
  }

  reactivate(user: AdminUserDto): void {
    this.openMenuId.set(null);
    this.svc.reactivateUser(user.id).subscribe({
      next: () => {
        this.notification.set({ type: 'success', message: `${user.displayName} has been reactivated.` });
        this.reload();
      },
      error: (err) => {
        this.notification.set({ type: 'error', message: err?.error?.error ?? 'Failed to reactivate user.' });
      },
    });
  }

  // ── Menu toggle ─────────────────────────────────────────────────────────────

  toggleMenu(id: string): void {
    this.openMenuId.update(current => (current === id ? null : id));
  }

  // ── Display helpers ──────────────────────────────────────────────────────────

  initials(name: string): string {
    const parts = name.trim().split(/\s+/);
    if (parts.length >= 2) return (parts[0][0] + parts.at(-1)![0]).toUpperCase();
    return name.substring(0, 2).toUpperCase();
  }

  avatarColor(role: string): string {
    const colors = [
      ['color-mix(in srgb, var(--stride-primary) 15%, transparent)', 'var(--stride-primary)'],
      ['color-mix(in srgb, #3B82F6 15%, transparent)', '#2563EB'],
      ['color-mix(in srgb, #10B981 15%, transparent)', '#059669'],
      ['color-mix(in srgb, #0891B2 15%, transparent)', '#0E7490'],
      ['color-mix(in srgb, #F59E0B 15%, transparent)', '#D97706'],
      ['color-mix(in srgb, #8B5CF6 15%, transparent)', '#7C3AED'],
    ];
    let hash = 0;
    for (let i = 0; i < role.length; i++) hash = (hash * 31 + role.codePointAt(i)!) & 0xffff;
    const [bg, fg] = colors[hash % colors.length];
    return `background:${bg};color:${fg}`;
  }

  roleBadgeStyle(role: string): string {
    const colors = [
      ['color-mix(in srgb, var(--stride-primary) 12%, transparent)', 'var(--stride-primary)'],
      ['color-mix(in srgb, #3B82F6 12%, transparent)', '#2563EB'],
      ['color-mix(in srgb, #10B981 12%, transparent)', '#059669'],
      ['color-mix(in srgb, #0891B2 12%, transparent)', '#0E7490'],
      ['color-mix(in srgb, #F59E0B 12%, transparent)', '#D97706'],
      ['color-mix(in srgb, #8B5CF6 12%, transparent)', '#7C3AED'],
    ];
    let hash = 0;
    for (let i = 0; i < role.length; i++) hash = (hash * 31 + role.codePointAt(i)!) & 0xffff;
    const [bg, fg] = colors[hash % colors.length];
    return `background:${bg};color:${fg}`;
  }

  statusBadgeClass(user: AdminUserDto): string {
    if (user.isPending) return 'badge-status--pending';
    if (!user.isActive) return 'badge-status--inactive';
    return 'badge-status--active';
  }

  statusLabel(user: AdminUserDto): string {
    if (user.isPending) return 'Pending';
    if (!user.isActive) return 'Inactive';
    return 'Active';
  }

  formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString('en-GB', {
      day: 'numeric', month: 'short', year: 'numeric',
    });
  }

  // ── Private ──────────────────────────────────────────────────────────────────

  reload(page?: number): void {
    this.svc.loadUsers(
      page ?? this.svc.page(),
      this.svc.pageSize(),
      this.searchTerm() || undefined,
      this.selectedRole  || undefined,
      this.selectedStatus || undefined,
    );
  }
}
