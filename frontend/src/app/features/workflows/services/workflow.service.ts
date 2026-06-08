import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  WorkflowDefinitionSummary,
  WorkflowDefinitionDetail,
  WorkflowInstanceSummary,
  WorkflowInstanceDetail,
  CreateWorkflowRequest,
  UpdateWorkflowRequest,
  BillableItemInput,
  RoleDto,
  MyTask,
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
  private readonly http = inject(HttpClient);

  private readonly base       = '/bff/workflows';
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

  /** List all workflow instances, optionally filtered by definition. */
  getInstances(definitionId?: string): Observable<WorkflowInstanceSummary[]> {
    const url = definitionId
      ? `${this.base}/definitions/${definitionId}/instances`
      : `${this.base}/instances`;
    return this.http.get<WorkflowInstanceSummary[]>(url);
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

  /** Mark a step instance as completed, optionally with billable items. */
  completeStep(
    instanceId: string,
    stepId: string,
    billableItems?: BillableItemInput[],
  ): Observable<void> {
    const body = billableItems?.length
      ? { billableItems }
      : {};
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
}
