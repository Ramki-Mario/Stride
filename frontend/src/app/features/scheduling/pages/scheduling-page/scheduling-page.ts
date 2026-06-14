import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { NgClass } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import cronstrue from 'cronstrue';

import { SchedulingService } from '../../services/scheduling.service';
import { ScheduleDefinitionSummaryDto } from '../../models/scheduling.models';

@Component({
  selector: 'app-scheduling-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgClass, FormsModule, ReactiveFormsModule],
  template: `
    <div class="sc-page">

      <!-- ── Header ─────────────────────────────────────────────────────── -->
      <div class="sc-header">
        <div>
          <h1 class="sc-title">Schedules</h1>
          <p class="sc-subtitle">
            @if (!svc.isLoading()) {
              {{ svc.schedules().length }} schedule{{ svc.schedules().length === 1 ? '' : 's' }} total
            } @else { Loading… }
          </p>
        </div>
        <button class="stride-btn stride-btn-primary" (click)="openCreateModal()">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" aria-hidden="true">
            <line x1="12" y1="5" x2="12" y2="19" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
            <line x1="5" y1="12" x2="19" y2="12" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
          </svg>
          New Schedule
        </button>
      </div>

      <!-- ── KPI cards ──────────────────────────────────────────────────── -->
      <div class="sc-kpis">
        <div class="sc-kpi sc-kpi--total">
          <span class="sc-kpi-label">Total</span>
          <span class="sc-kpi-value">{{ svc.schedules().length }}</span>
        </div>
        <div class="sc-kpi sc-kpi--active">
          <span class="sc-kpi-label">Active</span>
          <span class="sc-kpi-value">{{ svc.activeCount() }}</span>
        </div>
        <div class="sc-kpi sc-kpi--inactive">
          <span class="sc-kpi-label">Inactive</span>
          <span class="sc-kpi-value">{{ svc.inactiveCount() }}</span>
        </div>
      </div>

      <!-- ── Notification ───────────────────────────────────────────────── -->
      @if (notification()) {
        <div class="sc-notification" [class.sc-notification--error]="notification()!.type === 'error'">
          <span>{{ notification()!.message }}</span>
          <button class="sc-notif-close" (click)="notification.set(null)" aria-label="Dismiss">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none">
              <line x1="18" y1="6" x2="6" y2="18" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
              <line x1="6" y1="6" x2="18" y2="18" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
            </svg>
          </button>
        </div>
      }

      <!-- ── Loading skeleton ───────────────────────────────────────────── -->
      @if (svc.isLoading()) {
        <div class="sc-table-wrap">
          <table class="sc-table" aria-label="Loading schedules">
            <thead><tr>
              <th>Name</th><th>Cron</th><th>Status</th><th>Next Run</th><th>Created</th><th></th>
            </tr></thead>
            <tbody>
              @for (i of skeletons; track i) {
                <tr class="sk-row">
                  <td><div class="sk-line sk-line--name"></div></td>
                  <td><div class="sk-line sk-line--cron"></div></td>
                  <td><div class="sk-line sk-line--badge"></div></td>
                  <td><div class="sk-line sk-line--date"></div></td>
                  <td><div class="sk-line sk-line--date"></div></td>
                  <td></td>
                </tr>
              }
            </tbody>
          </table>
        </div>

      <!-- ── Error ──────────────────────────────────────────────────────── -->
      } @else if (svc.error()) {
        <div class="sc-empty">
          <svg width="40" height="40" viewBox="0 0 24 24" fill="none" aria-hidden="true">
            <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="1.5"/>
            <line x1="12" y1="8" x2="12" y2="12" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
            <line x1="12" y1="16" x2="12.01" y2="16" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
          </svg>
          <p>{{ svc.error() }}</p>
          <button class="stride-btn stride-btn-secondary" (click)="svc.loadSchedules()">Retry</button>
        </div>

      <!-- ── Empty ──────────────────────────────────────────────────────── -->
      } @else if (svc.isEmpty()) {
        <div class="sc-empty">
          <svg width="40" height="40" viewBox="0 0 24 24" fill="none" aria-hidden="true">
            <rect x="3" y="4" width="18" height="18" rx="2" stroke="currentColor" stroke-width="1.5"/>
            <line x1="3" y1="9" x2="21" y2="9" stroke="currentColor" stroke-width="1.5"/>
            <line x1="8" y1="2" x2="8" y2="6" stroke="currentColor" stroke-width="1.5" stroke-linecap="round"/>
            <line x1="16" y1="2" x2="16" y2="6" stroke="currentColor" stroke-width="1.5" stroke-linecap="round"/>
          </svg>
          <p>No schedules yet. Create your first schedule to get started.</p>
          <button class="stride-btn stride-btn-primary" (click)="openCreateModal()">New Schedule</button>
        </div>

      <!-- ── Table ──────────────────────────────────────────────────────── -->
      } @else {
        <div class="sc-table-wrap">
          <table class="sc-table" aria-label="Schedules">
            <thead>
              <tr>
                <th>Name</th>
                <th>Cron Expression</th>
                <th>Status</th>
                <th>Next Run</th>
                <th>Created</th>
                <th class="sc-th-actions"><span class="sr-only">Actions</span></th>
              </tr>
            </thead>
            <tbody>
              @for (schedule of svc.schedules(); track schedule.id) {
                <tr [class.sc-row--inactive]="!schedule.isActive">
                  <td class="sc-td-name">{{ schedule.name }}</td>
                  <td>
                    <span class="sc-cron-expr" title="{{ schedule.cronExpression }}">
                      {{ schedule.cronExpression }}
                    </span>
                    <span class="sc-cron-human">{{ describeCron(schedule.cronExpression) }}</span>
                  </td>
                  <td>
                    <span class="sc-badge" [ngClass]="schedule.isActive ? 'badge--active' : 'badge--inactive'">
                      {{ schedule.isActive ? 'Active' : 'Inactive' }}
                    </span>
                  </td>
                  <td class="sc-td-date">{{ schedule.nextRunAt ? formatDate(schedule.nextRunAt) : '—' }}</td>
                  <td class="sc-td-date">{{ formatDate(schedule.createdAt) }}</td>
                  <td class="sc-td-actions">
                    <div class="sc-menu-wrap">
                      <button class="sc-menu-trigger"
                              [class.sc-menu-trigger--open]="openMenuId() === schedule.id"
                              (click)="toggleMenu(schedule.id)"
                              aria-label="Schedule actions">
                        <svg width="16" height="16" viewBox="0 0 24 24" fill="none">
                          <circle cx="12" cy="5"  r="1.2" fill="currentColor"/>
                          <circle cx="12" cy="12" r="1.2" fill="currentColor"/>
                          <circle cx="12" cy="19" r="1.2" fill="currentColor"/>
                        </svg>
                      </button>
                      @if (openMenuId() === schedule.id) {
                        <div class="sc-menu" role="menu">
                          <button class="sc-menu-item" role="menuitem" (click)="openEditModal(schedule)">
                            Edit
                          </button>
                          @if (!schedule.isActive) {
                            <button class="sc-menu-item" role="menuitem" (click)="activateSchedule(schedule)">
                              Activate
                            </button>
                          }
                          @if (schedule.isActive) {
                            <button class="sc-menu-item" role="menuitem" (click)="deactivateSchedule(schedule)">
                              Deactivate
                            </button>
                          }
                          <button class="sc-menu-item sc-menu-item--danger" role="menuitem"
                                  (click)="deleteSchedule(schedule)">
                            Delete
                          </button>
                        </div>
                      }
                    </div>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </div>

    <!-- ═══════════════════════════════════════════════════════════════════ -->
    <!-- Create / Edit Schedule Modal                                        -->
    <!-- ═══════════════════════════════════════════════════════════════════ -->
    @if (showModal()) {
      <div class="modal-backdrop" (click)="closeModal()" role="presentation">
        <div class="modal" role="dialog" aria-modal="true"
             [attr.aria-labelledby]="editingSchedule() ? 'edit-modal-title' : 'create-modal-title'"
             (click)="$event.stopPropagation()">

          <div class="modal-header">
            @if (editingSchedule()) {
              <h2 class="modal-title" id="edit-modal-title">Edit Schedule</h2>
            } @else {
              <h2 class="modal-title" id="create-modal-title">New Schedule</h2>
            }
            <button class="modal-close" (click)="closeModal()" aria-label="Close">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none">
                <line x1="18" y1="6" x2="6"  y2="18" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
                <line x1="6"  y1="6" x2="18" y2="18" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
              </svg>
            </button>
          </div>

          <form class="modal-body" [formGroup]="scheduleForm" (ngSubmit)="submitModal()">

            <!-- Name -->
            <div class="form-field">
              <label class="form-label" for="sc-name">
                Name <span class="form-required">*</span>
              </label>
              <input id="sc-name" class="form-input" formControlName="name"
                     placeholder="Daily invoice run"
                     [class.form-input--error]="scheduleForm.controls.name.invalid && scheduleForm.controls.name.touched"/>
              @if (scheduleForm.controls.name.invalid && scheduleForm.controls.name.touched) {
                <p class="form-error">Name is required.</p>
              }
            </div>

            <!-- Description -->
            <div class="form-field">
              <label class="form-label" for="sc-desc">Description</label>
              <textarea id="sc-desc" class="form-input form-textarea" formControlName="description"
                        rows="2" placeholder="Optional description…"></textarea>
            </div>

            <!-- Workflow Definition ID -->
            <div class="form-field">
              <label class="form-label" for="sc-wf">
                Workflow Definition ID <span class="form-required">*</span>
              </label>
              <input id="sc-wf" class="form-input" formControlName="workflowDefinitionId"
                     placeholder="xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
                     [class.form-input--error]="scheduleForm.controls.workflowDefinitionId.invalid && scheduleForm.controls.workflowDefinitionId.touched"/>
              @if (scheduleForm.controls.workflowDefinitionId.invalid && scheduleForm.controls.workflowDefinitionId.touched) {
                <p class="form-error">A valid workflow definition ID (UUID) is required.</p>
              }
            </div>

            <!-- Cron Expression -->
            <div class="form-field">
              <label class="form-label" for="sc-cron">
                Cron Expression <span class="form-required">*</span>
              </label>
              <input id="sc-cron" class="form-input" formControlName="cronExpression"
                     placeholder="0 9 * * 1-5"
                     autocomplete="off"
                     [class.form-input--error]="scheduleForm.controls.cronExpression.invalid && scheduleForm.controls.cronExpression.touched"/>
              @if (cronPreview()) {
                <p class="form-hint">{{ cronPreview() }}</p>
              }
              @if (scheduleForm.controls.cronExpression.invalid && scheduleForm.controls.cronExpression.touched) {
                <p class="form-error">A valid 5-field cron expression is required.</p>
              }
            </div>

            <!-- Active toggle (create only) -->
            @if (!editingSchedule()) {
              <div class="form-field form-field--toggle">
                <label class="form-label" for="sc-active">Start active</label>
                <input id="sc-active" type="checkbox" class="form-toggle" formControlName="isActive"/>
              </div>
            }

            @if (modalError()) {
              <p class="form-error">{{ modalError() }}</p>
            }

            <div class="modal-footer">
              <button type="button" class="stride-btn stride-btn-secondary"
                      (click)="closeModal()" [disabled]="saving()">Cancel</button>
              <button type="submit" class="stride-btn stride-btn-primary"
                      [disabled]="scheduleForm.invalid || saving()">
                @if (saving()) {
                  @if (editingSchedule()) { Saving… } @else { Creating… }
                } @else {
                  @if (editingSchedule()) { Save Changes } @else { Create Schedule }
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
    .sc-page {
      max-width: 72rem;
      margin: 0 auto;
      padding: 1.5rem 1rem 3rem;
    }

    /* ── Header ─────────────────────────────────────────────────────────── */
    .sc-header {
      display: flex; align-items: flex-start; justify-content: space-between;
      gap: 1rem; margin-bottom: 1.25rem;
    }
    .sc-title {
      font-size: 1.375rem; font-weight: 700;
      color: var(--stride-text-primary); margin: 0 0 0.25rem;
    }
    .sc-subtitle { font-size: 0.875rem; color: var(--stride-text-muted); margin: 0; }

    /* ── KPI cards ──────────────────────────────────────────────────────── */
    .sc-kpis {
      display: grid; grid-template-columns: repeat(auto-fit, minmax(10rem, 1fr));
      gap: 0.875rem; margin-bottom: 1.25rem;
    }
    .sc-kpi {
      background: var(--stride-surface); border: 1px solid var(--stride-border-soft);
      border-radius: var(--stride-radius-lg); padding: 1rem 1.25rem;
      display: flex; flex-direction: column; gap: 0.25rem;
    }
    .sc-kpi-label {
      font-size: 0.75rem; font-weight: 600; text-transform: uppercase;
      letter-spacing: 0.05em; color: var(--stride-text-muted);
    }
    .sc-kpi-value { font-size: 1.5rem; font-weight: 700; color: var(--stride-text-primary); }
    .sc-kpi--total    { border-left: 3px solid var(--stride-primary); }
    .sc-kpi--active   { border-left: 3px solid #10B981; }
    .sc-kpi--inactive { border-left: 3px solid #6B7280; }

    /* ── Notification ────────────────────────────────────────────────────── */
    .sc-notification {
      display: flex; align-items: center; justify-content: space-between;
      gap: 0.75rem; padding: 0.75rem 1rem; margin-bottom: 1rem;
      border-radius: var(--stride-radius-md);
      background: color-mix(in srgb, #10B981 10%, var(--stride-surface));
      border: 1px solid color-mix(in srgb, #10B981 30%, transparent);
      color: #059669; font-size: 0.875rem;
    }
    .sc-notification--error {
      background: color-mix(in srgb, #EF4444 10%, var(--stride-surface));
      border-color: color-mix(in srgb, #EF4444 30%, transparent);
      color: #DC2626;
    }
    .sc-notif-close {
      background: none; border: none; cursor: pointer; color: inherit;
      padding: 0.125rem; display: flex; align-items: center; opacity: 0.7;
    }
    .sc-notif-close:hover { opacity: 1; }

    /* ── Table ───────────────────────────────────────────────────────────── */
    .sc-table-wrap {
      border: 1px solid var(--stride-border-soft);
      border-radius: var(--stride-radius-lg);
      overflow: visible;
      background: var(--stride-surface);
    }
    .sc-table {
      width: 100%; border-collapse: collapse; font-size: 0.875rem;
    }
    .sc-table thead tr {
      background: var(--stride-surface-secondary);
      border-bottom: 1px solid var(--stride-border-soft);
    }
    .sc-table thead tr th:first-child { border-top-left-radius: var(--stride-radius-lg); }
    .sc-table thead tr th:last-child  { border-top-right-radius: var(--stride-radius-lg); }
    .sc-table th {
      padding: 0.625rem 1rem; text-align: left;
      font-size: 0.75rem; font-weight: 600; text-transform: uppercase;
      letter-spacing: 0.05em; color: var(--stride-text-muted); white-space: nowrap;
    }
    .sc-th-actions { width: 3rem; }
    .sc-table tbody tr {
      border-bottom: 1px solid var(--stride-border-soft); transition: background 120ms;
    }
    .sc-table tbody tr:last-child { border-bottom: none; }
    .sc-table tbody tr:hover { background: var(--stride-surface-hover); }
    .sc-row--inactive { opacity: 0.6; }
    .sc-table td { padding: 0.875rem 1rem; vertical-align: middle; color: var(--stride-text-primary); }

    .sc-td-name { font-weight: 600; }
    .sc-td-date { color: var(--stride-text-muted); font-size: 0.8125rem; white-space: nowrap; }
    .sc-cron-expr {
      font-family: monospace; font-size: 0.8125rem;
      color: var(--stride-text-secondary); display: block;
    }
    .sc-cron-human {
      font-size: 0.75rem; color: var(--stride-text-muted);
      display: block; margin-top: 0.125rem;
    }

    /* ── Badges ──────────────────────────────────────────────────────────── */
    .sc-badge {
      display: inline-block; font-size: 0.6875rem; font-weight: 600;
      padding: 0.2rem 0.55rem; border-radius: 99px;
      text-transform: uppercase; letter-spacing: 0.04em; white-space: nowrap;
    }
    .badge--active   { background: color-mix(in srgb, #10B981 12%, transparent); color: #059669; }
    .badge--inactive { background: color-mix(in srgb, #6B7280 12%, transparent); color: #6B7280; }

    /* ── Action menu ──────────────────────────────────────────────────────── */
    .sc-td-actions { width: 3rem; text-align: center; }
    .sc-menu-wrap  { position: relative; display: inline-block; }
    .sc-menu-trigger {
      width: 2rem; height: 2rem; border-radius: var(--stride-radius-md);
      display: flex; align-items: center; justify-content: center;
      border: 1px solid transparent; background: transparent;
      color: var(--stride-text-muted); cursor: pointer; transition: all 120ms;
    }
    .sc-menu-trigger:hover, .sc-menu-trigger--open {
      background: var(--stride-surface-secondary);
      border-color: var(--stride-border-soft); color: var(--stride-text-primary);
    }
    .sc-menu {
      position: absolute; right: 0; top: calc(100% + 0.25rem);
      background: var(--stride-surface); border: 1px solid var(--stride-border-soft);
      border-radius: var(--stride-radius-md); box-shadow: var(--stride-shadow-lg);
      min-width: 9rem; z-index: 50; padding: 0.25rem 0;
    }
    .sc-menu-item {
      display: block; width: 100%; text-align: left; padding: 0.5rem 0.875rem;
      font-size: 0.875rem; font-family: inherit;
      background: none; border: none; color: var(--stride-text-primary);
      cursor: pointer; transition: background 100ms;
    }
    .sc-menu-item:hover { background: var(--stride-surface-hover); }
    .sc-menu-item--danger { color: #EF4444; }
    .sc-menu-item--danger:hover { background: color-mix(in srgb, #EF4444 8%, transparent); }

    /* ── Empty / error ─────────────────────────────────────────────────────── */
    .sc-empty {
      display: flex; flex-direction: column; align-items: center;
      gap: 0.75rem; padding: 4rem 1rem;
      color: var(--stride-text-muted); font-size: 0.9375rem; text-align: center;
    }

    /* ── Skeleton ──────────────────────────────────────────────────────────── */
    .sk-row { animation: pulse 1.5s ease-in-out infinite; }
    .sk-line { border-radius: 4px; background: var(--stride-border); height: 0.75rem; }
    .sk-line--name { width: 9rem; }
    .sk-line--cron { width: 8rem; }
    .sk-line--badge { width: 4rem; }
    .sk-line--date { width: 5rem; }
    @keyframes pulse { 0%, 100% { opacity: 1; } 50% { opacity: 0.5; } }

    /* ── Modal ─────────────────────────────────────────────────────────────── */
    .modal-backdrop {
      position: fixed; inset: 0; background: rgba(0,0,0,0.45);
      display: flex; align-items: center; justify-content: center;
      z-index: 100; padding: 1rem;
    }
    .modal {
      background: var(--stride-surface); border-radius: var(--stride-radius-xl);
      box-shadow: var(--stride-shadow-xl); width: 100%; max-width: 34rem; overflow: hidden;
    }
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
      padding: 1.25rem 1.5rem;
      display: flex; flex-direction: column; gap: 0.875rem;
      max-height: 75vh; overflow-y: auto;
    }
    .modal-footer { display: flex; justify-content: flex-end; gap: 0.625rem; padding-top: 0.5rem; }

    /* ── Form ───────────────────────────────────────────────────────────────── */
    .form-field { display: flex; flex-direction: column; gap: 0.375rem; }
    .form-field--toggle { flex-direction: row; align-items: center; gap: 0.75rem; }
    .form-label { font-size: 0.875rem; font-weight: 500; color: var(--stride-text-secondary); }
    .form-required { color: #EF4444; }
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
    .form-textarea { resize: vertical; min-height: 4rem; }
    .form-toggle { width: 1.125rem; height: 1.125rem; cursor: pointer; }
    .form-hint { font-size: 0.8125rem; color: var(--stride-text-muted); margin: 0; }
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
export class SchedulingPageComponent implements OnInit {
  readonly svc        = inject(SchedulingService);
  private readonly fb = inject(FormBuilder);

  readonly skeletons = [1, 2, 3, 4, 5];

  readonly notification   = signal<{ type: 'success' | 'error'; message: string } | null>(null);
  readonly openMenuId     = signal<string | null>(null);
  readonly showModal      = signal(false);
  readonly editingSchedule = signal<ScheduleDefinitionSummaryDto | null>(null);
  readonly saving         = signal(false);
  readonly modalError     = signal<string | null>(null);
  readonly cronPreview    = signal<string | null>(null);

  readonly scheduleForm = this.fb.group({
    name:                 ['', Validators.required],
    description:          [''],
    workflowDefinitionId: ['', [Validators.required, Validators.pattern(
      /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i
    )]],
    cronExpression: ['', Validators.required],
    isActive:       [true],
  });

  ngOnInit(): void {
    this.scheduleForm.controls.cronExpression.valueChanges.subscribe(v => {
      if (!v) { this.cronPreview.set(null); return; }
      try {
        this.cronPreview.set(cronstrue.toString(v));
      } catch {
        this.cronPreview.set(null);
      }
    });

    this.svc.loadSchedules();
  }

  openCreateModal(): void {
    this.editingSchedule.set(null);
    this.scheduleForm.reset({ isActive: true });
    this.cronPreview.set(null);
    this.modalError.set(null);
    this.openMenuId.set(null);
    this.showModal.set(true);
  }

  openEditModal(schedule: ScheduleDefinitionSummaryDto): void {
    this.openMenuId.set(null);
    this.modalError.set(null);

    this.svc.getScheduleById(schedule.id).subscribe({
      next: detail => {
        this.editingSchedule.set(schedule);
        this.scheduleForm.reset({
          name:                 detail.name,
          description:          detail.description ?? '',
          workflowDefinitionId: detail.workflowDefinitionId,
          cronExpression:       detail.cronExpression,
          isActive:             detail.isActive,
        });
        try {
          this.cronPreview.set(cronstrue.toString(detail.cronExpression));
        } catch {
          this.cronPreview.set(null);
        }
        this.showModal.set(true);
      },
      error: () => this.notify('error', 'Failed to load schedule details.'),
    });
  }

  closeModal(): void {
    if (this.saving()) return;
    this.showModal.set(false);
    this.editingSchedule.set(null);
  }

  submitModal(): void {
    if (this.scheduleForm.invalid) { this.scheduleForm.markAllAsTouched(); return; }

    const v        = this.scheduleForm.getRawValue();
    this.saving.set(true);
    this.modalError.set(null);

    const editing = this.editingSchedule();

    if (editing) {
      this.svc.updateSchedule(editing.id, {
        name:                 v.name!.trim(),
        description:          v.description?.trim() || null,
        workflowDefinitionId: v.workflowDefinitionId!.trim(),
        cronExpression:       v.cronExpression!.trim(),
      }).subscribe({
        next: () => {
          this.saving.set(false);
          this.showModal.set(false);
          this.editingSchedule.set(null);
          this.notify('success', `"${v.name}" updated.`);
          this.svc.loadSchedules();
        },
        error: err => {
          this.saving.set(false);
          this.modalError.set(err?.error?.detail ?? 'Failed to update schedule.');
        },
      });
    } else {
      this.svc.createSchedule({
        name:                 v.name!.trim(),
        description:          v.description?.trim() || null,
        workflowDefinitionId: v.workflowDefinitionId!.trim(),
        cronExpression:       v.cronExpression!.trim(),
        isActive:             v.isActive ?? true,
      }).subscribe({
        next: () => {
          this.saving.set(false);
          this.showModal.set(false);
          this.notify('success', `"${v.name}" created.`);
          this.svc.loadSchedules();
        },
        error: err => {
          this.saving.set(false);
          this.modalError.set(err?.error?.detail ?? 'Failed to create schedule.');
        },
      });
    }
  }

  activateSchedule(schedule: ScheduleDefinitionSummaryDto): void {
    this.openMenuId.set(null);
    this.svc.activateSchedule(schedule.id).subscribe({
      next: () => { this.notify('success', `"${schedule.name}" activated.`); this.svc.loadSchedules(); },
      error: err => this.notify('error', err?.error?.detail ?? 'Failed to activate schedule.'),
    });
  }

  deactivateSchedule(schedule: ScheduleDefinitionSummaryDto): void {
    this.openMenuId.set(null);
    this.svc.deactivateSchedule(schedule.id).subscribe({
      next: () => { this.notify('success', `"${schedule.name}" deactivated.`); this.svc.loadSchedules(); },
      error: err => this.notify('error', err?.error?.detail ?? 'Failed to deactivate schedule.'),
    });
  }

  deleteSchedule(schedule: ScheduleDefinitionSummaryDto): void {
    this.openMenuId.set(null);
    this.svc.deleteSchedule(schedule.id).subscribe({
      next: () => { this.notify('success', `"${schedule.name}" deleted.`); this.svc.loadSchedules(); },
      error: err => this.notify('error', err?.error?.detail ?? 'Failed to delete schedule.'),
    });
  }

  toggleMenu(id: string): void {
    this.openMenuId.update(cur => (cur === id ? null : id));
  }

  describeCron(expression: string): string {
    try {
      return cronstrue.toString(expression);
    } catch {
      return '';
    }
  }

  formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString('en-GB', {
      day: 'numeric', month: 'short', year: 'numeric',
    });
  }

  private notify(type: 'success' | 'error', message: string): void {
    this.notification.set({ type, message });
  }
}
