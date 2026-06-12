import {
  ChangeDetectionStrategy,
  Component,
  Input,
} from '@angular/core';
import { NgClass } from '@angular/common';
import { Router } from '@angular/router';
import { inject } from '@angular/core';
import { TeamWorkloadItem } from '../../models/dashboard-workload.models';

@Component({
  selector: 'app-workload-panel',
  standalone: true,
  imports: [NgClass],
  templateUrl: './workload-panel.component.html',
  styleUrl:    './workload-panel.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WorkloadPanelComponent {
  private readonly router = inject(Router);

  @Input() items:     TeamWorkloadItem[] = [];
  @Input() isLoading = false;

  initials(displayName: string): string {
    return displayName
      .split(' ')
      .slice(0, 2)
      .map(p => p.charAt(0).toUpperCase())
      .join('');
  }

  formatDue(dueAt: string | null): string {
    if (!dueAt) return '—';
    const d = new Date(dueAt);
    return d.toLocaleDateString('en-GB', { day: 'numeric', month: 'short' });
  }

  navigateToInstance(workflowInstanceId: string): void {
    this.router.navigate(['/workflows', 'instances', workflowInstanceId]);
  }
}
