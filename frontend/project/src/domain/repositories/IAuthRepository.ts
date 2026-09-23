import type { LoginRequestDTO } from '../../application/dto/auth/LoginRequestDTO';
import type { LoginResponseDTO } from '../../application/dto/auth/LoginResponseDTO';
import type { RefreshTokenRequestDTO } from '../../application/dto/auth/RefreshTokenRequestDTO';
import type { RefreshTokenResponseDTO } from '../../application/dto/auth/RefreshTokenResponseDTO';
import type { RegisterRequestDTO } from '../../application/dto/auth/RegisterRequestDTO';
import type { RegisterResponseDTO } from '../../application/dto/auth/RegisterResponseDTO';
import type { CurrentUserResponseDTO } from '../../application/dto/auth/CurrentUserResponseDTO';

/**
 * Port describing authentication operations. The infrastructure layer
 * provides the concrete HTTP implementation; services depend on this
 * abstraction only (Dependency Inversion).
 */
export interface IAuthRepository {
  register(request: RegisterRequestDTO): Promise<RegisterResponseDTO>;
  login(request: LoginRequestDTO): Promise<LoginResponseDTO>;
  refresh(request: RefreshTokenRequestDTO): Promise<RefreshTokenResponseDTO>;
  getCurrentUser(): Promise<CurrentUserResponseDTO>;
}
