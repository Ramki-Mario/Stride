import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  Input,
  OnChanges,
  OnInit,
  SimpleChanges,
  computed,
  inject,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap, catchError, finalize } from 'rxjs/operators';
import { of } from 'rxjs';

import { WorkflowService }       from '../../services/workflow.service';
import { WorkflowCommentDto, MentionSuggestionDto } from '../../models/workflow.models';

const PAGE_SIZE = 20;

/** A segment of a rendered comment body — either plain text or a @mention highlight. */
export interface CommentSegment {
  text: string;
  isMention: boolean;
}

/**
 * Comment thread for a workflow instance.
 *
 * Renders a chronological list of comments with inline edit/delete,
 * a new-comment textarea, and pagination.
 *
 * Supports @mention autocomplete: type @ followed by one or more characters to
 * see a dropdown of matching users. Select a user to insert @emailLocalPart.
 * Posted @mentions are highlighted in rendered comment text.
 *
 * Inputs:
 *   instanceId    — the workflow instance to thread on (required)
 *   currentUserId — ID of the logged-in user (for edit/delete permission checks)
 *   isManager     — true when the current user has the TenantAdmin role
 *                   (managers can soft-delete others' comments)
 */
@Component({
  selector: 'app-comment-thread',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './comment-thread.html',
  styleUrl:    './comment-thread.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CommentThreadComponent implements OnChanges, OnInit {
  @Input({ required: true }) instanceId!:   string;
  @Input() currentUserId = '';
  @Input() isManager     = false;

  private readonly svc        = inject(WorkflowService);
  private readonly destroyRef = inject(DestroyRef);

  // ── List state ────────────────────────────────────────────────────────────
  readonly comments    = signal<WorkflowCommentDto[]>([]);
  readonly totalCount  = signal(0);
  readonly page        = signal(1);
  readonly isLoading   = signal(false);
  readonly loadError   = signal<string | null>(null);

  // ── New comment state ─────────────────────────────────────────────────────
  readonly newBody    = signal('');
  readonly isPosting  = signal(false);
  readonly postError  = signal<string | null>(null);

  // ── Edit state ────────────────────────────────────────────────────────────
  readonly editingId  = signal<string | null>(null);
  readonly editBody   = signal('');
  readonly isSaving   = signal(false);
  readonly saveError  = signal<string | null>(null);

  // ── Delete state ──────────────────────────────────────────────────────────
  readonly deletingId  = signal<string | null>(null);
  readonly deleteError = signal<string | null>(null);

  // ── @mention autocomplete state ───────────────────────────────────────────
  /** null = dropdown closed; string = the partial word typed after @ */
  readonly mentionQuery       = signal<string | null>(null);
  readonly mentionSuggestions = signal<MentionSuggestionDto[]>([]);
  readonly mentionLoading     = signal(false);
  /** Which textarea is currently driving the mention dropdown. */
  private activeTextareaEl: HTMLTextAreaElement | null = null;
  /** Which body signal maps to the active textarea. */
  private activeMentionBody: (() => string) & { set: (v: string) => void } | null = null;

  /** Subject used to debounce mention search calls. */
  private readonly mentionSearch$ = new Subject<string>();

  // ── Computed ──────────────────────────────────────────────────────────────
  readonly totalPages = computed(() =>
    Math.max(1, Math.ceil(this.totalCount() / PAGE_SIZE)),
  );

  readonly hasPrev = computed(() => this.page() > 1);
  readonly hasNext = computed(() => this.page() < this.totalPages());

  // ── Lifecycle ─────────────────────────────────────────────────────────────

  ngOnInit(): void {
    // Debounce @mention searches to avoid hammering the server on each keystroke.
    this.mentionSearch$.pipe(
      debounceTime(200),
      distinctUntilChanged(),
      switchMap(q => {
        if (!q) return of([]);
        this.mentionLoading.set(true);
        return this.svc.searchUsersForMention(q).pipe(
          catchError(() => of([])),
          finalize(() => this.mentionLoading.set(false)),
        );
      }),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe(suggestions => this.mentionSuggestions.set(suggestions));
  }

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

    this.svc.listComments(this.instanceId, this.page(), PAGE_SIZE)
      .pipe(
        finalize(() => this.isLoading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (result) => {
          this.comments.set(result.items);
          this.totalCount.set(result.totalCount);
        },
        error: () => this.loadError.set('Failed to load comments.'),
      });
  }

  // ── Post ──────────────────────────────────────────────────────────────────

  post(): void {
    const body = this.newBody().trim();
    if (!body) return;

    this.isPosting.set(true);
    this.postError.set(null);

    this.svc.createComment(this.instanceId, body)
      .pipe(
        finalize(() => this.isPosting.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.newBody.set('');
          // Jump to the last page so the user sees their new comment.
          const lastPage = Math.max(1, Math.ceil((this.totalCount() + 1) / PAGE_SIZE));
          this.page.set(lastPage);
          this.load();
        },
        error: (err) => {
          this.postError.set(err?.error?.detail ?? 'Failed to post comment.');
        },
      });
  }

  // ── Edit ──────────────────────────────────────────────────────────────────

  startEdit(comment: WorkflowCommentDto): void {
    this.editingId.set(comment.id);
    this.editBody.set(comment.body);
    this.saveError.set(null);
    this.closeMention();
  }

  cancelEdit(): void {
    this.editingId.set(null);
    this.editBody.set('');
    this.saveError.set(null);
    this.closeMention();
  }

  saveEdit(commentId: string): void {
    const body = this.editBody().trim();
    if (!body) return;

    this.isSaving.set(true);
    this.saveError.set(null);

    this.svc.editComment(this.instanceId, commentId, body)
      .pipe(
        finalize(() => this.isSaving.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.editingId.set(null);
          this.editBody.set('');
          this.load();
        },
        error: (err) => {
          this.saveError.set(err?.error?.detail ?? 'Failed to save edit.');
        },
      });
  }

  // ── Delete ────────────────────────────────────────────────────────────────

  deleteComment(comment: WorkflowCommentDto): void {
    if (!confirm('Delete this comment? The text will be replaced with "(deleted)".')) return;

    this.deletingId.set(comment.id);
    this.deleteError.set(null);

    this.svc.deleteComment(this.instanceId, comment.id)
      .pipe(
        finalize(() => this.deletingId.set(null)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => this.load(),
        error: (err) => {
          this.deleteError.set(err?.error?.detail ?? 'Failed to delete comment.');
        },
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

  // ── @mention autocomplete ─────────────────────────────────────────────────

  /**
   * Called by (input) on the compose or edit textarea.
   * Inspects the text before the caret to detect an in-progress @mention.
   */
  onInput(event: Event, bodySignal: WritableSignal<string>): void {
    const textarea = event.target as HTMLTextAreaElement;
    bodySignal.set(textarea.value);

    const caret  = textarea.selectionStart ?? textarea.value.length;
    const before = textarea.value.substring(0, caret);
    const match  = before.match(/@([\w.\-]*)$/);

    if (match) {
      this.activeTextareaEl  = textarea;
      this.activeMentionBody = bodySignal as unknown as (() => string) & { set: (v: string) => void };
      this.mentionQuery.set(match[1]);
      this.mentionSearch$.next(match[1]);
    } else {
      this.closeMention();
    }
  }

  /**
   * Called when the user selects a suggestion from the dropdown.
   * Replaces the partial @handle before the caret with the full @emailLocalPart.
   */
  pickMention(suggestion: MentionSuggestionDto, event: MouseEvent): void {
    // Prevent the textarea blur that would otherwise close the dropdown before this fires.
    event.preventDefault();

    const textarea  = this.activeTextareaEl;
    const bodyFn    = this.activeMentionBody;
    if (!textarea || !bodyFn) { this.closeMention(); return; }

    const localPart = suggestion.email.split('@')[0].toLowerCase();
    const caret     = textarea.selectionStart ?? textarea.value.length;
    const before    = textarea.value.substring(0, caret);
    const after     = textarea.value.substring(caret);
    const replaced  = before.replace(/@([\w.\-]*)$/, `@${localPart} `);

    bodyFn.set(replaced + after);
    this.closeMention();

    // Restore focus and move caret to end of inserted token.
    setTimeout(() => {
      textarea.focus();
      const pos = replaced.length;
      textarea.setSelectionRange(pos, pos);
    }, 0);
  }

  /** Closes the mention dropdown and clears related state. */
  closeMention(): void {
    this.mentionQuery.set(null);
    this.mentionSuggestions.set([]);
    this.activeTextareaEl  = null;
    this.activeMentionBody = null;
  }

  /**
   * Derives the email local-part from a full email address.
   * Used in the suggestion dropdown to show the @handle that will be inserted.
   */
  localPart(email: string): string {
    return email.split('@')[0].toLowerCase();
  }

  // ── Mention rendering ─────────────────────────────────────────────────────

  /**
   * Splits a comment body into alternating plain-text and @mention segments
   * so the template can render mentions as highlighted spans.
   */
  parseMentions(body: string): CommentSegment[] {
    const mentionPattern = /@([\w.\-]+)/g;
    const segments: CommentSegment[] = [];
    let lastIndex = 0;
    let match: RegExpExecArray | null;

    while ((match = mentionPattern.exec(body)) !== null) {
      if (match.index > lastIndex) {
        segments.push({ text: body.substring(lastIndex, match.index), isMention: false });
      }
      segments.push({ text: match[0], isMention: true });
      lastIndex = match.index + match[0].length;
    }

    if (lastIndex < body.length) {
      segments.push({ text: body.substring(lastIndex), isMention: false });
    }

    return segments;
  }

  // ── Permissions ───────────────────────────────────────────────────────────

  isOwnComment(comment: WorkflowCommentDto): boolean {
    return !!this.currentUserId && comment.authorId === this.currentUserId;
  }

  canEdit(comment: WorkflowCommentDto): boolean {
    return !comment.isDeleted && this.isOwnComment(comment);
  }

  canDelete(comment: WorkflowCommentDto): boolean {
    return !comment.isDeleted && (this.isOwnComment(comment) || this.isManager);
  }

  // ── Helpers ───────────────────────────────────────────────────────────────

  formatRelative(iso: string): string {
    const diff = Date.now() - new Date(iso).getTime();
    const secs  = Math.floor(diff / 1000);
    if (secs < 60)   return 'just now';
    const mins = Math.floor(secs / 60);
    if (mins < 60)   return `${mins}m ago`;
    const hours = Math.floor(mins / 60);
    if (hours < 24)  return `${hours}h ago`;
    const days = Math.floor(hours / 24);
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

  trackById(_: number, c: WorkflowCommentDto): string {
    return c.id;
  }

  trackBySuggestion(_: number, s: MentionSuggestionDto): string {
    return s.id;
  }
}

/** TypeScript helper — WritableSignal shape used in the mention insertion path. */
interface WritableSignal<T> {
  (): T;
  set(value: T): void;
}
