import { Injectable } from '@angular/core';

/**
 * Bridges STRIDE CSS design tokens (--stride-chart-*) to Chart.js color config.
 *
 * Chart.js needs concrete color strings at dataset creation time — it cannot read
 * CSS variables directly. This service reads the computed values at runtime so the
 * charts always reflect the active theme (light / dark / palette switch).
 *
 * Usage:
 *   const colors = this.chartTheme.colors();
 *   // use colors.primary, colors.secondary, ... in Chart.js datasets
 */
@Injectable({ providedIn: 'root' })
export class ChartThemeService {
  /**
   * Reads the current computed CSS custom-property value from :root.
   * Falls back to the supplied default if the property is not set.
   */
  private css(varName: string, fallback: string): string {
    if (typeof document === 'undefined') return fallback; // SSR guard
    const value = getComputedStyle(document.documentElement)
      .getPropertyValue(varName)
      .trim();
    return value || fallback;
  }

  /** Returns a snapshot of chart colors from the active STRIDE theme tokens. */
  colors(): ChartColors {
    return {
      primary:   this.css('--stride-chart-primary',   '#B97AF9'),
      secondary: this.css('--stride-chart-secondary', '#5CABEC'),
      tertiary:  this.css('--stride-chart-tertiary',  '#57D5C5'),
      support:   this.css('--stride-chart-support',   '#FC9FA2'),
      surface:   this.css('--stride-surface',         '#FFFFFF'),
      border:    this.css('--stride-border',          '#E8E1F0'),
      textMuted: this.css('--stride-text-muted',      '#9CA3AF'),
    };
  }

  /** Returns Chart.js–compatible grid and axis tick colors for the active theme. */
  gridOptions() {
    const c = this.colors();
    return {
      gridColor:  c.border,
      tickColor:  c.textMuted,
      tooltipBg:  c.surface,
    };
  }

  /**
   * Builds a Chart.js line dataset config with theme colors and area fill.
   * @param label       Dataset legend label
   * @param color       Hex / rgb color string
   * @param fill        Whether to fill the area under the line
   */
  lineDataset(label: string, color: string, fill = false) {
    return {
      label,
      borderColor:          color,
      backgroundColor:      fill ? this.hexToRgba(color, 0.15) : 'transparent',
      pointBackgroundColor: color,
      pointBorderColor:     color,
      pointRadius:          3,
      pointHoverRadius:     5,
      borderWidth:          2,
      tension:              0.4,
      fill,
    };
  }

  /** Converts a hex colour to rgba with the given alpha. */
  private hexToRgba(hex: string, alpha: number): string {
    // Strip leading '#' and expand 3-digit hex
    const clean = hex.replace('#', '');
    const full  = clean.length === 3
      ? clean.split('').map(c => c + c).join('')
      : clean;
    const r = Number.parseInt(full.substring(0, 2), 16);
    const g = Number.parseInt(full.substring(2, 4), 16);
    const b = Number.parseInt(full.substring(4, 6), 16);
    return `rgba(${r}, ${g}, ${b}, ${alpha})`;
  }
}

export interface ChartColors {
  primary:   string;
  secondary: string;
  tertiary:  string;
  support:   string;
  surface:   string;
  border:    string;
  textMuted: string;
}
