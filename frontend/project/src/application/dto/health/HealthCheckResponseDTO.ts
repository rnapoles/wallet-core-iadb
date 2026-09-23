import type { ServiceHealthDTO } from './ServiceHealthDTO';

/** Wire shape returned by GET /api/health (HealthCheckResponse). */
export interface HealthCheckResponseDTO {
  overallStatus: number;
  services: Record<string, ServiceHealthDTO> | null;
  timestamp: string;
}
