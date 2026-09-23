# Security

This document explains the security decisions baked into this project and
what to double-check before deploying to production.

## Supply-chain hardening

- **`.npmrc`** pins `save-exact=true` so `npm install <pkg>` never writes a
  `^`/`~` range that could silently resolve to a newer, unreviewed version
  later. `package.json` itself only contains exact versions.
- **`ignore-scripts=true`** blocks `pre/post/install` lifecycle scripts from
  running automatically — the most common real-world supply-chain attack
  vector (a compromised transitive dependency executing arbitrary code on
  `npm install`). This has been verified not to break `npm install` or
  `npm run build` in this project. If you add a dependency that genuinely
  needs an install script (rare), install it once with
  `npm install <pkg> --ignore-scripts=false` after reading the script, then
  restore the flag.
- **Commit `package-lock.json`** and deploy with `npm ci`, never
  `npm install`, so CI/CD always installs the exact graph that was reviewed.
- Run `npm run audit` (or `npm audit`) regularly and in CI; treat new
  high/critical advisories as a blocking issue.
- CI should pin the Node/npm version (see `engines` in `package.json`) and
  ideally verify `package-lock.json` hasn't drifted (`npm ci` fails on
  drift automatically).

## XSS and content injection

- **React's JSX escaping is the primary XSS defense** — this project never
  uses `dangerouslySetInnerHTML`.
- `Content-Security-Policy` is set via a `<meta>` tag in `index.html`
  restricting script sources to `'self'` only (no inline scripts, no third-
  party script hosts) and disallowing `object-src`/framing. **In
  production, set the equivalent header (`Content-Security-Policy`) at your
  reverse proxy/CDN as well** — a `<meta>` CSP cannot cover response
  headers like `X-Frame-Options`, `X-Content-Type-Options`, or
  `Strict-Transport-Security`, which should be added server-side.
- `infrastructure/security/sanitize.ts` strips control characters and caps
  length on free-text fields (transaction description/reference) before
  they're sent to the API, as defense in depth.
- ESLint's `no-eval`, `no-implied-eval`, and `no-script-url` rules are
  enabled and enforced (`npm run lint`, zero warnings allowed).

## Authentication & token handling

- **Tokens are persisted in `localStorage`** (`infrastructure/storage/TokenStorage.ts`),
  by explicit product requirement: pressing F5 must restore the session
  instead of forcing a fresh sign-in. This is a deliberate trade-off, not
  an oversight — **`localStorage` is readable by any script on the page**,
  so a single XSS bug anywhere in the dependency tree can read and
  exfiltrate these tokens, unlike an in-memory-only store which has
  nothing left to read after a reload. What keeps this an acceptable
  trade-off in this codebase:
  - A strict `Content-Security-Policy` (`script-src 'self'`, see
    `index.html`) blocks loading any script not bundled by this build.
  - No `dangerouslySetInnerHTML` anywhere in the codebase; all rendered
    text goes through React's JSX escaping.
  - Access tokens are short-lived and refreshed proactively (see below),
    which shrinks the usable window for a stolen token.
  - **If this trade-off changes, the fix is localized:** swap
    `TokenStorage`'s `localStorage` calls for an in-memory `Map`/plain
    fields (the class's public API — `setTokens`/`getAccessToken`/
    `getRefreshToken`/`getExpiresAt`/`clear`/`isAuthenticated` — doesn't
    need to change), and remove the `restoreSession` call in
    `AuthProvider`.
  - **Recommended production hardening beyond this repo:** have the API
    set the refresh token as an `httpOnly`, `Secure`, `SameSite=Strict`
    cookie instead of returning it in the JSON body, so client-side
    JavaScript never has access to it at all, even from `localStorage`.
    The `HttpClient` refresh flow would need a small adjustment to stop
    sending `refreshToken` in the request body once the API does this.
- **Session restore on load (including F5):** `AuthProvider`'s startup
  effect checks whether `TokenStorage` has a persisted token and, if so,
  calls `GET /api/auth/me` to ask the server whether it's still valid — it
  never trusts the token's presence alone. `HttpClient`'s existing 401
  recovery handles the three possible outcomes: the token is still valid
  (succeeds immediately, dashboard renders); it's expired but the refresh
  token isn't (silently refreshed and retried, dashboard renders); or it's
  fully dead (refresh fails, the server confirms via `/me`, storage is
  cleared, and the user is redirected to `/login`). `ProtectedRoute` shows
  a spinner for the duration of this check so an authenticated route never
  flashes its content, or the login page, before the check resolves.
