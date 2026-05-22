import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { NgClass } from '@angular/common';
import { Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs/operators';
import { forkJoin } from 'rxjs';
import { ChartModule } from 'primeng/chart';

import { ReportingService } from '../../../reporting/services/reporting.service';
import { ChartThemeService } from '../../../../core/chart/chart-theme.service';
import { DashboardKpiDto, WorkflowTrendDto } from '../../../reporting/models/reporting.models';

interface TrendWindow {
  label: string;
  days: number;
}

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [NgClass, ChartModule],
  templateUrl: './dashboard-page.html',
  styleUrl: './dashboard-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardPageComponent implements OnInit {
  private readonly reportingService = inject(ReportingService);
  private readonly chartTheme       = inject(ChartThemeService);
  private readonly destroyRef       = inject(DestroyRef);
  private readonly router           = inject(Router);

  // ── Server state ─────────────────────────────────────────────────────────

  readonly kpi          = signal<DashboardKpiDto | null>(null);
  readonly trends       = signal<WorkflowTrendDto[]>([]);
  readonly isLoading    = signal(true);
  readonly errorMessage = signal<string | null>(null);

  // ── Trend window selection ────────────────────────────────────────────────

  readonly trendWindows: TrendWindow[] = [
    { label: '7d',  days: 7  },
    { label: '30d', days: 30 },
    { label: '90d', days: 90 },
  ];

  readonly selectedDays = signal(30);

  // ── Derived: KPI summary cards ────────────────────────────────────────────

  readonly kpiCards = computed(() => {
    const k = this.kpi();
    if (!k) return [];
    return [
      {
        label:       'Total Definitions',
        value:       k.totalDefinitions,
        sub:         `${k.activeDefinitions} active`,
        iconClass:   'pi pi-sitemap',
        colorToken:  'var(--stride-primary)',
        bgToken:     'var(--stride-primary-subtle)',
        accentClass: 'primary',
        route:       ['/workflows'],
        queryParams: {},
      },
      {
        label:       'Running Instances',
        value:       k.runningInstances,
        sub:         'in progress now',
        iconClass:   'pi pi-spin pi-spinner',
        colorToken:  'var(--stride-info)',
        bgToken:     'var(--stride-info-bg)',
        accentClass: 'info',
        route:       ['/workflows', 'instances'],
        queryParams: { status: 'Running' },
      },
      {
        label:       'Completed',
        value:       k.completedInstances,
        sub:         'all time',
        iconClass:   'pi pi-check-circle',
        colorToken:  'var(--stride-success)',
        bgToken:     'var(--stride-success-bg)',
        accentClass: 'success',
        route:       ['/workflows', 'instances'],
        queryParams: { status: 'Completed' },
      },
      {
        label:       'Failed',
        value:       k.failedInstances,
        sub:         `${k.cancelledInstances} cancelled`,
        iconClass:   'pi pi-times-circle',
        colorToken:  'var(--stride-support)',
        bgToken:     'var(--stride-support-bg)',
        accentClass: 'danger',
        route:       ['/workflows', 'instances'],
        queryParams: { status: 'Failed' },
      },
    ];
  });

  // ── Derived: trend line chart data ────────────────────────────────────────

  readonly trendChartData = computed(() => {
    const data   = this.trends();
    const colors = this.chartTheme.colors();

    if (!data.length) return null;

    return {
      labels:   data.map(t => this.formatTrendDate(t.trendDate)),
      datasets: [
        {
          ...this.chartTheme.lineDataset('Started',   colors.secondary, false),
          data: data.map(t => t.started),
        },
        {
          ...this.chartTheme.lineDataset('Completed', colors.primary, true),
          data: data.map(t => t.completed),
        },
        {
          ...this.chartTheme.lineDataset('Failed',    colors.support, false),
          data: data.map(t => t.failed),
        },
      ],
    };
  });

  readonly trendChartOptions = computed(() => {
    const g = this.chartTheme.gridOptions();
    return {
      responsive:          true,
      maintainAspectRatio: false,
      interaction:         { mode: 'index', intersect: false },
      plugins: {
        legend: {
          position: 'top',
          align:    'end',
          labels: {
            boxWidth:  10,
            boxHeight: 10,
            color:     g.tickColor,
            font:      { family: 'Inter, sans-serif', size: 12 },
          },
        },
        tooltip: {
          backgroundColor: g.tooltipBg,
          titleColor:      g.tickColor,
          bodyColor:       g.tickColor,
          borderColor:     g.gridColor,
          borderWidth:     1,
          padding:         10,
        },
      },
      scales: {
        x: {
          grid:  { color: g.gridColor, drawBorder: false },
          ticks: { color: g.tickColor, font: { family: 'Inter, sans-serif', size: 11 } },
        },
        y: {
          beginAtZero: true,
          grid:  { color: g.gridColor, drawBorder: false },
          ticks: {
            color:     g.tickColor,
            stepSize:  1,
            font:      { family: 'Inter, sans-serif', size: 11 },
          },
        },
      },
    };
  });

  // ── Derived: status doughnut chart ────────────────────────────────────────

  readonly doughnutChartData = computed(() => {
    const k = this.kpi();
    if (!k) return null;
    const colors = this.chartTheme.colors();

    return {
      labels:   ['Running', 'Completed', 'Failed', 'Cancelled'],
      datasets: [
        {
          data: [
            k.runningInstances,
            k.completedInstances,
            k.failedInstances,
            k.cancelledInstances,
          ],
          backgroundColor: [
            colors.secondary,
            colors.primary,
            colors.support,
            colors.tertiary,
          ],
          hoverBackgroundColor: [
            colors.secondary,
            colors.primary,
            colors.support,
            colors.tertiary,
          ],
          borderWidth:      2,
          borderColor:      colors.surface,
          hoverBorderColor: colors.surface,
        },
      ],
    };
  });

  readonly doughnutChartOptions = computed(() => {
    const g = this.chartTheme.gridOptions();
    return {
      responsive:          true,
      maintainAspectRatio: false,
      cutout:              '68%',
      plugins: {
        legend: {
          position: 'bottom',
          labels: {
            boxWidth:  10,
            boxHeight: 10,
            color:     g.tickColor,
            padding:   16,
            font:      { family: 'Inter, sans-serif', size: 12 },
          },
        },
        tooltip: {
          backgroundColor: g.tooltipBg,
          titleColor:      g.tickColor,
          bodyColor:       g.tickColor,
          borderColor:     g.gridColor,
          borderWidth:     1,
          padding:         10,
        },
      },
    };
  });

  // ── Derived: total instances for doughnut center label ────────────────────

  readonly totalInstances = computed(() => {
    const k = this.kpi();
    if (!k) return 0;
    return k.runningInstances + k.completedInstances + k.failedInstances + k.cancelledInstances;
  });

  // ── Lifecycle ─────────────────────────────────────────────────────────────

  ngOnInit(): void {
    this.loadDashboard(this.selectedDays());
  }

  // ── Actions ───────────────────────────────────────────────────────────────

  navigateCard(card: ReturnType<typeof this.kpiCards>[number]): void {
    this.router.navigate(card.route, { queryParams: card.queryParams });
  }

  selectTrendWindow(days: number): void {
    if (this.selectedDays() === days) return;
    this.selectedDays.set(days);
    this.loadTrends(days);
  }

  // ── Data loading ──────────────────────────────────────────────────────────

  private loadDashboard(days: number): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    forkJoin({
      kpi:    this.reportingService.getDashboardKpis(),
      trends: this.reportingService.getWorkflowTrends(days),
    })
      .pipe(
        finalize(() => this.isLoading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: ({ kpi, trends }) => {
          this.kpi.set(kpi);
          this.trends.set(trends);
        },
        error: () =>
          this.errorMessage.set('Failed to load dashboard data. Please try again.'),
      });
  }

  private loadTrends(days: number): void {
    this.reportingService
      .getWorkflowTrends(days)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next:  data  => this.trends.set(data),
        error: ()    => { /* trend reload failure is silent — KPIs still visible */ },
      });
  }

  // ── Display helpers ───────────────────────────────────────────────────────

  private formatTrendDate(iso: string): string {
    // DateOnly from backend serialises as "YYYY-MM-DD". Append T00:00:00 to
    // ensure the date is parsed as local time rather than UTC midnight (which
    // can shift the displayed day backwards by one in western timezones).
    return new Date(`${iso}T00:00:00`).toLocaleDateString('en-GB', {
      day:   'numeric',
      month: 'short',
    });
  }

  formatNumber(n: number): string {
    return n.toLocaleString('en-US');
  }
}
