import type { HttpClient } from '../http/HttpClient';
import { tokenStorage } from '../storage/TokenStorage';

/** How long before the token's real expiry to refresh it, absorbing request latency and clock skew. */
const REFRESH_BUFFER_MS = 60_000;

/** Floor on the scheduled delay, so a token that's already near/past expiry doesn't refresh in a tight loop. */
const MIN_DELAY_MS = 5_000;

/**
 * Proactively refreshes the access token on a timer, ahead of its JWT
 * expiry, so the user is silently kept signed in without ever hitting a
 * reactive 401. This is the primary refresh mechanism; `HttpClient`'s
 * reactive 401 handling (refresh-then-retry, falling back to a
 * GET /api/auth/me check) exists only as a safety net for the cases this
 * timer can't cover — e.g. the tab was asleep/backgrounded through the
 * refresh window, or the refresh token was revoked server-side.
 *
 * Owns exactly one pending `setTimeout` at a time. `start()`/`stop()` are
 * idempotent and safe to call from React effects.
 */
export class TokenRefreshScheduler {
  private timeoutId: ReturnType<typeof window.setTimeout> | undefined;

  public constructor(private readonly httpClient: HttpClient) {}

  /** (Re)schedules the next refresh based on the current token's expiry. No-op if there's no session. */
  public start(): void {
    this.scheduleNext();
  }

  public stop(): void {
    if (this.timeoutId !== undefined) {
      window.clearTimeout(this.timeoutId);
      this.timeoutId = undefined;
    }
  }

  private scheduleNext(): void {
    this.stop();
    const expiresAt = tokenStorage.getExpiresAt();
    if (expiresAt === null) {
      return;
    }
    const delay = Math.max(expiresAt - Date.now() - REFRESH_BUFFER_MS, MIN_DELAY_MS);
    this.timeoutId = window.setTimeout(() => {
      void this.refresh();
    }, delay);
  }

  private async refresh(): Promise<void> {
    const refreshed = await this.httpClient.refreshAccessToken();
    if (refreshed) {
      this.scheduleNext();
      return;
    }
    // The refresh token is missing/expired/revoked — there's nothing left to
    // wait for, so end the session the same way a failed reactive 401 does.
    this.stop();
    this.httpClient.triggerSessionExpired();
  }
}
