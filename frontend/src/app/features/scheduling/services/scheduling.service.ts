import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  ScheduleDefinitionSummaryDto,
  ScheduleDefinitionDto,
  CreateScheduleRequest,
  UpdateScheduleRequest,
} from '../models/scheduling.models';

@Injectable({ providedIn: 'root' })
export class SchedulingService {
  private readonly http = inject(HttpClient);
  private readonly base = '/bff/scheduling/schedules';

  readonly schedules  = signal<ScheduleDefinitionSummaryDto[]>([]);
  readonly isLoading  = signal(false);
  readonly error      = signal<string | null>(null);

  readonly isEmpty       = computed(() => !this.isLoading() && this.schedules().length === 0);
  readonly activeCount   = computed(() => this.schedules().filter(s => s.isActive).length);
  readonly inactiveCount = computed(() => this.schedules().filter(s => !s.isActive).length);

  loadSchedules(): void {
    this.isLoading.set(true);
    this.error.set(null);

    this.http.get<ScheduleDefinitionSummaryDto[]>(this.base).subscribe({
      next: items => {
        this.schedules.set(items);
        this.isLoading.set(false);
      },
      error: () => {
        this.error.set('Failed to load schedules.');
        this.isLoading.set(false);
      },
    });
  }

  getScheduleById(id: string): Observable<ScheduleDefinitionDto> {
    return this.http.get<ScheduleDefinitionDto>(`${this.base}/${id}`);
  }

  createSchedule(request: CreateScheduleRequest): Observable<ScheduleDefinitionDto> {
    return this.http.post<ScheduleDefinitionDto>(this.base, request);
  }

  updateSchedule(id: string, request: UpdateScheduleRequest): Observable<ScheduleDefinitionDto> {
    return this.http.put<ScheduleDefinitionDto>(`${this.base}/${id}`, request);
  }

  deleteSchedule(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }

  activateSchedule(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/activate`, {});
  }

  deactivateSchedule(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/deactivate`, {});
  }
}
