# Wallet System - .NET 10 Clean Architecture

A comprehensive wallet management system built with **.NET 10**, implementing Clean Architecture, CQRS, and secure financial transaction handling, with async messaging (MassTransit), pluggable caching, background workers, and demo tooling.

## Features

- **User Authentication**: Secure registration and login with JWT access tokens, refresh-token rotation (`POST /api/auth/refresh`), and Argon2id password hashing
- **Multiple Wallets**: Users can create and manage multiple wallets with different currencies
- **Transactions**:
  - Deposits with idempotency support (reference-based duplicate prevention)
  - Withdrawals with balance validation
  - Transfers between wallets with deadlock prevention
- **Security**:
  - Pessimistic database locking (`SELECT ... FOR UPDATE` on MySQL) to prevent race conditions
  - Transaction isolation for data consistency
  - Prevention of double-spending
- **Messaging**: Domain events published through MassTransit — `InMemory` transport for development/testing or `RabbitMQ` for production (configurable via `Messaging__Mode`)
- **Caching**: Pluggable cache providers via `CacheFactory` — `Redis`, `Memory`, `File`, or `Memcached`
- **Notifications**: SMTP email notifications (Mailpit in development) and admin notification service
- **Observability**: Health checks (`/api/health`, `/live`, `/ready`), Swagger/OpenAPI (Swashbuckle), Serilog structured logging with Seq sink
- **Architecture**: Clean Architecture with CQRS pattern using MediatR
- **Validation**: FluentValidation for request validation
- **Logging**: Structured logging with Serilog (Console + Seq)

## Solution Structure

The solution (`WalletSystem.sln`) contains 11 projects:

```
backend/
├── WalletSystem.Domain/             # Domain layer (Entities: Users, Wallets, Transactions; Contracts)
├── WalletSystem.Application/        # Application layer (Vertical Slices: Auth, Users, Wallets, Transactions; Common: Dtos, DomainEvents)
├── WalletSystem.Infrastructure/     # Infrastructure layer (Persistence, Cache, Security, Messaging/SMTP, EventBus, Notifications, HealthCheck, Id)
├── WalletSystem.Shared/             # Cross-cutting shared code (Seeders/SampleData, Settings extensions) — outside the layer hierarchy by design
├── WalletSystem.Api/                # API presentation layer (feature-per-controller slices, Middleware, DI Extensions, Swagger)
├── WalletSystem.Worker.Mail/        # Background worker: consumes deposit/withdrawal events and sends emails via SMTP
├── WalletSystem.Worker.EventListeners/ # Background worker: consumes all domain events (incl. user registered/login) + DLQ consumer, admin event consumers
├── WalletSystem.Demo.Client/        # Interactive CLI demo client exercising the API end-to-end
├── WalletSystem.Demo.Daemon/        # Demo daemon (continuous simulated activity)
├── WalletSystem.Demo.Shared/        # Shared demo commands/DTOs
└── WalletSystem.Tests/              # Unit & integration tests (xUnit): Api, Application, Infrastructure, plus NetArchTest-style Architecture tests
```

### Dockerfiles

Each runnable service has its own image definition: `Dockerfile.api`, `Dockerfile.worker`, `Dockerfile.worker-mail`, `Dockerfile.daemon`, `Dockerfile.demo`.

## Prerequisites

- .NET 10 SDK
- Docker & Docker Compose (for RabbitMQ, Redis, MySQL, Mailpit, Seq)
- SQLite (default) or MySQL (optional)

## Quick Start

### 1. Clone and Navigate

```bash
cd backend
```

### 2. Configure Environment Variables

Copy the example env file and adjust settings:

```bash
cp .env.example .env
```

Configuration priority (highest to lowest): **Environment variables → `.env` file → `appsettings.json`**.

Edit `.env` with your preferred settings for:
- Database connection strings (SQLite default, MySQL optional)
- JWT secret key
- Messaging mode (`InMemory` or `RabbitMQ`) and RabbitMQ credentials
- Cache provider (`Redis`, `Memory`, `File`, `Memcached`)
- SMTP settings for mail notifications

### 3. Run with Docker (Recommended)

From the repository root, start all services (API, workers, demo clients, RabbitMQ, Redis, MySQL, Mailpit, Seq):

```bash
docker-compose up -d
```

