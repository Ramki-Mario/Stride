import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AttachmentDto, WorkflowAttachmentDto } from '../models/attachment.models';

/**
 * Handles file attachment operations for workflow step instances and workflow instances.
 *
 * Step-level BFF routes:
 *   POST   /bff/workflows/instances/{iId}/steps/{sId}/attachments
 *   GET    /bff/workflows/instances/{iId}/steps/{sId}/attachments
 *   GET    /bff/workflows/instances/{iId}/steps/{sId}/attachments/{id}/download
 *   DELETE /bff/workflows/instances/{iId}/steps/{sId}/attachments/{id}
 *
 * Instance-level BFF routes (job documents — not tied to a step):
 *   POST   /bff/workflows/instances/{iId}/attachments
 *   GET    /bff/workflows/instances/{iId}/attachments     (returns all: step + instance)
 *   GET    /bff/workflows/instances/{iId}/attachments/{id}/download
 *   DELETE /bff/workflows/instances/{iId}/attachments/{id}
 */
@Injectable({ providedIn: 'root' })
export class AttachmentService {
  private readonly http = inject(HttpClient);

  private stepBase(instanceId: string, stepId: string): string {
    return `/bff/workflows/instances/${instanceId}/steps/${stepId}/attachments`;
  }

  /** Returns all non-deleted attachments for a step instance, newest first. */
  listStepAttachments(
    instanceId: string,
    stepId: string,
  ): Observable<AttachmentDto[]> {
    return this.http.get<AttachmentDto[]>(this.stepBase(instanceId, stepId));
  }

  /**
   * Uploads a single file as multipart/form-data.
   * Returns `{ id: string }` on success.
   */
  uploadStepAttachment(
    instanceId: string,
    stepId: string,
    file: File,
  ): Observable<{ id: string }> {
    const form = new FormData();
    form.append('file', file, file.name);
    return this.http.post<{ id: string }>(this.stepBase(instanceId, stepId), form);
  }

  /**
   * Returns the download URL for direct browser navigation.
   * The BFF injects the session cookie automatically, so `<a href="...">` works.
   */
  stepAttachmentDownloadUrl(
    instanceId: string,
    stepId: string,
    attachmentId: string,
  ): string {
    return `${this.stepBase(instanceId, stepId)}/${attachmentId}/download`;
  }

  /** Soft-deletes an attachment. Returns 204 on success. */
  deleteStepAttachment(
    instanceId: string,
    stepId: string,
    attachmentId: string,
  ): Observable<void> {
    return this.http.delete<void>(
      `${this.stepBase(instanceId, stepId)}/${attachmentId}`,
    );
  }

  // ── Instance-level attachments ──────────────────────────────────────────

  private instanceBase(instanceId: string): string {
    return `/bff/workflows/instances/${instanceId}/attachments`;
  }

  /**
   * Returns all non-deleted attachments for a workflow instance, both step-level
   * (with stepInstanceId/stepName set) and instance-level (stepInstanceId = null).
   * Sorted newest first.
   */
  listWorkflowAttachments(instanceId: string): Observable<WorkflowAttachmentDto[]> {
    return this.http.get<WorkflowAttachmentDto[]>(this.instanceBase(instanceId));
  }

  /**
   * Uploads a job-level document attached directly to the workflow instance (not a step).
   * Returns `{ id: string }` on success.
   */
  uploadInstanceAttachment(instanceId: string, file: File): Observable<{ id: string }> {
    const form = new FormData();
    form.append('file', file, file.name);
    return this.http.post<{ id: string }>(this.instanceBase(instanceId), form);
  }

  /** Returns the download URL for an instance-level attachment. */
  instanceAttachmentDownloadUrl(instanceId: string, attachmentId: string): string {
    return `${this.instanceBase(instanceId)}/${attachmentId}/download`;
  }

  /** Soft-deletes an instance-level attachment. Returns 204 on success. */
  deleteInstanceAttachment(instanceId: string, attachmentId: string): Observable<void> {
    return this.http.delete<void>(
      `${this.instanceBase(instanceId)}/${attachmentId}`,
    );
  }
}
