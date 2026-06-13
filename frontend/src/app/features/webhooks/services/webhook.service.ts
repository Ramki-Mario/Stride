import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  WebhookSubscriptionDto,
  WebhookSecretDto,
  WebhookEventTypeDto,
  WebhookTestResult,
  WebhookDeliveryDto,
  CreateWebhookSubscriptionRequest,
  UpdateWebhookSubscriptionRequest,
} from '../models/webhook.models';

@Injectable({ providedIn: 'root' })
export class WebhookService {
  private readonly http = inject(HttpClient);
  private readonly base = '/bff/webhooks';

  // ── Server state ──────────────────────────────────────────────────────────
  readonly subscriptions = signal<WebhookSubscriptionDto[]>([]);
  readonly eventTypes     = signal<WebhookEventTypeDto[]>([]);
  readonly isLoading      = signal(false);
  readonly error          = signal<string | null>(null);

  // ── Derived ───────────────────────────────────────────────────────────────
  readonly isEmpty     = computed(() => !this.isLoading() && this.subscriptions().length === 0);
  readonly activeCount = computed(() => this.subscriptions().filter(s => s.isActive).length);

  // ── Load ──────────────────────────────────────────────────────────────────

  loadSubscriptions(): void {
    this.isLoading.set(true);
    this.error.set(null);
    this.http.get<WebhookSubscriptionDto[]>(`${this.base}/subscriptions`).subscribe({
      next: subs => {
        this.subscriptions.set(subs);
        this.isLoading.set(false);
      },
      error: () => {
        this.error.set('Failed to load webhooks.');
        this.isLoading.set(false);
      },
    });
  }

  loadEventTypes(): void {
    this.http.get<WebhookEventTypeDto[]>(`${this.base}/event-types`).subscribe({
      next: types => this.eventTypes.set(types),
      error: () => {},
    });
  }

  // ── CRUD ──────────────────────────────────────────────────────────────────

  create(request: CreateWebhookSubscriptionRequest): Observable<WebhookSecretDto> {
    return this.http.post<WebhookSecretDto>(`${this.base}/subscriptions`, request);
  }

  update(id: string, request: UpdateWebhookSubscriptionRequest): Observable<void> {
    return this.http.put<void>(`${this.base}/subscriptions/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/subscriptions/${id}`);
  }

  test(id: string): Observable<WebhookTestResult> {
    return this.http.post<WebhookTestResult>(`${this.base}/subscriptions/${id}/test`, {});
  }

  regenerateSecret(id: string): Observable<WebhookSecretDto> {
    return this.http.post<WebhookSecretDto>(`${this.base}/subscriptions/${id}/regenerate-secret`, {});
  }

  getDeliveries(id: string, limit = 50): Observable<WebhookDeliveryDto[]> {
    return this.http.get<WebhookDeliveryDto[]>(`${this.base}/subscriptions/${id}/deliveries?limit=${limit}`);
  }

  retryDelivery(subscriptionId: string, deliveryId: string): Observable<void> {
    return this.http.post<void>(
      `${this.base}/subscriptions/${subscriptionId}/deliveries/${deliveryId}/retry`, {});
  }
}
