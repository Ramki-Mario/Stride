import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import {
  AbstractControl,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs/operators';

import { AuthService } from '../../../../core/auth/auth.service';
import { ThemeService } from '../../../../core/theme/theme.service';

function passwordMatchValidator(ctrl: AbstractControl): ValidationErrors | null {
  const group = ctrl as FormGroup;
  const pw  = group.get('password')?.value  ?? '';
  const cpw = group.get('confirm')?.value ?? '';
  return pw && cpw && pw !== cpw ? { mismatch: true } : null;
}

function strengthScore(pw: string): number {
  let score = 0;
  if (pw.length >= 8)  score++;
  if (pw.length >= 12) score++;
  if (/[A-Z]/.test(pw)) score++;
  if (/\d/.test(pw)) score++;
  if (/[^A-Za-z0-9]/.test(pw)) score++;
  return score; // 0-5
}

@Component({
  selector: 'app-accept-invite-page',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './accept-invite-page.html',
  styleUrl: './accept-invite-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AcceptInvitePageComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly route       = inject(ActivatedRoute);
  private readonly router      = inject(Router);
  private readonly destroyRef  = inject(DestroyRef);
  readonly theme               = inject(ThemeService);

  /** Raw token from query param — never displayed back to user. */
  private rawToken = '';

  readonly isValidating   = signal(true);
  readonly tokenError     = signal<string | null>(null);
  readonly inviteeEmail   = signal<string | null>(null);
  readonly inviteeName    = signal<string | null>(null);

  readonly isSubmitting   = signal(false);
  readonly submitError    = signal<string | null>(null);
  readonly showPassword   = signal(false);
  readonly showConfirm    = signal(false);
  readonly pwStrength     = signal(0);

  readonly form = new FormGroup(
    {
      password: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required, Validators.minLength(8)],
      }),
      confirm: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required],
      }),
    },
    { validators: passwordMatchValidator }
  );

  get pwCtrl()  { return this.form.controls.password; }
  get cfmCtrl() { return this.form.controls.confirm; }

  ngOnInit(): void {
    this.rawToken = this.route.snapshot.queryParamMap.get('token') ?? '';

    if (!this.rawToken) {
      this.isValidating.set(false);
      this.tokenError.set('No invite token found. Please use the link from your invitation email.');
      return;
    }

    this.pwCtrl.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((v) => this.pwStrength.set(strengthScore(v)));

    this.authService
      .validateInviteToken(this.rawToken)
      .pipe(
        finalize(() => this.isValidating.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (info) => {
          this.inviteeEmail.set(info.email);
          this.inviteeName.set(info.displayName);
        },
        error: () => {
          this.tokenError.set(
            'This invite link is invalid or has expired. Please contact your admin for a new invitation.'
          );
        },
      });
  }

  togglePassword(): void { this.showPassword.update((v) => !v); }
  toggleConfirm():  void { this.showConfirm.update((v) => !v); }

  strengthLabel(): string {
    const s = this.pwStrength();
    if (s <= 1) return 'Weak';
    if (s <= 2) return 'Fair';
    if (s <= 3) return 'Good';
    return 'Strong';
  }

  strengthClass(): string {
    const s = this.pwStrength();
    if (s <= 1) return 'weak';
    if (s <= 2) return 'fair';
    if (s <= 3) return 'good';
    return 'strong';
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.submitError.set(null);

    const { password } = this.form.getRawValue();

    this.authService
      .acceptInvite(this.rawToken, password)
      .pipe(
        finalize(() => this.isSubmitting.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => this.router.navigate(['/dashboard']),
        error: (err) => {
          const message =
            err?.error?.detail ??
            err?.error?.title ??
            'Something went wrong. Please try again or contact your admin.';
          this.submitError.set(message);
        },
      });
  }
}
