# 🚀 Blog API - Enterprise Grade .NET 8 Boilerplate

![Build Status](https://img.shields.io/github/actions/workflow/status/your-username/blog-api/.github/workflows/dotnet.yml?branch=main&style=flat-square)
![License](https://img.shields.io/badge/license-MIT-blue?style=flat-square)
![Platform](https://img.shields.io/badge/platform-.NET%208-purple?style=flat-square)
![Docker](https://img.shields.io/badge/docker-ready-blue?style=flat-square)

> **A production-ready REST API** built with Clean Architecture, Domain-Driven Design (DDD), and CQRS principles. Designed for scalability, maintainability, and high performance.

---

## 📑 Table of Contents
1. [Architecture Overview](#-architecture-overview)
2. [Technology Stack](#-technology-stack)
3. [Key Features](#-key-features)
4. [Getting Started](#-getting-started)
5. [Configuration Guide](#-configuration-guide)
6. [Developer Guide](#-developer-guide)
7. [Deployment (Docker)](#-deployment)
8. [Testing Strategy](#-testing-strategy)

---

## 🏗 Architecture Overview

The solution follows **Clean Architecture** principles, enforcing a strict dependency rule: **Dependencies only point inwards.**

### 📦 Project Structure

| Project / Layer | Namespace | Responsibilities | Dependencies |
|----------------|-----------|------------------|--------------|
| **Domain** | `BlogApi.Domain` | Enterprise Logic, Entities, Value Objects. **Core of the system.** | *None* |
| **Application** | `BlogApi.Application` | Use Cases (MediatR Handlers), DTOs, Interfaces, Validators. | `Domain` |
| **Infrastructure** | `BlogApi.Infrastructure` | External concerns: Database (EF/Dapper), File System, Email, Jwt. | `Application` |
| **Presentation** | `BlogApi.Controllers` | API Endpoints, Filters, Middleware. | `Application` |

### � Folder Structure
```text
src/BlogApi
├── Application/
│   ├── Common/             # Shared Interfaces, Behaviors (Logging, Validation), Exceptions
│   ├── Features/           # Vertical Slices (e.g. Posts, Auth) containing Commands/Queries
│   └── Constants/          # System-wide Constants (No Magic Strings)
├── Domain/
│   ├── Entities/           # Core Entities (Post, User, Comment)
│   ├── ValueObjects/       # Immutable objects (Email, Money)
│   └── Exceptions/         # Domain-specific Errors
├── Infrastructure/
│   ├── Data/               # DbContext, Migrations, Seeding
│   ├── Repositories/       # Implementations of Repositories
│   └── Services/           # Implementations of Interfaces (EmailService, FileService)
└── Controllers/            # API Entry Points
```

---

## 🛠 Technology Stack

### Core
- **.NET 8** (LTS) providing high-performance runtime.
- **MediatR**: Implements Mediator pattern for decoupling features.
- **AutoMapper**: Handles object-to-object mapping.
- **FluentValidation**: Strong-typed validation rules.

### Data Access
- **SQL Server**: Primary relational database.
- **Entity Framework Core 8**: Code-First ORM for Command operations (Write).
- **Dapper**: Micro-ORM for high-performance Query operations (Read).
- **SQLite**: Used for isolated Integration Tests.

### Real-time & Security
- **SignalR**: WebSocket support for real-time features.
- **JWT (Reference Tokens)**: Secure authentication with support for centralized revocation (Blacklist).
- **AspNetCoreRateLimit**: IP-based rate limiting to prevent abuse.

### Documentation & logs
- **Scalar**: Next-gen API documentation UI.
- **Serilog**: Structured logging with file and console sinks.

---

## ✨ Key Features

### 🔐 Security First
- **Centralized Revocation**: Tokens can be blacklisted instantly (e.g., on logout or breach).
- **IP Rate Limiting**: Protects login endpoints (`limit: 5/minute`) and general API (`limit: 100/minute`).
- **Policy Authorization**: Granular permissions (e.g., "MustBeAuthor" policy).

### ⚡ Performance Optimized
- **CQRS**: Separates Reads (Dapper) from Writes (EF Core).
- **Pagination**: Efficient cursor-based or offset-based pagination.
- **Caching**: Preparation for Redis distributed caching.

### 📝 Developer Experience
- **No Manual Mapping**: AutoMapper handles boring mapping work.
- **Unified Result**: Standardized API responses (Success/Fail wrappers).
- **Development Seeding**: Auto-generates Admin user and sample data.

---

## 🚀 Getting Started

### Prerequisites
- .NET 8 SDK
- SQL Server (LocalDB or Docker)

### 1️⃣ Clone & Restore
```bash
git clone https://github.com/your-username/blog-api.git
cd blog-api
dotnet restore
```

### 2️⃣ Database Setup
Ensure your connection string in `appsettings.json` is correct (`DefaultConnection`).
```bash
# Apply migrations and create database
dotnet ef database update
```

### 3️⃣ Run Application
```bash
dotnet watch run --project BlogApi
```
- API URL: `https://localhost:7066`
- Documentation: `https://localhost:7066/scalar/v1`

### 🔑 Default Credentials
- **Admin**: `admin@blogapi.com` / `Admin123!`
- **User**: Register a new user via API.

---

## 🌍 Multi-Database Support

Switch between **SQL Server** and **PostgreSQL** easily via configuration.

| Provider | Description |
|----------|-------------|
| `SqlServer` | Default for Local Development. Uses `Microsoft.EntityFrameworkCore.SqlServer`. |
| `PostgreSQL` | Recommended for Docker/Render deployment. Uses `Npgsql.EntityFrameworkCore.PostgreSQL`. |

To switch, update `appsettings.json` or Environment Variables:
```json
"DatabaseProvider": "PostgreSQL"
```

---

## ⚙ Configuration Guide

The `appsettings.json` file controls the application behavior.

| Section | Key | Description |
|---------|-----|-------------|
| **ConnectionStrings** | `DefaultConnection` | SQL Server connection string. |
| | `Redis` | Redis connection (optional). |
| **Jwt** | `Secret` | **CRITICAL**: Must be 32+ chars. Used to sign tokens. |
| | `ExpiryMinutes` | Token lifetime (default 60). |
| **FileStorage** | `UploadFolder` | Directory name for uploaded files (default `uploads`). |
| **IpRateLimiting** | `GeneralRules` | Set global or endpoint-specific limits. |

---

## �‍💻 Developer Guide

### adding a New Feature (Vertical Slice)

1.  **Define Domain**: Add Entity in `Domain/Entities`.
2.  **Define Contract**: Create DTOs in `Application/Features/[Feature]/DTOs`.
3.  **Implement Logic**:
    - Create `Command` or `Query` record (MediatR).
    - Implement `IRequestHandler`.
    - Add `Validator` (FluentValidation).
4.  **Expose API**: Create Endpoint in `Controllers`.
5.  **Map Data**: Update `MappingProfile.cs`.

---

## 🐳 Deployment

### Docker Build
We use a **Multi-stage build** for optimized image size.

```bash
docker build -t blog-api .
docker run -p 8080:8080 -e "ConnectionStrings__DefaultConnection=..." blog-api
```

### Environment Variables
Override settings in Docker using environment variables (double underscore `__` for nesting):
- `ConnectionStrings__DefaultConnection`
- `Jwt__Secret`

---

## 🧪 Testing Strategy

We prioritize **Integration Tests** to ensure system reliability.

- **Framework**: xUnit + WebApplicationFactory.
- **Database**: SQLite In-Memory (Persistent Mode).
- **Strategy**: Every test spins up a fresh scope but shares the connection for performance.

```bash
# Run all tests
dotnet test

# Run specific suite
dotnet test --filter "FullyQualifiedName~FilesControllerTests"
```

---

## 🤝 Contribution

Contributions are welcome! Please follow the **Pull Request** workflow.
1. Fork repo.
2. Create branch `feature/your-feature`.
3. Commit and Push.
4. Open PR targeting `main`.

---

**Happy Coding!** 🚀
