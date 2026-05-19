<div align="center">

# 🚀 FloraCore

### Enterprise-Grade .NET 9 REST API Boilerplate

[![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![EF Core](https://img.shields.io/badge/EF%20Core-9.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://learn.microsoft.com/en-us/ef/core/)
[![Tests](https://img.shields.io/badge/tests-36%20passed-2ea44f?style=for-the-badge)](./FloraCore.Tests)
[![Docker](https://img.shields.io/badge/Docker-Ready-2496ED?style=for-the-badge&logo=docker&logoColor=white)](./Dockerfile)
[![License](https://img.shields.io/badge/License-MIT-yellow?style=for-the-badge)](./LICENSE)

**Clean Architecture** · **CQRS** · **MediatR** · **Hybrid Cache** · **Outbox Pattern** · **OpenTelemetry**

[Getting Started](#-getting-started) · [Architecture](#-architecture) · [API Reference](#-api-reference) · [Deployment](#-deployment)

</div>

---

## ✨ Highlights

| Category | What's Inside |
|----------|---------------|
| 🏗️ **Architecture** | Clean Architecture + CQRS + Vertical Slices via MediatR |
| ⚡ **Performance** | HybridCache (L1 Memory + L2 Redis), Brotli/Gzip compression, `.AsNoTracking()` |
| 🔐 **Security** | JWT with Refresh Token Rotation & Reuse Detection, Security Headers, Data Masking |
| 🛡️ **Resilience** | Polly (Retry + Circuit Breaker), Outbox Pattern, Hangfire background jobs |
| 📊 **Observability** | Serilog structured logging, OpenTelemetry tracing, Health Checks |
| 🧪 **Testing** | 36 Integration Tests, Architecture Tests, SQLite In-Memory |
| 🐳 **DevOps** | Docker, Docker Compose, Kubernetes manifests, GitHub Actions CI |

---

## 🏗 Architecture

The project enforces a **strict dependency rule** — dependencies only flow inward.

```
┌──────────────────────────────────────────────────────────┐
│                    Presentation                          │
│         Controllers · Filters · Middleware               │
│                                                          │
│   ┌──────────────────────────────────────────────────┐   │
│   │                 Application                      │   │
│   │    Features (CQRS) · Behaviors · Interfaces      │   │
│   │                                                  │   │
│   │   ┌──────────────────────────────────────────┐   │   │
│   │   │              Domain                      │   │   │
│   │   │     Entities · Value Objects · Errors    │   │   │
│   │   └──────────────────────────────────────────┘   │   │
│   └──────────────────────────────────────────────────┘   │
│                                                          │
│   ┌──────────────────────────────────────────────────┐   │
│   │              Infrastructure                      │   │
│   │   EF Core · Dapper · Redis · Cloudinary · JWT    │   │
│   │   SignalR · Hangfire · OpenTelemetry             │   │
│   └──────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────────┘
```

### 📁 Project Structure

```text
FloraCore/
├── Application/                     # Use Cases Layer
│   ├── Common/
│   │   ├── Attributes/              # [Cacheable], [Authorize] custom attributes
│   │   ├── Behaviors/               # MediatR Pipeline: Logging, Validation, Caching, Auth
│   │   ├── Constants/               # No magic strings
│   │   ├── Extensions/              # DI registration, Service extensions
│   │   ├── Interfaces/              # Contracts (IGenericRepository, IUnitOfWork, etc.)
│   │   └── Models/                  # ApiResponse<T>, QueryOptions, PagedResult
│   └── Features/                    # Vertical Slices
│       ├── Auth/                    # Login, Register, Refresh Token Rotation
│       ├── Posts/                   # CRUD + Search + Rating
│       ├── Products/                # E-commerce catalog
│       ├── Cart/                    # Shopping cart
│       ├── Chat/                    # Real-time messaging
│       └── ...
├── Domain/                          # Core Business Layer
│   ├── Entities/                    # Post, AppUser, Product, RefreshToken, OutboxMessage
│   └── Exceptions/                  # DomainException, EntityNotFoundException
├── Infrastructure/                  # External Concerns
│   ├── Data/                        # AppDbContext, Migrations, DatabaseSeeder, UnitOfWork
│   ├── Repositories/                # GenericRepository, PostQueryService (Dapper)
│   ├── Services/                    # JWT, Cloudinary, Notification, OutboxProcessor
│   ├── Hubs/                        # SignalR: ChatHub, NotificationHub
│   ├── Logging/                     # LogMaskingEnricher (Serilog data protection)
│   └── Security/                    # HangfireDashboardAuthFilter
├── Controllers/                     # 12 API Controllers (versioned: /api/v1/...)
├── Middleware/                      # Exception Handling, Security Headers, Token Blacklist
├── Filters/                         # ApiResponseFilter (auto-wrap responses)
└── FloraCore.Tests/                   # Test Suite
    ├── IntegrationTests/            # 7 test classes, 36 tests
    ├── ArchitectureTests/           # Clean Architecture enforcement
    └── Mocks/                       # FakeFileService
```

---

## 🛠 Technology Stack

### Core Runtime
| Technology | Version | Purpose |
|------------|---------|---------|
| **.NET** | 9.0 | Runtime & SDK |
| **C#** | 13 | Language features |
| **MediatR** | 12.4 | CQRS + Mediator pattern |
| **FluentValidation** | 11.11 | Request validation pipeline |
| **AutoMapper** | 16.1 | Object-to-object mapping |

### Data & Caching
| Technology | Purpose |
|------------|---------|
| **EF Core 9** | ORM for write operations (Code-First) |
| **Dapper** | Micro-ORM for high-performance reads |
| **SQL Server / PostgreSQL** | Multi-provider database support |
| **HybridCache** | Native .NET 9 — L1 (Memory) + L2 (Redis) |
| **Redis** | Distributed cache, rate limiting, session |

### Security & Real-time
| Technology | Purpose |
|------------|---------|
| **JWT + Refresh Token Rotation** | Auth with reuse detection & chain revocation |
| **ASP.NET Core Identity** | User management, roles, claims |
| **SignalR** | WebSocket for chat & notifications |
| **AspNetCoreRateLimit** | IP-based request throttling |

### Infrastructure & Observability
| Technology | Purpose |
|------------|---------|
| **Serilog** | Structured logging + data masking |
| **OpenTelemetry** | Distributed tracing |
| **Hangfire** | Background job processing |
| **Polly** | Retry + Circuit Breaker resilience |
| **Cloudinary** | Cloud file storage & CDN |
| **Scalar** | API documentation UI |

---

## 🔐 Security Architecture

```
Request → Security Headers → Exception Handler → Rate Limiting
       → JWT Authentication → Token Blacklist Check → Authorization
       → MediatR Pipeline → [Logging → Validation → Caching → Auth → Handler]
```

### Refresh Token Rotation & Reuse Detection

```
Client                          Server
  │                               │
  │── Login ──────────────────────▶│ → Returns: AccessToken + RefreshToken_A
  │                               │
  │── Refresh (Token_A) ─────────▶│ → Marks Token_A as "used"
  │◀── New AccessToken + Token_B ─│   Creates Token_B linked to Token_A
  │                               │
  │── Refresh (Token_A) ─────────▶│ ⚠️ REUSE DETECTED!
  │◀── 401 + ALL tokens revoked ──│   Entire token chain invalidated
```

### Security Headers (Auto-injected)

| Header | Value |
|--------|-------|
| `X-Content-Type-Options` | `nosniff` |
| `X-Frame-Options` | `DENY` |
| `X-XSS-Protection` | `1; mode=block` |
| `Referrer-Policy` | `strict-origin-when-cross-origin` |
| `Content-Security-Policy` | `default-src 'self'; frame-ancestors 'none'` |
| `Strict-Transport-Security` | `max-age=31536000` *(production only)* |

---

## ⚡ Performance Features

### HybridCache (Native .NET 9)

```
Request → L1 Memory Cache (μs) → L2 Redis (ms) → Database (10-50ms)
              Cache Hit?              Cache Hit?        Execute Query
                 ↓ Yes                   ↓ Yes              ↓
             Return data           Return + Backfill L1   Store L1+L2
```

- **L1 hit**: ~microseconds (in-process memory)
- **L2 hit**: ~1-5ms (Redis network hop)
- **Cache miss**: full DB query, then populate both layers
- **Stampede protection**: built-in locking prevents duplicate factory calls

### Response Compression

| Algorithm | Priority | Compression Ratio |
|-----------|----------|-------------------|
| **Brotli** | Primary | ~20-26% of original |
| **Gzip** | Fallback | ~30-35% of original |

Targeted MIME types: `application/json`, `text/plain`, `image/svg+xml`

### EF Core Optimization

- **`.AsNoTracking()`** on all read-only queries
- **Bulk operations** via `ExecuteUpdateAsync` (no N+1)
- **Split Queries** for complex includes
- **UnitOfWork** with `Stage*` methods for atomic multi-step operations

---

## 🛡 Resilience & Reliability

### Resilient Telemetry & HTTP Communications

FloraCore implements industry-standard resilience policies (Retry, Circuit Breaker) via **Polly v8/v9** integrated directly with **`IHttpClientFactory`**.

#### 1. Resilient HTTP Client (`ResilientClient`)
Instead of manually instantiating `HttpClient` (which causes socket exhaustion), the project uses a named resilient client registered in `Program.cs`. Services injecting `IHttpClientFactory` receive a client with transparent, pre-configured resilience:

```csharp
// Resolve from pre-configured factory
var httpClient = httpClientFactory.CreateClient("ResilientClient");
var bytes = await httpClient.GetByteArrayAsync(fileUrl); // Resiliency is fully automated!
```

#### 2. Polly Pipeline: `external-services`
For non-HTTP operations or wrapping third-party SDK calls (like Cloudinary SDK uploads/destroys), a global pipeline `external-services` is exposed via the `ResiliencePipelineProvider`:

```
Request → Retry (3x, exponential + jitter) → Circuit Breaker → Execute
                                                    │
                                              Opens after 50%
                                              failure in 30s window
                                              (min 5 calls)
                                                    │
                                              Breaks for 15s
```

### Outbox Pattern

```
Handler Transaction:
  1. Save Post to DB         ─┐
  2. Save OutboxMessage to DB ─┘ ← Single atomic transaction

Hangfire (every minute):
  1. Read unprocessed OutboxMessages
  2. Execute side-effect (Notification, Email, etc.)
  3. Mark as processed
  4. Retry failed messages (max 5 attempts)
```

---

## 📡 API Reference

### Authentication (`/api/v1/auth`)

| Method | Endpoint | Auth | Description |
|--------|----------|:----:|-------------|
| `POST` | `/register` | ❌ | Create new account |
| `POST` | `/login` | ❌ | Get access + refresh tokens |
| `POST` | `/refresh` | ❌ | Rotate refresh token |
| `POST` | `/logout` | ✅ | Blacklist current token |

### Posts (`/api/v1/posts`)

| Method | Endpoint | Auth | Description |
|--------|----------|:----:|-------------|
| `GET` | `/` | ❌ | List posts (cursor pagination) |
| `GET` | `/{id}` | ❌ | Get post detail (cached 5min) |
| `GET` | `/search` | ❌ | Search with filters |
| `POST` | `/search` | ❌ | Search with complex filter model |
| `POST` | `/` | ✅ | Create post |
| `PUT` | `/{id}` | ✅ | Update post (owner only) |
| `DELETE` | `/{id}` | ✅ | Delete post (owner only) |
| `POST` | `/{id}/rate` | ✅ | Rate a post (1-5) |

### Products (`/api/v1/products`)

| Method | Endpoint | Auth | Description |
|--------|----------|:----:|-------------|
| `GET` | `/` | ❌ | List products (paged) |
| `GET` | `/{id}` | ❌ | Get product detail |
| `POST` | `/` | ✅ | Create product |
| `PUT` | `/{id}` | ✅ | Update product |
| `DELETE` | `/{id}` | ✅ | Delete product |

### Also available: Cart, Favorites, Chat, Notifications, Files, Users, Reviews, Categories

---

## 🚀 Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- SQL Server (LocalDB) or PostgreSQL
- Redis *(optional — falls back to in-memory)*
- [Cloudinary account](https://cloudinary.com) *(free tier is sufficient)*

### Quick Start

```bash
# 1. Clone
git clone https://github.com/your-username/flora-core.git
cd flora-core

# 2. Configure
cp .env.example .env
# Edit .env with your credentials

# 3. Restore & Run
dotnet restore
dotnet run

# 4. Open API Docs
# → http://localhost:5000/scalar/v1
```

### 🔑 Default Credentials

| Role | Email | Password |
|------|-------|----------|
| Admin | `admin@floracore.com` | `Admin123!` |

---

## ⚙️ Configuration

### Environment Variables (`.env`)

```bash
# Database
DB_PASSWORD=your_secure_password
DatabaseProvider=SqlServer              # or "PostgreSQL"

# JWT (CRITICAL: min 32 characters)
JWT_SECRET=YourSuperSecretKeyWithAtLeast32Characters!

# Cloudinary
CLOUDINARY_CLOUD_NAME=your_cloud_name
CLOUDINARY_API_KEY=your_api_key
CLOUDINARY_API_SECRET=your_api_secret

# Redis (optional)
REDIS_CONNECTION=localhost:6379
```

### Multi-Database Support

| Provider | Config Value | Dialect Strategy | Use Case |
|----------|-------------|------------------|----------|
| SQL Server | `"SqlServer"` | `SqlServerPostQueryDialect` | Local development (default) |
| PostgreSQL | `"PostgreSQL"` | `PostgresPostQueryDialect` | Docker / Cloud deployment |
| SQLite / Other | Default / Any | `SqlitePostQueryDialect` | Integration testing |

```json
// appsettings.json
{
  "DatabaseProvider": "PostgreSQL"
}
```

#### 🛡️ SQL Dialect Strategy Pattern (`IPostQueryDialect`)

To ensure full database engine compatibility and strictly follow the **Open-Closed Principle (OCP)**, raw SQL queries (using Dapper) are decoupled from the query service into dialect-specific strategies.

If you want to add support for a new database provider (e.g., MySQL):
1. Implement the [IPostQueryDialect](./Application/Common/Interfaces/IPostQueryDialect.cs) interface in the Application/Common layer.
2. Create your provider-specific implementation (e.g., `MySqlPostQueryDialect`) in the Infrastructure layer.
3. Update the Dependency Injection registration in **[DependencyInjection.cs](./Infrastructure/DependencyInjection.cs)** to resolve the new dialect when the configured `DatabaseProvider` matches your database.

### Rate Limiting

| Endpoint | Limit | Window |
|----------|-------|--------|
| `*` (all) | 100 requests | 1 minute |
| `POST /auth/login` | 5 requests | 1 minute |
| `POST /auth/register` | 3 requests | 1 minute |
| `POST /auth/refresh` | 10 requests | 1 minute |

---

## 📊 Observability & Monitoring

FloraCore comes with a fully containerized, production-grade **APM & Observability Stack**. All application metrics and distributed traces are streamed over OTLP to a centralized telemetry pipeline.

*   **Traces**: Auto-captures incoming requests, outgoing HTTP calls, and EF Core database queries (including raw SQL).
*   **Metrics**: Measures request latency, CPU/memory, and .NET Garbage Collection.
*   **Health Checks**: Diagnostics endpoint at `/health` returning connection details for PostgreSQL and Redis.

### 📡 Telemetry Services

When running with Docker Compose, you can access the following dashboards:

*   **Grafana**: [http://localhost:3100](http://localhost:3100) (Default login: `admin` / `admin`)
*   **Jaeger (Traces)**: [http://localhost:16686](http://localhost:16686)
*   **Prometheus (Metrics)**: [http://localhost:9090](http://localhost:9090)
*   **Detailed JSON Health Checks**: [http://localhost:8080/health](http://localhost:8080/health)

For a complete architectural overview and configurations, see the **[Observability Guide](./docs/observability.md)**.

---

## 🐳 Deployment

### Docker Compose (Recommended)

```bash
# Start all services (API + PostgreSQL + Redis)
docker-compose up -d

# Auto-rebuild on code changes
docker-compose up --watch
```

### One-Click Deploy (Windows)

```powershell
.\deploy-docker.ps1
```

### Manual Docker

```bash
docker build -t flora-core .
docker run -p 8080:8080 --env-file .env flora-core
```

### Kubernetes

```bash
kubectl apply -f k8s/
```

---

## 🧪 Testing

```bash
# Run all 36 tests
dotnet test

# Run specific test class
dotnet test --filter "PostsControllerTests"

# With verbosity
dotnet test --logger "console;verbosity=detailed"
```

### Test Infrastructure

| Component | Implementation |
|-----------|----------------|
| **Framework** | xUnit 2.9 + VSTest 18 |
| **Server** | `WebApplicationFactory<Program>` |
| **Database** | SQLite In-Memory (persistent connection) |
| **File Storage** | `FakeFileService` (no Cloudinary needed) |
| **Auth** | Real JWT flow per test |

### Test Coverage

| Test Class | Tests | What It Covers |
|-----------|:-----:|----------------|
| `PostsControllerTests` | 7 | CRUD, search, pagination, rating, ownership |
| `AuthControllerTests` | 4 | Register, login, refresh, token blacklisting |
| `UsersControllerTests` | 5 | Profile, roles, admin operations |
| `FilesControllerTests` | 6 | Upload, download, privacy, ownership deletion |
| `PostCategoriesControllerTests` | 4 | CRUD for categories |
| `ChatAndNotificationTests` | 3 | SignalR message flow, notification delivery |
| `ArchitectureTests` | 7 | Dependency rule enforcement |

---

## 📋 MediatR Pipeline

Every request flows through a configurable pipeline of cross-cutting concerns:

```
Request
  → LoggingBehavior          # Logs entry/exit + duration
  → ValidationBehavior       # FluentValidation rules
  → AuthorizationBehavior    # Role/Policy checks
  → CachingBehavior          # HybridCache (if [Cacheable])
  → Handler                  # Business logic
Response
```

---

## 👨‍💻 Developer Guide

### AI Developer Harness (Local Agent)

The project includes an **AI Developer Harness** (`scripts/ai_developer_harness.py`) — a local agentic workspace that lets an AI Agent safely write code, run compiler commands, execute test suites, and self-heal on failures. It also features a **GAN-Style Adversarial Evaluator** to automatically enforce architectural and code quality rules.

```bash
# Run harness locally in interactive mode
python scripts/ai_developer_harness.py "Your coding request here"

# Run harness with autonomous mode (auto-approve safe commands)
python scripts/ai_developer_harness.py "Your coding request here" --auto-approve
```

For more details, see the **[Harness Engineering Guide](./docs/guides/harness_guide.md)**.

### Adding a New Feature

```text
1. Domain     →  Create Entity in Domain/Entities/
2. Application →  Create Command/Query + Handler in Features/
3. Validation  →  Add FluentValidation Validator
4. Controller  →  Create API endpoint in Controllers/
5. Test        →  Add integration test in FloraCore.Tests/
```

### Outbox Pattern Usage

```csharp
// In your handler — both operations in the same DB transaction:
await _postRepository.StageAddAsync(post);
await _outboxRepository.StageAddAsync(new OutboxMessage
{
    Id = Guid.NewGuid(),
    Type = "Notification",
    Content = JsonSerializer.Serialize(notification),
    OccurredOnUtc = DateTime.UtcNow
});
await _unitOfWork.SaveChangesAsync(cancellationToken);
// → Hangfire picks up the outbox message within 1 minute
```

---

## 📄 License

This project is licensed under the **MIT License** — see the [LICENSE](LICENSE) file for details.

---

<div align="center">

**Built with ❤️ using .NET 9**

*Clean Architecture · CQRS · Domain-Driven Design*

</div>
