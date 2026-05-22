import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { HealthReport } from '../models/health.models';

@Injectable({ providedIn: 'root' })
export class HealthService {
  private readonly http = inject(HttpClient);
  private readonly base = '/bff/health';

  readonly report    = signal<HealthReport | null>(null);
  readonly isLoading = signal(false);
  readonly error     = signal<string | null>(null);
  readonly lastChecked = signal<Date | null>(null);

  loadHealth(): void {
    this.isLoading.set(true);
    this.error.set(null);

    this.http.get<HealthReport>(this.base).subscribe({
      next: data => {
        this.report.set(data);
        this.lastChecked.set(new Date());
        this.isLoading.set(false);
      },
      error: () => {
        this.error.set('Could not reach the health endpoint.');
        this.isLoading.set(false);
      },
    });
  }
}
