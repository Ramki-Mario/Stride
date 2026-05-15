import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { WorkflowDefinitionSummary, WorkflowInstanceSummary } from '../models/workflow.models';

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

  private readonly base = '/bff/workflows';

  /** List all workflow definitions for the current tenant. */
  getDefinitions(): Observable<WorkflowDefinitionSummary[]> {
    return this.http.get<WorkflowDefinitionSummary[]>(`${this.base}/definitions`);
  }

  /** List all workflow instances, optionally filtered by definition. */
  getInstances(definitionId?: string): Observable<WorkflowInstanceSummary[]> {
    const url = definitionId
      ? `${this.base}/definitions/${definitionId}/instances`
      : `${this.base}/instances`;
    return this.http.get<WorkflowInstanceSummary[]>(url);
  }

  /** Soft-delete a workflow definition. */
  deleteDefinition(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/definitions/${id}`);
  }

  /** Cancel a running workflow instance. */
  cancelInstance(instanceId: string): Observable<void> {
    return this.http.post<void>(`${this.base}/instances/${instanceId}/cancel`, {});
  }
}
