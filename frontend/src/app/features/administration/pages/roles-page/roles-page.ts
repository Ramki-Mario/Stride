import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  inject,
  signal,
  computed,
} from '@angular/core';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';

import { RoleService } from '../../services/role.service';
import { PermissionDto } from '../../models/role.models';

// Friendly labels for each permission key
const PERMISSION_LABELS: Record<string, string> = {
  'workflow.view':            'View Workflows',
  'workflow.create':          'Create & Edit Workflows',
  'workflow.activate':        'Activate / Archive Workflows',
  'workflow.run':             'Start Workflow Instances',
  'workflow.manage_instances':'Manage All Instances',
  'user.invite':              'Invite Users',
  'user.manage':              'Manage Users & Roles',
  'role.view':                'View Role Catalog',
  'role.manage':              'Create, Edit & Delete Roles',
  'tenant.settings':          'Edit Tenant Settings',
};

// Group keys for the permission picker
const PERMISSION_GROUPS = [
  { label: 'Workflows', keys: ['workflow.view','workflow.create','workflow.activate','workflow.run','workflow.manage_instances'] },
  { label: 'Users',     keys: ['user.invite','user.manage'] },
  { label: 'Roles',     keys: ['role.view','role.manage'] },
  { label: 'Tenant',    keys: ['tenant.settings'] },
];

