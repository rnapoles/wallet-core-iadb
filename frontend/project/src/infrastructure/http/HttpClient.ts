import type { AxiosError } from 'axios';
import axios, { type AxiosInstance, type AxiosRequestConfig, type InternalAxiosRequestConfig } from 'axios';
import { env } from '../config/env';
import { tokenStorage } from '../storage/TokenStorage';
import { ApiError } from '../../domain/errors/ApiError';
import { apiEndpoints } from './apiEndpoints';

const MUTATING_METHODS = new Set(['post', 'put', 'patch', 'delete']);

/**
 * Endpoints that must fail a 401 immediately rather than trigger the
 * refresh-and-retry / session-verification flow: a 401 from login or
 * register means "wrong credentials", not "expired session", and refresh
 * itself must never try to recover from its own failure.
 * `/api/auth/me` is deliberately *not* here — a direct GET /me call (e.g.
 * restoring a session from localStorage on app startup) should still get a
 * silent refresh-and-retry attempt like any other authenticated request.
 */
const RECOVERY_EXEMPT_ENDPOINTS: ReadonlySet<string> = new Set([
  apiEndpoints.auth.login,
  apiEndpoints.auth.register,
  apiEndpoints.auth.refresh,
]);

type TrackedRequestConfig = InternalAxiosRequestConfig & {
  /** Set once a 401 on this request has already triggered a retry, to prevent retry loops. */
  _retried?: boolean;
  /** Marks the GET /api/auth/me call made purely to verify a session, so the response
   *  interceptor doesn't recursively re-run the 401 recovery flow on it. */
  _isSessionCheck?: boolean;
};

/**
 * Thin, typed wrapper around axios. Centralizes:
 *  - attaching the bearer token
 *  - attaching an Idempotency-Key on mutating requests (deposit/withdraw/
 *    transfer are financial operations that must be safe to retry on
 *    network failure without double-applying)
 *  - access-token refresh, both proactively (see `TokenRefreshScheduler`,
 *    which calls `refreshAccessToken` on a timer) and reactively on a 401
 *  - normalizing all failures into the domain-level ApiError
 *
 * This is the *only* module in the codebase allowed to know about axios;
 * every repository depends on this class, never on axios directly.
 */
export class HttpClient {
  private readonly axiosInstance: AxiosInstance;
  private refreshInFlight: Promise<boolean> | null = null;
  private onSessionExpired: (() => void) | null = null;
  private baseURL: string;

  public constructor() {
  
    const version = env.VITE_API_VERSION ?? '';
    const baseURL = env.VITE_API_BASE_URL ?? '/';
    this.baseURL = baseURL.endsWith('/') ? `${baseURL}${version}` : `${baseURL}/${version}`;

    this.axiosInstance = axios.create({
      baseURL: this.baseURL,
      timeout: env.VITE_API_TIMEOUT_MS,
      headers: {
        'Content-Type': 'application/json',
      },
      // Never send cookies cross-site; this API is authenticated via bearer token only.
      withCredentials: false,
    });

    this.axiosInstance.interceptors.request.use((config) => this.attachRequestMetadata(config));
    this.axiosInstance.interceptors.response.use(
      (response) => response,
      (error: unknown) => this.handleResponseError(error),
    );
  }

  /** Registers a callback invoked once the session is confirmed dead (logout + redirect to login). */
  public setSessionExpiredHandler(handler: () => void): void {
    this.onSessionExpired = handler;
  }

  /**
   * Clears local session state and notifies the registered handler.
   * Called both by the reactive 401 → `/api/auth/me` verification flow below
   * and by `TokenRefreshScheduler` when a proactive background refresh fails,
   * so there is exactly one place that decides "the session is over".
   */
  public triggerSessionExpired(): void {
    tokenStorage.clear();
    this.onSessionExpired?.();
  }

  /**
   * Exchanges the current refresh token for a new access/refresh token pair.
   * Concurrent callers (the proactive timer and a reactive 401 firing at the
   * same moment, or several failed requests at once) are coalesced into a
   * single network call.
   */
  public async refreshAccessToken(): Promise<boolean> {
    const refreshToken = tokenStorage.getRefreshToken();
    const accessToken = tokenStorage.getAccessToken();
    if (!refreshToken || !accessToken) {
      return false;
    }
    this.refreshInFlight ??= this.performRefresh(refreshToken, accessToken);
    const result = await this.refreshInFlight;
    this.refreshInFlight = null;
    return result;
  }

