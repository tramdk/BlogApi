# 🚀 Blog API - Enterprise Grade .NET 8 Boilerplate

![Build Status](https://img.shields.io/github/actions/workflow/status/your-username/blog-api/.github/workflows/dotnet.yml?branch=main&style=flat-square)
![License](https://img.shields.io/badge/license-MIT-blue?style=flat-square)
![Platform](https://img.shields.io/badge/platform-.NET%208-purple?style=flat-square)
![Docker](https://img.shields.io/badge/docker-ready-blue?style=flat-square)
![Cloudinary](https://img.shields.io/badge/storage-Cloudinary-3448C5?style=flat-square&logo=cloudinary)

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
| **Infrastructure** | `BlogApi.Infrastructure` | External concerns: Database (EF/Dapper), Cloudinary, Jwt. | `Application` |
| **Presentation** | `BlogApi.Controllers` | API Endpoints, Filters, Middleware. | `Application` |

### 📁 Folder Structure
```text
src/BlogApi
├── Application/
│   ├── Common/             # Shared Interfaces, Behaviors (Logging, Validation), Exceptions
│   ├── Features/           # Vertical Slices (e.g. Posts, Auth) containing Commands/Queries
│   └── Constants/          # System-wide Constants (No Magic Strings)
├── Domain/
│   ├── Entities/           # Core Entities (Post, User, FileMetadata)
│   ├── ValueObjects/       # Immutable objects (Email, Money)
│   └── Exceptions/         # Domain-specific Errors
├── Infrastructure/
│   ├── Data/               # DbContext, Migrations, Seeding
│   ├── Repositories/       # Implementations of Repositories
│   └── Services/           # Implementations of Interfaces (CloudinaryFileService, JwtService)
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
- **SQL Server / PostgreSQL**: Multi-database support via configuration.
- **Entity Framework Core 8**: Code-First ORM for Command operations (Write).
- **Dapper**: Micro-ORM for high-performance Query operations (Read).
- **SQLite**: Used for isolated Integration Tests.

### ☁️ File Storage
- **Cloudinary**: Cloud-based file storage for all uploads. Files are stored and served directly from Cloudinary CDN, eliminating the need for server-side disk storage.
- Package: `CloudinaryDotNet` (official SDK).

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
- **File Ownership**: Only the file owner can delete their files; private files are accessible only to their owner.

### ☁️ Cloud File Storage
- **Cloudinary Integration**: Files are uploaded directly to Cloudinary and served via CDN URLs.
- **Zero Disk Usage**: No server-side `uploads/` folder required for file serving.
- **Graceful Deletion**: If a file is already missing on Cloudinary, the database metadata is still cleaned up automatically.
- **Secure CDN URLs**: `ViewUrl` and `DownloadUrl` in all file responses point directly to Cloudinary CDN.

### ⚡ Performance Optimized
- **CQRS**: Separates Reads (Dapper) from Writes (EF Core).
- **Pagination**: Efficient cursor-based or offset-based pagination.
- **CDN**: Cloudinary CDN handles file delivery globally with low latency.
- **Caching**: Redis distributed caching for rate limiting and session data.

### 📝 Developer Experience
- **No Manual Mapping**: AutoMapper handles boring mapping work.
- **Unified Result**: Standardized API responses (Success/Fail wrappers).
- **Development Seeding**: Auto-generates Admin user and sample data.
- **Test-Friendly**: `FakeFileService` mock allows running all tests without Cloudinary credentials.

---

## 🚀 Getting Started

### Prerequisites
- .NET 8 SDK
- SQL Server (LocalDB or Docker)
- A **Cloudinary** account ([cloudinary.com](https://cloudinary.com) — free tier is sufficient)

### 1️⃣ Clone & Restore
```bash
git clone https://github.com/your-username/blog-api.git
cd blog-api
dotnet restore
```

### 2️⃣ Configure Cloudinary
Get your credentials from the [Cloudinary Dashboard](https://cloudinary.com/console). Add them to your `.env` file:
```bash
CLOUDINARY_CLOUD_NAME=your_cloud_name
CLOUDINARY_API_KEY=your_api_key
CLOUDINARY_API_SECRET=your_api_secret
```

Or directly in `appsettings.json` (not recommended for production):
```json
"Cloudinary": {
  "CloudName": "your_cloud_name",
  "ApiKey": "your_api_key",
  "ApiSecret": "your_api_secret"
}
```

### 3️⃣ Database Setup
Ensure your connection string in `appsettings.json` is correct (`DefaultConnection`).
```bash
# Apply migrations and create database
dotnet ef database update
```

### 4️⃣ Run Application
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
| **ConnectionStrings** | `DefaultConnection` | Database connection string (SQL Server or PostgreSQL). |
| | `Redis` | Redis connection (optional, falls back to in-memory). |
| **Jwt** | `Secret` | **CRITICAL**: Must be 32+ chars. Used to sign tokens. |
| | `ExpiryMinutes` | Token lifetime (default 60). |
| **Cloudinary** | `CloudName` | Your Cloudinary cloud name. |
| | `ApiKey` | Your Cloudinary API key. |
| | `ApiSecret` | **CRITICAL**: Your Cloudinary API secret. Never commit to Git. |
| **IpRateLimiting** | `GeneralRules` | Set global or endpoint-specific limits. |

### File API Endpoints

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `POST` | `/api/files/upload` | ✅ Required | Upload a file to Cloudinary. Returns CDN URL. |
| `POST` | `/api/files/metadata` | ❌ Public | Get file metadata by `objectId`. |
| `GET` | `/api/files/download/{id}` | ❌ Public | Download file by ID (proxied from Cloudinary). |
| `GET` | `/api/files/view/{id}` | ❌ Public | View file inline (proxied from Cloudinary). |
| `GET` | `/api/files/view/object/{objectId}` | ❌ Public | View latest file for a given object. |
| `DELETE` | `/api/files/{id}` | ✅ Required | Delete file from Cloudinary and remove metadata from DB. |

---

## 👨‍💻 Developer Guide

### Adding a New Feature (Vertical Slice)

1.  **Define Domain**: Add Entity in `Domain/Entities`.
2.  **Define Contract**: Create DTOs in `Application/Features/[Feature]/DTOs`.
3.  **Implement Logic**:
    - Create `Command` or `Query` record (MediatR).
    - Implement `IRequestHandler`.
    - Add `Validator` (FluentValidation).
4.  **Expose API**: Create Endpoint in `Controllers`.
5.  **Map Data**: Update `MappingProfile.cs`.

---

## 🐳 Deployment (Docker)

We provide automated scripts to make local deployment on **Docker Desktop** seamless.

### 🔐 Environment Setup
Copy `.env.example` to `.env` and fill in your credentials:
```bash
cp .env.example .env
```

Required variables in `.env`:
```bash
# Database
DB_PASSWORD=your_secure_password

# JWT
JWT_SECRET=your_secret_key_at_least_32_chars

# Cloudinary (required for file uploads)
CLOUDINARY_CLOUD_NAME=your_cloud_name
CLOUDINARY_API_KEY=your_api_key
CLOUDINARY_API_SECRET=your_api_secret
```

These values are automatically injected into the Docker container via `docker-compose.yml`.

### 💨 One-Click Automatic Deployment
The `deploy-docker.ps1` script handles everything: stopping old containers, building, starting services, and opening the API docs once ready.

```powershell
# In PowerShell (Project Root)
.\deploy-docker.ps1
```

### 🔁 Auto-Sync Developer Mode (Watch)
Work in real-time. Use **Docker Compose Watch** to automatically rebuild and restart the API whenever you save a code change.

```powershell
# Start with watch mode
docker-compose up --watch
```

### 📦 Manual Build
```bash
docker build -t blog-api .
docker run -p 8080:8080 blog-api
```

---

## 🧪 Testing Strategy

We prioritize **Integration Tests** to ensure system reliability.

- **Framework**: xUnit + WebApplicationFactory.
- **Database**: SQLite In-Memory (Persistent Mode) — no real database needed.
- **File Storage**: `FakeFileService` is registered in the test environment, replacing `CloudinaryFileService`. **No Cloudinary API keys are required to run tests.**
- **Strategy**: Every test spins up a fresh scope but shares the in-memory connection for performance.

```bash
# Run all tests
dotnet test

# Run file-specific tests
dotnet test --filter "FilesControllerTests"
```

### Test Coverage for File API

| Test | What It Verifies |
|------|-----------------|
| `UploadAndDownloadFile_Works` | Upload succeeds and response includes Cloudinary CDN URLs. |
| `PrivateFile_CannotBeAccessedByOthers` | Private files are blocked for non-owners (403 Forbidden). |
| `PublicFile_CanBeAccessedByOthers` | Public files are accessible to all authenticated users. |
| `DeleteFile_Works` | File is removed from cloud storage and metadata cleaned from DB. |

---

## 🤝 Contribution

Contributions are welcome! Please follow the **Pull Request** workflow.
1. Fork repo.
2. Create branch `feature/your-feature`.
3. Commit and Push.
4. Open PR targeting `main`.

---

**Happy Coding!** 🚀
