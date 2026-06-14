import {
  Directive,
  Input,
  TemplateRef,
  ViewContainerRef,
  effect,
  inject,
  signal,
} from '@angular/core';
import { PermissionService } from './permission.service';

/**
 * Structural directive that renders its host element only when the current user holds
 * the given permission. Hidden elements are removed from the DOM entirely (not merely
 * `display:none`), so a user who lacks a permission cannot see — or tab to — the action.
 *
 * Usage:
 *   <button *hasPermission="'workflow.create'">New Workflow</button>
 *   <a *hasPermission="'role.view'" routerLink="/administration/roles">Roles</a>
 *
 * The check is reactive: an `effect` re-evaluates whenever the session's permission set
 * changes (e.g. after login or a role change), so the view appears/disappears live.
 *
 * This is the structural-directive complement to driving `@if` conditions directly off
 * {@link PermissionService}. Prefer this directive for elements whose visibility is purely
 * permission-gated; fold the permission into an existing `computed()` when an element is
 * already conditional on other state (e.g. workflow status).
 */
@Directive({
  selector: '[hasPermission]',
  standalone: true,
})
export class HasPermissionDirective {
  private readonly templateRef = inject(TemplateRef<unknown>);
  private readonly viewContainer = inject(ViewContainerRef);
  private readonly permissions = inject(PermissionService);

  private readonly required = signal('');
  private rendered = false;

  constructor() {
    effect(() => {
      const allowed = this.permissions.has(this.required());
      if (allowed && !this.rendered) {
        this.viewContainer.createEmbeddedView(this.templateRef);
        this.rendered = true;
      } else if (!allowed && this.rendered) {
        this.viewContainer.clear();
        this.rendered = false;
      }
    });
  }

  @Input({ required: true })
  set hasPermission(permission: string) {
    this.required.set(permission);
  }
}
