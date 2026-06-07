import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AttachmentDto } from '../models/attachment.models';

/**
 * Handles file attachment operations against workflow step instances.
 *
 * BFF routes proxied to Host:
 *   POST   /bff/workflows/instances/{iId}/steps/{sId}/attachments          → upload
 *   GET    /bff/workflows/instances/{iId}/steps/{sId}/attachments          → list
 *   GET    /bff/workflows/instances/{iId}/steps/{sId}/attachments/{id}/download → stream
 *   DELETE /bff/workflows/instances/{iId}/steps/{sId}/attachments/{id}     → soft-delete
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
}
