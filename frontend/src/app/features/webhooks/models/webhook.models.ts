// Read projection of a webhook subscription. The signing secret is NEVER part of
// this shape — it is returned only once by create / regenerate (WebhookSecretDto).
export interface WebhookSubscriptionDto {
  id:         string;
  url:        string;
  eventTypes: string[];
  isActive:   boolean;
  createdAt:  string;   // ISO 8601
}

// Returned once on create / regenerate — carries the plaintext signing secret.
export interface WebhookSecretDto {
  id:            string;
  signingSecret: string;
}

// Catalog entry: subscribable event key + human-readable label for checkboxes.
export interface WebhookEventTypeDto {
  key:   string;
  label: string;
}

// Result of firing a sample ping at the endpoint.
export interface WebhookTestResult {
  success:    boolean;
  statusCode: number | null;
  error:      string | null;
  elapsedMs:  number;
}

export interface CreateWebhookSubscriptionRequest {
  url:        string;
  eventTypes: string[];
}

export interface UpdateWebhookSubscriptionRequest {
  url:        string;
  eventTypes: string[];
  isActive:   boolean;
}