@Component({
  selector: 'app-roles-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, ReactiveFormsModule],
  template: `
    <div class="rl-page">

      <!-- ── Header ─────────────────────────────────────────────────────── -->
      <div class="rl-header">
        <div>
          <h1 class="rl-title">Roles</h1>
          <p class="rl-subtitle">
            @if (!svc.isLoading()) {
              {{ svc.totalCount() }} role{{ svc.totalCount() === 1 ? '' : 's' }} total
            } @else { Loading… }
          </p>
        </div>
        <button class="stride-btn stride-btn-primary" (click)="openCreateModal()">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" aria-hidden="true">
            <line x1="12" y1="5" x2="12" y2="19" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
            <line x1="5" y1="12" x2="19" y2="12" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
          </svg>
          New Role
        </button>
      </div>

      <!-- ── KPI cards ──────────────────────────────────────────────────── -->
      <div class="rl-kpis">
        <div class="rl-kpi rl-kpi--total">
          <span class="rl-kpi-label">Total Roles</span>
          <span class="rl-kpi-value">{{ svc.totalCount() }}</span>
        </div>
        <div class="rl-kpi rl-kpi--custom">
          <span class="rl-kpi-label">Custom</span>
          <span class="rl-kpi-value">{{ customCount() }}</span>
        </div>
        <div class="rl-kpi rl-kpi--system">
          <span class="rl-kpi-label">System</span>
          <span class="rl-kpi-value">{{ systemCount() }}</span>
        </div>
      </div>

      <!-- ── Notification ────────────────────────────────────────────────── -->
      @if (notification()) {
        <div class="rl-notification" [class.rl-notification--error]="notification()!.type === 'error'">
          <span>{{ notification()!.message }}</span>
          <button class="rl-notif-close" (click)="notification.set(null)" aria-label="Dismiss">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none">
              <line x1="18" y1="6" x2="6" y2="18" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
              <line x1="6" y1="6" x2="18" y2="18" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
            </svg>
          </button>
        </div>
      }

      <!-- ── Loading skeleton ────────────────────────────────────────────── -->
      @if (svc.isLoading()) {
        <div class="rl-table-wrap">
          <table class="rl-table" aria-label="Loading roles">
            <thead><tr>
              <th>Name</th><th>Description</th><th>Permissions</th><th>Type</th><th></th>
            </tr></thead>
            <tbody>
              @for (i of skeletons; track i) {
                <tr class="sk-row">
                  <td><div class="sk-line sk-line--name"></div></td>
                  <td><div class="sk-line sk-line--desc"></div></td>
                  <td><div class="sk-line sk-line--perms"></div></td>
                  <td><div class="sk-line sk-line--badge"></div></td>
                  <td></td>
                </tr>
              }
            </tbody>
          </table>
        </div>

      <!-- ── Error ──────────────────────────────────────────────────────── -->
      } @else if (svc.error()) {
        <div class="rl-empty">
          <svg width="40" height="40" viewBox="0 0 24 24" fill="none" aria-hidden="true">
            <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="1.5"/>
            <line x1="12" y1="8" x2="12" y2="12" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
            <line x1="12" y1="16" x2="12.01" y2="16" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
          </svg>
          <p>{{ svc.error() }}</p>
          <button class="stride-btn stride-btn-secondary" (click)="svc.loadRoles()">Retry</button>
        </div>

      <!-- ── Empty ──────────────────────────────────────────────────────── -->
      } @else if (svc.isEmpty()) {
        <div class="rl-empty">
          <svg width="40" height="40" viewBox="0 0 24 24" fill="none" aria-hidden="true">
            <rect x="3" y="3" width="18" height="18" rx="2" stroke="currentColor" stroke-width="1.5"/>
            <path d="M9 9h6M9 12h6M9 15h4" stroke="currentColor" stroke-width="1.5" stroke-linecap="round"/>
          </svg>
          <p>No roles yet. Create your first custom role.</p>
          <button class="stride-btn stride-btn-primary" (click)="openCreateModal()">New Role</button>
        </div>

      <!-- ── Table ──────────────────────────────────────────────────────── -->
      } @else {
        <div class="rl-table-wrap">
          <table class="rl-table" aria-label="Roles">
            <thead>
              <tr>
                <th>Name</th>
                <th>Description</th>
                <th>Permissions</th>
                <th>Type</th>
                <th class="rl-th-actions"><span class="sr-only">Actions</span></th>
              </tr>
            </thead>
            <tbody>
              @for (role of svc.roles(); track role.id) {
                <tr>
                  <td class="rl-td-name">{{ role.name }}</td>
                  <td class="rl-td-muted">{{ role.description || '—' }}</td>
                  <td>
                    <div class="rl-perm-chips">
                      @for (p of getPermissions(role); track p.id) {
                        <span class="rl-perm-chip">{{ permLabel(p.key) }}</span>
                      }
                      @if (getPermissions(role).length === 0) {
                        <span class="rl-td-muted">None</span>
                      }
                    </div>
                  </td>
                  <td>
                    @if (isSystem(role)) {
                      <span class="rl-badge rl-badge--system">System</span>
                    } @else {
                      <span class="rl-badge rl-badge--custom">Custom</span>
                    }
                  </td>
                  <td class="rl-td-actions">
                    @if (!isSystem(role)) {
                      <div class="rl-menu-wrap">
                        <button class="rl-menu-trigger"
                                [class.rl-menu-trigger--open]="openMenuId() === role.id"
                                (click)="toggleMenu(role.id)"
                                aria-label="Role actions">
                          <svg width="16" height="16" viewBox="0 0 24 24" fill="none">
                            <circle cx="12" cy="5"  r="1.2" fill="currentColor"/>
                            <circle cx="12" cy="12" r="1.2" fill="currentColor"/>
                            <circle cx="12" cy="19" r="1.2" fill="currentColor"/>
                          </svg>
                        </button>
                        @if (openMenuId() === role.id) {
                          <div class="rl-menu" role="menu">
                            <button class="rl-menu-item" role="menuitem" (click)="openEditModal(role.id)">
                              Edit
                            </button>
                            <button class="rl-menu-item rl-menu-item--danger" role="menuitem"
                                    (click)="openDeleteConfirm(role)">
                              Delete
                            </button>
                          </div>
                        }
                      </div>
                    } @else {
                      <span class="rl-lock" title="System roles cannot be modified">
                        <svg width="14" height="14" viewBox="0 0 24 24" fill="none">
                          <rect x="3" y="11" width="18" height="11" rx="2" stroke="currentColor" stroke-width="1.75"/>
                          <path d="M7 11V7a5 5 0 0 1 10 0v4" stroke="currentColor" stroke-width="1.75" stroke-linecap="round"/>
                        </svg>
                      </span>
                    }
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </div>

    <!-- ═══════════════════════════════════════════════════════════════════ -->
    <!-- Create / Edit Role Modal                                            -->
    <!-- ═══════════════════════════════════════════════════════════════════ -->
    @if (showModal()) {
      <div class="modal-backdrop" (click)="closeModal()" role="presentation">
        <div class="modal" role="dialog" aria-modal="true"
             [attr.aria-labelledby]="editingId() ? 'edit-role-title' : 'create-role-title'"
             (click)="$event.stopPropagation()">

          <div class="modal-header">
            <h2 class="modal-title" [id]="editingId() ? 'edit-role-title' : 'create-role-title'">
              {{ editingId() ? 'Edit Role' : 'New Role' }}
            </h2>
            <button class="modal-close" (click)="closeModal()" aria-label="Close">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none">
                <line x1="18" y1="6" x2="6"  y2="18" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
                <line x1="6"  y1="6" x2="18" y2="18" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
              </svg>
            </button>
          </div>

          <form class="modal-body" [formGroup]="roleForm" (ngSubmit)="submitModal()">

            <!-- Name -->
            <div class="form-field">
              <label class="form-label" for="rl-name">
                Name <span class="form-required">*</span>
              </label>
              <input id="rl-name" class="form-input" formControlName="name"
                     placeholder="e.g. Field Supervisor"
                     autocomplete="off"
                     [class.form-input--error]="roleForm.controls.name.invalid && roleForm.controls.name.touched"/>
              @if (roleForm.controls.name.invalid && roleForm.controls.name.touched) {
                <p class="form-error">Role name is required (max 100 characters).</p>
              }
            </div>

            <!-- Description -->
            <div class="form-field">
              <label class="form-label" for="rl-desc">Description</label>
              <input id="rl-desc" class="form-input" formControlName="description"
                     placeholder="Brief description of this role's purpose"/>
            </div>

            <!-- Permissions -->
            <div class="form-field">
              <label class="form-label">Permissions</label>
              @if (permissionsLoading()) {
                <p class="form-hint">Loading permissions…</p>
              } @else {
                <div class="perm-groups">
                  @for (group of permissionGroups; track group.label) {
                    <div class="perm-group">
                      <div class="perm-group-label">{{ group.label }}</div>
                      <div class="perm-grid">
                        @for (key of group.keys; track key) {
                          @if (permissionByKey(key); as p) {
                            <label class="perm-check" [class.perm-check--checked]="isChecked(p.id)">
                              <input type="checkbox" class="perm-checkbox"
                                     [checked]="isChecked(p.id)"
                                     (change)="togglePermission(p.id)"/>
                              <span class="perm-check-label">{{ permLabel(key) }}</span>
                            </label>
                          }
                        }
                      </div>
                    </div>
                  }
                </div>
              }
            </div>

            @if (modalError()) {
              <p class="form-error">{{ modalError() }}</p>
            }

            <div class="modal-footer">
              <button type="button" class="stride-btn stride-btn-secondary"
                      (click)="closeModal()" [disabled]="saving()">Cancel</button>
              <button type="submit" class="stride-btn stride-btn-primary"
                      [disabled]="roleForm.invalid || saving()">
                @if (saving()) {
                  @if (editingId()) { Saving… } @else { Creating… }
                } @else {
                  @if (editingId()) { Save Changes } @else { Create Role }
                }
              </button>
            </div>
          </form>
        </div>
      </div>
    }

    <!-- ═══════════════════════════════════════════════════════════════════ -->
    <!-- Delete Confirmation Modal                                           -->
    <!-- ═══════════════════════════════════════════════════════════════════ -->
    @if (showDeleteModal()) {
      <div class="modal-backdrop" (click)="closeDeleteModal()" role="presentation">
        <div class="modal modal--sm" role="dialog" aria-modal="true"
             aria-labelledby="delete-role-title"
             (click)="$event.stopPropagation()">

          <div class="modal-header">
            <h2 class="modal-title" id="delete-role-title">Delete Role</h2>
            <button class="modal-close" (click)="closeDeleteModal()" aria-label="Close">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none">
                <line x1="18" y1="6" x2="6"  y2="18" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
                <line x1="6"  y1="6" x2="18" y2="18" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
              </svg>
            </button>
          </div>

          <div class="modal-body modal-body--confirm">
            <svg class="confirm-icon" width="40" height="40" viewBox="0 0 24 24" fill="none">
              <circle cx="12" cy="12" r="10" stroke="#EF4444" stroke-width="1.5"/>
              <line x1="12" y1="8" x2="12" y2="12" stroke="#EF4444" stroke-width="2" stroke-linecap="round"/>
              <line x1="12" y1="16" x2="12.01" y2="16" stroke="#EF4444" stroke-width="2" stroke-linecap="round"/>
            </svg>
            <p class="confirm-text">
              Are you sure you want to delete <strong>{{ deletingRole()?.name }}</strong>?
              Users assigned this role will lose its permissions immediately.
            </p>
            @if (deleteError()) {
              <p class="form-error">{{ deleteError() }}</p>
            }
          </div>

          <div class="modal-footer modal-footer--confirm">
            <button class="stride-btn stride-btn-secondary"
                    (click)="closeDeleteModal()" [disabled]="deleting()">Cancel</button>
            <button class="stride-btn stride-btn-danger"
                    (click)="confirmDelete()" [disabled]="deleting()">
              {{ deleting() ? 'Deleting…' : 'Delete Role' }}
            </button>
          </div>
        </div>
      </div>
    }

    <!-- Click-away for dropdown menus -->
    @if (openMenuId()) {
      <div class="click-away" (click)="openMenuId.set(null)" aria-hidden="true"></div>
    }
  `,
  styles: [`
    .rl-page {
      max-width: 72rem;
      margin: 0 auto;
      padding: 1.5rem 1rem 3rem;
    }

    /* ── Header ─────────────────────────────────────────────────────────── */
    .rl-header {
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      gap: 1rem;
      margin-bottom: 1.25rem;
    }
    .rl-title {
      font-size: 1.375rem; font-weight: 700;
      color: var(--stride-text-primary); margin: 0 0 0.25rem;
    }
    .rl-subtitle {
      font-size: 0.875rem; color: var(--stride-text-muted); margin: 0;
    }

    /* ── KPI cards ──────────────────────────────────────────────────────── */
    .rl-kpis {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(10rem, 1fr));
      gap: 0.875rem;
      margin-bottom: 1.25rem;
    }
    .rl-kpi {
      background: var(--stride-surface);
      border: 1px solid var(--stride-border-soft);
      border-radius: var(--stride-radius-lg);
      padding: 1rem 1.25rem;
      display: flex; flex-direction: column; gap: 0.25rem;
    }
    .rl-kpi-label {
      font-size: 0.75rem; font-weight: 600;
      text-transform: uppercase; letter-spacing: 0.05em;
      color: var(--stride-text-muted);
    }
    .rl-kpi-value {
      font-size: 1.5rem; font-weight: 700; color: var(--stride-text-primary);
    }
    .rl-kpi--total  { border-left: 3px solid var(--stride-primary); }
    .rl-kpi--custom { border-left: 3px solid #10B981; }
    .rl-kpi--system { border-left: 3px solid #6B7280; }

    /* ── Notification ────────────────────────────────────────────────────── */
    .rl-notification {
      display: flex; align-items: center; justify-content: space-between;
      gap: 0.75rem; padding: 0.75rem 1rem; margin-bottom: 1rem;
      border-radius: var(--stride-radius-md);
      background: color-mix(in srgb, #10B981 10%, var(--stride-surface));
      border: 1px solid color-mix(in srgb, #10B981 30%, transparent);
      color: #059669; font-size: 0.875rem;
    }
    .rl-notification--error {
      background: color-mix(in srgb, #EF4444 10%, var(--stride-surface));
      border-color: color-mix(in srgb, #EF4444 30%, transparent);
      color: #DC2626;
    }
    .rl-notif-close {
      background: none; border: none; cursor: pointer; color: inherit;
      padding: 0.125rem; display: flex; align-items: center; opacity: 0.7;
    }
    .rl-notif-close:hover { opacity: 1; }

    /* ── Table ───────────────────────────────────────────────────────────── */
    .rl-table-wrap {
      border: 1px solid var(--stride-border-soft);
      border-radius: var(--stride-radius-lg);
      overflow: visible;
      background: var(--stride-surface);
    }
    .rl-table {
      width: 100%; border-collapse: collapse; font-size: 0.875rem;
    }
    .rl-table thead tr {
      background: var(--stride-surface-secondary);
      border-bottom: 1px solid var(--stride-border-soft);
    }
    .rl-table thead tr th:first-child { border-top-left-radius: var(--stride-radius-lg); }
    .rl-table thead tr th:last-child  { border-top-right-radius: var(--stride-radius-lg); }
    .rl-table tbody tr:last-child td:first-child { border-bottom-left-radius: var(--stride-radius-lg); }
    .rl-table tbody tr:last-child td:last-child  { border-bottom-right-radius: var(--stride-radius-lg); }
    .rl-table th {
      padding: 0.625rem 1rem; text-align: left;
      font-size: 0.75rem; font-weight: 600; text-transform: uppercase;
      letter-spacing: 0.05em; color: var(--stride-text-muted); white-space: nowrap;
    }
    .rl-th-actions { width: 3rem; }
    .rl-table tbody tr {
      border-bottom: 1px solid var(--stride-border-soft); transition: background 120ms;
    }
    .rl-table tbody tr:last-child { border-bottom: none; }
    .rl-table tbody tr:hover { background: var(--stride-surface-hover); }
    .rl-table td { padding: 0.875rem 1rem; vertical-align: middle; color: var(--stride-text-primary); }

    .rl-td-name  { font-weight: 600; white-space: nowrap; }
    .rl-td-muted { color: var(--stride-text-secondary); font-size: 0.875rem; }

    /* ── Permission chips ────────────────────────────────────────────────── */
    .rl-perm-chips { display: flex; flex-wrap: wrap; gap: 0.3rem; }
    .rl-perm-chip {
      display: inline-block; font-size: 0.6875rem; font-weight: 500;
      padding: 0.15rem 0.5rem; border-radius: 99px;
      background: color-mix(in srgb, var(--stride-primary) 10%, transparent);
      color: var(--stride-primary); white-space: nowrap;
    }

    /* ── Role type badge ────────────────────────────────────────────────── */
    .rl-badge {
      display: inline-block; font-size: 0.6875rem; font-weight: 600;
      padding: 0.2rem 0.55rem; border-radius: 99px;
      text-transform: uppercase; letter-spacing: 0.04em; white-space: nowrap;
    }
    .rl-badge--system { background: color-mix(in srgb, #6B7280 12%, transparent); color: #6B7280; }
    .rl-badge--custom { background: color-mix(in srgb, #10B981 12%, transparent); color: #059669; }

    /* ── Lock icon ───────────────────────────────────────────────────────── */
    .rl-lock {
      display: inline-flex; align-items: center; justify-content: center;
      width: 2rem; height: 2rem; color: var(--stride-text-muted);
      cursor: default;
    }

    /* ── Action menu ──────────────────────────────────────────────────────── */
    .rl-td-actions { width: 3rem; text-align: center; }
    .rl-menu-wrap  { position: relative; display: inline-block; }
    .rl-menu-trigger {
      width: 2rem; height: 2rem; border-radius: var(--stride-radius-md);
      display: flex; align-items: center; justify-content: center;
      border: 1px solid transparent; background: transparent;
      color: var(--stride-text-muted); cursor: pointer; transition: all 120ms;
    }
    .rl-menu-trigger:hover, .rl-menu-trigger--open {
      background: var(--stride-surface-secondary);
      border-color: var(--stride-border-soft);
      color: var(--stride-text-primary);
    }
    .rl-menu {
      position: absolute; right: 0; top: calc(100% + 0.25rem);
      background: var(--stride-surface); border: 1px solid var(--stride-border-soft);
      border-radius: var(--stride-radius-md); box-shadow: var(--stride-shadow-lg);
      min-width: 8rem; z-index: 50; padding: 0.25rem 0;
    }
    .rl-menu-item {
      display: block; width: 100%; text-align: left; padding: 0.5rem 0.875rem;
      font-size: 0.875rem; font-family: inherit; background: none; border: none;
      color: var(--stride-text-primary); cursor: pointer; transition: background 100ms;
    }
    .rl-menu-item:hover { background: var(--stride-surface-hover); }
    .rl-menu-item--danger { color: #EF4444; }
    .rl-menu-item--danger:hover { background: color-mix(in srgb, #EF4444 8%, transparent); }

    /* ── Empty / error ─────────────────────────────────────────────────────── */
    .rl-empty {
      display: flex; flex-direction: column; align-items: center;
      gap: 0.75rem; padding: 4rem 1rem;
      color: var(--stride-text-muted); font-size: 0.9375rem; text-align: center;
    }

    /* ── Skeleton ──────────────────────────────────────────────────────────── */
    .sk-row { animation: pulse 1.5s ease-in-out infinite; }
    .sk-line { border-radius: 4px; background: var(--stride-border); height: 0.75rem; }
    .sk-line--name  { width: 8rem; }
    .sk-line--desc  { width: 12rem; }
    .sk-line--perms { width: 16rem; }
    .sk-line--badge { width: 4rem; }
    @keyframes pulse { 0%, 100% { opacity: 1; } 50% { opacity: 0.5; } }

    /* ── Modal ─────────────────────────────────────────────────────────────── */
    .modal-backdrop {
      position: fixed; inset: 0; background: rgba(0,0,0,0.45);
      display: flex; align-items: center; justify-content: center;
      z-index: 100; padding: 1rem;
    }
    .modal {
      background: var(--stride-surface); border-radius: var(--stride-radius-xl);
      box-shadow: var(--stride-shadow-xl); width: 100%; max-width: 38rem; overflow: hidden;
    }
    .modal--sm { max-width: 26rem; }
    .modal-header {
      display: flex; align-items: center; justify-content: space-between;
      padding: 1.25rem 1.5rem 1rem; border-bottom: 1px solid var(--stride-border-soft);
    }
    .modal-title { font-size: 1.0625rem; font-weight: 700; color: var(--stride-text-primary); margin: 0; }
    .modal-close {
      background: none; border: none; cursor: pointer; color: var(--stride-text-muted);
      padding: 0.25rem; border-radius: var(--stride-radius-sm);
      display: flex; align-items: center; transition: color 120ms;
    }
    .modal-close:hover { color: var(--stride-text-primary); }
    .modal-body {
      padding: 1.25rem 1.5rem; display: flex; flex-direction: column; gap: 0.875rem;
      max-height: 76vh; overflow-y: auto;
    }
    .modal-body--confirm {
      padding: 1.5rem 1.5rem 0.75rem;
      display: flex; flex-direction: column; align-items: center; gap: 1rem;
      text-align: center;
    }
    .confirm-icon { flex-shrink: 0; }
    .confirm-text { font-size: 0.9375rem; color: var(--stride-text-secondary); margin: 0; line-height: 1.55; }
    .modal-footer {
      display: flex; justify-content: flex-end; gap: 0.625rem; padding-top: 0.5rem;
    }
    .modal-footer--confirm {
      padding: 1rem 1.5rem 1.25rem;
      display: flex; justify-content: flex-end; gap: 0.625rem;
      border-top: 1px solid var(--stride-border-soft);
    }

    /* ── Form ───────────────────────────────────────────────────────────────── */
    .form-field { display: flex; flex-direction: column; gap: 0.375rem; }
    .form-label { font-size: 0.875rem; font-weight: 500; color: var(--stride-text-secondary); }
    .form-required { color: #EF4444; }
    .form-hint { font-size: 0.8125rem; color: var(--stride-text-muted); margin: 0; }
    .form-input {
      padding: 0.5rem 0.75rem; border: 1px solid var(--stride-border-soft);
      border-radius: var(--stride-radius-md); background: var(--stride-surface);
      color: var(--stride-text-primary); font-size: 0.875rem; font-family: inherit;
    }
    .form-input:focus {
      outline: none; border-color: var(--stride-primary);
      box-shadow: 0 0 0 3px color-mix(in srgb, var(--stride-primary) 15%, transparent);
    }
    .form-input--error { border-color: #EF4444; }
    .form-error { font-size: 0.8125rem; color: #EF4444; margin: 0; }

    /* ── Permission picker ───────────────────────────────────────────────── */
    .perm-groups { display: flex; flex-direction: column; gap: 1rem; }
    .perm-group {}
    .perm-group-label {
      font-size: 0.6875rem; font-weight: 700; text-transform: uppercase;
      letter-spacing: 0.07em; color: var(--stride-text-muted);
      margin-bottom: 0.4rem;
    }
    .perm-grid {
      display: grid; grid-template-columns: 1fr 1fr; gap: 0.35rem;
    }
    .perm-check {
      display: flex; align-items: center; gap: 0.5rem;
      padding: 0.45rem 0.625rem; border-radius: var(--stride-radius-md);
      border: 1px solid var(--stride-border-soft);
      cursor: pointer; transition: all 120ms;
      user-select: none;
    }
    .perm-check:hover { border-color: var(--stride-primary); }
    .perm-check--checked {
      border-color: var(--stride-primary);
      background: color-mix(in srgb, var(--stride-primary) 8%, transparent);
    }
    .perm-checkbox { accent-color: var(--stride-primary); flex-shrink: 0; cursor: pointer; }
    .perm-check-label { font-size: 0.8125rem; color: var(--stride-text-primary); }

    /* ── Danger button ───────────────────────────────────────────────────── */
    .stride-btn-danger {
      background: #EF4444; color: #fff; border: none;
      padding: 0.5rem 1.125rem; border-radius: var(--stride-radius-md);
      font-size: 0.875rem; font-weight: 600; cursor: pointer;
      font-family: inherit; transition: background 120ms;
    }
    .stride-btn-danger:hover:not(:disabled) { background: #DC2626; }
    .stride-btn-danger:disabled { opacity: 0.55; cursor: not-allowed; }

    /* ── Click-away ─────────────────────────────────────────────────────────── */
    .click-away { position: fixed; inset: 0; z-index: 40; }

    /* ── A11y ─────────────────────────────────────────────────────────────── */
    .sr-only {
      position: absolute; width: 1px; height: 1px; padding: 0;
      margin: -1px; overflow: hidden; clip: rect(0,0,0,0); white-space: nowrap; border: 0;
    }

    /* ── Mobile ───────────────────────────────────────────────────────────── */
    @media (max-width: 640px) {
      .rl-page { padding: 1rem 0.75rem 2rem; }
      .perm-grid { grid-template-columns: 1fr; }
      .rl-table th:nth-child(2),
      .rl-table td:nth-child(2) { display: none; }
    }
  `],
})
export class RolesPageComponent implements OnInit {
  readonly svc = inject(RoleService);
  private readonly fb = inject(FormBuilder);

