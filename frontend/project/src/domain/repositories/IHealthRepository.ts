import type { HealthCheckResponseDTO } from '../../application/dto/health/HealthCheckResponseDTO';

/**
 * Port describing system health checks, mapped to `/api/health`.
 */
export interface IHealthRepository {
  check(): Promise<HealthCheckResponseDTO>;
}
