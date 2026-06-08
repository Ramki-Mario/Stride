import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  Input,
  OnChanges,
  SimpleChanges,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs/operators';

import { WorkflowService }           from '../../services/workflow.service';
import { WorkflowActivityEventDto }  from '../../models/workflow.models';

const PAGE_SIZE = 20;

/** Maps the numeric ActivityEventType to a Material-style icon name. */
function iconFor(eventType: number): string {
  switch (eventType) {
    case 1:  return 'play_circle';        // WorkflowStarted
    case 2:  return 'check_circle';       // WorkflowCompleted
    case 3:  return 'cancel';             // WorkflowCancelled
    case 4:  return 'alarm_off';          // WorkflowSlaBreached
    case 5:  return 'pause_circle';       // WorkflowPaused
    case 6:  return 'play_circle_filled'; // WorkflowResumed
    case 10: return 'person_pin';         // StepAssigned
    case 12: return 'task_alt';           // StepCompleted
    case 13: return 'error';              // StepFailed
    case 14: return 'skip_next';          // StepSkipped
    case 15: return 'schedule';           // StepOverdue
    case 20: return 'chat_bubble';        // CommentPosted
    case 30: return 'attach_file';        // AttachmentUploaded
    default: return 'info';
  }
}

/** Maps eventType to a CSS modifier class for colour. */
function colorFor(eventType: number): string {
  switch (eventType) {
    case 2:  return 'at--success';  // Completed
    case 3:  return 'at--error';    // Cancelled
    case 4:  return 'at--error';    // SLA breached
    case 13: return 'at--error';    // Step failed
    case 15: return 'at--warning';  // Step overdue
    default: return 'at--default';
  }
}

/**
 * Activity timeline for a workflow instance.
 *
 * Renders a vertical timeline of every recorded lifecycle event —
 * workflow and step transitions, comments posted, and file uploads.
 *
 * Inputs:
 *   instanceId  — the workflow instance to display (required)
 */
@Component({
  selector:        'app-activity-timeline',
  standalone:      true,
  imports:         [],
  templateUrl:     './activity-timeline.html',
  styleUrl:        './activity-timeline.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ActivityTimelineComponent implements OnChanges {
  @Input({ required: true }) instanceId!: string;

  private readonly svc        = inject(WorkflowService);
  private readonly destroyRef = inject(DestroyRef);

  // ── State ─────────────────────────────────────────────────────────────────
  readonly events     = signal<WorkflowActivityEventDto[]>([]);
  readonly totalCount = signal(0);
  readonly page       = signal(1);
  readonly isLoading  = signal(false);
  readonly loadError  = signal<string | null>(null);

  // ── Computed ──────────────────────────────────────────────────────────────
  readonly totalPages = computed(() =>
    Math.max(1, Math.ceil(this.totalCount() / PAGE_SIZE)),
  );
  readonly hasPrev = computed(() => this.page() > 1);
  readonly hasNext = computed(() => this.page() < this.totalPages());

  // ── Lifecycle ─────────────────────────────────────────────────────────────

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['instanceId'] && this.instanceId) {
      this.page.set(1);
      this.load();
    }
  }

  // ── Load ──────────────────────────────────────────────────────────────────

  load(): void {
    if (!this.instanceId) return;

    this.isLoading.set(true);
    this.loadError.set(null);

    this.svc.getActivityTimeline(this.instanceId, this.page(), PAGE_SIZE, 'desc')
      .pipe(
        finalize(() => this.isLoading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (result) => {
          this.events.set(result.items);
          this.totalCount.set(result.totalCount);
        },
        error: () => this.loadError.set('Failed to load activity timeline.'),
      });
  }

  // ── Pagination ────────────────────────────────────────────────────────────

  prevPage(): void {
    if (!this.hasPrev()) return;
    this.page.update(p => p - 1);
    this.load();
  }

  nextPage(): void {
    if (!this.hasNext()) return;
    this.page.update(p => p + 1);
    this.load();
  }

  // ── Helpers ───────────────────────────────────────────────────────────────

  iconFor   = iconFor;
  colorFor  = colorFor;

  formatRelative(iso: string): string {
    const diff  = Date.now() - new Date(iso).getTime();
    const secs  = Math.floor(diff / 1000);
    if (secs < 60)   return 'just now';
    const mins  = Math.floor(secs / 60);
    if (mins < 60)   return `${mins}m ago`;
    const hours = Math.floor(mins / 60);
    if (hours < 24)  return `${hours}h ago`;
    const days  = Math.floor(hours / 24);
    if (days < 30)   return `${days}d ago`;
    return new Date(iso).toLocaleDateString('en-GB', {
      day: 'numeric', month: 'short', year: 'numeric',
    });
  }

  formatAbsolute(iso: string): string {
    return new Date(iso).toLocaleString('en-GB', {
      day: 'numeric', month: 'short', year: 'numeric',
      hour: '2-digit', minute: '2-digit',
    });
  }

  shortId(id: string): string {
    return id.substring(0, 8).toUpperCase();
  }

  trackById(_: number, e: WorkflowActivityEventDto): string {
    return e.id;
  }
}
