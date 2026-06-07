import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  Input,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs/operators';

import { AttachmentService } from '../../services/attachment.service';
import {
  AttachmentDto,
  ALLOWED_ATTACHMENT_MIME_TYPES,
  MAX_ATTACHMENT_SIZE_BYTES,
} from '../../models/attachment.models';

@Component({
  selector: 'app-step-attachments',
  standalone: true,
  imports: [],
  templateUrl: './step-attachments.html',
  styleUrl:    './step-attachments.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StepAttachmentsComponent implements OnInit {
  @Input({ required: true }) instanceId!: string;
  @Input({ required: true }) stepId!:     string;
  @Input() currentUserId = '';

  private readonly svc        = inject(AttachmentService);
  private readonly destroyRef = inject(DestroyRef);

  // ── State ─────────────────────────────────────────────────────────────────

  readonly attachments  = signal<AttachmentDto[]>([]);
  readonly isLoading    = signal(false);
  readonly loadError    = signal<string | null>(null);

  readonly isUploading  = signal(false);
  readonly uploadError  = signal<string | null>(null);

  readonly isDragOver   = signal(false);
  readonly deletingId   = signal<string | null>(null);
  readonly deleteError  = signal<string | null>(null);

  // ── Computed ──────────────────────────────────────────────────────────────

  readonly allowedMimes = ALLOWED_ATTACHMENT_MIME_TYPES;
  readonly hasAttachments = computed(() => this.attachments().length > 0);

  // ── Lifecycle ─────────────────────────────────────────────────────────────

  ngOnInit(): void {
    this.load();
  }

  // ── Load ──────────────────────────────────────────────────────────────────

  load(): void {
    this.isLoading.set(true);
    this.loadError.set(null);

    this.svc.listStepAttachments(this.instanceId, this.stepId)
      .pipe(
        finalize(() => this.isLoading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next:  items => this.attachments.set(items),
        error: ()    => this.loadError.set('Failed to load attachments.'),
      });
  }

  // ── Upload ────────────────────────────────────────────────────────────────

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file  = input.files?.[0];
    if (file) this.upload(file);
    input.value = '';  // reset so same file can be re-selected
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

    // Client-side pre-checks (server re-validates; this just gives faster feedback)
    if (file.size > MAX_ATTACHMENT_SIZE_BYTES) {
      this.uploadError.set(`File "${file.name}" exceeds the 10 MB limit.`);
      return;
    }

    this.isUploading.set(true);

    this.svc.uploadStepAttachment(this.instanceId, this.stepId, file)
      .pipe(
        finalize(() => this.isUploading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next:  () => this.load(),   // refresh list on success
        error: (err) => {
          const msg = err?.error?.error ?? 'Upload failed. Check the file type and size.';
          this.uploadError.set(msg);
        },
      });
  }

  // ── Download ──────────────────────────────────────────────────────────────

  downloadUrl(attachmentId: string): string {
    return this.svc.stepAttachmentDownloadUrl(this.instanceId, this.stepId, attachmentId);
  }

  // ── Delete ────────────────────────────────────────────────────────────────

  deleteAttachment(att: AttachmentDto): void {
    if (!confirm(`Delete "${att.fileName}"? This cannot be undone.`)) return;

    this.deletingId.set(att.id);
    this.deleteError.set(null);

    this.svc.deleteStepAttachment(this.instanceId, this.stepId, att.id)
      .pipe(
        finalize(() => this.deletingId.set(null)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next:  () => this.attachments.update(list => list.filter(a => a.id !== att.id)),
        error: (err) => {
          const msg = err?.error?.error ?? 'Could not delete attachment.';
          this.deleteError.set(msg);
        },
      });
  }

  // ── Helpers ───────────────────────────────────────────────────────────────

  /** PrimeIcon class based on MIME type. */
  fileIcon(contentType: string): string {
    if (contentType.startsWith('image/'))                         return 'pi pi-image';
    if (contentType === 'application/pdf')                        return 'pi pi-file-pdf';
    if (contentType.includes('word') || contentType.includes('document')) return 'pi pi-file-word';
    if (contentType === 'text/plain')                             return 'pi pi-file';
    return 'pi pi-paperclip';
  }

  /** Human-readable file size (KB / MB). */
  formatSize(bytes: number): string {
    if (bytes < 1024)         return `${bytes} B`;
    if (bytes < 1024 * 1024)  return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(2)} MB`;
  }

  /** True if the current user may delete this attachment. */
  canDelete(att: AttachmentDto): boolean {
    return !this.currentUserId || att.uploadedByUserId === this.currentUserId;
  }
}
