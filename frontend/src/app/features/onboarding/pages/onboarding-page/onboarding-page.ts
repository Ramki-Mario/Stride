import {
  Component,
  ChangeDetectionStrategy,
  signal,
  computed,
  inject,
  OnDestroy,
} from '@angular/core';
import {
  FormBuilder,
  FormGroup,
  Validators,
  ReactiveFormsModule,
  AbstractControl,
  ValidationErrors,
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { ThemeService } from '../../../../core/theme/theme.service';
import { AuthService } from '../../../../core/auth/auth.service';
import { firstValueFrom } from 'rxjs';

const PREVIEW_STYLE_ID = 'onboarding-preview';

// ── Custom validator: passwords must match ────────────────────────────────────
function passwordMatchValidator(group: AbstractControl): ValidationErrors | null {
  const pw  = group.get('password')?.value;
  const cpw = group.get('confirmPassword')?.value;
  return pw && cpw && pw !== cpw ? { passwordMismatch: true } : null;
}

@Component({
  selector: 'app-onboarding-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="ob-shell" [style.background]="shellBg()">

      <!-- ── Progress bar ──────────────────────────────────────────────── -->
      <div class="ob-progress" role="progressbar"
           [attr.aria-valuenow]="step()" [attr.aria-valuemax]="4">
        @for (s of [1,2,3,4]; track s) {
          <div class="ob-progress-step"
               [class.ob-progress-step--done]="step() > s"
               [class.ob-progress-step--active]="step() === s">
            <div class="ob-progress-dot">
              @if (step() > s) {
                <svg width="10" height="10" viewBox="0 0 10 10" fill="none">
                  <path d="M2 5l2 2 4-4" stroke="white" stroke-width="1.5" stroke-linecap="round"/>
                </svg>
              } @else {
                {{ s }}
              }
            </div>
            <span class="ob-progress-label">{{ stepLabels[s - 1] }}</span>
          </div>
          @if (s < 4) { <div class="ob-progress-line" [class.ob-progress-line--done]="step() > s"></div> }
        }
      </div>

      <!-- ── Card ─────────────────────────────────────────────────────── -->
      <div class="ob-card">

        <!-- Brand -->
        <div class="ob-brand">
          <div class="ob-logo">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none">
              <path d="M13 2L3 14h9l-1 8 10-12h-9l1-8z" fill="white" stroke="white"
                    stroke-width="1" stroke-linejoin="round"/>
            </svg>
          </div>
          <span class="ob-brand-name">STRIDE</span>
        </div>

        <!-- ── Error banner ──────────────────────────────────────────── -->
        @if (serverError()) {
          <div class="ob-alert" role="alert">
            <svg width="16" height="16" viewBox="0 0 16 16" fill="none" aria-hidden="true">
              <circle cx="8" cy="8" r="7" stroke="currentColor" stroke-width="1.5"/>
              <path d="M8 5v3.5M8 10.5v.5" stroke="currentColor" stroke-width="1.5" stroke-linecap="round"/>
            </svg>
            {{ serverError() }}
          </div>
        }

        <!-- ══════════════════════════════════════════════════════════════
             STEP 1 — Organisation
        ═══════════════════════════════════════════════════════════════ -->
        @if (step() === 1) {
          <div class="ob-step" role="region" aria-label="Organisation details">
            <h1 class="ob-step-title">Set up your organisation</h1>
            <p class="ob-step-sub">This will be your workspace name inside STRIDE.</p>

            <form [formGroup]="orgForm" (ngSubmit)="nextStep()" novalidate class="ob-form">

              <div class="ob-field">
                <label class="ob-label" for="orgName">Organisation name</label>
                <input id="orgName" type="text" class="ob-input"
                       formControlName="orgName"
                       [class.ob-input--error]="orgName.invalid && orgName.touched"
                       placeholder="Acme Corp"
                       (input)="onOrgNameInput()" />
                @if (orgName.touched && orgName.hasError('required')) {
                  <span class="ob-field-error">Organisation name is required.</span>
                }
              </div>

              <div class="ob-field">
                <label class="ob-label" for="slug">Workspace slug</label>
                <div class="ob-slug-wrapper">
                  <span class="ob-slug-prefix">stride.app/</span>
                  <input id="slug" type="text" class="ob-input ob-input--slug"
                         formControlName="slug"
                         [class.ob-input--error]="slug.invalid && slug.touched"
                         placeholder="acme-corp" />
                </div>
                @if (slug.touched && slug.hasError('required')) {
                  <span class="ob-field-error">Slug is required.</span>
                } @else if (slug.touched && slug.hasError('pattern')) {
                  <span class="ob-field-error">Only lowercase letters, numbers and hyphens (e.g. acme-corp).</span>
                } @else if (slugTaken()) {
                  <span class="ob-field-error">This slug is already taken — try another.</span>
                }
              </div>

              <div class="ob-field">
                <label class="ob-label">Plan</label>
                <div class="ob-plan-grid">
                  @for (plan of plans; track plan.value) {
                    <button type="button" class="ob-plan-card"
                            [class.ob-plan-card--selected]="orgForm.value.plan === plan.value"
                            (click)="orgForm.patchValue({ plan: plan.value })">
                      <span class="ob-plan-name">{{ plan.label }}</span>
                      <span class="ob-plan-price">{{ plan.price }}</span>
                      <ul class="ob-plan-bullets">
                        @for (b of plan.bullets; track b) { <li>{{ b }}</li> }
                      </ul>
                    </button>
                  }
                </div>
              </div>

              <div class="ob-actions">
                <span></span>
                <button type="submit" class="ob-btn ob-btn--primary"
                        [disabled]="orgForm.invalid || slugTaken()">
                  Continue <span aria-hidden="true">→</span>
                </button>
              </div>
            </form>
          </div>
        }

        <!-- ══════════════════════════════════════════════════════════════
             STEP 2 — Admin Account
        ═══════════════════════════════════════════════════════════════ -->
        @if (step() === 2) {
          <div class="ob-step" role="region" aria-label="Admin account details">
            <h1 class="ob-step-title">Create your admin account</h1>
            <p class="ob-step-sub">You'll use these credentials to sign in.</p>

            <form [formGroup]="adminForm" (ngSubmit)="nextStep()" novalidate class="ob-form">

              <div class="ob-field">
                <label class="ob-label" for="displayName">Full name</label>
                <input id="displayName" type="text" class="ob-input"
                       formControlName="displayName"
                       [class.ob-input--error]="displayName.invalid && displayName.touched"
                       placeholder="Alex Johnson" />
                @if (displayName.touched && displayName.hasError('required')) {
                  <span class="ob-field-error">Full name is required.</span>
                }
              </div>

              <div class="ob-field">
                <label class="ob-label" for="adminEmail">Work email</label>
                <input id="adminEmail" type="email" class="ob-input"
                       formControlName="email"
                       [class.ob-input--error]="adminEmail.invalid && adminEmail.touched"
                       placeholder="you@company.com" autocomplete="email" />
                @if (adminEmail.touched && adminEmail.hasError('required')) {
                  <span class="ob-field-error">Email is required.</span>
                } @else if (adminEmail.touched && adminEmail.hasError('email')) {
                  <span class="ob-field-error">Enter a valid email address.</span>
                }
              </div>

              <div class="ob-field">
                <label class="ob-label" for="password">Password</label>
                <input id="password" type="password" class="ob-input"
                       formControlName="password"
                       [class.ob-input--error]="adminPassword.invalid && adminPassword.touched"
                       placeholder="Min. 8 characters" autocomplete="new-password" />
                <!-- Strength meter -->
                <div class="ob-strength">
                  @for (seg of [1,2,3,4]; track seg) {
                    <div class="ob-strength-seg"
                         [class.ob-strength-seg--filled]="passwordStrength() >= seg"
                         [class.ob-strength-seg--weak]="passwordStrength() === 1 && seg === 1"
                         [class.ob-strength-seg--fair]="passwordStrength() === 2 && seg <= 2"
                         [class.ob-strength-seg--good]="passwordStrength() === 3 && seg <= 3"
                         [class.ob-strength-seg--strong]="passwordStrength() === 4">
                    </div>
                  }
                  <span class="ob-strength-label">{{ strengthLabel() }}</span>
                </div>
                @if (adminPassword.touched && adminPassword.hasError('required')) {
                  <span class="ob-field-error">Password is required.</span>
                } @else if (adminPassword.touched && adminPassword.hasError('minlength')) {
                  <span class="ob-field-error">Password must be at least 8 characters.</span>
                }
              </div>

              <div class="ob-field">
                <label class="ob-label" for="confirmPassword">Confirm password</label>
                <input id="confirmPassword" type="password" class="ob-input"
                       formControlName="confirmPassword"
                       [class.ob-input--error]="confirmPw.invalid && confirmPw.touched"
                       placeholder="Re-enter password" autocomplete="new-password" />
                @if ((confirmPw.touched || adminPassword.touched) && adminForm.hasError('passwordMismatch')) {
                  <span class="ob-field-error">Passwords do not match.</span>
                }
              </div>

              <div class="ob-actions">
                <button type="button" class="ob-btn ob-btn--ghost" (click)="prevStep()">← Back</button>
                <button type="submit" class="ob-btn ob-btn--primary" [disabled]="adminForm.invalid">
                  Continue <span aria-hidden="true">→</span>
                </button>
              </div>
            </form>
          </div>
        }

        <!-- ══════════════════════════════════════════════════════════════
             STEP 3 — Appearance
        ═══════════════════════════════════════════════════════════════ -->
        @if (step() === 3) {
          <div class="ob-step" role="region" aria-label="Appearance settings">
            <h1 class="ob-step-title">Choose your look</h1>
            <p class="ob-step-sub">Pick a palette and theme — you can change these anytime.</p>

            <div class="ob-form">

              <div class="ob-field">
                <label class="ob-label">Colour palette</label>
                <div class="ob-palette-row">
                  @for (p of palettes; track p.value) {
                    <button type="button" class="ob-palette-chip"
                            [class.ob-palette-chip--selected]="selectedPalette() === p.value"
                            [style.background]="p.color"
                            (click)="applyPalette(p.value)"
                            [attr.aria-label]="p.label"
                            [attr.title]="p.label">
                      @if (selectedPalette() === p.value) {
                        <svg width="14" height="14" viewBox="0 0 14 14" fill="none">
                          <path d="M2 7l3 3 7-7" stroke="white" stroke-width="2" stroke-linecap="round"/>
                        </svg>
                      }
                    </button>
                  }
                </div>
              </div>

              <div class="ob-field">
                <label class="ob-label">Theme</label>
                <div class="ob-theme-row">
                  <button type="button" class="ob-theme-btn"
                          [class.ob-theme-btn--selected]="!theme.isDark()"
                          (click)="theme.isDark() && theme.toggle()">
                    <svg width="18" height="18" viewBox="0 0 24 24" fill="none"
                         stroke="currentColor" stroke-width="1.75" aria-hidden="true">
                      <circle cx="12" cy="12" r="4"/>
                      <path d="M12 2v2M12 20v2M4.93 4.93l1.41 1.41M17.66 17.66l1.41 1.41M2 12h2M20 12h2M4.93 19.07l1.41-1.41M17.66 6.34l1.41-1.41"/>
                    </svg>
                    Light
                  </button>
                  <button type="button" class="ob-theme-btn"
                          [class.ob-theme-btn--selected]="theme.isDark()"
                          (click)="!theme.isDark() && theme.toggle()">
                    <svg width="18" height="18" viewBox="0 0 24 24" fill="none"
                         stroke="currentColor" stroke-width="1.75" aria-hidden="true">
                      <path d="M21 12.79A9 9 0 1111.21 3 7 7 0 0021 12.79z"/>
                    </svg>
                    Dark
                  </button>
                </div>
              </div>

              <div class="ob-actions">
                <button type="button" class="ob-btn ob-btn--ghost" (click)="prevStep()">← Back</button>
                <button type="button" class="ob-btn ob-btn--primary" (click)="nextStep()">
                  Continue <span aria-hidden="true">→</span>
                </button>
              </div>
            </div>
          </div>
        }

        <!-- ══════════════════════════════════════════════════════════════
             STEP 4 — Invite Team (optional)
        ═══════════════════════════════════════════════════════════════ -->
        @if (step() === 4) {
          <div class="ob-step" role="region" aria-label="Invite team members">
            <h1 class="ob-step-title">Invite your team <span class="ob-optional">(optional)</span></h1>
            <p class="ob-step-sub">Add up to 5 colleagues — they'll receive an invite link.</p>

            <div class="ob-form">
              @for (ctrl of inviteControls; track $index) {
                <div class="ob-field">
                  <label class="ob-label" [for]="'invite-' + $index">
                    Teammate {{ $index + 1 }}
                  </label>
                  <input [id]="'invite-' + $index" type="email" class="ob-input"
                         [formControl]="ctrl"
                         [class.ob-input--error]="ctrl.invalid && ctrl.touched"
                         placeholder="colleague@company.com" />
                  @if (ctrl.touched && ctrl.hasError('email')) {
                    <span class="ob-field-error">Enter a valid email address.</span>
                  }
                </div>
              }

              <div class="ob-actions">
                <button type="button" class="ob-btn ob-btn--ghost" (click)="prevStep()">← Back</button>
                <button type="button" class="ob-btn ob-btn--primary"
                        [disabled]="submitting()"
                        (click)="submit()">
                  @if (submitting()) {
                    <span class="ob-spinner" aria-hidden="true"></span>
                    <span>Creating workspace…</span>
                  } @else {
                    <span>Create workspace</span>
                    <span aria-hidden="true">→</span>
                  }
                </button>
              </div>
            </div>
          </div>
        }

      </div>
      <!-- /.ob-card -->

      <p class="ob-signin-link">
        Already have a workspace? <a routerLink="/login">Sign in</a>
      </p>

    </div>
  `,
  styles: [`
    :host { display: block; }

    .ob-shell {
      min-height: 100vh;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: flex-start;
      padding: 2rem 1rem 4rem;
      background: var(--stride-bg);
      transition: background 300ms;
    }

    /* ── Progress ────────────────────────────────────────────────────── */
    .ob-progress {
      display: flex;
      align-items: center;
      gap: 0;
      margin-bottom: 2rem;
      max-width: 32rem;
      width: 100%;
    }

    .ob-progress-step {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 0.375rem;
      flex-shrink: 0;
    }

    .ob-progress-dot {
      width: 1.75rem;
      height: 1.75rem;
      border-radius: 50%;
      border: 2px solid var(--stride-border);
      background: var(--stride-surface);
      color: var(--stride-text-muted);
      font-size: 0.75rem;
      font-weight: 600;
      display: flex;
      align-items: center;
      justify-content: center;
      transition: all 200ms;
    }

    .ob-progress-step--active .ob-progress-dot {
      border-color: var(--stride-primary);
      background: var(--stride-primary);
      color: #fff;
    }

    .ob-progress-step--done .ob-progress-dot {
      border-color: var(--stride-primary);
      background: var(--stride-primary);
      color: #fff;
    }

    .ob-progress-label {
      font-size: 0.6875rem;
      color: var(--stride-text-muted);
      white-space: nowrap;
    }

    .ob-progress-step--active .ob-progress-label,
    .ob-progress-step--done .ob-progress-label {
      color: var(--stride-primary);
      font-weight: 600;
    }

    .ob-progress-line {
      flex: 1;
      height: 2px;
      background: var(--stride-border);
      margin: 0 0.375rem;
      margin-bottom: 1.25rem;
      transition: background 200ms;
    }

    .ob-progress-line--done { background: var(--stride-primary); }

    /* ── Card ────────────────────────────────────────────────────────── */
    .ob-card {
      background: var(--stride-surface);
      border: 1px solid var(--stride-border);
      border-radius: var(--stride-radius-2xl);
      box-shadow: var(--stride-shadow-card);
      padding: 2rem 2.5rem;
      width: 100%;
      max-width: 32rem;
    }

    /* ── Brand ───────────────────────────────────────────────────────── */
    .ob-brand {
      display: flex;
      align-items: center;
      gap: 0.625rem;
      margin-bottom: 1.75rem;
    }

    .ob-logo {
      width: 2rem;
      height: 2rem;
      border-radius: 0.5rem;
      background: var(--stride-primary);
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
    }

    .ob-brand-name {
      font-size: 1rem;
      font-weight: 700;
      letter-spacing: 0.04em;
      color: var(--stride-text-primary);
    }

    /* ── Error alert ─────────────────────────────────────────────────── */
    .ob-alert {
      display: flex;
      align-items: flex-start;
      gap: 0.625rem;
      padding: 0.75rem 1rem;
      background: var(--stride-error-bg);
      border: 1px solid var(--stride-error-border);
      border-radius: var(--stride-radius-md);
      color: var(--stride-error);
      font-size: 0.875rem;
      margin-bottom: 1.25rem;
    }

    /* ── Step content ─────────────────────────────────────────────────── */
    .ob-step-title {
      font-size: 1.375rem;
      font-weight: 700;
      color: var(--stride-text-primary);
      margin: 0 0 0.375rem;
    }

    .ob-step-sub {
      font-size: 0.9rem;
      color: var(--stride-text-secondary);
      margin: 0 0 1.5rem;
    }

    .ob-optional {
      font-size: 0.8rem;
      font-weight: 400;
      color: var(--stride-text-muted);
    }

    /* ── Form ─────────────────────────────────────────────────────────── */
    .ob-form { display: flex; flex-direction: column; gap: 1.125rem; }

    .ob-field { display: flex; flex-direction: column; gap: 0.375rem; }

    .ob-label {
      font-size: 0.875rem;
      font-weight: 550;
      color: var(--stride-text-primary);
    }

    .ob-input {
      width: 100%;
      padding: 0.625rem 0.875rem;
      border: 1px solid var(--stride-border);
      border-radius: var(--stride-radius-xl);
      background: var(--stride-surface);
      color: var(--stride-text-primary);
      font-size: 0.9rem;
      outline: none;
      transition: border-color 150ms, box-shadow 150ms;
      box-sizing: border-box;
    }

    .ob-input:focus {
      border-color: var(--stride-border-focus);
      box-shadow: 0 0 0 3px var(--stride-primary-ring);
    }

    .ob-input--error { border-color: var(--stride-error) !important; }

    .ob-field-error {
      font-size: 0.8rem;
      color: var(--stride-error);
    }

    /* ── Slug ─────────────────────────────────────────────────────────── */
    .ob-slug-wrapper {
      display: flex;
      align-items: center;
      border: 1px solid var(--stride-border);
      border-radius: var(--stride-radius-xl);
      overflow: hidden;
      background: var(--stride-surface);
      transition: border-color 150ms, box-shadow 150ms;
    }

    .ob-slug-wrapper:focus-within {
      border-color: var(--stride-border-focus);
      box-shadow: 0 0 0 3px var(--stride-primary-ring);
    }

    .ob-slug-prefix {
      padding: 0.625rem 0.75rem;
      background: var(--stride-surface-secondary);
      color: var(--stride-text-muted);
      font-size: 0.875rem;
      white-space: nowrap;
      border-right: 1px solid var(--stride-border);
    }

    .ob-input--slug {
      border: none;
      border-radius: 0;
      flex: 1;
      box-shadow: none !important;
    }

    .ob-input--slug:focus { box-shadow: none !important; }

    /* ── Plan cards ──────────────────────────────────────────────────── */
    .ob-plan-grid {
      display: grid;
      grid-template-columns: repeat(3, 1fr);
      gap: 0.75rem;
    }

    .ob-plan-card {
      border: 2px solid var(--stride-border);
      border-radius: var(--stride-radius-lg);
      padding: 0.875rem 0.75rem;
      background: var(--stride-surface);
      cursor: pointer;
      text-align: left;
      transition: border-color 150ms, background 150ms;
    }

    .ob-plan-card:hover { border-color: var(--stride-primary); }

    .ob-plan-card--selected {
      border-color: var(--stride-primary);
      background: var(--stride-primary-subtle);
    }

    .ob-plan-name {
      display: block;
      font-size: 0.875rem;
      font-weight: 700;
      color: var(--stride-text-primary);
      margin-bottom: 0.25rem;
    }

    .ob-plan-price {
      display: block;
      font-size: 0.75rem;
      color: var(--stride-text-muted);
      margin-bottom: 0.5rem;
    }

    .ob-plan-bullets {
      list-style: none;
      padding: 0;
      margin: 0;
      display: flex;
      flex-direction: column;
      gap: 0.2rem;
    }

    .ob-plan-bullets li {
      font-size: 0.6875rem;
      color: var(--stride-text-secondary);
    }

    .ob-plan-bullets li::before { content: '✓ '; color: var(--stride-success); }

    /* ── Password strength ──────────────────────────────────────────── */
    .ob-strength {
      display: flex;
      align-items: center;
      gap: 0.375rem;
      margin-top: 0.375rem;
    }

    .ob-strength-seg {
      flex: 1;
      height: 4px;
      border-radius: 2px;
      background: var(--stride-border);
      transition: background 200ms;
    }

    .ob-strength-seg--filled.ob-strength-seg--weak    { background: var(--stride-error); }
    .ob-strength-seg--filled.ob-strength-seg--fair    { background: var(--stride-warning); }
    .ob-strength-seg--filled.ob-strength-seg--good    { background: #3B82F6; }
    .ob-strength-seg--filled.ob-strength-seg--strong  { background: var(--stride-success); }

    .ob-strength-label {
      font-size: 0.6875rem;
      color: var(--stride-text-muted);
      min-width: 3rem;
    }

    /* ── Palette chips ──────────────────────────────────────────────── */
    .ob-palette-row { display: flex; gap: 0.875rem; flex-wrap: wrap; }

    .ob-palette-chip {
      width: 2.5rem;
      height: 2.5rem;
      border-radius: 50%;
      border: 3px solid transparent;
      cursor: pointer;
      display: flex;
      align-items: center;
      justify-content: center;
      transition: transform 150ms, border-color 150ms;
      flex-shrink: 0;
    }

    .ob-palette-chip:hover { transform: scale(1.1); }

    .ob-palette-chip--selected {
      border-color: var(--stride-text-primary);
      transform: scale(1.1);
    }

    /* ── Theme buttons ──────────────────────────────────────────────── */
    .ob-theme-row { display: flex; gap: 0.75rem; }

    .ob-theme-btn {
      flex: 1;
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 0.5rem;
      padding: 0.75rem;
      border: 2px solid var(--stride-border);
      border-radius: var(--stride-radius-lg);
      background: var(--stride-surface);
      color: var(--stride-text-secondary);
      font-size: 0.9rem;
      cursor: pointer;
      transition: border-color 150ms, background 150ms;
    }

    .ob-theme-btn:hover { border-color: var(--stride-primary); }

    .ob-theme-btn--selected {
      border-color: var(--stride-primary);
      background: var(--stride-primary-subtle);
      color: var(--stride-primary);
      font-weight: 600;
    }

    /* ── Actions row ────────────────────────────────────────────────── */
    .ob-actions {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-top: 0.5rem;
    }

    .ob-btn {
      display: inline-flex;
      align-items: center;
      gap: 0.375rem;
      padding: 0.625rem 1.25rem;
      border-radius: var(--stride-radius-xl);
      font-size: 0.9rem;
      font-weight: 600;
      cursor: pointer;
      border: none;
      transition: background 150ms, opacity 150ms;
    }

    .ob-btn:disabled { opacity: 0.55; cursor: not-allowed; }

    .ob-btn--primary {
      background: var(--stride-primary);
      color: #fff;
    }

    .ob-btn--primary:hover:not(:disabled) { background: var(--stride-primary-hover); }

    .ob-btn--ghost {
      background: transparent;
      color: var(--stride-text-secondary);
      border: 1px solid var(--stride-border);
    }

    .ob-btn--ghost:hover { background: var(--stride-surface-hover); }

    /* ── Spinner ─────────────────────────────────────────────────────── */
    .ob-spinner {
      width: 1rem;
      height: 1rem;
      border: 2px solid rgba(255,255,255,0.4);
      border-top-color: #fff;
      border-radius: 50%;
      animation: ob-spin 0.7s linear infinite;
    }

    @keyframes ob-spin { to { transform: rotate(360deg); } }

    /* ── Sign-in link ────────────────────────────────────────────────── */
    .ob-signin-link {
      margin-top: 1.5rem;
      font-size: 0.875rem;
      color: var(--stride-text-muted);
    }

    .ob-signin-link a {
      color: var(--stride-text-link);
      text-decoration: none;
      font-weight: 600;
    }

    .ob-signin-link a:hover { text-decoration: underline; }

    /* ── Responsive ─────────────────────────────────────────────────── */
    @media (max-width: 640px) {
      .ob-card { padding: 1.5rem 1.25rem; }
      .ob-plan-grid { grid-template-columns: 1fr; }
    }
  `],
})
export class OnboardingPageComponent implements OnDestroy {
  protected readonly theme    = inject(ThemeService);
  private   readonly auth     = inject(AuthService);
  private   readonly router   = inject(Router);
  private   readonly http     = inject(HttpClient);
  private   readonly fb       = inject(FormBuilder);

  // ── Stepper state ─────────────────────────────────────────────────────────
  protected readonly step        = signal(1);
  protected readonly submitting  = signal(false);
  protected readonly serverError = signal<string | null>(null);
  protected readonly slugTaken   = signal(false);
  protected readonly selectedPalette = signal<'purple' | 'indigo'>('purple');

  protected readonly stepLabels = ['Organisation', 'Admin Account', 'Appearance', 'Invite Team'];

  // ── Shell background reacts to palette ───────────────────────────────────
  protected readonly shellBg = computed(() => {
    // Subtle tinted background mirrors the chosen palette during onboarding
    return undefined; // uses CSS var(--stride-bg) which ThemeService already updates
  });

  // ── Plan options ──────────────────────────────────────────────────────────
  protected readonly plans = [
    { value: 'Starter',    label: 'Starter',    price: 'Free',     bullets: ['Up to 5 users', '3 workflows', 'Community support'] },
    { value: 'Pro',        label: 'Pro',         price: '$29 / mo', bullets: ['Up to 25 users', 'Unlimited workflows', 'Email support'] },
    { value: 'Enterprise', label: 'Enterprise',  price: 'Custom',   bullets: ['Unlimited users', 'SSO / SAML', 'Dedicated CSM'] },
  ];

  // ── Palette options ───────────────────────────────────────────────────────
  protected readonly palettes = [
    { value: 'purple' as const, label: 'Purple (default)', color: '#B97AF9' },
    { value: 'indigo' as const, label: 'Indigo',           color: '#6366F1' },
  ];

  // ── Step 1: Organisation form ──────────────────────────────────────────────
  protected readonly orgForm = this.fb.group({
    orgName: ['', [Validators.required, Validators.maxLength(200)]],
    slug:    ['', [Validators.required, Validators.maxLength(100),
                   Validators.pattern(/^[a-z0-9]+(?:-[a-z0-9]+)*$/)]],
    plan:    ['Starter', Validators.required],
  });

  // ── Step 2: Admin account form ─────────────────────────────────────────────
  protected readonly adminForm = this.fb.group({
    displayName:     ['', [Validators.required, Validators.maxLength(100)]],
    email:           ['', [Validators.required, Validators.email, Validators.maxLength(256)]],
    password:        ['', [Validators.required, Validators.minLength(8)]],
    confirmPassword: ['', Validators.required],
  }, { validators: passwordMatchValidator });

  // ── Step 4: Invite controls (up to 5) ────────────────────────────────────
  protected readonly inviteControls = [0, 1, 2, 3, 4].map(() =>
    this.fb.control('', Validators.email));

  // ── Convenience getters ───────────────────────────────────────────────────
  get orgName()      { return this.orgForm.controls.orgName; }
  get slug()         { return this.orgForm.controls.slug; }
  get displayName()  { return this.adminForm.controls.displayName; }
  get adminEmail()   { return this.adminForm.controls.email; }
  get adminPassword(){ return this.adminForm.controls.password; }
  get confirmPw()    { return this.adminForm.controls.confirmPassword; }

  // ── Password strength ──────────────────────────────────────────────────────
  protected readonly passwordStrength = computed(() => {
    const pw = this.adminForm.controls.password.value ?? '';
    let score = 0;
    if (pw.length >= 8)  score++;
    if (/[A-Z]/.test(pw)) score++;
    if (/[0-9]/.test(pw)) score++;
    if (/[^A-Za-z0-9]/.test(pw)) score++;
    return score;
  });

  protected readonly strengthLabel = computed(() => {
    const s = this.passwordStrength();
    return s === 0 ? '' : s === 1 ? 'Weak' : s === 2 ? 'Fair' : s === 3 ? 'Good' : 'Strong';
  });

  // ── Navigation ─────────────────────────────────────────────────────────────
  protected nextStep(): void {
    this.serverError.set(null);
    this.step.update(s => Math.min(s + 1, 4));
  }

  protected prevStep(): void {
    this.serverError.set(null);
    this.step.update(s => Math.max(s - 1, 1));
  }

  // ── Auto-generate slug from org name ──────────────────────────────────────
  protected onOrgNameInput(): void {
    const raw = this.orgName.value ?? '';
    const slug = raw
      .toLowerCase()
      .replace(/[^a-z0-9\s-]/g, '')
      .trim()
      .replace(/\s+/g, '-')
      .replace(/-+/g, '-');
    this.orgForm.patchValue({ slug }, { emitEvent: false });
    this.slugTaken.set(false);
  }

  // ── Appearance ─────────────────────────────────────────────────────────────
  protected applyPalette(palette: 'purple' | 'indigo'): void {
    this.selectedPalette.set(palette);
    this.theme.applyPaletteFromSession(palette);
  }

  // ── Submit ─────────────────────────────────────────────────────────────────
  protected async submit(): Promise<void> {
    this.serverError.set(null);
    this.submitting.set(true);

    const inviteEmails = this.inviteControls
      .map(c => c.value?.trim() ?? '')
      .filter(e => !!e);

    try {
      await firstValueFrom(this.http.post('/bff/tenants/register', {
        orgName:          this.orgForm.value.orgName,
        slug:             this.orgForm.value.slug,
        plan:             this.orgForm.value.plan,
        adminEmail:       this.adminForm.value.email,
        adminPassword:    this.adminForm.value.password,
        adminDisplayName: this.adminForm.value.displayName,
      }));

      // Session cookie is now set; re-hydrate AuthService and redirect
      await firstValueFrom(this.auth.checkSession());
      this.removePreviewStyle();
      this.router.navigate(['/dashboard']);
    } catch (err: unknown) {
      const msg = this.extractError(err);
      if (msg.toLowerCase().includes('slug')) {
        this.slugTaken.set(true);
        this.step.set(1);
        this.serverError.set(msg);
      } else {
        this.serverError.set(msg);
      }
    } finally {
      this.submitting.set(false);
    }
  }

  // ── Cleanup ────────────────────────────────────────────────────────────────
  ngOnDestroy(): void {
    this.removePreviewStyle();
  }

  private removePreviewStyle(): void {
    document.getElementById(PREVIEW_STYLE_ID)?.remove();
  }

  private extractError(err: unknown): string {
    if (err && typeof err === 'object') {
      const e = err as Record<string, unknown>;
      const status = e['status'];
      const error  = e['error'];
      if (status === 409) return 'This workspace slug is already taken — please choose another.';
      if (error && typeof error === 'object') {
        const detail = (error as Record<string, unknown>)['detail'] ??
                       (error as Record<string, unknown>)['error'];
        if (typeof detail === 'string') return detail;
      }
    }
    return 'Registration failed. Please try again.';
  }
}
