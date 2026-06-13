import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  inject,
  signal,
  computed,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ChartModule } from 'primeng/chart';

import { AnalyticsService } from '../../services/analytics.service';
import {
  AnalyticsWorkflowDefinitionDto,
  CompletionTimeAnalyticsDto,
} from '../../models/analytics.models';

@Component({
  selector: 'app-analytics-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, ChartModule],
  templateUrl: './analytics-page.html',
  styleUrl: './analytics-page.scss',
})
export class AnalyticsPageComponent implements OnInit {
  private readonly svc = inject(AnalyticsService);

  // ── Filter state ──────────────────────────────────────────────────────────
  readonly fromDate = signal(this.defaultFromDate());
  readonly toDate   = signal(this.defaultToDate());
  readonly selectedDefinitionId = signal<string>('');

  // ── Server state ──────────────────────────────────────────────────────────
  readonly definitions = signal<AnalyticsWorkflowDefinitionDto[]>([]);
  readonly analytics   = signal<CompletionTimeAnalyticsDto | null>(null);
  readonly isLoading   = signal(false);
  readonly error       = signal<string | null>(null);

  // ── Derived chart data ────────────────────────────────────────────────────
  readonly byDefinitionChart = computed(() => {
    const data = this.analytics();
    if (!data?.byDefinition.length) return null;
    return {
      labels: data.byDefinition.map(d => d.definitionName),
      datasets: [{
        label: 'Avg. Completion Time (min)',
        data:  data.byDefinition.map(d => Math.round(d.avgDurationMinutes)),
        backgroundColor: 'rgba(124, 58, 237, 0.7)',
        borderColor:     '#7c3aed',
        borderWidth:     1,
      }],
    };
  });

  readonly bottleneckChart = computed(() => {
    const data = this.analytics();
    if (!data?.bottlenecks.length) return null;
    const rows = [...data.bottlenecks].slice(0, 10); // top 10 bottlenecks
    return {
      labels: rows.map(b => b.stepName),
      datasets: [{
        label: 'Avg. Step Duration (min)',
        data:  rows.map(b => Math.round(b.avgDurationMinutes)),
        backgroundColor: 'rgba(234, 88, 12, 0.7)',
        borderColor:     '#ea580c',
        borderWidth:     1,
      }],
    };
  });

  readonly trendChart = computed(() => {
    const data = this.analytics();
    if (!data?.weeklyTrend.length) return null;
    return {
      labels: data.weeklyTrend.map(t => t.weekStart),
      datasets: [{
        label:       'Avg. Completion Time (min)',
        data:        data.weeklyTrend.map(t => Math.round(t.avgDurationMinutes)),
        fill:        false,
        tension:     0.35,
        borderColor: '#7c3aed',
        backgroundColor: 'rgba(124, 58, 237, 0.15)',
        pointBackgroundColor: '#7c3aed',
      }],
    };
  });

  readonly isEmpty = computed(() =>
    !this.isLoading() && this.analytics() !== null &&
    !this.analytics()!.byDefinition.length &&
    !this.analytics()!.bottlenecks.length &&
    !this.analytics()!.weeklyTrend.length);

  // ── Chart options ─────────────────────────────────────────────────────────
  readonly barOptions = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: { legend: { display: false } },
    scales: {
      x: { grid: { display: false } },
      y: { beginAtZero: true, title: { display: true, text: 'Minutes' } },
    },
  };

  readonly horizontalBarOptions = {
    responsive: true,
    maintainAspectRatio: false,
    indexAxis: 'y' as const,
    plugins: { legend: { display: false } },
    scales: {
      x: { beginAtZero: true, title: { display: true, text: 'Minutes' } },
      y: { grid: { display: false } },
    },
  };

  readonly lineOptions = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: { legend: { display: false } },
    scales: {
      x: { grid: { display: false } },
      y: { beginAtZero: true, title: { display: true, text: 'Minutes' } },
    },
  };

  ngOnInit(): void {
    this.svc.getWorkflowDefinitions().subscribe({
      next: defs => this.definitions.set(defs),
      error: () => {},
    });
    this.load();
  }

  load(): void {
    this.isLoading.set(true);
    this.error.set(null);
    this.analytics.set(null);

    this.svc.getCompletionTimes(
      this.fromDate(),
      this.toDate(),
      this.selectedDefinitionId() || undefined,
    ).subscribe({
      next: data => {
        this.analytics.set(data);
        this.isLoading.set(false);
      },
      error: () => {
        this.error.set('Failed to load analytics data.');
        this.isLoading.set(false);
      },
    });
  }

  export(): void {
    const url = this.svc.getExportUrl(
      this.fromDate(),
      this.toDate(),
      this.selectedDefinitionId() || undefined,
    );
    window.open(url, '_self');
  }

  private defaultFromDate(): string {
    const d = new Date();
    d.setDate(d.getDate() - 90);
    return d.toISOString().slice(0, 10);
  }

  private defaultToDate(): string {
    return new Date().toISOString().slice(0, 10);
  }
}
