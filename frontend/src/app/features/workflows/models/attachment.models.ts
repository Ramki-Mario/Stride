/** Single file attachment returned by the list endpoint. */
export interface AttachmentDto {
  id:               string;
  fileName:         string;
  contentType:      string;
  fileSizeBytes:    number;
  uploadedByUserId: string;
  createdAt:        string;   // ISO-8601
  isImage:          boolean;
}

/** MIME types accepted by the server — mirror the server-side allowlist. */
export const ALLOWED_ATTACHMENT_MIME_TYPES =
  'image/jpeg,image/png,image/webp,application/pdf,application/msword,text/plain,' +
  'application/vnd.openxmlformats-officedocument.wordprocessingml.document';

export const MAX_ATTACHMENT_SIZE_BYTES = 10 * 1024 * 1024; // 10 MB
