import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { NgClass } from '@angular/common';
import { Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs/operators';

import { MyTask, STEP_INSTANCE_STATUS_CONFIG } from '../../models/workflow.models';
import { WorkflowService } from '../../services/workflow.service';

@Component({
  selector: 'app-my-tasks-page',
  standalone: true,
  imports: [NgClass],
  templateUrl: './my-tasks-page.html',
  styleUrl:    './my-tasks-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyTasksPageComponent implements OnInit {
  private readonly wfService  = inject(WorkflowService);
  private readonly router     = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly STEP_STATUS_CONFIG = STEP_INSTANCE_STATUS_CONFIG;

  // ── State ────────────────────────────────────────────────────────────────
  readonly tasks              = signal<MyTask[]>([]);
  readonly isLoading          = signal(true);
  readonly errorMessage       = signal<string | null>(null);
  readonly quickCompletingId  = signal<string | null>(null);
  readonly quickCompleteError = signal<string | null>(null);

  // ── Lifecycle ────────────────────────────────────────────────────────────

  ngOnInit(): void {
    this.loadTasks();
  }

  // ── Data loading ─────────────────────────────────────────────────────────

  loadTasks(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.wfService
      .getMyTasks()
      .pipe(
        finalize(() => this.isLoading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next:  data => this.tasks.set(data),
        error: ()   => this.errorMessage.set('Failed to load your tasks. Please try again.'),
      });
  }

  // ── Navigation ───────────────────────────────────────────────────────────

  openInstance(task: MyTask): void {
    this.router.navigate(['/workflows', 'instances', task.workflowInstanceId]);
  }

  // ── Quick complete ────────────────────────────────────────────────────────

  quickComplete(task: MyTask, event: Event): void {
    event.stopPropagation();
    this.quickCompleteError.set(null);
    this.quickCompletingId.set(task.stepInstanceId);

    this.wfService
      .completeStep(task.workflowInstanceId, task.stepInstanceId)
      .pipe(
        finalize(() => this.quickCompletingId.set(null)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next:  () => this.loadTasks(),
        error: (err) => {
          const msg = err?.error?.error ?? 'Could not complete step. Try opening the detail page.';
          this.quickCompleteError.set(msg);
        },
      });
  }

  canQuickComplete(task: MyTask): boolean {
    return task.stepStatus === 'Pending' || task.stepStatus === 'InProgress';
  }

  // ── Display helpers ──────────────────────────────────────────────────────

  stepStatusClass(status: string): string {
    return this.STEP_STATUS_CONFIG[status as keyof typeof this.STEP_STATUS_CONFIG]?.cssClass ?? '';
  }

  stepStatusLabel(status: string): string {
    return this.STEP_STATUS_CONFIG[status as keyof typeof this.STEP_STATUS_CONFIG]?.label ?? status;
  }

  stepStatusIcon(status: string): string {
    return this.STEP_STATUS_CONFIG[status as keyof typeof this.STEP_STATUS_CONFIG]?.icon ?? 'pi-circle';
  }

  relativeDate(iso: string | null): string {
    if (!iso) return '—';
    const diff = Date.now() - new Date(iso).getTime();
    const days = Math.floor(diff / 86_400_000);
    const hrs  = Math.floor(diff / 3_600_000);
    const mins = Math.floor(diff / 60_000);
    if (mins < 1)   return 'Just now';
    if (mins < 60)  return `${mins}m ago`;
    if (hrs  < 24)  return `${hrs}h ago`;
    if (days === 1) return 'Yesterday';
    if (days < 30)  return `${days}d ago`;
    return new Date(iso).toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' });
  }

  trackById(_: number, task: MyTask): string {
    return task.stepInstanceId;
  }
}