  // ── Permission catalog ────────────────────────────────────────────────────
  readonly allPermissions      = signal<PermissionDto[]>([]);
  readonly permissionsLoading  = signal(false);
  readonly checkedPermIds      = signal<Set<string>>(new Set());

  readonly permissionGroups = PERMISSION_GROUPS;

  // ── Derived KPIs ─────────────────────────────────────────────────────────
  readonly systemCount = computed(() =>
    this.svc.roles().filter(r => r.isSystemRole).length);
  readonly customCount = computed(() =>
    this.svc.roles().filter(r => !r.isSystemRole).length);

  // ── UI state ───────────────────────────────────────────────────────────────
  readonly notification = signal<{ type: 'success' | 'error'; message: string } | null>(null);
  readonly openMenuId   = signal<string | null>(null);
  readonly skeletons    = [1, 2, 3, 4, 5];

  // ── Create/Edit modal ─────────────────────────────────────────────────────
  readonly showModal  = signal(false);
  readonly editingId  = signal<string | null>(null);
  readonly saving     = signal(false);
  readonly modalError = signal<string | null>(null);

  // ── Delete confirm modal ──────────────────────────────────────────────────
  readonly showDeleteModal = signal(false);
  readonly deletingRole    = signal<{ id: string; name: string } | null>(null);
  readonly deleting        = signal(false);
  readonly deleteError     = signal<string | null>(null);

