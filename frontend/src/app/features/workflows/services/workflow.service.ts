import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, from } from 'rxjs';
import { map } from 'rxjs/operators';
import { ConnectivityService } from '../../../core/pwa/connectivity.service';
import { OfflineQueueService } from '../../../core/pwa/offline-queue.service';
import {
  WorkflowDefinitionSummary,
  WorkflowDefinitionDetail,
  WorkflowInstanceSummary,
  WorkflowInstanceDetail,
  CreateWorkflowRequest,
  UpdateWorkflowRequest,
  BillableItemInput,
  FieldValueInput,
  RoleDto,
  MyTask,
  PagedCommentsDto,
  PagedActivityDto,
  MentionSuggestionDto,
} from '../models/workflow.models';

/**
 * Calls the BFF endpoints for workflow data.
 *
 * BFF route → Host route mapping:
 *   GET  /bff/workflows/definitions          → GET /api/workflows
 *   GET  /bff/workflows/definitions/:id      → GET /api/workflows/:id
 *   POST /bff/workflows/definitions          → POST /api/workflows
 *   GET  /bff/workflows/instances            → GET /api/workflows/instances (all, tenant-scoped)
 *   GET  /bff/workflows/definitions/:id/instances → GET /api/workflows/:id/instances
 *
 * Note: BFF workflow proxy is wired in EP-021 (Host) but the BFF forwarding rules
 * are deferred until the BFF project is extended in a future epic. The URL paths
 * here are the intended BFF surface — change only the base path if the BFF routes differ.
 */
@Injectable({ providedIn: 'root' })
export class WorkflowService {
  private readonly http         = inject(HttpClient);
  private readonly connectivity = inject(ConnectivityService);
  private readonly queue        = inject(OfflineQueueService);

  private readonly base         = '/bff/workflows';
  private readonly identityBase = '/bff/identity';

  /** Returns all active roles for the tenant, used by the workflow builder role dropdown. */
  listRoles(): Observable<RoleDto[]> {
    return this.http.get<RoleDto[]>(`${this.identityBase}/roles`);
  }

  /** Returns all non-terminal step instances assigned to the current user. */
  getMyTasks(): Observable<MyTask[]> {
    return this.http.get<MyTask[]>(`${this.base}/my-tasks`);
  }

  /** List all workflow definitions for the current tenant. */
  getDefinitions(): Observable<WorkflowDefinitionSummary[]> {
    return this.http.get<WorkflowDefinitionSummary[]>(`${this.base}/definitions`);
  }

  /** Get a single workflow definition with all step definitions. */
  getDefinition(id: string): Observable<WorkflowDefinitionDetail> {
    return this.http.get<WorkflowDefinitionDetail>(`${this.base}/definitions/${id}`);
  }

