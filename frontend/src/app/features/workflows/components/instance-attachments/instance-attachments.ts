import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  EventEmitter,
  Input,
  OnChanges,
  Output,
  SimpleChanges,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs/operators';

import { AttachmentService } from '../../services/attachment.service';
import {
  WorkflowAttachmentDto,
  ALLOWED_ATTACHMENT_MIME_TYPES,
  MAX_ATTACHMENT_SIZE_BYTES,
} from '../../models/attachment.models';

/**
 * Unified attachment management panel for a workflow instance.
 *
 * Shows all attachments — instance-level (job documents) and step-level (step evidence) —
 * grouped in separate sections. The top section also includes an upload zone for new
 * job-level documents.
 *
 * Emits `countChange` whenever the total attachment count changes so the parent
 * can show a badge on the tab button.
 */
@Component({
  selector: 'app-instance-attachments',
  standalone: true,
  imports: [],
  templateUrl: './instance-attachments.html',
  styleUrl:    './instance-attachments.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InstanceAttachmentsComponent implements OnChanges {
  @Input({ required: true }) instanceId!: string;
  @Input() currentUserId = '';

  /** Fires whenever the total attachment count changes. */
  @Output() countChange = new EventEmitter<number>();

  private readonly svc        = inject(AttachmentService);
  private readonly destroyRef = inject(DestroyRef);

  // ── State ─────────────────────────────────────────────────────────────────

  readonly allAttachments = signal<WorkflowAttachmentDto[]>([]);
  readonly isLoading      = signal(false);
  readonly loadError      = signal<string | null>(null);

  readonly isUploading  = signal(false);
  readonly uploadError  = signal<string | null>(null);

  readonly isDragOver  = signal(false);
  readonly deletingId  = signal<string | null>(null);
  readonly deleteError = signal<string | null>(null);

  // ── Computed ──────────────────────────────────────────────────────────────

  readonly allowedMimes = ALLOWED_ATTACHMENT_MIME_TYPES;

  /** Instance-level attachments (no step) — job documents. */
  readonly instanceAttachments = computed(() =>
    this.allAttachments().filter(a => a.stepInstanceId === null),
  );

  /**
   * Step-level attachments grouped by step.
   * Returns an array of `{ stepInstanceId, stepName, items[] }` sorted by step name.
   */
  readonly stepGroups = computed(() => {
    const map = new Map<string, { stepInstanceId: string; stepName: string; items: WorkflowAttachmentDto[] }>();

    for (const att of this.allAttachments()) {
      if (!att.stepInstanceId) continue;
      const key = att.stepInstanceId;
      if (!map.has(key)) {
        map.set(key, {
          stepInstanceId: att.stepInstanceId,
          stepName:       att.stepName ?? 'Unknown Step',
          items:          [],
        });
      }
      map.get(key)!.items.push(att);
    }

    return [...map.values()].sort((a, b) => a.stepName.localeCompare(b.stepName));
  });

  readonly totalCount = computed(() => this.allAttachments().length);

  // ── Lifecycle ─────────────────────────────────────────────────────────────

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['instanceId']) {
      this.load();
    }
  }

  // ── Load ──────────────────────────────────────────────────────────────────

  load(): void {
    if (!this.instanceId) return;

    this.isLoading.set(true);
    this.loadError.set(null);

    this.svc.listWorkflowAttachments(this.instanceId)
      .pipe(
        finalize(() => this.isLoading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: items => {
          this.allAttachments.set(items);
          this.countChange.emit(items.length);
        },
        error: () => this.loadError.set('Failed to load attachments.'),
      });
  }

  // ── Upload ────────────────────────────────────────────────────────────────

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file  = input.files?.[0];
    if (file) this.upload(file);
    input.value = '';
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.isDragOver.set(true);
  }

  onDragLeave(): void {
    this.isDragOver.set(false);
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.isDragOver.set(false);
    const file = event.dataTransfer?.files[0];
    if (file) this.upload(file);
  }

  private upload(file: File): void {
    this.uploadError.set(null);

    if (file.size > MAX_ATTACHMENT_SIZE_BYTES) {
      this.uploadError.set(`File "${file.name}" exceeds the 10 MB limit.`);
      return;
    }

    this.isUploading.set(true);

    this.svc.uploadInstanceAttachment(this.instanceId, file)
      .pipe(
        finalize(() => this.isUploading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next:  () => this.load(),
        error: (err) => {
          const msg = err?.error?.error ?? 'Upload failed. Check the file type and size.';
          this.uploadError.set(msg);
        },
      });
  }

  // ── Download ──────────────────────────────────────────────────────────────

  downloadUrl(attachmentId: string): string {
    return this.svc.instanceAttachmentDownloadUrl(this.instanceId, attachmentId);
  }

  // ── Delete ────────────────────────────────────────────────────────────────

  deleteAttachment(att: WorkflowAttachmentDto): void {
    if (!confirm(`Delete "${att.fileName}"? This cannot be undone.`)) return;

    this.deletingId.set(att.id);
    this.deleteError.set(null);

    this.svc.deleteInstanceAttachment(this.instanceId, att.id)
      .pipe(
        finalize(() => this.deletingId.set(null)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next:  () => {
          const updated = this.allAttachments().filter(a => a.id !== att.id);
          this.allAttachments.set(updated);
          this.countChange.emit(updated.length);
        },
        error: (err) => {
          const msg = err?.error?.error ?? 'Could not delete attachment.';
          this.deleteError.set(msg);
        },
      });
  }

  // ── Helpers ───────────────────────────────────────────────────────────────

  canDelete(att: WorkflowAttachmentDto): boolean {
    return !this.currentUserId || att.uploadedByUserId === this.currentUserId;
  }

  fileIcon(contentType: string): string {
    if (contentType.startsWith('image/'))                                    return 'pi pi-image';
    if (contentType === 'application/pdf')                                   return 'pi pi-file-pdf';
    if (contentType.includes('word') || contentType.includes('document'))   return 'pi pi-file-word';
    if (contentType === 'text/plain')                                        return 'pi pi-file';
    return 'pi pi-paperclip';
  }

  formatSize(bytes: number): string {
    if (bytes < 1024)         return `${bytes} B`;
    if (bytes < 1024 * 1024)  return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(2)} MB`;
  }
}
