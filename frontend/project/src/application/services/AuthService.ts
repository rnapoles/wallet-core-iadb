import type { IAuthRepository } from '../../domain/repositories/IAuthRepository';
import type { User } from '../../domain/entities/User';
import { ValidationError } from '../../domain/errors/ValidationError';
import { UserMapper } from '../mappers/UserMapper';
import { tokenStorage } from '../../infrastructure/storage/TokenStorage';
import { sanitizeUserInput } from '../../infrastructure/security/sanitize';
import { loginSchema, type LoginFormValues } from '../validators/LoginValidator';
import { registerSchema, type RegisterFormValues } from '../validators/RegisterValidator';
import { zodIssuesToFieldErrors } from '../validators/zodIssuesToFieldErrors';

/**
 * Application service for authentication use cases. Validates input,
 * delegates I/O to the injected repository (port), and persists resulting
 * tokens — the presentation layer never talks to `IAuthRepository` directly.
 */
export class AuthService {
  public constructor(private readonly authRepository: IAuthRepository) {}

  public async register(values: RegisterFormValues): Promise<{ userId: string; email: string }> {
    const parsed = registerSchema.safeParse(values);
    if (!parsed.success) {
      throw new ValidationError(zodIssuesToFieldErrors(parsed.error));
    }
    const response = await this.authRepository.register({
      email: parsed.data.email,
      password: parsed.data.password,
      firstName: sanitizeUserInput(parsed.data.firstName, 100),
      lastName: sanitizeUserInput(parsed.data.lastName, 100),
    });
    return { userId: response.userId, email: response.email ?? parsed.data.email };
  }

  public async login(values: LoginFormValues): Promise<void> {
    const parsed = loginSchema.safeParse(values);
    if (!parsed.success) {
      throw new ValidationError(zodIssuesToFieldErrors(parsed.error));
    }
    const response = await this.authRepository.login(parsed.data);
    if (!response.token || !response.refreshToken) {
      throw new Error('Login succeeded but no session token was issued.');
    }
    tokenStorage.setTokens(response.token, response.refreshToken, response.expiresAt);
  }

  public async getCurrentUser(): Promise<User> {
    const dto = await this.authRepository.getCurrentUser();
    return UserMapper.fromCurrentUserDTO(dto);
  }

  public logout(): void {
    tokenStorage.clear();
  }

  public get isAuthenticated(): boolean {
    return tokenStorage.isAuthenticated;
  }
}