  /** Activate a Draft workflow definition (Draft → Active). */
  activateDefinition(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/definitions/${id}/activate`, {});
  }

  /** Start a new workflow instance from an Active definition. */
  startInstance(definitionId: string): Observable<{ workflowInstanceId: string; workflowName: string }> {
    return this.http.post<{ workflowInstanceId: string; workflowName: string }>(
      `${this.base}/definitions/${definitionId}/start`, {},
    );
  }

  /** List all workflow instances, optionally filtered by definition or team. */
  getInstances(definitionId?: string, teamId?: string): Observable<WorkflowInstanceSummary[]> {
    if (definitionId) {
      return this.http.get<WorkflowInstanceSummary[]>(
        `${this.base}/definitions/${definitionId}/instances`,
      );
    }
    const params = teamId ? new HttpParams().set('teamId', teamId) : undefined;
    return this.http.get<WorkflowInstanceSummary[]>(`${this.base}/instances`, { params });
  }

  /** Create a new workflow definition (Draft status). */
  createDefinition(req: CreateWorkflowRequest): Observable<{ workflowDefinitionId: string }> {
    return this.http.post<{ workflowDefinitionId: string }>(`${this.base}/definitions`, req);
  }

  /** Update a Draft workflow definition's name and description. */
  updateDefinition(id: string, req: UpdateWorkflowRequest): Observable<void> {
    return this.http.put<void>(`${this.base}/definitions/${id}`, req);
  }

  /** Soft-delete a workflow definition. */
  deleteDefinition(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/definitions/${id}`);
  }

  /**
   * Assigns (or clears) a team on a workflow instance.
   * Pass null to remove the current team assignment.
   */
  assignTeam(instanceId: string, teamId: string | null): Observable<void> {
    return this.http.put<void>(
      `${this.base}/instances/${instanceId}/assign-team`,
      { teamId },
    );
  }

  /** Cancel a running workflow instance. */
  cancelInstance(instanceId: string): Observable<void> {
    return this.http.post<void>(`${this.base}/instances/${instanceId}/cancel`, {});
  }

  /** Get a single workflow instance with all step instances and their status. */
  getInstanceDetail(instanceId: string): Observable<WorkflowInstanceDetail> {
    return this.http.get<WorkflowInstanceDetail>(`${this.base}/instances/${instanceId}`);
  }

  /** Assign a step instance to a user (provide their userId as assigneeId). */
  assignStep(instanceId: string, stepId: string, assigneeId: string): Observable<void> {
    return this.http.post<void>(
      `${this.base}/instances/${instanceId}/steps/${stepId}/assign`,
      { assigneeId },
    );
  }

  /** Claim a step for the current user (self-assignment). */
  claimStep(instanceId: string, stepId: string): Observable<void> {
    return this.http.post<void>(
      `${this.base}/instances/${instanceId}/steps/${stepId}/claim`,
      {},
    );
  }

  /** Mark a step instance as completed, optionally with billable items.
   *  When offline, queues the completion in IndexedDB for later sync. */
  completeStep(
    instanceId: string,
    stepId: string,
    billableItems?: BillableItemInput[],
    fieldValues?: FieldValueInput[],
  ): Observable<void> {
    const body: { billableItems?: BillableItemInput[]; fieldValues?: FieldValueInput[] } = {};
    if (billableItems?.length) body.billableItems = billableItems;
    if (fieldValues?.length) body.fieldValues = fieldValues;

    if (!this.connectivity.isOnline()) {
      // Queue for later and report success immediately so the UI can move on.
      const queued = from(this.queue.enqueue({
        id:         crypto.randomUUID(),
        instanceId,
        stepId,
        payload:    JSON.stringify(body),
        queuedAt:   Date.now(),
      })).pipe(map(() => undefined as void));
      return queued;
    }

    return this.http.post<void>(
      `${this.base}/instances/${instanceId}/steps/${stepId}/complete`,
      body,
    );
  }

  /** Mark a step instance as failed, providing a mandatory failure reason. */
  failStep(instanceId: string, stepId: string, reason: string): Observable<void> {
    return this.http.post<void>(
      `${this.base}/instances/${instanceId}/steps/${stepId}/fail`,
      { reason },
    );
  }

  /** Skip a step instance (permitted for optional steps only). */
  skipStep(instanceId: string, stepId: string): Observable<void> {
    return this.http.post<void>(
      `${this.base}/instances/${instanceId}/steps/${stepId}/skip`,
      {},
    );
  }

  // ── Comment thread ─────────────────────────────────────────────────────────

  /** Returns a page of comments for a workflow instance (chronological order). */
  listComments(instanceId: string, page = 1, pageSize = 20): Observable<PagedCommentsDto> {
    return this.http.get<PagedCommentsDto>(
      `${this.base}/instances/${instanceId}/comments`,
      { params: { page: page.toString(), pageSize: pageSize.toString() } },
    );
  }

  /** Posts a new comment on a workflow instance. */
  createComment(instanceId: string, body: string): Observable<{ commentId: string }> {
    return this.http.post<{ commentId: string }>(
      `${this.base}/instances/${instanceId}/comments`,
      { body },
    );
  }

  /** Edits an existing comment (author-only). */
  editComment(instanceId: string, commentId: string, newBody: string): Observable<void> {
    return this.http.put<void>(
      `${this.base}/instances/${instanceId}/comments/${commentId}`,
      { newBody },
    );
  }

  /** Soft-deletes a comment (author or manager). */
  deleteComment(instanceId: string, commentId: string): Observable<void> {
    return this.http.delete<void>(
      `${this.base}/instances/${instanceId}/comments/${commentId}`,
    );
  }

  /** Returns a page of activity timeline events for a workflow instance. */
  getActivityTimeline(
    instanceId: string,
    page       = 1,
    pageSize   = 20,
    order: 'asc' | 'desc' = 'asc',
  ): Observable<PagedActivityDto> {
    return this.http.get<PagedActivityDto>(
      `${this.base}/instances/${instanceId}/activity`,
      { params: { page, pageSize, order } },
    );
  }

  // ── @mention autocomplete ──────────────────────────────────────────────────

  /**
   * Searches tenant users by display name or email prefix, returning a small
   * result set for @mention autocomplete.  Reuses the administration users
   * endpoint — no new BFF route is needed.
   */
  searchUsersForMention(query: string, pageSize = 8): Observable<MentionSuggestionDto[]> {
    const params = new HttpParams()
      .set('search',   query)
      .set('pageSize', pageSize)
      .set('page',     1);

    return this.http.get<{ items: MentionSuggestionDto[] }>(
      '/bff/administration/users',
      { params },
    ).pipe(map(r => r.items));
  }

  /** Export all captured step field values for an instance as a CSV blob. */
  exportFieldValuesCsv(instanceId: string): Observable<Blob> {
    return this.http.get(
      `${this.base}/instances/${instanceId}/field-values/csv`,
      { responseType: 'blob' },
    );
  }
}
