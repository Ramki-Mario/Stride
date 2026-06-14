import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  inject,
  signal,
  computed,
} from '@angular/core';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { AuthService } from '../../core/auth/auth.service';
import { RoleService } from '../../features/administration/services/role.service';
import { AdminService } from '../../features/administration/services/admin.service';
import { TenantSettingsService } from '../../features/administration/services/tenant-settings.service';
import { PermissionDto } from '../../features/administration/models/role.models';

// ── Constants ──────────────────────────────────────────────────────────────────

const STORAGE_KEY = 'stride:onboarding:step';

const PERMISSION_LABELS: Record<string, string> = {
  'workflow.view':             'View Workflows',
  'workflow.create':           'Create & Edit Workflows',
  'workflow.activate':         'Activate / Archive Workflows',
  'workflow.run':              'Start Workflow Instances',
  'workflow.manage_instances': 'Manage All Instances',
  'user.invite':               'Invite Users',
  'user.manage':               'Manage Users & Roles',
  'role.view':                 'View Role Catalog',
  'role.manage':               'Create, Edit & Delete Roles',
  'tenant.settings':           'Edit Tenant Settings',
};

const PERMISSION_GROUPS = [
  { label: 'Workflows', keys: ['workflow.view','workflow.create','workflow.activate','workflow.run','workflow.manage_instances'] },
  { label: 'Users',     keys: ['user.invite','user.manage'] },
  { label: 'Roles',     keys: ['role.view','role.manage'] },
  { label: 'Tenant',    keys: ['tenant.settings'] },
];

const DEFAULT_PERMISSIONS = ['workflow.view','workflow.create','workflow.run'];

interface RoleRow { name: string; description: string; permissionIds: string[]; saved: boolean; }
interface InviteRow { email: string; roleId: string; }

