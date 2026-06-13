import {
  Component,
  ChangeDetectionStrategy,
  OnInit,
  signal,
  inject,
} from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { CommonModule, DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { PublicJobViewDto, PublicJobViewService } from '../../services/public-job-view.service';

@Component({
  selector: 'app-public-job-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, DatePipe],
  templateUrl: './public-job-page.html',
  styleUrl: './public-job-page.scss',
})
export class PublicJobPageComponent implements OnInit {
  private readonly route   = inject(ActivatedRoute);
  private readonly service = inject(PublicJobViewService);

  readonly job        = signal<PublicJobViewDto | null>(null);
  readonly isLoading  = signal(true);
  readonly isGone     = signal(false);
  readonly errorMsg   = signal<string | null>(null);

  ngOnInit(): void {
    const token = this.route.snapshot.paramMap.get('token') ?? '';
    this.service.getJobView(token).subscribe({
      next: (dto) => {
        this.job.set(dto);
        this.isLoading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.isLoading.set(false);
        if (err.status === 410) {
          this.isGone.set(true);
        } else {
          this.errorMsg.set('Unable to load this job. Please try again later.');
        }
      },
    });
  }

  statusLabel(status: string): string {
    const map: Record<string, string> = {
      Running:   'In Progress',
      Completed: 'Completed',
      Paused:    'Paused',
      Cancelled: 'Cancelled',
      Halted:    'On Hold',
    };
    return map[status] ?? status;
  }

  stepStatusClass(status: string): string {
    const map: Record<string, string> = {
      Completed: 'step--done',
      InProgress: 'step--active',
      Skipped:    'step--skipped',
      Failed:     'step--failed',
    };
    return map[status] ?? 'step--pending';
  }

  slaClass(sla: string | null): string {
    if (!sla) return '';
    const map: Record<string, string> = {
      OnTime:   'sla--ok',
      AtRisk:   'sla--risk',
      Breached: 'sla--breach',
    };
    return map[sla] ?? '';
  }

  slaLabel(sla: string | null): string {
    if (!sla) return '';
    const map: Record<string, string> = {
      OnTime:   'On Track',
      AtRisk:   'At Risk',
      Breached: 'SLA Breached',
    };
    return map[sla] ?? sla;
  }
}
