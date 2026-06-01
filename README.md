# Distributed Transaction Coordinator API

A production-ready **.NET 8 Web API** for a **Multi-Tenant B2B SaaS platform**, built with Clean Architecture, CQRS, and MediatR.

---

## Architecture Overview

```
solution/
├── src/
│   ├── DistributedTransactionCoordinator.Domain          # Zero external dependencies
│   ├── DistributedTransactionCoordinator.Application     # CQRS, MediatR, validators
│   ├── DistributedTransactionCoordinator.Infrastructure  # EF Core, Redis, RabbitMQ
│   └── DistributedTransactionCoordinator.WebApi          # Controllers, middleware, JWT
└── tests/
    └── DistributedTransactionCoordinator.Tests            # xUnit, Moq, FluentAssertions
```

### Design Principles

| Principle | Implementation |
|---|---|
| **Clean Architecture** | Four isolated layers; Domain has zero external dependencies |
| **CQRS** | Commands and Queries are separate MediatR request types |
| **Repository Pattern** | `IProductRepository` abstracts all DB operations |
| **Multi-Tenancy (RLS)** | `TenantMiddleware` reads `tenant_id` JWT claim; EF Core global query filter enforces isolation |
| **Outbox Pattern** | Domain events written atomically; background worker publishes to RabbitMQ |

---

## Tech Stack

- **.NET 8** Web API
- **PostgreSQL** via Npgsql + EF Core 8
- **Redis** distributed cache (`StackExchange.Redis`)
- **RabbitMQ** event bus (`RabbitMQ.Client`)
- **MediatR 12** for CQRS
- **FluentValidation** for command validation
- **JWT Bearer** authentication
- **Swagger/OpenAPI** with bearer token support

---

## Getting Started

### Prerequisites

- .NET 8 SDK
- PostgreSQL
- Redis
- RabbitMQ

### Configuration

Copy `appsettings.json` and fill in your credentials:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=dtc_db;Username=postgres;******",
    "Redis": "localhost:6379",
    "RabbitMq": "amqp://localhost:5672/"
  },
  "Jwt": {
    "Key": "<256-bit-secret>",
    "Issuer": "DistributedTransactionCoordinator",
    "Audience": "DistributedTransactionCoordinator.Clients"
  }
}
```

### Run EF Core Migrations

```bash
cd src/DistributedTransactionCoordinator.Infrastructure
dotnet ef migrations add InitialCreate --startup-project ../DistributedTransactionCoordinator.WebApi
dotnet ef database update --startup-project ../DistributedTransactionCoordinator.WebApi
```

### Run the API

```bash
cd src/DistributedTransactionCoordinator.WebApi
dotnet run
```

Swagger UI: `https://localhost:<port>/swagger`

---

## Running Tests

```bash
dotnet test tests/DistributedTransactionCoordinator.Tests
```

### Test Coverage

| Test | Pattern | Description |
|---|---|---|
| `Handle_ValidCommand_ReturnsNewProductId` | AAA | Happy path: valid CreateProductCommand returns a non-empty GUID |
| `Handle_ValidCommand_StampsProductWithCurrentTenantId` | AAA | Tenant isolation: product is stamped with the current tenant |
| `Handle_ValidCommand_PropagatesCancellationToken` | AAA | CancellationToken forwarded to repo and UoW |
| `Handle_WhenCacheMiss_QueriesRepositoryAndPopulatesCache` | AAA | Cache miss → DB query → cache populated |
| `Handle_WhenCacheHit_ReturnsCachedDataWithoutQueryingRepository` | AAA | Cache hit → repository never called |
| `Handle_CacheKey_IsScopedToCurrentTenant` | AAA | Cache key contains tenant ID to prevent cross-tenant leakage |

---

## Multi-Tenancy Flow

```
Request → [JWT Validated] → TenantMiddleware → TenantContext.SetTenantId()
                                                      ↓
                                          AppDbContext global query filter
                                          WHERE tenant_id = @currentTenantId
```

Every EF Core query is automatically tenant-scoped via the global query filter on `AppDbContext`. No handler needs to manually filter by tenant.

## Outbox Pattern (Reliable Messaging)

1. `Product.Create()` raises `ProductCreatedEvent` (domain event)
2. `AppDbContext.SaveChangesAsync()` serialises domain events → `OutboxMessage` rows (same transaction)
3. A background worker (to be implemented) polls `outbox_messages` and publishes to RabbitMQ
4. Guarantees **at-least-once delivery** without distributed transactions