@Component({
  selector: 'app-onboarding-wizard',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, ReactiveFormsModule],
  template: `
    <!-- ── Backdrop ───────────────────────────────────────────────────────── -->
    <div class="owiz-backdrop" aria-hidden="true"></div>

    <!-- ── Panel ──────────────────────────────────────────────────────────── -->
    <div class="owiz-panel" role="dialog" aria-modal="true" aria-labelledby="owiz-title">

      <!-- ── Nav bar ──────────────────────────────────────────────────────── -->
      <div class="owiz-nav">
        @for (step of steps; track step.num) {
          <div class="owiz-step"
               [class.owiz-step--active]="currentStep() === step.num"
               [class.owiz-step--done]="currentStep() > step.num">
            <span class="owiz-step-dot"></span>
            <span class="owiz-step-label">{{ step.label }}</span>
          </div>
        }
      </div>

      <!-- ── Content ──────────────────────────────────────────────────────── -->
      <div class="owiz-body">

        <!-- ── Step 1: Welcome ──────────────────────────────────────────── -->
        @if (currentStep() === 1) {
          <div class="owiz-welcome">
            <div class="owiz-logo" aria-hidden="true">
              <svg width="48" height="48" viewBox="0 0 48 48" fill="none">
                <circle cx="24" cy="24" r="24" fill="var(--stride-primary)"/>
                <path d="M14 17h20M14 24h14M14 31h8" stroke="#fff" stroke-width="2.5" stroke-linecap="round"/>
              </svg>
            </div>
            <h1 id="owiz-title" class="owiz-title">Welcome to STRIDE</h1>
            <p class="owiz-subtitle">
              Let's get your workspace set up in a few quick steps.
            </p>

            <div class="owiz-concepts">
              <div class="owiz-concept owiz-concept--wf">
                <span class="owiz-concept-icon pi pi-sitemap"></span>
                <strong>Workflows</strong>
                <p>Define the steps that make up any business process.</p>
              </div>
              <div class="owiz-concept owiz-concept--inst">
                <span class="owiz-concept-icon pi pi-play-circle"></span>
                <strong>Instances</strong>
                <p>Run a workflow for a specific job, client, or task.</p>
              </div>
              <div class="owiz-concept owiz-concept--team">
                <span class="owiz-concept-icon pi pi-users"></span>
                <strong>Team</strong>
                <p>Assign steps to colleagues — each with the right permissions.</p>
              </div>
            </div>
          </div>
        }

        <!-- ── Step 2: Create Roles ─────────────────────────────────────── -->
        @if (currentStep() === 2) {
          <div class="owiz-roles">
            <h2 id="owiz-title" class="owiz-step-title">Create Your Roles</h2>
            <p class="owiz-step-desc">
              Set up at least one custom role for your team members. You can always
              refine roles later from the Administration → Roles page.
            </p>

            @if (roleError()) {
              <div class="owiz-error">{{ roleError() }}</div>
            }

            <!-- Role form -->
            <div class="owiz-role-form">
              <div class="owiz-field">
                <label class="owiz-label">Role name</label>
                <input class="owiz-input" type="text" [(ngModel)]="roleName"
                       placeholder="e.g. Field Technician" maxlength="100"/>
              </div>
              <div class="owiz-field">
                <label class="owiz-label">Description (optional)</label>
                <input class="owiz-input" type="text" [(ngModel)]="roleDesc"
                       placeholder="Brief description of this role" maxlength="500"/>
              </div>

              <div class="owiz-perm-groups">
                @for (group of permissionGroups; track group.label) {
                  <div class="owiz-perm-group">
                    <span class="owiz-perm-group-label">{{ group.label }}</span>
                    <div class="owiz-perm-checks">
                      @for (key of group.keys; track key) {
                        <label class="owiz-check-label">
                          <input type="checkbox"
                                 [checked]="isPermSelected(key)"
                                 (change)="togglePerm(key)"
                                 class="owiz-checkbox"/>
                          {{ permLabel(key) }}
                        </label>
                      }
                    </div>
                  </div>
                }
              </div>

              <button class="stride-btn stride-btn-primary owiz-add-role-btn"
                      [disabled]="!roleName.trim() || isSavingRole()"
                      (click)="saveRole()">
                @if (isSavingRole()) { Saving… } @else { + Add Role }
              </button>
            </div>

            <!-- Saved roles -->
            @if (savedRoles().length > 0) {
              <div class="owiz-saved-roles">
                <span class="owiz-saved-label">Added this session:</span>
                @for (r of savedRoles(); track r.name) {
                  <span class="owiz-saved-chip">{{ r.name }}</span>
                }
              </div>
            }
          </div>
        }

        <!-- ── Step 3: Invite Team ───────────────────────────────────────── -->
        @if (currentStep() === 3) {
          <div class="owiz-invite">
            <h2 id="owiz-title" class="owiz-step-title">Invite Your Team</h2>
            <p class="owiz-step-desc">
              Add email addresses and assign a role. Everyone will receive an
              invite link. You can skip this and invite from User Management later.
            </p>

            @if (inviteError()) {
              <div class="owiz-error">{{ inviteError() }}</div>
            }

            <div class="owiz-invite-rows">
              @for (row of inviteRows(); track $index; let i = $index) {
                <div class="owiz-invite-row">
                  <input class="owiz-input owiz-input--email" type="email"
                         [ngModel]="row.email"
                         (ngModelChange)="updateInviteEmail(i, $event)"
                         placeholder="colleague@example.com"/>
                  <select class="owiz-select"
                          [ngModel]="row.roleId"
                          (ngModelChange)="updateInviteRole(i, $event)">
                    <option value="">— role —</option>
                    @for (r of availableRoles(); track r.id) {
                      <option [value]="r.id">{{ r.name }}</option>
                    }
                  </select>
                  @if (inviteRows().length > 1) {
                    <button class="owiz-rm-btn" (click)="removeInviteRow(i)"
                            aria-label="Remove">×</button>
                  }
                </div>
              }
            </div>

            @if (inviteRows().length < 5) {
              <button class="owiz-add-link" (click)="addInviteRow()">+ Add another</button>
            }
          </div>
        }

        <!-- ── Step 4: Done ──────────────────────────────────────────────── -->
        @if (currentStep() === 4) {
          <div class="owiz-done">
            <div class="owiz-done-icon" aria-hidden="true">
              <svg width="64" height="64" viewBox="0 0 64 64" fill="none">
                <circle cx="32" cy="32" r="32" fill="var(--stride-success-light, #d1fae5)"/>
                <path d="M20 33l9 9 16-18" stroke="var(--stride-success, #10b981)"
                      stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/>
              </svg>
            </div>
            <h2 id="owiz-title" class="owiz-step-title owiz-done-title">You're all set!</h2>
            <p class="owiz-step-desc">
              Your workspace is configured. You can revisit this wizard any time
              from <strong>Administration → Settings</strong>.
            </p>
            @if (skippedAny()) {
              <p class="owiz-skipped-note">
                <span class="pi pi-info-circle"></span>
                Some steps were skipped — complete them from Administration when ready.
              </p>
            }
          </div>
        }

      </div>

      <!-- ── Footer ───────────────────────────────────────────────────────── -->
      <div class="owiz-footer">
        <div class="owiz-footer-left">
          @if (currentStep() > 1 && currentStep() < 4) {
            <button class="stride-btn stride-btn-secondary" (click)="back()">Back</button>
          }
        </div>
        <div class="owiz-footer-right">
          @if (canSkip()) {
            <button class="owiz-skip-btn" (click)="skip()">Skip for now</button>
          }
          @if (currentStep() === 1) {
            <button class="stride-btn stride-btn-primary" (click)="next()">Get Started →</button>
          } @else if (currentStep() === 2) {
            <button class="stride-btn stride-btn-primary"
                    [disabled]="savedRoles().length === 0 && !skippedStep2()"
                    (click)="next()">Next →</button>
          } @else if (currentStep() === 3) {
            <button class="stride-btn stride-btn-primary"
                    [disabled]="isSendingInvites()"
                    (click)="sendInvitesAndNext()">
              @if (isSendingInvites()) { Sending… } @else { Send Invites & Finish }
            </button>
          } @else if (currentStep() === 4) {
            <button class="stride-btn stride-btn-primary" (click)="finish()">Go to Dashboard</button>
          }
        </div>
      </div>

    </div>
  `,
  styles: [`
    .owiz-backdrop {
      position: fixed; inset: 0;
      background: rgba(0,0,0,.55);
      backdrop-filter: blur(4px);
      z-index: 1000;
      animation: owiz-fade-in .2s ease;
    }
    .owiz-panel {
      position: fixed;
      inset: 0;
      z-index: 1001;
      display: flex;
      flex-direction: column;
      max-width: 680px;
      max-height: 90dvh;
      margin: auto;
      background: var(--stride-surface, #fff);
      border-radius: 16px;
      overflow: hidden;
      box-shadow: 0 24px 80px rgba(0,0,0,.25);
      animation: owiz-slide-up .3s cubic-bezier(.16,1,.3,1);
    }
    @keyframes owiz-fade-in { from { opacity: 0 } }
    @keyframes owiz-slide-up { from { transform: translateY(40px); opacity: 0 } }

    /* Nav */
    .owiz-nav {
      display: flex; align-items: center; gap: 0;
      padding: 1rem 1.5rem;
      border-bottom: 1px solid var(--stride-border);
      background: var(--stride-surface-alt, #f8f9ff);
    }
    .owiz-step {
      display: flex; align-items: center; gap: .5rem;
      flex: 1; font-size: .8rem; color: var(--stride-text-muted);
      transition: color .2s;
    }
    .owiz-step--active { color: var(--stride-primary); font-weight: 600; }
    .owiz-step--done   { color: var(--stride-success, #10b981); }
    .owiz-step-dot {
      width: 8px; height: 8px; border-radius: 50%;
      background: currentColor;
      flex-shrink: 0;
    }
    .owiz-step--active .owiz-step-dot { width: 10px; height: 10px; }

    /* Body */
    .owiz-body { flex: 1; overflow-y: auto; padding: 2rem 1.75rem; }

    /* Welcome */
    .owiz-welcome { text-align: center; }
    .owiz-logo { margin-bottom: 1.25rem; }
    .owiz-title { font-size: 1.6rem; font-weight: 700; color: var(--stride-text); margin: 0 0 .5rem; }
    .owiz-subtitle { color: var(--stride-text-muted); margin-bottom: 2rem; }
    .owiz-concepts {
      display: grid; grid-template-columns: repeat(3, 1fr); gap: 1rem;
      text-align: left;
    }
    @media (max-width: 640px) { .owiz-concepts { grid-template-columns: 1fr; } }
    .owiz-concept {
      padding: 1rem; border-radius: 10px;
      border: 1.5px solid var(--stride-border);
      background: var(--stride-surface-alt, #f8f9ff);
    }
    .owiz-concept-icon {
      display: block; font-size: 1.5rem;
      color: var(--stride-primary); margin-bottom: .5rem;
    }
    .owiz-concept strong { display: block; margin-bottom: .25rem; color: var(--stride-text); }
    .owiz-concept p { margin: 0; font-size: .85rem; color: var(--stride-text-muted); }

    /* Shared step */
    .owiz-step-title { font-size: 1.35rem; font-weight: 700; color: var(--stride-text); margin: 0 0 .4rem; }
    .owiz-step-desc  { color: var(--stride-text-muted); margin-bottom: 1.5rem; font-size: .9rem; }
    .owiz-error { background: var(--stride-error-light, #fee2e2); color: var(--stride-error, #dc2626);
                  padding: .6rem .9rem; border-radius: 8px; margin-bottom: 1rem; font-size: .85rem; }

    /* Role form */
    .owiz-field { margin-bottom: .9rem; }
    .owiz-label { display: block; font-size: .82rem; font-weight: 600; color: var(--stride-text-muted); margin-bottom: .3rem; }
    .owiz-input {
      width: 100%; padding: .55rem .75rem; border-radius: 8px;
      border: 1.5px solid var(--stride-border); background: var(--stride-input-bg, #fff);
      color: var(--stride-text); font-size: .9rem; outline: none;
    }
    .owiz-input:focus { border-color: var(--stride-primary); }
    .owiz-perm-groups { display: flex; flex-direction: column; gap: .75rem; margin-bottom: 1rem; }
    .owiz-perm-group-label { font-size: .78rem; font-weight: 700; text-transform: uppercase;
                             letter-spacing: .05em; color: var(--stride-text-muted); }
    .owiz-perm-checks { display: flex; flex-wrap: wrap; gap: .4rem .75rem; margin-top: .3rem; }
    .owiz-check-label { display: flex; align-items: center; gap: .35rem; font-size: .85rem;
                        cursor: pointer; color: var(--stride-text); }
    .owiz-checkbox { accent-color: var(--stride-primary); cursor: pointer; }
    .owiz-add-role-btn { width: 100%; justify-content: center; }
    .owiz-saved-roles { display: flex; flex-wrap: wrap; gap: .4rem; margin-top: 1rem; align-items: center; }
    .owiz-saved-label { font-size: .8rem; color: var(--stride-text-muted); }
    .owiz-saved-chip {
      background: var(--stride-primary-light, #ede9fe); color: var(--stride-primary);
      border-radius: 999px; padding: .2rem .65rem; font-size: .8rem; font-weight: 600;
    }

    /* Invite */
    .owiz-invite-rows { display: flex; flex-direction: column; gap: .6rem; margin-bottom: .75rem; }
    .owiz-invite-row  { display: flex; gap: .5rem; align-items: center; }
    .owiz-input--email { flex: 1; min-width: 0; }
    .owiz-select {
      padding: .55rem .6rem; border-radius: 8px; border: 1.5px solid var(--stride-border);
      background: var(--stride-input-bg, #fff); color: var(--stride-text);
      font-size: .9rem; cursor: pointer; outline: none;
    }
    .owiz-select:focus { border-color: var(--stride-primary); }
    .owiz-rm-btn {
      flex-shrink: 0; background: none; border: none; cursor: pointer;
      color: var(--stride-text-muted); font-size: 1.25rem; line-height: 1;
    }
    .owiz-add-link { background: none; border: none; cursor: pointer;
                     color: var(--stride-primary); font-size: .88rem; padding: 0; }

    /* Done */
    .owiz-done { text-align: center; padding-top: 1rem; }
    .owiz-done-icon { margin-bottom: 1.25rem; }
    .owiz-done-title { font-size: 1.6rem; }
    .owiz-skipped-note { font-size: .85rem; color: var(--stride-text-muted); margin-top: .75rem; }

    /* Footer */
    .owiz-footer {
      display: flex; justify-content: space-between; align-items: center;
      padding: 1rem 1.75rem;
      border-top: 1px solid var(--stride-border);
      background: var(--stride-surface-alt, #f8f9ff);
    }
    .owiz-footer-left, .owiz-footer-right { display: flex; gap: .75rem; align-items: center; }
    .owiz-skip-btn {
      background: none; border: none; cursor: pointer;
      color: var(--stride-text-muted); font-size: .88rem;
    }
    .owiz-skip-btn:hover { color: var(--stride-text); }
  `],
})
export class OnboardingWizardComponent implements OnInit {
  private readonly auth         = inject(AuthService);
  private readonly roleSvc      = inject(RoleService);
  private readonly adminSvc     = inject(AdminService);
  private readonly settingsSvc  = inject(TenantSettingsService);
  private readonly router       = inject(Router);

