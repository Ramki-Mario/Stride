import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface PublicStepDto {
  stepName:    string;
  order:       number;
  isRequired:  boolean;
  status:      string;
  completedAt: string | null;
  dueAt:       string | null;
}

export interface PublicJobViewDto {
  workflowName: string;
  status:       string;
  startedAt:    string;
  completedAt:  string | null;
  deadlineAt:   string | null;
  slaStatus:    string | null;
  steps:        PublicStepDto[];
}

@Injectable({ providedIn: 'root' })
export class PublicJobViewService {
  private readonly http = inject(HttpClient);

  getJobView(token: string): Observable<PublicJobViewDto> {
    return this.http.get<PublicJobViewDto>(`/bff/public/jobs/${token}`);
  }
}