Compose services: `api`, `frontend`, `demo-http`, `demo-daemon`, `worker-simple` (event listeners), `worker-mail`, `event-bus-server` (RabbitMQ), `cache-server` (Redis), `smtp-server` (Mailpit), `db-server` (MySQL), `log-server` (Seq).

The API is exposed at `http://localhost:5000` (container port 8080 mapped to host 5000).

### 4. Run Without Docker (SQLite only)

```bash
# Restore packages
dotnet restore

# Run the API (database is created automatically via EnsureCreated)
dotnet run --project WalletSystem.Api
```

With the `http` launch profile the API runs at `http://localhost:5264`; Swagger UI is available at `/swagger`.

## Database

- **Default engine**: SQLite (`Data Source=walletsystem.db`), configured via `Database:Engine` / `ConnectionStrings:DefaultConnection`
- **MySQL** supported via `ConnectionStrings:MySqlConnection` (pessimistic `FOR UPDATE` locking is enabled on MySQL)
- The schema is created on startup with `EnsureCreatedAsync()`; switch to `MigrateAsync()` for production deployments

## Demo Data Seeding

When running in **Development mode**, the application seeds the database with demo data on startup (`DatabaseSeeder` + shared `SampleData`):

- **4 Demo Users**: Alice, Bob, Charlie, and Diana with pre-configured passwords
- **Multiple Wallets** per user in different currencies (USD, EUR, GBP, CAD, JPY) with initial balances
- **Demo Transactions**: including deposits, withdrawals, and transfers between users

### Demo User Credentials

| User | Email | Password |
|------|-------|----------|
| Alice | alice@example.com | Alice123! |
| Bob | bob@example.com | Bob123! |
| Charlie | charlie@example.com | Charlie123! |
| Diana | diana@example.com | Diana123! |

> **Note**: Seeding only occurs when the application is running in Development mode and the data does not already exist. Production deployments will not seed demo data.

## API Endpoints

Controllers follow a feature-per-controller layout under `WalletSystem.Api/Controllers`.

### Authentication (`/api/auth`)

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| POST | `/api/auth/register` | Register new user | No |
| POST | `/api/auth/login` | Login and get JWT token | No |
| POST | `/api/auth/refresh` | Refresh an expired access token | No (refresh token) |
| GET | `/api/auth/me` | Get current authenticated user | Yes |

### Wallets (`/api/wallets`)

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| POST | `/api/wallets` | Create new wallet | Yes |
| GET | `/api/wallets` | Get all user wallets | Yes |
| GET | `/api/wallets/{id}` | Get wallet by ID | Yes |

### Transactions (`/api/transactions`)

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| POST | `/api/transactions/deposit` | Deposit funds | Yes |
| POST | `/api/transactions/withdraw` | Withdraw funds | Yes |
| POST | `/api/transactions/transfer` | Transfer between wallets | Yes |
| GET | `/api/transactions/wallet/{walletId}` | Get wallet transactions | Yes |
| GET | `/api/transactions/{id}` | Get transaction by ID | Yes |

### Health (`/api/health`)

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| GET | `/api/health` | Overall service status | No |
| GET | `/api/health/live` | Liveness probe | No |
| GET | `/api/health/ready` | Readiness probe (checks dependencies) | No |

## Richardson Maturity Model