  readonly steps = [
    { num: 1, label: 'Welcome'      },
    { num: 2, label: 'Roles'        },
    { num: 3, label: 'Invite Team'  },
    { num: 4, label: 'Done'         },
  ];

  readonly permissionGroups = PERMISSION_GROUPS;

  // ── Step state ─────────────────────────────────────────────────────────────
  readonly currentStep = signal<number>(1);

  readonly skippedStep2 = signal(false);
  readonly skippedStep3 = signal(false);
  readonly skippedAny   = computed(() => this.skippedStep2() || this.skippedStep3());

  readonly canSkip = computed(() => this.currentStep() === 2 || this.currentStep() === 3);

  // ── Role form state ────────────────────────────────────────────────────────
  roleName = '';
  roleDesc = '';

  private permissions = signal<PermissionDto[]>([]);
  readonly selectedPermIds = signal<Set<string>>(new Set([]));
  readonly savedRoles      = signal<RoleRow[]>([]);
  readonly isSavingRole    = signal(false);
  readonly roleError       = signal<string | null>(null);

  // ── Invite state ───────────────────────────────────────────────────────────
  readonly inviteRows = signal<InviteRow[]>([{ email: '', roleId: '' }]);
  readonly isSendingInvites = signal(false);
  readonly inviteError      = signal<string | null>(null);
  readonly availableRoles   = computed(() => this.roleSvc.roles());

