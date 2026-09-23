import type { IHealthRepository } from '../../domain/repositories/IHealthRepository';
import { serviceHealthStatusFromApi } from '../../domain/enums/ServiceHealthStatus';

export interface SystemHealthSummary {
  overallStatus: ReturnType<typeof serviceHealthStatusFromApi>;
  checkedAt: Date;
  services: { name: string; status: ReturnType<typeof serviceHealthStatusFromApi>; description: string | null }[];
}

/** Application service for the system health use case. */
export class HealthService {
  public constructor(private readonly healthRepository: IHealthRepository) {}

  public async getSummary(): Promise<SystemHealthSummary> {
    const dto = await this.healthRepository.check();
    const services = Object.entries(dto.services ?? {}).map(([key, value]) => ({
      name: value.name ?? key,
      status: serviceHealthStatusFromApi(value.status),
      description: value.description,
    }));
    return {
      overallStatus: serviceHealthStatusFromApi(dto.overallStatus),
      checkedAt: new Date(dto.timestamp),
      services,
    };
  }
}