The API (see `docs/openapi-generated.yaml`) currently sits at **Level 2 (HTTP Verbs)** of the
[Richardson Maturity Model](https://martinfowler.com/articles/richardsonMaturityModel.html),
at the lower end of that level:

| Level | Description | Status |
|-------|-------------|--------|
| **0 – The Swamp of POX** | Single endpoint, all operations via one verb (e.g. POST with an action in the payload) | ✅ Passed |
| **1 – Resources** | Individual URIs per resource/type | ⚠️ Partially |
| **2 – HTTP Verbs** | Correct use of HTTP methods (GET/POST/PUT/DELETE) and status codes | ⚠️ Partially |
| **3 – Hypermedia (HATEOAS)** | Responses include links to related resources / available actions | ❌ Not supported |

### Level 1 – Resources: ✅ Mostly there

- Resource-oriented URIs: `/api/wallets`, `/api/wallets/{id}`, `/api/transactions/{id}`.
- Caveat: transaction operations are RPC-style action endpoints (`/api/transactions/deposit`,
  `/api/transactions/withdraw`, `/api/transactions/transfer`) rather than pure resources.

### Level 2 – HTTP Verbs: ⚠️ Partial

- Correct GET/POST separation: reads use `GET`, state-changing operations use `POST`.
- Gaps:
  - No `PUT`/`PATCH`/`DELETE` anywhere in the spec (wallets cannot be updated or deleted).
  - Every documented response is only `200 OK`; e.g. `POST /api/wallets` returns `Ok()`
    instead of `201 Created` with a `Location` header.
  - Error statuses (`400`, `401`, `404`, `503`) are produced by the `GlobalExceptionHandler`
    but are not declared in the OpenAPI contract.

### Level 3 – HATEOAS: ❌ Not supported

- No hypermedia controls (`_links`, HAL, `Link` headers) in responses; clients must
  construct URIs out-of-band.

### How to advance

1. Return `201 Created` + `Location` for resource creation and document error responses
   (ideally RFC 7807 problem details) in the OpenAPI spec → solidifies Level 2.
2. Add `PUT`/`DELETE` for wallets and model transactions as sub-resources.
3. Embed hypermedia links in responses (e.g. wallet → its transactions, available actions)
   to reach Level 3.

## Example Requests

### Register User

```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "SecurePass123!",
    "firstName": "John",
    "lastName": "Doe"
  }'
```

### Login

```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "SecurePass123!"
  }'
```

### Create Wallet (requires auth token)

```bash
curl -X POST http://localhost:5000/api/wallets \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "userId": "USER_ID_FROM_LOGIN",
    "name": "Savings",
    "currency": "USD"
  }'
```

### Deposit Funds

```bash
curl -X POST http://localhost:5000/api/transactions/deposit \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "walletId": "WALLET_ID",
    "amount": 1000.00,
    "description": "Initial deposit",
    "reference": "DEP-001"
  }'
```

### Transfer Between Wallets

```bash
curl -X POST http://localhost:5000/api/transactions/transfer \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "fromWalletId": "WALLET_ID_1",
    "toWalletId": "WALLET_ID_2",
    "amount": 100.00,
    "description": "Transfer to savings",
    "reference": "TRF-001"
  }'
```

## Messaging & Workers

Domain events are defined in `WalletSystem.Application/Common/DomainEvents`:

- `UserRegistered`, `UserLogin`
- `TransactionCreated`, `DepositCompleted`, `WithdrawalCompleted`, `TransferCompleted`

Consumers:

| Worker | Consumes | Action |
|--------|----------|--------|
| `WalletSystem.Worker.Mail` | `DepositCompleted`, `WithdrawalCompleted` | Sends confirmation emails via SMTP (Mailpit in dev) |
| `WalletSystem.Worker.EventListeners` | All domain events + admin variants | Event logging/listening, includes a Dead Letter Queue (DLQ) consumer |

Transport is selected by `Messaging__Mode`: `InMemory` (single process, dev/test) or `RabbitMQ` (distributed, production).

## Running Tests

```bash
cd backend
dotnet test WalletSystem.Tests/WalletSystem.Tests.csproj
```

Tests mirror the slice structure (`Api`, `Application`, `Infrastructure`) and include `Architecture/CleanArchitectureTests.cs`, which enforces layer dependency rules.

## Architecture Principles

### Clean Architecture Layers

1. **Domain Layer**: Core business entities (`User`, `Wallet`, `Transaction`) and contracts
2. **Application Layer**: Business logic as vertical feature slices (commands/handlers/validators), DTOs, domain events
3. **Infrastructure Layer**: EF Core persistence (DbContext, repositories, unit of work, interceptors), caching, security/JWT, messaging, notifications
4. **API Layer**: Feature-per-controller endpoints, middleware, DI extensions, configuration
5. **Shared Layer**: `WalletSystem.Shared` sits outside the layer hierarchy to share seed data and settings extensions without violating the Dependency Rule

### CQRS Pattern

- Commands for write operations (Register, Login, CreateWallet, Deposit, Withdraw, Transfer)
- Queries for read operations (GetWalletById, GetWalletsByUserId, GetTransactionById, GetTransactionsByWalletId, GetCurrentUser)
- MediatR for mediator pattern implementation (with pipeline behaviors in `Infrastructure/EventBus/Behaviors`)

### Transaction Integrity

- **Pessimistic Locking**: `SELECT ... FOR UPDATE` locks on wallets during transactions (MySQL)
- **Deadlock Prevention**: Consistent lock ordering by comparing Guids
- **Idempotency**: Reference-based duplicate transaction prevention
- **Atomic Operations**: All balance updates within database transactions (Unit of Work)

### Security Features

- Argon2id password hashing
- JWT authentication with configurable expiration + refresh tokens
- Role-based authorization (expandable)
- Input validation with FluentValidation
- Global exception handling middleware (no internal detail leakage)

## Configuration

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ConnectionStrings__DefaultConnection` | DB connection string | `Data Source=walletsystem.db` (SQLite) |
| `ConnectionStrings__MySqlConnection` | MySQL connection string | - |
| `JwtSettings__SecretKey` | JWT signing key (≥32 chars) | - |
| `JwtSettings__Issuer` / `JwtSettings__Audience` | Token issuer/audience | `WalletSystem.Api` / `WalletSystem.Users` |
| `JwtSettings__ExpirationMinutes` | Access token expiration | 60 |
| `JwtSettings__RefreshTokenDays` | Refresh token lifetime | 7 |
| `Messaging__Mode` | `InMemory` or `RabbitMQ` | `InMemory` |
| `RabbitMQSettings__Host` / `Port` / `Username` / `Password` / `VirtualHost` | RabbitMQ connection | `event-bus-server` / 5672 / guest / guest / `/` |
| `Cache__Provider` | `Redis`, `Memory`, `File`, or `Memcached` | `Redis` |
| `Cache__RedisConnection` | Redis endpoint | `cache-server:6379` |
| `SmtpSettings__Host` / `Port` / `From` | SMTP notification settings | `smtp-server` / 1025 / `noreply@walletsystem.com` |
| `AdminNotification__Enabled` / `EmailAddress` | Admin alert emails | `true` / `admin@walletsystem.com` |

## Docker Support

### Services (repo-root `docker-compose.yml`)

- **api**: ASP.NET Core Web API (`Dockerfile.api`, port 8080 → host 5000)
- **worker-simple**: Event listener worker (`Dockerfile.worker`)
- **worker-mail**: Mail worker (`Dockerfile.worker-mail`)
- **demo-http / demo-daemon**: Demo client and daemon
- **event-bus-server**: RabbitMQ 3 (management UI on 15672)
- **cache-server**: Redis 7
- **db-server**: MySQL 8 (optional; SQLite used by default in development)
- **smtp-server**: Mailpit (development SMTP catcher)
- **log-server**: Seq (structured log explorer)

### Build and Run

```bash
# from repository root
docker-compose build
docker-compose up
```

## Troubleshooting

### Database Connection Issues

Ensure the connection string in `appsettings.json` or `.env` is correct. For SQLite the file `walletsystem.db` is created next to the API working directory; the `sqlite_data` compose volume persists it in Docker.

### RabbitMQ Connection Issues

Verify RabbitMQ is running and `Messaging__Mode=RabbitMQ`:
```bash
docker ps | grep rabbitmq
```

If messages fail repeatedly, check the dead-letter queue handled by `Worker.EventListeners/Consumers/Dlq/DlqConsumer.cs`.

### Cache Issues

Switch providers via `Cache__Provider` (e.g., `Memory` to run without Redis). Use `WalletSystem.Samples` to exercise each provider standalone.

## Todo

- [ ] Zero Trust Security and end-to-end HTTPS.
- [ ] Complete Richardson Maturity Model Level 2 and evaluate HATEOAS for Level 3.
- [ ] Pagination and consistent API response standards.
- [ ] Role-based and permission-based authorization.
- [ ] Rate limiting.
- [ ] Administrative back office.
- [ ] Elasticsearch or Meilisearch.
- [ ] Result Pattern and custom domain exceptions.
- [ ] Aggregate Roots and a richer domain model.
- [ ] Tell, Don't Ask in entities and aggregates.
- [ ] EF Core Migrations.
- [ ] Support for multiple persistence providers, including EF Core and NHibernate.
- [ ] Improved observability with metrics such as CPU, memory, RPS, latency, and slow queries.
- [ ] OLAP for statistics, reporting, and Business Intelligence.
- [ ] Read replicas and database scalability strategies.
- [ ] Sharding.
- [ ] Event Sourcing.
- [ ] Consul for service discovery.
- [ ] Vault for secrets and configuration management.
- [ ] Migration to .NET Aspire.



## License

MIT License

## Contributing

1. Fork the repository
2. Create a feature branch
3. Write tests for new features (including architecture-rule compliance)
4. Submit a pull request
