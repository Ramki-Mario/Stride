/** Maps to the JSON produced by UIResponseWriter.WriteHealthCheckUIResponse */
export type HealthStatus = 'Healthy' | 'Degraded' | 'Unhealthy';

export interface HealthEntry {
  status:      HealthStatus;
  duration:    string;   // ISO 8601 duration, e.g. "00:00:00.0234567"
  description: string | null;
  exception:   string | null;
  tags:        string[];
  data:        Record<string, unknown>;
}

export interface HealthReport {
  status:        HealthStatus;
  totalDuration: string;
  entries:       Record<string, HealthEntry>;
}
