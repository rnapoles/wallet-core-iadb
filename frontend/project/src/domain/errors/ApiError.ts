import { DomainError } from './DomainError';

/**
 * Raised when the API responds with a non-2xx status. Carries the HTTP
 * status so the UI can distinguish auth failures, conflicts, etc.
 */
export class ApiError extends DomainError {
  public constructor(
    message: string,
    public readonly status: number,
    public readonly requestId?: string,
  ) {
    super(message);
  }

  public get isUnauthorized(): boolean {
    return this.status === 401;
  }

  public get isForbidden(): boolean {
    return this.status === 403;
  }

  public get isConflict(): boolean {
    return this.status === 409;
  }

  public get isServerError(): boolean {
    return this.status >= 500;
  }
}
