import type { IHealthRepository } from '../../domain/repositories/IHealthRepository';
import type { HealthCheckResponseDTO } from '../../application/dto/health/HealthCheckResponseDTO';
import type { HttpClient } from '../http/HttpClient';
import { apiEndpoints } from '../http/apiEndpoints';

/** Concrete `IHealthRepository` backed by the WalletSystem REST API. */
export class HealthRepository implements IHealthRepository {
  public constructor(private readonly http: HttpClient) {}

  public check(): Promise<HealthCheckResponseDTO> {
    return this.http.get<HealthCheckResponseDTO>(apiEndpoints.health.check);
  }
}
