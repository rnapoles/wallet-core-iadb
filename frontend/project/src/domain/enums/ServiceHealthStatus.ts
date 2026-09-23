/**
 * Mirrors the `ServiceHealthStatus` integer enum defined in the OpenAPI spec.
 * The API serializes this as 0 | 1 | 2, so the numeric values here are load-bearing.
 */
export enum ServiceHealthStatus {
  Healthy = 0,
  Degraded = 1,
  Unhealthy = 2,
}

const VALID_STATUSES: readonly number[] = [
  ServiceHealthStatus.Healthy,
  ServiceHealthStatus.Degraded,
  ServiceHealthStatus.Unhealthy,
];

export function serviceHealthStatusFromApi(value: number): ServiceHealthStatus {
  return VALID_STATUSES.includes(value) ? (value) : ServiceHealthStatus.Unhealthy;
}
