import {
  ChangeDetectionStrategy,
  Component,
  inject,
  OnInit,
  signal,
  computed,
} from '@angular/core';
import { NgClass, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { SelectModule } from 'primeng/select';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { ReportingService } from '../../services/reporting.service';
import {
  ReportSummaryDto,
  ReportType,
  REPORT_TYPE_LABELS,
} from '../../models/reporting.models';

@Component({
  selector: 'app-reporting-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgClass, DecimalPipe, FormsModule, ButtonModule, SelectModule, ToastModule],
  providers: [MessageService],
  templateUrl: './reporting-page.html',
  styleUrl: './reporting-page.scss',
})
export class ReportingPageComponent implements OnInit {
  private readonly svc = inject(ReportingService);
  private readonly msg = inject(MessageService);

  // ── State ────────────────────────────────────────────────────────────────

  protected readonly reports     = signal<ReportSummaryDto[]>([]);
  protected readonly loading     = signal(true);
  protected readonly showForm    = signal(false);
  protected readonly generating  = signal(false);
  protected readonly exportingId = signal<string | null>(null);

  /** Selected report type in the generate form */
  protected selectedType: ReportType = 'DashboardKpi';

  // ── Derived ──────────────────────────────────────────────────────────────

  protected readonly isEmpty = computed(
    () => !this.loading() && this.reports().length === 0,
  );

  protected readonly reportTypeOptions = Object.entries(REPORT_TYPE_LABELS).map(
    ([value, label]) => ({ label, value: value as ReportType }),
  );

  protected readonly REPORT_TYPE_LABELS = REPORT_TYPE_LABELS;

  // ── Lifecycle ────────────────────────────────────────────────────────────

  ngOnInit(): void {
    this.loadReports();
  }

  // ── Actions ──────────────────────────────────────────────────────────────

  protected toggleForm(): void {
    this.showForm.update(v => !v);
    if (!this.showForm()) this.selectedType = 'DashboardKpi';
  }

  protected generateReport(): void {
    this.generating.set(true);
    this.svc.generateReport({ reportType: this.selectedType }).subscribe({
      next: () => {
        this.msg.add({
          severity: 'success',
          summary: 'Report generated',
          detail: `${REPORT_TYPE_LABELS[this.selectedType]} saved to your report list.`,
          life: 4000,
        });
        this.showForm.set(false);
        this.selectedType = 'DashboardKpi';
        this.loadReports();
      },
      error: () => {
        this.msg.add({
          severity: 'error',
          summary: 'Generation failed',
          detail: 'Please try again.',
        });
        this.generating.set(false);
      },
    });
  }

  protected exportCsv(report: ReportSummaryDto): void {
    this.exportingId.set(report.reportId);
    this.svc.exportReportCsv(report.reportId).subscribe({
      next: (blob) => {
        const url  = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href     = url;
        link.download = `${report.name.toLowerCase().replace(/\s+/g, '-')}.csv`;
        link.click();
        URL.revokeObjectURL(url);
        this.exportingId.set(null);
      },
      error: () => {
        this.msg.add({
          severity: 'error',
          summary: 'Export failed',
          detail: 'Could not download CSV.',
        });
        this.exportingId.set(null);
      },
    });
  }

  protected typeBadgeClass(type: ReportType): string {
    const map: Record<ReportType, string> = {
      DashboardKpi:    'badge--kpi',
      WorkflowTrend:   'badge--trend',
      WorkflowSummary: 'badge--summary',
    };
    return map[type];
  }

  protected formatDate(iso: string): string {
    return new Date(iso).toLocaleString('en-GB', {
      day: '2-digit', month: 'short', year: 'numeric',
      hour: '2-digit', minute: '2-digit',
    });
  }

  // ── Private ──────────────────────────────────────────────────────────────

  private loadReports(): void {
    this.loading.set(true);
    this.generating.set(false);
    this.svc.getReports().subscribe({
      next:  (data) => { this.reports.set(data); this.loading.set(false); },
      error: ()     => { this.loading.set(false); },
    });
  }
}
