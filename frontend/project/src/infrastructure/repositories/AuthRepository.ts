import type { IAuthRepository } from '../../domain/repositories/IAuthRepository';
import type { RegisterRequestDTO } from '../../application/dto/auth/RegisterRequestDTO';
import type { RegisterResponseDTO } from '../../application/dto/auth/RegisterResponseDTO';
import type { LoginRequestDTO } from '../../application/dto/auth/LoginRequestDTO';
import type { LoginResponseDTO } from '../../application/dto/auth/LoginResponseDTO';
import type { RefreshTokenRequestDTO } from '../../application/dto/auth/RefreshTokenRequestDTO';
import type { RefreshTokenResponseDTO } from '../../application/dto/auth/RefreshTokenResponseDTO';
import type { CurrentUserResponseDTO } from '../../application/dto/auth/CurrentUserResponseDTO';
import type { HttpClient } from '../http/HttpClient';
import { apiEndpoints } from '../http/apiEndpoints';

/** Concrete `IAuthRepository` backed by the WalletSystem REST API. */
export class AuthRepository implements IAuthRepository {
  public constructor(private readonly http: HttpClient) {}

  public register(request: RegisterRequestDTO): Promise<RegisterResponseDTO> {
    return this.http.post<RegisterResponseDTO>(apiEndpoints.auth.register, request);
  }

  public login(request: LoginRequestDTO): Promise<LoginResponseDTO> {
    return this.http.post<LoginResponseDTO>(apiEndpoints.auth.login, request);
  }

  public refresh(request: RefreshTokenRequestDTO): Promise<RefreshTokenResponseDTO> {
    return this.http.post<RefreshTokenResponseDTO>(apiEndpoints.auth.refresh, request);
  }

  public getCurrentUser(): Promise<CurrentUserResponseDTO> {
    return this.http.get<CurrentUserResponseDTO>(apiEndpoints.auth.me);
  }
}
