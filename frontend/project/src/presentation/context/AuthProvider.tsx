import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react';
import { useNavigate } from 'react-router-dom';
import { AuthContext } from './AuthContext';
import type { User } from '../../domain/entities/User';
import { container, tokenRefreshScheduler } from '../../infrastructure/config/container';
import { httpClient } from '../../infrastructure/http/HttpClient';
import { tokenStorage } from '../../infrastructure/storage/TokenStorage';
import { routes } from '../../shared/constants/routes';

/**
 * Owns authentication state for the whole app. Tokens live in `tokenStorage`
 * (localStorage-backed — see `TokenStorage` for the security trade-off that
 * involves), so a session survives a page reload: on mount, `restoreSession`
 * below checks whether a token is present and, if so, verifies it with the
 * server before deciding whether to show the dashboard or the login page.
 *
 * Also owns the session's lifecycle relative to `TokenRefreshScheduler` and
 * `HttpClient`'s session-expiry hook:
 *  - `TokenRefreshScheduler` is started after a successful login/register or
 *    session restore, and stopped on logout, so the access token is
 *    silently refreshed in the background on a timer, ahead of its JWT
 *    expiry.
 *  - If that proactive refresh ever fails (refresh token revoked/expired),
 *    or a request gets a reactive 401 that a GET /api/auth/me confirms is a
 *    dead session, `HttpClient` calls the handler registered below, which
 *    clears local user state and redirects to the login page.
 */
export function AuthProvider({ children }: { children: ReactNode }): React.JSX.Element {
  const [user, setUser] = useState<User | null>(null);
  const [isInitializing, setIsInitializing] = useState(true);
  const navigate = useNavigate();

  useEffect(() => {
    httpClient.setSessionExpiredHandler(() => {
      tokenRefreshScheduler.stop();
      setUser(null);
      void navigate(routes.login, { replace: true });
    });

    /**
     * Runs once on app startup (including after an F5 reload). If
     * `tokenStorage` has a persisted token, ask the API who it belongs to:
     * `HttpClient`'s 401 recovery already handles a token that's expired
     * but still refreshable (silent refresh + retry) versus one that's
     * truly dead (clears storage and fires the session-expired handler
     * above). Either way, once this resolves we know definitively whether
     * to render the dashboard or send the user to the login page.
     */
    async function restoreSession(): Promise<void> {
      if (!tokenStorage.isAuthenticated) {
        return;
      }
      try {
        const currentUser = await container.authService.getCurrentUser();
        setUser(currentUser);
        tokenRefreshScheduler.start();
      } catch {
        // A dead/invalid token already triggered `triggerSessionExpired()`
        // inside HttpClient (clearing storage and calling the handler
        // above); nothing further to do here.
      }
    }

    void restoreSession().finally(() => setIsInitializing(false));

    // Stop the scheduler's pending timer if the app itself unmounts.
    return () => tokenRefreshScheduler.stop();
  }, [navigate]);

  const login = useCallback(async (email: string, password: string) => {
    await container.authService.login({ email, password });
    const currentUser = await container.authService.getCurrentUser();
    setUser(currentUser);
    tokenRefreshScheduler.start();
  }, []);

  const register = useCallback(
    async (values: {
      email: string;
      password: string;
      confirmPassword: string;
      firstName: string;
      lastName: string;
    }) => {
      await container.authService.register(values);
      await login(values.email, values.password);
    },
    [login],
  );

  const logout = useCallback(() => {
    tokenRefreshScheduler.stop();
    container.authService.logout();
    setUser(null);
  }, []);

  const value = useMemo(
    () => ({
      user,
      isAuthenticated: user !== null,
      isInitializing,
      login,
      register,
      logout,
    }),
    [user, isInitializing, login, register, logout],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