  readonly roleForm = this.fb.group({
    name:        ['', [Validators.required, Validators.maxLength(100)]],
    description: ['', Validators.maxLength(500)],
  });

  // ── Lifecycle ─────────────────────────────────────────────────────────────
  ngOnInit(): void {
    this.svc.loadRoles();
    this.loadPermissions();
  }

  private loadPermissions(): void {
    this.permissionsLoading.set(true);
    this.svc.getPermissions().subscribe({
      next:  perms => { this.allPermissions.set(perms); this.permissionsLoading.set(false); },
      error: ()    => this.permissionsLoading.set(false),
    });
  }

  // ── Permission helpers ─────────────────────────────────────────────────────
  permLabel(key: string): string {
    return PERMISSION_LABELS[key] ?? key;
  }

  permissionByKey(key: string): PermissionDto | undefined {
    return this.allPermissions().find(p => p.key === key);
  }

  isChecked(id: string): boolean {
    return this.checkedPermIds().has(id);
  }

  togglePermission(id: string): void {
    const s = new Set(this.checkedPermIds());
    s.has(id) ? s.delete(id) : s.add(id);
    this.checkedPermIds.set(s);
  }

  // ── Role helpers ─────────────────────────────────────────────────────────
  isSystem(role: any): boolean {
    return !!role.isSystemRole;
  }