  public async get<T>(url: string, config?: AxiosRequestConfig): Promise<T> {
    const response = await this.axiosInstance.get<T>(url, config);
    return response.data;
  }

  public async post<T>(url: string, body?: unknown, config?: AxiosRequestConfig): Promise<T> {
    const response = await this.axiosInstance.post<T>(url, body, config);
    return response.data;
  }

  private attachRequestMetadata(config: InternalAxiosRequestConfig): InternalAxiosRequestConfig {
    const token = tokenStorage.getAccessToken();
    if (token) {
      config.headers.set('Authorization', `Bearer ${token}`);
    }
    if (MUTATING_METHODS.has((config.method ?? '').toLowerCase())) {
      config.headers.set('Idempotency-Key', crypto.randomUUID());
    }
    if (env.VITE_ENABLE_API_LOGGING) {
       
      console.debug(`[api] ${config.method?.toUpperCase()} ${this.baseURL}${config.url}`);
    }
    return config;
  }

  private async handleResponseError(error: unknown): Promise<never> {
    if (!axios.isAxiosError(error)) {
      throw new ApiError('An unexpected error occurred.', 0);
    }
    const axiosError = error as AxiosError<{ message?: string; title?: string }>;
    const status = axiosError.response?.status ?? 0;
    const originalRequest = axiosError.config as TrackedRequestConfig | undefined;

    const isExemptEndpoint = originalRequest?.url ? RECOVERY_EXEMPT_ENDPOINTS.has(originalRequest.url) : false;
    const shouldAttemptRecovery =
      status === 401 &&
      originalRequest !== undefined &&
      !originalRequest._retried &&
      !isExemptEndpoint &&
      !originalRequest._isSessionCheck;

    if (shouldAttemptRecovery && originalRequest) {
      originalRequest._retried = true;

      // 1) Try a silent token refresh first; if it succeeds, replay the original request.
      const refreshed = await this.refreshAccessToken();
      if (refreshed) {
        return this.axiosInstance.request(originalRequest) as never;
      }

      // 2) Refresh wasn't possible (no refresh token, or the API rejected it). Before
      //    giving up, double-check with the server whether the session is actually
      //    dead: GET /api/auth/me. Only a confirmed 401 there ends the session — a
      //    network hiccup or an unrelated failure on the original request shouldn't
      //    log the user out.
      const sessionIsValid = await this.verifySession();
      if (!sessionIsValid) {
        this.triggerSessionExpired();
      }
    }

    const message =
      axiosError.response?.data?.message ??
      axiosError.response?.data?.title ??
      axiosError.message ??
      'Request failed.';
    const requestId = axiosError.response?.headers?.['x-request-id'] as string | undefined;
    throw new ApiError(message, status, requestId);
  }

  /** Calls GET /api/auth/me to confirm whether the current session is still valid. */
  private async verifySession(): Promise<boolean> {
    try {
      await this.axiosInstance.get(apiEndpoints.auth.me, {
        _isSessionCheck: true,
      } as AxiosRequestConfig);
      return true;
    } catch (verificationError) {
      if (axios.isAxiosError(verificationError) && verificationError.response?.status === 401) {
        return false;
      }
      // A non-401 failure (network error, 5xx, timeout) doesn't tell us the session is
      // invalid — treat it as "still logged in" and let the caller's own error surface.
      return true;
    }
  }

  private async performRefresh(refreshToken: string, accessToken: string): Promise<boolean> {
    try {
      const response = await axios.post(
        `${env.VITE_API_BASE_URL}${apiEndpoints.auth.refresh}`,
        { refreshToken, accessToken },
        { timeout: env.VITE_API_TIMEOUT_MS },
      );
      const { newAccessToken, newRefreshToken, expiresAt } = response.data as {
        newAccessToken: string | null;
        newRefreshToken: string | null;
        expiresAt: string;
      };
      if (!newAccessToken || !newRefreshToken) return false;
      tokenStorage.setTokens(newAccessToken, newRefreshToken, expiresAt);
      return true;
    } catch {
      return false;
    }
  }
}

/** Single shared HTTP client instance for the whole app. */
export const httpClient = new HttpClient();
