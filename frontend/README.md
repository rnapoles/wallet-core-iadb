# WalletSystem — Frontend

A sleek, futuristic React + TypeScript UI for the WalletSystem API: multi-currency
wallets, deposits, withdrawals, and transfers with a live ledger view.

Built with Clean Architecture / Domain-Driven Design: the domain model has zero
knowledge of HTTP, React, or any framework, and every layer depends only on the
layer beneath it through interfaces.

## Stack

- **React 19 + TypeScript** (strict mode, `noUncheckedIndexedAccess`, `noImplicitReturns`)
- **Vite** for dev/build tooling
- **React Router 7** for client-side routing
- **Zod** for runtime validation (env config + form validators)
- **Axios** for HTTP, wrapped in a single typed `HttpClient`
- **Framer Motion** for the small set of intentional, orchestrated animations
- **CSS Modules** for component styling with a shared design-token system

## Getting started

```bash
cd frontend
cp .env.example .env   # then edit VITE_API_BASE_URL to point at your API
npm ci                 # installs the exact versions from package-lock.json
npm run dev
```

Other scripts:

```bash
npm run build       # type-check (tsc -b) then production build (vite build)
npm run typecheck   # type-check only, no emit
npm run lint         # ESLint, zero warnings allowed
npm run lint:fix     # ESLint with autofix
npm run audit        # npm audit, production dependencies only
npm run preview      # preview the production build locally
```

## Project structure

```
src/
  domain/               # Framework-free core: entities, value objects, enums,
                         # domain errors, and repository interfaces (ports).
    entities/            User, Wallet, Transaction
    value-objects/        Money, Email, EntityId — invariant-enforcing wrappers
    enums/                Currency, TransactionType, ServiceHealthStatus
    errors/               DomainError and its subtypes
    repositories/         IAuthRepository, IWalletRepository, ITransactionRepository, IHealthRepository

  application/           # Use-case orchestration. Depends only on domain interfaces.
    dto/                  Wire-shape types matching the OpenAPI spec exactly (one per file)
    mappers/              DTO → domain entity mappers
    validators/            Zod schemas for every form/command
    services/              AuthService, WalletService, TransactionService, HealthService

  infrastructure/         # Concrete, framework-specific implementations of domain ports.
    http/                  HttpClient (axios wrapper: auth header, idempotency keys, token refresh,
                            reactive 401 → /api/auth/me session verification)
    auth/                  TokenRefreshScheduler (proactive, timer-based access-token refresh)
    repositories/           Concrete *Repository classes implementing the domain interfaces
    storage/                TokenStorage (in-memory — see SECURITY.md)
    security/               Input sanitization helpers
    config/                 env.ts (validated environment config), container.ts (DI composition root)

  presentation/            React. Depends on application services only — never on infrastructure directly.
    components/ui/          Design-system primitives: Button, TextField, SelectField, Card, Modal, ...
    components/layout/      AppShell, Sidebar, AuthLayout
    components/feature/     Auth, wallet, and transaction feature components
    pages/                   One component per route
    routes/                  AppRoutes (route table), ProtectedRoute
    context/                 AuthContext/AuthProvider, ToastContext/ToastProvider
    hooks/                   useAuth, useToast, useWallets, useTransactions
    styles/                  tokens.css (design tokens), global.css (reset + base styles)

  shared/                  Cross-cutting, framework-agnostic utilities (route paths, date
                           formatting, error-message extraction).
```

### Why this layering?

- **Dependency Inversion:** `application/services` depend on
  `domain/repositories` (interfaces), never on `infrastructure/repositories`
  (concrete axios-backed classes) directly. The only place a concrete
  repository is instantiated and bound to its interface is the composition
  root, `infrastructure/config/container.ts`.
- **Testability:** because services take an interface in their constructor,
  every service can be unit-tested with an in-memory fake repository, no
  HTTP mocking required.
- **One type per file:** every entity, value object, enum, DTO, and error
  lives in its own file, so a file's name always tells you exactly what it
  contains and imports stay explicit.

## Security

See [`SECURITY.md`](./SECURITY.md) for the full rationale behind the
in-memory token storage, CSP, supply-chain hardening (`.npmrc`), and
idempotency-key strategy used throughout this project.

## API contract

DTOs in `src/application/dto/` mirror the OpenAPI spec's schemas exactly.
Endpoint paths are centralized in `src/infrastructure/http/apiEndpoints.ts`.
If the API contract changes, update the relevant DTO(s), the matching
mapper in `src/application/mappers/`, and the endpoint path in one place.