  getPermissions(role: any): PermissionDto[] {
    return role.permissions ?? [];
  }

  // ── Create modal ──────────────────────────────────────────────────────────
  openCreateModal(): void {
    this.editingId.set(null);
    this.roleForm.reset();
    this.checkedPermIds.set(new Set());
    this.modalError.set(null);
    this.openMenuId.set(null);
    this.showModal.set(true);
  }

  // ── Edit modal ────────────────────────────────────────────────────────────
  openEditModal(roleId: string): void {
    this.openMenuId.set(null);
    this.editingId.set(roleId);
    this.modalError.set(null);
    this.roleForm.reset();
    this.checkedPermIds.set(new Set());
    this.showModal.set(true);

    this.svc.getRole(roleId).subscribe({
      next: detail => {
        this.roleForm.setValue({
          name:        detail.name,
          description: detail.description ?? '',
        });
        this.checkedPermIds.set(new Set(detail.permissions.map(p => p.id)));
      },
      error: () => this.modalError.set('Failed to load role details.'),
    });
  }

  closeModal(): void {
    if (this.saving()) return;
    this.showModal.set(false);
    this.editingId.set(null);
  }

  submitModal(): void {
    if (this.roleForm.invalid) { this.roleForm.markAllAsTouched(); return; }

    const v = this.roleForm.getRawValue();
    const permissionIds = [...this.checkedPermIds()];

    this.saving.set(true);
    this.modalError.set(null);

    const editId = this.editingId();

    if (editId) {
      this.svc.updateRole(editId, {
        name:          v.name!.trim(),
        description:   v.description?.trim() ?? '',
        permissionIds,
      }).subscribe({
        next: () => {
          this.saving.set(false);
          this.showModal.set(false);
          this.editingId.set(null);
          this.notify('success', `Role updated successfully.`);
          this.svc.loadRoles();
        },
        error: err => {
          this.saving.set(false);
          this.modalError.set(err?.error?.error ?? err?.error?.detail ?? 'Failed to update role.');
        },
      });
    } else {
      this.svc.createRole({
        name:          v.name!.trim(),
        description:   v.description?.trim() ?? '',
        permissionIds,
      }).subscribe({
        next: () => {
          this.saving.set(false);
          this.showModal.set(false);
          this.notify('success', `Role "${v.name}" created.`);
          this.svc.loadRoles();
        },
        error: err => {
          this.saving.set(false);
          this.modalError.set(err?.error?.error ?? err?.error?.detail ?? 'Failed to create role.');
        },
      });
    }
  }

