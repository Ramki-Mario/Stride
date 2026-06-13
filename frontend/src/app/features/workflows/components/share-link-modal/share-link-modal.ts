import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  EventEmitter,
  Input,
  OnInit,
  Output,
  inject,
  signal,
} from '@angular/core';
import { DatePipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs/operators';

import { WorkflowService } from '../../services/workflow.service';
import { SharedLink } from '../../models/workflow.models';

/**
 * ShareLinkModalComponent (EP-058 / US-177)
 *
 * Lets a workflow owner generate, copy, and revoke secure client-facing links for a
 * workflow instance. The token in each link is the only credential — these links give
 * unauthenticated read access to the public job view (US-178), so revocation is immediate.
 */
@Component({
  selector: 'app-share-link-modal',
  standalone: true,
  imports: [DatePipe],
  templateUrl: './share-link-modal.html',
  styleUrl: './share-link-modal.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShareLinkModalComponent implements OnInit {
  private readonly wfService = inject(WorkflowService);
  private readonly destroyRef = inject(DestroyRef);

  @Input({ required: true }) instanceId!: string;
  @Output() dismissed = new EventEmitter<void>();

  readonly links        = signal<SharedLink[]>([]);
  readonly isLoading    = signal(true);
  readonly isGenerating = signal(false);
  readonly error        = signal<string | null>(null);
  /** Id of the link whose URL was most recently copied — drives the "Copied!" affordance. */
  readonly copiedId     = signal<string | null>(null);
  /** Id of a link currently being revoked, to disable its button. */
  readonly revokingId   = signal<string | null>(null);

  ngOnInit(): void {
    this.loadLinks();
  }

  dismiss(): void {
    this.dismissed.emit();
  }

  private loadLinks(): void {
    this.isLoading.set(true);
    this.wfService
      .listSharedLinks(this.instanceId)
      .pipe(finalize(() => this.isLoading.set(false)), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next:  links => this.links.set(links),
        error: ()    => this.error.set('Could not load shared links.'),
      });
  }

  generate(): void {
    this.isGenerating.set(true);
    this.error.set(null);
    this.wfService
      .createSharedLink(this.instanceId)
      .pipe(finalize(() => this.isGenerating.set(false)), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: link => {
          this.links.update(current => [link, ...current]);
          void this.copy(link); // immediately copy the fresh link — the common next action
        },
        error: () => this.error.set('Could not generate a link. Please try again.'),
      });
  }

  async copy(link: SharedLink): Promise<void> {
    try {
      await navigator.clipboard.writeText(link.shareUrl);
      this.copiedId.set(link.id);
      setTimeout(() => {
        if (this.copiedId() === link.id) this.copiedId.set(null);
      }, 2000);
    } catch {
      // Clipboard API unavailable (insecure context) — surface the URL so the user can copy manually.
      this.error.set('Copy failed — select and copy the link manually.');
    }
  }

  revoke(link: SharedLink): void {
    this.revokingId.set(link.id);
    this.wfService
      .revokeSharedLink(this.instanceId, link.id)
      .pipe(finalize(() => this.revokingId.set(null)), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next:  () => this.links.update(current => current.filter(l => l.id !== link.id)),
        error: () => this.error.set('Could not revoke the link. Please try again.'),
      });
  }
}