  // ── Init ───────────────────────────────────────────────────────────────────

  ngOnInit(): void {
    const saved = localStorage.getItem(STORAGE_KEY);
    if (saved) {
      const n = parseInt(saved, 10);
      if (n >= 1 && n <= 4) this.currentStep.set(n);
    }

    this.roleSvc.loadRoles();
    this.roleSvc.getPermissions().subscribe(perms => {
      this.permissions.set(perms);
      // Pre-select sensible defaults
      const defaults = new Set(
        perms.filter(p => DEFAULT_PERMISSIONS.includes(p.key)).map(p => p.id),
      );
      this.selectedPermIds.set(defaults);
    });
  }

  // ── Navigation ─────────────────────────────────────────────────────────────

  next(): void {
    const n = Math.min(this.currentStep() + 1, 4);
    this.currentStep.set(n);
    localStorage.setItem(STORAGE_KEY, String(n));
  }

  back(): void {
    const n = Math.max(this.currentStep() - 1, 1);
    this.currentStep.set(n);
    localStorage.setItem(STORAGE_KEY, String(n));
  }

  skip(): void {
    if (this.currentStep() === 2) this.skippedStep2.set(true);
    if (this.currentStep() === 3) this.skippedStep3.set(true);
    this.next();
  }

  finish(): void {
    localStorage.removeItem(STORAGE_KEY);
    this.settingsSvc.completeOnboarding().pipe(
      catchError(() => of(null)),
    ).subscribe(() => {
      this.auth.markOnboardingComplete();
      this.router.navigate(['/dashboard']);
    });
  }

