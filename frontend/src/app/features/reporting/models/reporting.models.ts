/**
 * Client-side models for the Reporting module.
 * These match the DTOs returned by GET /bff/reporting/dashboard/kpis
 * and GET /bff/reporting/dashboard/trends.
 */

export interface DashboardKpiDto {
  totalDefinitions: number;
  activeDefinitions: number;
  runningInstances: number;
  completedInstances: number;
  failedInstances: number;
  cancelledInstances: number;
}

export interface WorkflowTrendDto {
  /** ISO date string e.g. "2026-05-19" */
  date: string;
  started: number;
  completed: number;
  failed: number;
}

export interface ReportSummaryDto {
  reportId: string;
  name: string;
  reportType: ReportType;
  recordCount: number;
  generatedAt: string;
  requestedBy: string;
}

export type ReportType = 'DashboardKpi' | 'WorkflowTrend' | 'WorkflowSummary';

/** KPI card display metadata */
export interface KpiCardConfig {
  label: string;
  field: keyof DashboardKpiDto;
  iconClass: string;
  iconBg: string;
  colorVar: string;
  description: string;
}
