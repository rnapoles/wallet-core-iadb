const STORAGE_KEY = 'walletsystem.session';

interface StoredSession {
  accessToken: string;
  refreshToken: string;
  /** Milliseconds since epoch. Stored as a number so no re-parsing is needed on reload. */
  expiresAt: number;
}

/**
 * Holds auth tokens in `localStorage`, so a session survives a page reload
 * (F5) instead of forcing the user to sign in again.
 *
 * Security trade-off, by explicit product requirement: `localStorage` is
 * readable by any script running on the page, so a single XSS bug anywhere
 * in the dependency tree can read and exfiltrate these tokens — unlike an
 * in-memory-only store, which disappears on reload and has nothing for such
 * a bug to read. See SECURITY.md for the full rationale and the mitigations
 * that make this an acceptable trade-off here (strict CSP, no
 * `dangerouslySetInnerHTML` anywhere, no third-party script hosts,
 * short-lived access tokens backed by proactive refresh).
 */
class TokenStorage {
  private accessToken: string | null = null;
  private refreshToken: string | null = null;
  private expiresAt: number | null = null;

  public constructor() {
    this.hydrateFromStorage();
  }

  public setTokens(accessToken: string, refreshToken: string, expiresAt: string): void {
    const expiresAtMs = Date.parse(expiresAt);
    this.accessToken = accessToken;
    this.refreshToken = refreshToken;
    this.expiresAt = expiresAtMs;
    this.persist(accessToken, refreshToken, expiresAtMs);
  }

  public getAccessToken(): string | null {
    return this.accessToken;
  }

  public getRefreshToken(): string | null {
    return this.refreshToken;
  }

  /** Milliseconds-since-epoch when the current access token expires, or null if there is no session. */
  public getExpiresAt(): number | null {
    return this.expiresAt;
  }

  public isExpired(): boolean {
    if (this.expiresAt === null) return true;
    // Treat tokens as expired 30s before their real expiry to absorb clock skew.
    return Date.now() >= this.expiresAt - 30_000;
  }

  public clear(): void {
    this.accessToken = null;
    this.refreshToken = null;
    this.expiresAt = null;
    this.removeFromStorage();
  }

  public get isAuthenticated(): boolean {
    return this.accessToken !== null;
  }

  /** Reads a previously persisted session on startup (e.g. after an F5 reload). */
  private hydrateFromStorage(): void {
    try {
      const raw = window.localStorage.getItem(STORAGE_KEY);
      if (!raw) return;

      const parsed: unknown = JSON.parse(raw);
      if (!isStoredSession(parsed)) {
        this.removeFromStorage();
        return;
      }

      this.accessToken = parsed.accessToken;
      this.refreshToken = parsed.refreshToken;
      this.expiresAt = parsed.expiresAt;
    } catch {
      // Corrupted JSON, storage disabled (private browsing), or blocked by the
      // browser — fail safe by starting logged out rather than throwing.
      this.removeFromStorage();
    }
  }

  private persist(accessToken: string, refreshToken: string, expiresAt: number): void {
    try {
      const session: StoredSession = { accessToken, refreshToken, expiresAt };
      window.localStorage.setItem(STORAGE_KEY, JSON.stringify(session));
    } catch {
      // Storage unavailable or full (e.g. quota exceeded, Safari private
      // mode). The session still works for the current tab via the
      // in-memory fields above — it just won't survive a reload.
    }
  }

  private removeFromStorage(): void {
    try {
      window.localStorage.removeItem(STORAGE_KEY);
    } catch {
      // Storage unavailable — nothing to clean up.
    }
  }
}

function isStoredSession(value: unknown): value is StoredSession {
  if (typeof value !== 'object' || value === null) return false;
  const candidate = value as Record<string, unknown>;
  return (
    typeof candidate.accessToken === 'string' &&
    typeof candidate.refreshToken === 'string' &&
    typeof candidate.expiresAt === 'number'
  );
}

/** Single shared instance — token state is inherently app-wide singleton state. */
export const tokenStorage = new TokenStorage();