- **Proactive refresh:** `infrastructure/auth/TokenRefreshScheduler.ts`
  reads the JWT's `expiresAt` from `TokenStorage` and schedules a single
  `setTimeout` to refresh the access token ~60 seconds before it expires
  (`REFRESH_BUFFER_MS`), rescheduling itself after every successful
  refresh. It's started by `AuthProvider` after login/register and after a
  successful session restore, and stopped on logout, so under normal
  conditions the user is kept silently signed in and never hits a reactive
  401 at all.
- **Reactive fallback (401 handling):** proactive refresh can still miss —
  the tab was asleep/backgrounded through the refresh window, the device
  was offline, etc. For that case `HttpClient.handleResponseError`
  (`infrastructure/http/HttpClient.ts`) reacts to any 401 (other than on
  `/api/auth/login`, `/api/auth/register`, or `/api/auth/refresh`
  themselves, to avoid recursion or misinterpreting bad credentials as an
  expired session) by: (1) attempting a silent `refreshAccessToken()` and
  replaying the original request if it succeeds; (2) if refresh isn't
  possible, calling `GET /api/auth/me` to positively confirm whether the
  session is actually dead — a network blip or unrelated 5xx on the
  original request must never log the user out, only a confirmed 401 from
  `/me` does. Both the scheduler's failure path and this reactive path
  fail through a single `HttpClient.triggerSessionExpired()` method, so
  there's exactly one place that decides "the session is over": it clears
  `TokenStorage` and invokes the handler `AuthProvider` registers, which
  clears the in-memory user and redirects to `/login`.
- `HttpClient` attaches the bearer token via an Authorization header (never
  a cookie for the access token), coalesces concurrent refresh attempts
  (proactive timer + one or more reactive 401s firing at once) into a
  single network call, and clears all tokens once the session is confirmed
  expired.
- `axios.withCredentials` is explicitly `false` — this SPA does not rely on
  cookies for API authentication, which also limits CSRF exposure.

## Input validation

- Every mutating use case (register, login, create wallet, deposit,
  withdraw, transfer) validates input with `zod` schemas in
  `application/validators/` **before** any network call is made, and
  throws a typed `ValidationError` the UI renders as field-level errors.
- Withdrawal and transfer amounts are additionally checked client-side
  against the wallet's last-known balance (`InsufficientFundsError`) for
  instant feedback — **the API remains the source of truth** and is
  expected to re-validate server-side; the client check is a UX
  convenience only, never a security boundary.

## Idempotency

- `HttpClient` attaches a fresh `Idempotency-Key` header (a v4 UUID) to
  every mutating request (`POST`/`PUT`/`PATCH`/`DELETE`), so a network
  failure and client-side retry of a deposit/withdraw/transfer cannot be
  applied twice — **provided the API honors this header**. Confirm the
  WalletSystem API implements idempotency-key deduplication for
  `/api/transactions/*` before relying on this in production.

## Environment configuration

- Only variables prefixed `VITE_` are exposed to the client bundle, and
  `infrastructure/config/env.ts` validates them eagerly at startup with
  `zod` — a misconfigured deployment fails fast with a clear error instead
  of producing confusing runtime behavior.
- **Never put secrets in `.env` or any `VITE_*` variable.** Anything read
  by client code ends up readable in the compiled browser bundle. This
  frontend only needs a public API base URL; it holds no API keys.
- `.env` is gitignored; `.env.example` is explicitly un-ignored and must be
  kept in sync with `infrastructure/config/env.ts` whenever a new variable
  is added.

## Reporting a vulnerability

If you find a security issue in this codebase, please open a private
report to your security team rather than a public issue, and include
reproduction steps.

## Demo data (`src/shared/data/bank-info.json`)

This file bundles **plaintext seed credentials and cross-account wallet
data** into the client build, to power the login page's "quick demo
sign-in" selector and the transfer form's cross-account destination list.
This is safe only because it's throwaway local/demo data:

- **Never put real user credentials or production data in this file.**
  Anything in `src/shared/data/` ends up readable in the compiled
  JavaScript bundle, exactly like a `VITE_*` environment variable — there
  is no server-side gate on it.
- **Remove `bank-info.json` and the two features that read it
  (`LoginPage`'s quick sign-in selector, `MoneyMovementModal`'s
  bank-info-sourced transfer destination list) before shipping to
  production**, or gate them behind `import.meta.env.DEV` so they're
  stripped from production builds automatically.
- The wallet-transfer use of this file also reflects a real API gap worth
  calling out: `GET /api/wallets` only returns the signed-in user's own
  wallets, so there is currently no legitimate way for the client to look
  up *someone else's* wallet id to transfer to. In production this should
  come from a proper API (e.g. "look up a wallet by id" or "search
  wallets/recipients"), not a bundled JSON file.
