import {
  Component,
  ChangeDetectionStrategy,
  inject,
  signal,
  computed,
  OnInit,
  effect,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { KitOpsService } from '../../services/kit-ops.service';
import {
  KitCatalogItemDto,
  MyKitCheckoutDto,
  MyKitReservationDto,
} from '../../models/kit-ops.models';
import { ConnectivityService } from '../../../../core/pwa/connectivity.service';
import { KitSyncService }      from '../../../../core/pwa/kit-sync.service';
import { KitOfflineQueueService } from '../../../../core/pwa/kit-offline-queue.service';

type ModalMode = 'checkout' | 'request' | null;

/** Checkout row augmented with local optimistic state — never sent to the server. */
type CheckoutRow = MyKitCheckoutDto & { pendingReturn?: boolean };

@Component({
  selector: 'app-kit-field-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="kfp-page">

      <!-- ── Offline banner ─────────────────────────────────────────────────── -->
      @if (isOffline()) {
        <div class="kfp-offline-banner" role="status" aria-live="polite">
          <i class="pi pi-wifi" style="opacity:.6"></i>
          <span><strong>You are offline.</strong>
            Checkout and return actions will be saved and synced when connectivity is restored.
          </span>
        </div>
      }

      <!-- ── Sync status bar ───────────────────────────────────────────────── -->
      @if (kitSync.pendingCount() > 0 || kitSync.isSyncing()) {
        <div class="kfp-sync-bar" [class.kfp-sync-bar--active]="kitSync.isSyncing()" role="status" aria-live="polite">
          @if (kitSync.isSyncing()) {
            <i class="pi pi-spin pi-spinner"></i>
            <span>Syncing {{ kitSync.pendingCount() }} queued action(s)…</span>
          } @else {
            <i class="pi pi-clock"></i>
            <span>{{ kitSync.pendingCount() }} action(s) queued — will sync on reconnect.</span>
          }
        </div>
      }

      @if (kitSync.lastSyncError()) {
        <div class="kfp-error-banner" role="alert">
          <i class="pi pi-exclamation-circle"></i>
          <span>{{ kitSync.lastSyncError() }}</span>
        </div>
      }

      <!-- ── Header ─────────────────────────────────────────────────────── -->
      <div class="kfp-header">
        <div>
          <h1 class="kfp-title">My Kit</h1>
          <p class="kfp-subtitle">Browse available equipment, check items in and out, or request gear that is currently unavailable.</p>
        </div>
      </div>

      <!-- ── Global error ───────────────────────────────────────────────── -->
      @if (error()) {
        <div class="kfp-error-banner" role="alert">
          <i class="pi pi-exclamation-circle"></i>
          <span>{{ error() }}</span>
          <button class="kfp-error-close" (click)="error.set(null)" aria-label="Dismiss">
            <i class="pi pi-times"></i>
          </button>
        </div>
      }

      <!-- ── Action feedback ────────────────────────────────────────────── -->
      @if (successMsg()) {
        <div class="kfp-success-banner" role="status" aria-live="polite">
          <i class="pi pi-check-circle"></i>
          <span>{{ successMsg() }}</span>
        </div>
      }

      <!-- ══════════════════════════════════════════════════════════════════
           SECTION 1: Browse catalog
      ══════════════════════════════════════════════════════════════════════ -->
      <section class="kfp-section">
        <h2 class="kfp-section-title">
          <i class="pi pi-box"></i>
          Available Equipment
        </h2>

        @if (catalogLoading()) {
          <div class="kfp-card-grid">
            @for (sk of [1,2,3,4,5,6]; track sk) {
              <div class="kfp-kit-card kfp-skeleton"></div>
            }
          </div>
        } @else if (catalog().length === 0) {
          <div class="kfp-empty">
            <i class="pi pi-box kfp-empty-icon"></i>
            <p>No kit items are available right now.</p>
          </div>
        } @else {
          <div class="kfp-card-grid">
            @for (item of catalog(); track item.kitItemId) {
              <div class="kfp-kit-card">
                <div class="kfp-kit-card-header">
                  <span class="kfp-category-tag">{{ item.category }}</span>
                  <span class="kfp-avail-badge" [class.kfp-avail-none]="item.availableQuantity === 0">
                    {{ item.availableQuantity }} / {{ item.totalQuantity }} available
                  </span>
                </div>
                <h3 class="kfp-kit-name">{{ item.name }}</h3>
                @if (item.description) {
                  <p class="kfp-kit-desc">{{ item.description }}</p>
                }
                @if (item.overdueCheckouts > 0) {
                  <p class="kfp-overdue-note">
                    <i class="pi pi-exclamation-triangle"></i>
                    {{ item.overdueCheckouts }} overdue
                  </p>
                }
                <div class="kfp-kit-actions">
                  @if (item.availableQuantity > 0) {
                    <button class="btn btn-primary btn-sm"
                            (click)="openCheckoutModal(item)"
                            [disabled]="actionInProgress()">
                      <i class="pi pi-download"></i> Check Out
                    </button>
                  } @else {
                    <button class="btn btn-secondary btn-sm"
                            (click)="openRequestModal(item)"
                            [disabled]="actionInProgress()">
                      <i class="pi pi-clock"></i> Request
                    </button>
                  }
                </div>
              </div>
            }
          </div>
        }
      </section>

      <!-- ══════════════════════════════════════════════════════════════════
           SECTION 2: My active checkouts
      ══════════════════════════════════════════════════════════════════════ -->
      <section class="kfp-section">
        <h2 class="kfp-section-title">
          <i class="pi pi-shopping-bag"></i>
          My Checkouts
          @if (activeCheckouts().length > 0) {
            <span class="kfp-count-pill">{{ activeCheckouts().length }}</span>
          }
        </h2>

        @if (checkoutsLoading()) {
          <div class="kfp-list-skeleton">
            @for (sk of [1,2]; track sk) {
              <div class="kfp-list-row kfp-skeleton" style="height:4rem"></div>
            }
          </div>
        } @else if (activeCheckouts().length === 0) {
          <p class="kfp-empty-inline">No active checkouts.</p>
        } @else {
          <div class="kfp-list">
            @for (co of activeCheckouts(); track co.checkoutId) {
              <div class="kfp-list-row" [class.kfp-row--pending]="co.pendingReturn">
                <div class="kfp-list-info">
                  <div class="kfp-list-name">
                    {{ co.kitItemName }}
                    @if (isOverdue(co)) {
                      <span class="kfp-overdue-tag">Overdue</span>
                    }
                    @if (co.pendingReturn) {
                      <span class="kfp-queued-tag">Return queued</span>
                    }
                  </div>
                  <div class="kfp-list-meta">
                    {{ co.category }} &bull;
                    Checked out {{ co.checkedOutAt | date:'mediumDate' }} &bull;
                    Due {{ co.expectedReturnAt | date:'mediumDate' }}
                  </div>
                  @if (co.notes) {
                    <div class="kfp-list-notes">{{ co.notes }}</div>
                  }
                </div>
                <button class="btn btn-outline btn-sm"
                        (click)="returnCheckout(co.checkoutId, co.kitItemName)"
                        [disabled]="actionInProgress() || !!co.pendingReturn">
                  <i class="pi pi-reply"></i> Return
                </button>
              </div>
            }
          </div>
        }
      </section>

      <!-- ══════════════════════════════════════════════════════════════════
           SECTION 3: My pending requests
      ══════════════════════════════════════════════════════════════════════ -->
      <section class="kfp-section">
        <h2 class="kfp-section-title">
          <i class="pi pi-clock"></i>
          My Requests
          @if (pendingReservations().length > 0) {
            <span class="kfp-count-pill">{{ pendingReservations().length }}</span>
          }
        </h2>

        @if (reservationsLoading()) {
          <div class="kfp-list-skeleton">
            @for (sk of [1]; track sk) {
              <div class="kfp-list-row kfp-skeleton" style="height:4rem"></div>
            }
          </div>
        } @else if (pendingReservations().length === 0) {
          <p class="kfp-empty-inline">No pending requests.</p>
        } @else {
          <div class="kfp-list">
            @for (res of pendingReservations(); track res.reservationId) {
              <div class="kfp-list-row">
                <div class="kfp-list-info">
                  <div class="kfp-list-name">{{ res.kitItemName }}</div>
                  <div class="kfp-list-meta">
                    {{ res.category }} &bull;
                    Requested {{ res.requestedAt | date:'mediumDate' }}
                  </div>
                  @if (res.notes) {
                    <div class="kfp-list-notes">{{ res.notes }}</div>
                  }
                </div>
                <button class="btn btn-outline btn-sm btn-danger"
                        (click)="cancelReservation(res.reservationId)"
                        [disabled]="actionInProgress()">
                  <i class="pi pi-times"></i> Cancel
                </button>
              </div>
            }
          </div>
        }
      </section>
    </div>

    <!-- ══════════════════════════════════════════════════════════════════════
         Checkout / Request Modal
    ══════════════════════════════════════════════════════════════════════════ -->
    @if (modalMode() !== null) {
      <div class="modal-backdrop" (click)="closeModal()">
        <div class="modal-box" (click)="$event.stopPropagation()">
          <div class="modal-header">
            <h3 class="modal-title">
              {{ modalMode() === 'checkout' ? 'Check Out' : 'Request' }}: {{ selectedItem()?.name }}
            </h3>
            <button class="modal-close" (click)="closeModal()" aria-label="Close">
              <i class="pi pi-times"></i>
            </button>
          </div>
          <div class="modal-body">
            @if (isOffline() && modalMode() === 'checkout') {
              <div class="kfp-modal-offline-note">
                <i class="pi pi-wifi" style="opacity:.6"></i>
                You are offline. This checkout will be queued and submitted when connectivity is restored.
              </div>
            }
            @if (modalMode() === 'checkout') {
              <div class="form-field">
                <label class="form-label" for="kf-days">Number of days <span class="required">*</span></label>
                <input id="kf-days"
                       type="number"
                       class="form-input"
                       [(ngModel)]="modalDays"
                       min="1"
                       max="365"
                       placeholder="e.g. 7" />
                @if (modalDaysError()) {
                  <span class="form-error">{{ modalDaysError() }}</span>
                }
              </div>
            }
            <div class="form-field">
              <label class="form-label" for="kf-notes">Notes (optional)</label>
              <textarea id="kf-notes"
                        class="form-textarea"
                        [(ngModel)]="modalNotes"
                        rows="3"
                        maxlength="500"
                        placeholder="Any additional details…"></textarea>
            </div>
            @if (modalError()) {
              <p class="form-error">{{ modalError() }}</p>
            }
          </div>
          <div class="modal-footer">
            <button class="btn btn-secondary" (click)="closeModal()">Cancel</button>
            <button class="btn btn-primary"
                    (click)="confirmModal()"
                    [disabled]="actionInProgress()">
              @if (actionInProgress()) {
                <i class="pi pi-spin pi-spinner"></i>
              }
              {{ modalMode() === 'checkout' ? (isOffline() ? 'Queue Checkout' : 'Check Out') : 'Submit Request' }}
            </button>
          </div>
        </div>
      </div>
    }
  `,
  styles: [`
    .kfp-page {
      padding: 1.5rem 2rem;
      max-width: 1100px;
      display: flex;
      flex-direction: column;
      gap: 1.5rem;
    }

    /* ── Offline banner ─── */
    .kfp-offline-banner {
      display: flex;
      align-items: center;
      gap: 0.625rem;
      padding: 0.75rem 1rem;
      border-radius: var(--stride-radius-md);
      font-size: 0.875rem;
      background: #fef9c3;
      color: #854d0e;
      border: 1px solid #fde68a;
    }

    /* ── Sync status bar ─── */
    .kfp-sync-bar {
      display: flex;
      align-items: center;
      gap: 0.625rem;
      padding: 0.625rem 1rem;
      border-radius: var(--stride-radius-md);
      font-size: 0.8125rem;
      font-weight: 500;
      background: var(--stride-primary-surface, #eff6ff);
      color: var(--stride-primary);
      border: 1px solid var(--stride-primary-border, #bfdbfe);
    }
    .kfp-sync-bar--active {
      animation: kfp-sync-pulse 1.8s ease-in-out infinite;
    }
    @keyframes kfp-sync-pulse {
      0%, 100% { opacity: 1; }
      50%       { opacity: .7; }
    }

    /* ── Queued / pending-return row decorators ─── */
    .kfp-row--pending {
      opacity: .75;
      border-style: dashed;
    }
    .kfp-queued-tag {
      font-size: 0.6875rem;
      font-weight: 700;
      background: #fef3c7;
      color: #92400e;
      padding: 0.1rem 0.4rem;
      border-radius: 999px;
    }

    /* ── Offline note inside modal ─── */
    .kfp-modal-offline-note {
      display: flex;
      align-items: flex-start;
      gap: 0.5rem;
      font-size: 0.8125rem;
      padding: 0.625rem 0.875rem;
      border-radius: var(--stride-radius-md);
      background: #fef9c3;
      color: #854d0e;
      border: 1px solid #fde68a;
    }

    /* ── Header ─── */
    .kfp-header {
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      gap: 1rem;
    }
    .kfp-title {
      font-size: 1.5rem;
      font-weight: 700;
      color: var(--stride-text-primary);
      margin: 0 0 0.25rem;
    }
    .kfp-subtitle {
      font-size: 0.875rem;
      color: var(--stride-text-secondary);
      margin: 0;
      max-width: 50rem;
    }

    /* ── Banners ─── */
    .kfp-error-banner,
    .kfp-success-banner {
      display: flex;
      align-items: center;
      gap: 0.625rem;
      padding: 0.75rem 1rem;
      border-radius: var(--stride-radius-md);
      font-size: 0.875rem;
      font-weight: 500;
    }
    .kfp-error-banner {
      background: var(--stride-danger-surface, #fef2f2);
      color: var(--stride-danger, #dc2626);
      border: 1px solid var(--stride-danger-border, #fecaca);
    }
    .kfp-success-banner {
      background: var(--stride-success-surface, #f0fdf4);
      color: var(--stride-success, #16a34a);
      border: 1px solid var(--stride-success-border, #bbf7d0);
    }
    .kfp-error-close {
      margin-left: auto;
      background: none;
      border: none;
      cursor: pointer;
      color: inherit;
      padding: 0;
      display: flex;
    }

    /* ── Section ─── */
    .kfp-section {
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }
    .kfp-section-title {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      font-size: 1.0625rem;
      font-weight: 600;
      color: var(--stride-text-primary);
      margin: 0;
    }
    .kfp-count-pill {
      background: var(--stride-primary);
      color: #fff;
      font-size: 0.6875rem;
      font-weight: 700;
      padding: 0.1rem 0.5rem;
      border-radius: 999px;
    }

    /* ── Catalog cards ─── */
    .kfp-card-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(260px, 1fr));
      gap: 1rem;
    }
    .kfp-kit-card {
      background: var(--stride-surface-card, #fff);
      border: 1px solid var(--stride-border);
      border-radius: var(--stride-radius-lg, 0.75rem);
      padding: 1rem 1.125rem;
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
      transition: box-shadow 150ms;
    }
    .kfp-kit-card:hover {
      box-shadow: 0 2px 12px rgba(0,0,0,.08);
    }
    .kfp-kit-card-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 0.5rem;
    }
    .kfp-category-tag {
      font-size: 0.6875rem;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.04em;
      color: var(--stride-primary);
      background: var(--stride-primary-surface, #eff6ff);
      padding: 0.15rem 0.5rem;
      border-radius: 999px;
    }
    .kfp-avail-badge {
      font-size: 0.75rem;
      font-weight: 600;
      color: var(--stride-success, #16a34a);
    }
    .kfp-avail-badge.kfp-avail-none {
      color: var(--stride-text-secondary);
    }
    .kfp-kit-name {
      font-size: 0.9375rem;
      font-weight: 600;
      color: var(--stride-text-primary);
      margin: 0;
    }
    .kfp-kit-desc {
      font-size: 0.8125rem;
      color: var(--stride-text-secondary);
      margin: 0;
      line-height: 1.4;
    }
    .kfp-overdue-note {
      font-size: 0.75rem;
      color: var(--stride-warning, #d97706);
      margin: 0;
      display: flex;
      align-items: center;
      gap: 0.25rem;
    }
    .kfp-kit-actions {
      margin-top: auto;
      padding-top: 0.5rem;
    }

    /* ── Checkout/reservation list ─── */
    .kfp-list {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }
    .kfp-list-row {
      display: flex;
      align-items: center;
      gap: 1rem;
      background: var(--stride-surface-card, #fff);
      border: 1px solid var(--stride-border);
      border-radius: var(--stride-radius-md);
      padding: 0.75rem 1rem;
    }
    .kfp-list-info {
      flex: 1;
      min-width: 0;
    }
    .kfp-list-name {
      font-size: 0.9375rem;
      font-weight: 600;
      color: var(--stride-text-primary);
      display: flex;
      align-items: center;
      gap: 0.5rem;
      flex-wrap: wrap;
    }
    .kfp-overdue-tag {
      font-size: 0.6875rem;
      font-weight: 700;
      background: var(--stride-danger, #dc2626);
      color: #fff;
      padding: 0.1rem 0.4rem;
      border-radius: 999px;
    }
    .kfp-list-meta {
      font-size: 0.8125rem;
      color: var(--stride-text-secondary);
      margin-top: 0.125rem;
    }
    .kfp-list-notes {
      font-size: 0.75rem;
      color: var(--stride-text-secondary);
      font-style: italic;
      margin-top: 0.125rem;
    }
    .kfp-empty-inline {
      font-size: 0.875rem;
      color: var(--stride-text-secondary);
      margin: 0;
    }

    /* ── Empty state ─── */
    .kfp-empty {
      display: flex;
      flex-direction: column;
      align-items: center;
      padding: 3rem 1rem;
      color: var(--stride-text-secondary);
      gap: 0.75rem;
    }
    .kfp-empty-icon {
      font-size: 2.5rem;
      opacity: .4;
    }

    /* ── Skeleton ─── */
    .kfp-skeleton {
      background: var(--stride-skeleton-base, #f1f5f9);
      border-radius: var(--stride-radius-md);
      animation: kfp-pulse 1.4s ease-in-out infinite;
      min-height: 10rem;
    }
    @keyframes kfp-pulse {
      0%, 100% { opacity: 1; }
      50% { opacity: .5; }
    }

    /* ── Shared button helpers ─── */
    .btn { display: inline-flex; align-items: center; gap: 0.375rem; padding: 0.5rem 1rem;
           border-radius: var(--stride-radius-md); font-size: 0.875rem; font-weight: 500;
           cursor: pointer; border: 1px solid transparent; transition: background 150ms, opacity 150ms; }
    .btn:disabled { opacity: .55; cursor: not-allowed; }
    .btn-sm { padding: 0.35rem 0.75rem; font-size: 0.8125rem; }
    .btn-primary { background: var(--stride-primary); color: #fff; }
    .btn-primary:hover:not(:disabled) { background: var(--stride-primary-hover, #1d4ed8); }
    .btn-secondary { background: var(--stride-surface-card, #f8fafc); color: var(--stride-text-primary);
                     border-color: var(--stride-border); }
    .btn-secondary:hover:not(:disabled) { background: var(--stride-surface-hover, #f1f5f9); }
    .btn-outline { background: transparent; color: var(--stride-primary); border-color: var(--stride-primary); }
    .btn-outline:hover:not(:disabled) { background: var(--stride-primary-surface, #eff6ff); }
    .btn-danger { color: var(--stride-danger, #dc2626); border-color: var(--stride-danger, #dc2626); }
    .btn-danger:hover:not(:disabled) { background: var(--stride-danger-surface, #fef2f2); }

    /* ── Modal ─── */
    .modal-backdrop {
      position: fixed; inset: 0;
      background: rgba(0,0,0,.45);
      display: flex; align-items: center; justify-content: center;
      z-index: 1000;
      padding: 1rem;
    }
    .modal-box {
      background: var(--stride-surface-card, #fff);
      border-radius: var(--stride-radius-lg, .75rem);
      box-shadow: 0 20px 60px rgba(0,0,0,.2);
      width: 100%; max-width: 28rem;
      display: flex; flex-direction: column;
    }
    .modal-header {
      display: flex; align-items: center; justify-content: space-between;
      padding: 1.25rem 1.5rem 0;
    }
    .modal-title { font-size: 1.0625rem; font-weight: 600; color: var(--stride-text-primary); margin: 0; }
    .modal-close {
      background: none; border: none; cursor: pointer;
      color: var(--stride-text-secondary); padding: .25rem; display: flex;
    }
    .modal-body { padding: 1rem 1.5rem; display: flex; flex-direction: column; gap: 1rem; }
    .modal-footer {
      display: flex; align-items: center; justify-content: flex-end; gap: .75rem;
      padding: .75rem 1.5rem 1.25rem;
      border-top: 1px solid var(--stride-border);
    }

    .form-field { display: flex; flex-direction: column; gap: .375rem; }
    .form-label { font-size: .875rem; font-weight: 500; color: var(--stride-text-primary); }
    .form-input, .form-textarea {
      padding: .5rem .75rem;
      border: 1px solid var(--stride-border);
      border-radius: var(--stride-radius-md);
      font-size: .875rem;
      color: var(--stride-text-primary);
      background: var(--stride-surface-input, #fff);
      width: 100%; box-sizing: border-box;
      transition: border-color 150ms;
    }
    .form-input:focus, .form-textarea:focus {
      outline: none; border-color: var(--stride-primary);
    }
    .form-textarea { resize: vertical; }
    .form-error { font-size: .8125rem; color: var(--stride-danger, #dc2626); }
    .required { color: var(--stride-danger, #dc2626); }

    /* ── Mobile ─── */
    @media (max-width: 600px) {
      .kfp-page { padding: 1rem; gap: 1.25rem; }
      .kfp-card-grid { grid-template-columns: 1fr; }
    }
  `],
})
export class KitFieldPageComponent implements OnInit {
  private readonly svc          = inject(KitOpsService);
  private readonly connectivity = inject(ConnectivityService);
  protected readonly kitSync    = inject(KitSyncService);
  private readonly kitQueue     = inject(KitOfflineQueueService);

  readonly isOffline = computed(() => !this.connectivity.isOnline());

  // ── Page state ────────────────────────────────────────────────────────────
  readonly catalog          = signal<KitCatalogItemDto[]>([]);
  readonly checkouts        = signal<CheckoutRow[]>([]);
  readonly reservations     = signal<MyKitReservationDto[]>([]);

  readonly catalogLoading      = signal(false);
  readonly checkoutsLoading    = signal(false);
  readonly reservationsLoading = signal(false);

  readonly error      = signal<string | null>(null);
  readonly successMsg = signal<string | null>(null);
  readonly actionInProgress = signal(false);

  readonly activeCheckouts    = computed(() => this.checkouts().filter(c => c.statusValue === 0 || c.statusValue === 2));
  readonly pendingReservations = computed(() => this.reservations().filter(r => r.statusValue === 0));

  // ── Modal state ────────────────────────────────────────────────────────────
  readonly modalMode    = signal<ModalMode>(null);
  readonly selectedItem = signal<KitCatalogItemDto | null>(null);
  modalDays  = 1;
  modalNotes = '';

  readonly modalDaysError = computed(() => {
    if (this.modalMode() !== 'checkout') return null;
    if (!this.modalDays || this.modalDays < 1) return 'Please enter at least 1 day.';
    return null;
  });
  readonly modalError = signal<string | null>(null);

  constructor() {
    // When we come back online after queuing actions, reload data once sync finishes.
    effect(() => {
      const syncing = this.kitSync.isSyncing();
      if (!syncing && !this.isOffline()) {
        // isSyncing just flipped to false while online — refresh to reflect server state.
        // Guard against the initial false→false at startup with pendingCount check.
        if (this.kitSync.pendingCount() === 0 && this.checkouts().length > 0) {
          this.loadAll();
        }
      }
    });
  }

  ngOnInit(): void {
    this.loadAll();
  }

  private loadAll(): void {
    this.loadCatalog();
    this.loadCheckouts();
    this.loadReservations();
  }

  private loadCatalog(): void {
    this.catalogLoading.set(true);
    this.svc.getCatalog().subscribe({
      next: items => { this.catalog.set(items); this.catalogLoading.set(false); },
      error: () => {
        this.error.set('Failed to load kit catalog.');
        this.catalogLoading.set(false);
      },
    });
  }

  private loadCheckouts(): void {
    this.checkoutsLoading.set(true);
    this.svc.getMyCheckouts().subscribe({
      next: list => { this.checkouts.set(list); this.checkoutsLoading.set(false); },
      error: () => {
        this.error.set('Failed to load your checkouts.');
        this.checkoutsLoading.set(false);
      },
    });
  }

  private loadReservations(): void {
    this.reservationsLoading.set(true);
    this.svc.getMyReservations().subscribe({
      next: list => { this.reservations.set(list); this.reservationsLoading.set(false); },
      error: () => {
        this.error.set('Failed to load your requests.');
        this.reservationsLoading.set(false);
      },
    });
  }

  isOverdue(co: MyKitCheckoutDto): boolean {
    return co.statusValue === 2 || (co.returnedAt === null && new Date(co.expectedReturnAt) < new Date());
  }

  // ── Modal ─────────────────────────────────────────────────────────────────
  openCheckoutModal(item: KitCatalogItemDto): void {
    this.selectedItem.set(item);
    this.modalDays  = 1;
    this.modalNotes = '';
    this.modalError.set(null);
    this.modalMode.set('checkout');
  }

  openRequestModal(item: KitCatalogItemDto): void {
    this.selectedItem.set(item);
    this.modalNotes = '';
    this.modalError.set(null);
    this.modalMode.set('request');
  }

  closeModal(): void {
    this.modalMode.set(null);
    this.selectedItem.set(null);
    this.modalError.set(null);
  }

  confirmModal(): void {
    if (this.modalMode() === 'checkout') {
      this.submitCheckout();
    } else {
      this.submitRequest();
    }
  }

  private submitCheckout(): void {
    if (this.modalDaysError()) return;
    const item = this.selectedItem();
    if (!item) return;

    const body = {
      kitItemId: item.kitItemId,
      days:      this.modalDays,
      notes:     this.modalNotes.trim() || null,
    };

    if (this.isOffline()) {
      this.queueCheckout(item, body);
      return;
    }

    this.actionInProgress.set(true);
    this.modalError.set(null);

    this.svc.checkout(body).subscribe({
      next: () => {
        this.actionInProgress.set(false);
        this.closeModal();
        this.flash(`Checked out "${item.name}" successfully.`);
        this.loadAll();
      },
      error: (err) => {
        this.actionInProgress.set(false);
        this.modalError.set(err?.error?.error ?? 'Checkout failed. Please try again.');
      },
    });
  }

  private async queueCheckout(item: KitCatalogItemDto, body: object): Promise<void> {
    await this.kitQueue.enqueue({
      id:          crypto.randomUUID(),
      type:        'checkout',
      kitItemId:   item.kitItemId,
      kitItemName: item.name,
      payload:     JSON.stringify(body),
      queuedAt:    Date.now(),
    });
    await this.kitSync.refreshCount();

    // Optimistic: decrement available count in catalog
    this.catalog.update(list =>
      list.map(i => i.kitItemId === item.kitItemId
        ? { ...i, availableQuantity: Math.max(0, i.availableQuantity - 1) }
        : i
      )
    );

    this.closeModal();
    this.flash(`Checkout for "${item.name}" queued — will sync when back online.`);
  }

  private submitRequest(): void {
    const item = this.selectedItem();
    if (!item) return;

    this.actionInProgress.set(true);
    this.modalError.set(null);

    this.svc.createReservation({
      kitItemId: item.kitItemId,
      notes:     this.modalNotes.trim() || null,
    }).subscribe({
      next: () => {
        this.actionInProgress.set(false);
        this.closeModal();
        this.flash(`Reservation request submitted for "${item.name}".`);
        this.loadReservations();
      },
      error: (err) => {
        this.actionInProgress.set(false);
        this.modalError.set(err?.error?.error ?? 'Request failed. Please try again.');
      },
    });
  }

  returnCheckout(checkoutId: string, kitItemName: string): void {
    if (this.isOffline()) {
      void this.queueReturn(checkoutId, kitItemName);
      return;
    }

    this.actionInProgress.set(true);
    this.svc.returnCheckout(checkoutId).subscribe({
      next: () => {
        this.actionInProgress.set(false);
        this.flash('Kit item returned successfully.');
        this.loadAll();
      },
      error: (err) => {
        this.actionInProgress.set(false);
        this.error.set(err?.error?.error ?? 'Return failed. Please try again.');
      },
    });
  }

  private async queueReturn(checkoutId: string, kitItemName: string): Promise<void> {
    await this.kitQueue.enqueue({
      id:          crypto.randomUUID(),
      type:        'return',
      checkoutId,
      kitItemName,
      payload:     '{}',
      queuedAt:    Date.now(),
    });
    await this.kitSync.refreshCount();

    // Optimistic: mark the checkout row as pending-return in local state
    this.checkouts.update(list =>
      list.map(c => c.checkoutId === checkoutId ? { ...c, pendingReturn: true } : c)
    );

    this.flash(`Return for "${kitItemName}" queued — will sync when back online.`);
  }

  cancelReservation(reservationId: string): void {
    this.actionInProgress.set(true);
    this.svc.cancelReservation(reservationId).subscribe({
      next: () => {
        this.actionInProgress.set(false);
        this.flash('Reservation cancelled.');
        this.loadReservations();
      },
      error: (err) => {
        this.actionInProgress.set(false);
        this.error.set(err?.error?.error ?? 'Cancel failed. Please try again.');
      },
    });
  }

  private flash(msg: string): void {
    this.successMsg.set(msg);
    setTimeout(() => this.successMsg.set(null), 4000);
  }
}
