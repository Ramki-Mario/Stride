import { Injectable, computed, inject } from '@angular/core';
import { AuthService } from './auth.service';

/**
 * System-wide permission keys, mirroring the backend `DefaultPermissions` catalog
 * (STRIDE.Modules.Identity.Domain.DefaultPermissions). Use these constants rather
 * than raw strings so a typo is a compile error, not a silently-failing check.
 */
export const Permissions = {
  WorkflowView:            'workflow.view',
  WorkflowCreate:          'workflow.create',
  WorkflowActivate:        'workflow.activate',
  WorkflowRun:             'workflow.run',
  WorkflowManageInstances: 'workflow.manage_instances',
  UserInvite:              'user.invite',
  UserManage:              'user.manage',
  RoleView:                'role.view',
  RoleManage:              'role.manage',
  TenantSettings:          'tenant.settings',
  KitOpsReportView:        'kitops.report.view',
} as const;

export type PermissionKey = (typeof Permissions)[keyof typeof Permissions];

/**
 * Resolves the current user's effective permission set from the session signal.
 *
 * The permission set is delivered by the BFF `/auth/me` endpoint (fetched once on app
 * boot and re-fetched on login), cached in-memory by {@link AuthService}. This service
 * simply derives a reactive `Set` over that cached data — no extra HTTP calls. Because
 * `has()` reads the `AuthService.user` signal synchronously, it composes correctly with
 * Angular `computed()` and `effect()` (e.g. the {@link HasPermissionDirective}).
 */
@Injectable({ providedIn: 'root' })
export class PermissionService {
  private readonly auth = inject(AuthService);

  /** Reactive set of the current user's permission keys. */
  private readonly permissionSet = computed(
    () => new Set(this.auth.user()?.permissions ?? []),
  );

  /** True when the current user holds the given permission key. */
  has(permission: string): boolean {
    return this.permissionSet().has(permission);
  }

  /** True when the current user holds at least one of the given keys. */
  hasAny(...permissions: string[]): boolean {
    const set = this.permissionSet();
    return permissions.some(p => set.has(p));
  }

  /** True when the current user holds every one of the given keys. */
  hasAll(...permissions: string[]): boolean {
    const set = this.permissionSet();
    return permissions.every(p => set.has(p));
  }
}