  // ── Delete confirm ────────────────────────────────────────────────────────
  openDeleteConfirm(role: { id: string; name: string }): void {
    this.openMenuId.set(null);
    this.deletingRole.set(role);
    this.deleteError.set(null);
    this.showDeleteModal.set(true);
  }

  closeDeleteModal(): void {
    if (this.deleting()) return;
    this.showDeleteModal.set(false);
    this.deletingRole.set(null);
  }

  confirmDelete(): void {
    const role = this.deletingRole();
    if (!role) return;

    this.deleting.set(true);
    this.deleteError.set(null);

    this.svc.deleteRole(role.id).subscribe({
      next: () => {
        this.deleting.set(false);
        this.showDeleteModal.set(false);
        this.deletingRole.set(null);
        this.notify('success', `Role "${role.name}" deleted.`);
        this.svc.loadRoles();
      },
      error: err => {
        this.deleting.set(false);
        this.deleteError.set(err?.error?.error ?? err?.error?.detail ?? 'Failed to delete role.');
      },
    });
  }

  // ── Menu toggle ────────────────────────────────────────────────────────────
  toggleMenu(id: string): void {
    this.openMenuId.update(cur => (cur === id ? null : id));
  }

  private notify(type: 'success' | 'error', message: string): void {
    this.notification.set({ type, message });
    setTimeout(() => this.notification.set(null), 4000);
  }
}
