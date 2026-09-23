C#/.NET 10 REST API for an account and multi-wallet system using 
MediatR, MassTransit, Serilog, RabbitMQ, and SQLite/MySQL.

Features:

- User registration and login with secure authentication.
- Multiple wallets per account.
- Deposits, withdrawals, and transfers between accounts/wallets.
- Strict transactional integrity and pessimistic database locking to prevent race conditions, double-spending, and inconsistent balances.
- Asynchronous messaging with MassTransit/RabbitMQ.
- Provide migrations, configuration, Docker support, tests, and clear documentation.
- Clean Architecture, DDD, Vertical Slicing, SOLID principles, CQRS, dependency injection, validation, and structured logging.

[Backend README](backend/README.md)

[Frontend README](frontend/README.md)

## Quick Start

### 1. Clone and Navigate

```bash
docker-compose up -d
```

- Frontend [http://localhost:5001](http://localhost:5001)
- API [http://localhost:5000](http://localhost:5000)
- Webmail [http://localhost:8025/](http://localhost:8025/)
- Seq metrics [http://localhost:8081/](http://localhost:8081/)
- RabbitMQ [http://localhost:15672/](http://localhost:15672/)



