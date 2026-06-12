import {
  ChangeDetectionStrategy,
  Component,
  Input,
} from '@angular/core';
import { NgClass } from '@angular/common';

@Component({
  selector: 'app-alert-panel',
  standalone: true,
  imports: [NgClass],
  templateUrl: './alert-panel.component.html',
  styleUrl: './alert-panel.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AlertPanelComponent {
  @Input() title     = '';
  @Input() icon      = '';
  @Input() count     = 0;
  @Input() accent: 'overdue' | 'unassigned' | 'at-risk' | 'invoice' = 'overdue';
  @Input() isLoading = false;
}