  // ── Role form ──────────────────────────────────────────────────────────────

  isPermSelected(key: string): boolean {
    const perm = this.permissions().find(p => p.key === key);
    return !!perm && this.selectedPermIds().has(perm.id);
  }

  togglePerm(key: string): void {
    const perm = this.permissions().find(p => p.key === key);
    if (!perm) return;
    this.selectedPermIds.update(set => {
      const next = new Set(set);
      next.has(perm.id) ? next.delete(perm.id) : next.add(perm.id);
      return next;
    });
  }

  permLabel(key: string): string {
    return PERMISSION_LABELS[key] ?? key;
  }

  saveRole(): void {
    const name = this.roleName.trim();
    if (!name) return;
    this.isSavingRole.set(true);
    this.roleError.set(null);

    this.roleSvc.createRole({
      name,
      description:   this.roleDesc.trim(),
      permissionIds: [...this.selectedPermIds()],
    }).subscribe({
      next: () => {
        this.savedRoles.update(r => [...r, { name, description: this.roleDesc.trim(), permissionIds: [...this.selectedPermIds()], saved: true }]);
        this.roleSvc.loadRoles();
        this.roleName = '';
        this.roleDesc = '';
        this.isSavingRole.set(false);
      },
      error: () => {
        this.roleError.set('Failed to create role. Please check the name and try again.');
        this.isSavingRole.set(false);
      },
    });
  }

  // ── Invite form ────────────────────────────────────────────────────────────

  updateInviteEmail(index: number, email: string): void {
    this.inviteRows.update(rows => rows.map((r, i) => i === index ? { ...r, email } : r));
  }

  updateInviteRole(index: number, roleId: string): void {
    this.inviteRows.update(rows => rows.map((r, i) => i === index ? { ...r, roleId } : r));
  }

  addInviteRow(): void {
    if (this.inviteRows().length < 5)
      this.inviteRows.update(r => [...r, { email: '', roleId: '' }]);
  }

  removeInviteRow(index: number): void {
    this.inviteRows.update(rows => rows.filter((_, i) => i !== index));
  }

  sendInvitesAndNext(): void {
    const valid = this.inviteRows().filter(r => r.email.trim());
    if (valid.length === 0) { this.next(); return; }

    this.isSendingInvites.set(true);
    this.inviteError.set(null);

    const calls = valid.map(r =>
      this.adminSvc.inviteUser({ email: r.email.trim(), displayName: '', roleId: r.roleId || undefined })
        .pipe(catchError(() => of(null))),
    );

    forkJoin(calls).subscribe(() => {
      this.isSendingInvites.set(false);
      this.next();
    });
  }
}
