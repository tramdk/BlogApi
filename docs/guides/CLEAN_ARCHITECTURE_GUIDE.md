# 📘 Clean Architecture - Hướng dẫn Chi tiết

**Tác giả**: Robert C. Martin (Uncle Bob)  
**Mục đích**: Xây dựng hệ thống phần mềm dễ bảo trì, mở rộng và kiểm thử

---

## 📑 Mục lục

1. [Giới thiệu Clean Architecture](#-giới-thiệu-clean-architecture)
2. [Các nguyên tắc cốt lõi](#-các-nguyên-tắc-cốt-lõi)
3. [Cấu trúc 4 Layer](#️-cấu-trúc-4-layer)
4. [Dependency Rule](#-dependency-rule---quy-tắc-quan-trọng-nhất)
5. [SOLID Principles](#-solid-principles)
6. [Patterns thường dùng](#-patterns-thường-dùng)
7. [Ví dụ thực tế](#-ví-dụ-thực-tế-từ-blogapi)
8. [Best Practices](#-best-practices)
9. [Common Mistakes](#-common-mistakes---lỗi-thường-gặp)
10. [Testing Strategy](#-testing-strategy)
11. [Migration Guide](#-migration-guide)

---

## 🎯 Giới thiệu Clean Architecture

### Clean Architecture là gì?

**Clean Architecture** là một kiến trúc phần mềm được thiết kế để:

1. **Độc lập với Framework**: Không bị ràng buộc bởi framework cụ thể
2. **Testable**: Dễ dàng viết và chạy tests
3. **Độc lập với UI**: UI có thể thay đổi mà không ảnh hưởng business logic
4. **Độc lập với Database**: Có thể swap database dễ dàng
5. **Độc lập với External Services**: Business logic không biết về external services

### Tại sao cần Clean Architecture?

#### ❌ Vấn đề với kiến trúc truyền thống:

```
┌─────────────────────────────────────┐
│           UI Layer                  │
│  (Controllers, Views)               │
│  ↓ directly calls                   │
│  Database, External APIs            │
└─────────────────────────────────────┘
```

**Hậu quả**:
- 🔴 Khó test (phải mock database, external services)
- 🔴 Khó thay đổi database
- 🔴 Business logic lẫn lộn với infrastructure code
- 🔴 Tight coupling giữa các components

#### ✅ Giải pháp với Clean Architecture:

```
┌─────────────────────────────────────┐
│         Presentation                │
│              ↓                      │
│         Application                 │
│         (Business Logic)            │
│              ↓                      │
│           Domain                    │
│         (Core Entities)             │
│              ↑                      │
│       Infrastructure                │
│    (Database, External APIs)        │
└─────────────────────────────────────┘
```

**Lợi ích**:
- ✅ Business logic độc lập, dễ test
- ✅ Dễ thay đổi implementation
- ✅ Loose coupling
- ✅ High cohesion

---

## 🎨 Các nguyên tắc cốt lõi

### 1. **Separation of Concerns** (Tách biệt trách nhiệm)

Mỗi layer có một trách nhiệm riêng biệt:

```
Domain       → Business entities và rules
Application  → Use cases và business logic
Infrastructure → Technical implementation
Presentation → User interface
```

### 2. **Dependency Inversion** (Đảo ngược phụ thuộc)

```csharp
// ❌ SAI - High-level module phụ thuộc vào low-level module
public class OrderService
{
    private SqlServerRepository _repository; // Phụ thuộc cụ thể
    
    public void CreateOrder(Order order)
    {
        _repository.Save(order); // Tied to SQL Server
    }
}

// ✅ ĐÚNG - Cả hai phụ thuộc vào abstraction
public interface IOrderRepository
{
    void Save(Order order);
}

public class OrderService
{
    private readonly IOrderRepository _repository; // Phụ thuộc abstraction
    
    public OrderService(IOrderRepository repository)
    {
        _repository = repository;
    }
    
    public void CreateOrder(Order order)
    {
        _repository.Save(order); // Không quan tâm implementation
    }
}

// Implementation có thể là bất kỳ:
public class SqlServerRepository : IOrderRepository { }
public class MongoDbRepository : IOrderRepository { }
public class InMemoryRepository : IOrderRepository { }
```

### 3. **Testability First**

Code phải được thiết kế để dễ test:

```csharp
// ✅ Dễ test vì sử dụng interfaces
[Fact]
public async Task CreateOrder_ShouldSaveOrder()
{
    // Arrange
    var mockRepo = new Mock<IOrderRepository>();
    var service = new OrderService(mockRepo.Object);
    var order = new Order { Id = Guid.NewGuid() };
    
    // Act
    await service.CreateOrder(order);
    
    // Assert
    mockRepo.Verify(r => r.Save(order), Times.Once);
}
```

---

## 🏗️ Cấu trúc 4 Layer

### Sơ đồ tổng quan:

```
┌─────────────────────────────────────────────────────────┐
│                    PRESENTATION LAYER                    │
│  Controllers, Views, API Endpoints, CLI, Web UI         │
│  - Nhận input từ user                                   │
│  - Gọi Application layer                                │
│  - Trả về response                                      │
└────────────────────┬────────────────────────────────────┘
                     │ depends on (calls)
                     ↓
┌─────────────────────────────────────────────────────────┐
│                   APPLICATION LAYER                      │
│  Use Cases, Commands, Queries, DTOs, Interfaces         │
│  - Business logic và workflows                          │
│  - Định nghĩa INTERFACES (contracts)                    │
│  - Orchestrate domain objects                           │
└────────────────────┬────────────────────────────────────┘
                     │ depends on (uses)
                     ↓
┌─────────────────────────────────────────────────────────┐
│                     DOMAIN LAYER                         │
│  Entities, Value Objects, Domain Events, Enums          │
│  - Core business entities                               │
│  - Business rules và validations                        │
│  - KHÔNG phụ thuộc vào bất kỳ layer nào                │
└─────────────────────────────────────────────────────────┘
                     ↑
                     │ implements (provides)
┌────────────────────┴────────────────────────────────────┐
│                 INFRASTRUCTURE LAYER                     │
│  Repositories, DbContext, External Services, Hubs       │
│  - IMPLEMENT interfaces từ Application                  │
│  - Database access                                      │
│  - External API calls                                   │
│  - File system, Email, SMS, etc.                        │
└─────────────────────────────────────────────────────────┘
```

---

### Layer 1: Domain Layer (Innermost - Core)

**Trách nhiệm**: Chứa business entities và business rules cốt lõi

**Đặc điểm**:
- ✅ **KHÔNG** phụ thuộc vào bất kỳ layer nào khác
- ✅ **KHÔNG** có references đến frameworks
- ✅ Chỉ chứa pure C# code
- ✅ Chứa business logic thuần túy

**Nội dung**:
```
Domain/
├── Entities/           # Business entities
│   ├── Post.cs
│   ├── Product.cs
│   └── User.cs
├── ValueObjects/       # Immutable objects
│   ├── Money.cs
│   └── Address.cs
├── Enums/             # Business enums
│   └── OrderStatus.cs
└── Exceptions/        # Domain exceptions
    └── InvalidOrderException.cs
```

**Ví dụ - Entity**:
```csharp
namespace BlogApi.Domain.Entities;

public class Post
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public Guid AuthorId { get; set; }
    public AppUser Author { get; set; } = null!;
    
    // Business logic trong Domain
    public double AverageRating { get; private set; }
    public int TotalRatings { get; private set; }
    
    public void AddRating(int score)
    {
        // Business rule: Rating phải từ 1-5
        if (score < 1 || score > 5) 
            throw new ArgumentOutOfRangeException(nameof(score), 
                "Rating must be between 1 and 5");
        
        // Business logic: Tính average rating
        double currentTotalScore = AverageRating * TotalRatings;
        TotalRatings++;
        AverageRating = (currentTotalScore + score) / TotalRatings;
    }
}
```

**Ví dụ - Value Object**:
```csharp
namespace BlogApi.Domain.ValueObjects;

public record Money
{
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";
    
    // Business rule: Amount không được âm
    public Money(decimal amount, string currency = "USD")
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative");
            
        Amount = amount;
        Currency = currency;
    }
    
    // Business logic: Cộng tiền
    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot add different currencies");
            
        return new Money(Amount + other.Amount, Currency);
    }
}
```

---

### Layer 2: Application Layer

**Trách nhiệm**: Chứa business logic và use cases

**Đặc điểm**:
- ✅ Phụ thuộc vào **Domain** layer
- ✅ **ĐỊNH NGHĨA** interfaces (contracts)
- ✅ **KHÔNG** phụ thuộc vào Infrastructure
- ✅ Chứa Commands, Queries, DTOs

**Nội dung**:
```
Application/
├── Common/
│   ├── Interfaces/          # ✅ Interfaces được định nghĩa ở đây
│   │   ├── IGenericRepository.cs
│   │   ├── IPostRepository.cs
│   │   ├── IChatService.cs
│   │   └── IEmailService.cs
│   ├── Behaviors/           # MediatR pipeline behaviors
│   │   ├── ValidationBehavior.cs
│   │   ├── LoggingBehavior.cs
│   │   └── AuthorizationBehavior.cs
│   ├── Models/              # Shared models
│   │   ├── PaginatedList.cs
│   │   └── Result.cs
│   └── Services/            # Interface definitions only
│       ├── IJwtService.cs
│       └── ICurrentUserService.cs
└── Features/                # Vertical Slices
    ├── Posts/
    │   ├── Commands/
    │   │   ├── CreatePostCommand.cs
    │   │   ├── UpdatePostCommand.cs
    │   │   └── DeletePostCommand.cs
    │   ├── Queries/
    │   │   ├── GetPostsQuery.cs
    │   │   └── GetPostByIdQuery.cs
    │   └── DTOs/
    │       ├── PostDto.cs
    │       └── PostDetailDto.cs
    └── Products/
        └── ... (tương tự)
```

**Ví dụ - Interface Definition**:
```csharp
// ✅ Application/Common/Interfaces/IPostRepository.cs
namespace BlogApi.Application.Common.Interfaces;

public interface IPostRepository : IGenericRepository<Post, Guid>
{
    Task<Post?> GetByIdWithAuthorAsync(Guid id);
    Task<IEnumerable<Post>> GetPostsByAuthorAsync(Guid authorId);
}
```

**Ví dụ - Command (CQRS)**:
```csharp
// ✅ Application/Features/Posts/Commands/CreatePostCommand.cs
using BlogApi.Application.Common.Interfaces; // ✅ ĐÚNG
using BlogApi.Domain.Entities;
using MediatR;

namespace BlogApi.Application.Features.Posts.Commands;

public record CreatePostCommand(string Title, string Content) : IRequest<Guid>;

public class CreatePostHandler : IRequestHandler<CreatePostCommand, Guid>
{
    private readonly IPostRepository _repository;      // ✅ Interface từ Application
    private readonly ICurrentUserService _userService; // ✅ Interface từ Application
    
    public CreatePostHandler(
        IPostRepository repository, 
        ICurrentUserService userService)
    {
        _repository = repository;
        _userService = userService;
    }
    
    public async Task<Guid> Handle(CreatePostCommand request, CancellationToken ct)
    {
        var userId = _userService.UserId 
            ?? throw new UnauthorizedAccessException();
        
        var post = new Post 
        { 
            Id = Guid.NewGuid(),
            Title = request.Title, 
            Content = request.Content,
            AuthorId = userId
        };
        
        await _repository.AddAsync(post);
        return post.Id;
    }
}
```

**Ví dụ - Query (CQRS)**:
```csharp
// ✅ Application/Features/Posts/Queries/GetPostsQuery.cs
using BlogApi.Application.Common.Interfaces;
using BlogApi.Application.Features.Posts.DTOs;
using MediatR;

namespace BlogApi.Application.Features.Posts.Queries;

public record GetPostsQuery(int PageSize = 10) : IRequest<List<PostDto>>;

public class GetPostsHandler : IRequestHandler<GetPostsQuery, List<PostDto>>
{
    private readonly IPostQueryService _queryService; // ✅ Interface
    
    public GetPostsHandler(IPostQueryService queryService)
    {
        _queryService = queryService;
    }
    
    public async Task<List<PostDto>> Handle(GetPostsQuery request, CancellationToken ct)
    {
        return await _queryService.GetPostsAsync(request.PageSize);
    }
}
```

**Ví dụ - DTO**:
```csharp
// ✅ Application/Features/Posts/DTOs/PostDto.cs
namespace BlogApi.Application.Features.Posts.DTOs;

public record PostDto(
    Guid Id,
    string Title,
    string AuthorName,
    double AverageRating,
    DateTime CreatedAt
);
```

---

### Layer 3: Infrastructure Layer

**Trách nhiệm**: Implement các interfaces từ Application layer

**Đặc điểm**:
- ✅ Phụ thuộc vào **Application** và **Domain**
- ✅ **IMPLEMENT** interfaces từ Application
- ✅ Chứa code tương tác với external systems
- ✅ Database, APIs, File system, etc.

**Nội dung**:
```
Infrastructure/
├── Data/
│   └── AppDbContext.cs      # EF Core DbContext
├── Repositories/            # ✅ Implementations
│   ├── GenericRepository.cs
│   ├── PostRepository.cs
│   └── PostQueryService.cs
├── Services/                # ✅ Implementations
│   ├── JwtService.cs
│   ├── EmailService.cs
│   ├── ChatService.cs
│   └── NotificationService.cs
└── Hubs/                    # SignalR Hubs
    ├── ChatHub.cs
    └── NotificationHub.cs
```

**Ví dụ - Repository Implementation**:
```csharp
// ✅ Infrastructure/Repositories/PostRepository.cs
using BlogApi.Application.Common.Interfaces; // ✅ Import interface từ Application
using BlogApi.Domain.Entities;
using BlogApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BlogApi.Infrastructure.Repositories;

public class PostRepository : GenericRepository<Post, Guid>, IPostRepository
{
    public PostRepository(AppDbContext context) : base(context) { }
    
    public async Task<Post?> GetByIdWithAuthorAsync(Guid id)
    {
        return await _dbSet
            .Include(p => p.Author)
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id);
    }
    
    public async Task<IEnumerable<Post>> GetPostsByAuthorAsync(Guid authorId)
    {
        return await _dbSet
            .Where(p => p.AuthorId == authorId)
            .Include(p => p.Author)
            .ToListAsync();
    }
}
```

**Ví dụ - Service Implementation**:
```csharp
// ✅ Infrastructure/Services/EmailService.cs
using BlogApi.Application.Common.Services; // ✅ Import interface từ Application

namespace BlogApi.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    
    public EmailService(IConfiguration config)
    {
        _config = config;
    }
    
    public async Task SendEmailAsync(string to, string subject, string body)
    {
        // Implementation sử dụng SMTP, SendGrid, etc.
        // Infrastructure concern - không liên quan đến business logic
    }
}
```

**Ví dụ - DbContext**:
```csharp
// ✅ Infrastructure/Data/AppDbContext.cs
using BlogApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BlogApi.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Product> Products => Set<Product>();
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Configure relationships, indexes, etc.
        builder.Entity<Post>()
            .HasOne(p => p.Author)
            .WithMany(u => u.Posts)
            .HasForeignKey(p => p.AuthorId);
    }
}
```

---

### Layer 4: Presentation Layer

**Trách nhiệm**: Nhận input, gọi Application layer, trả về response

**Đặc điểm**:
- ✅ Phụ thuộc vào **Application** layer
- ✅ Không chứa business logic
- ✅ Chỉ orchestrate và format response

**Nội dung**:
```
Controllers/
├── PostsController.cs
├── ProductsController.cs
└── AuthController.cs

Program.cs              # DI configuration
Middleware/
├── ExceptionHandlingMiddleware.cs
└── AuthenticationMiddleware.cs
```

**Ví dụ - Controller**:
```csharp
// ✅ Controllers/PostsController.cs
using BlogApi.Application.Features.Posts.Commands;
using BlogApi.Application.Features.Posts.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BlogApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostsController : ControllerBase
{
    private readonly IMediator _mediator;
    
    public PostsController(IMediator mediator) => _mediator = mediator;
    
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int pageSize = 10)
    {
        var query = new GetPostsQuery(pageSize);
        var result = await _mediator.Send(query);
        return Ok(result);
    }
    
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreatePostCommand command)
    {
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var query = new GetPostByIdQuery(id);
        var result = await _mediator.Send(query);
        return result != null ? Ok(result) : NotFound();
    }
}
```

**Ví dụ - DI Configuration**:
```csharp
// ✅ Program.cs
using BlogApi.Application.Common.Interfaces;
using BlogApi.Application.Common.Services;
using BlogApi.Infrastructure.Repositories;
using BlogApi.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Application Services (interfaces only)
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Infrastructure Services (implementations)
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IChatService, ChatService>();

// Repositories (implementations)
builder.Services.AddScoped(typeof(IGenericRepository<,>), typeof(GenericRepository<,>));
builder.Services.AddScoped<IPostRepository, PostRepository>();

// MediatR
builder.Services.AddMediatR(cfg => 
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

var app = builder.Build();
app.Run();
```

---

## 🔒 Dependency Rule - Quy tắc quan trọng nhất

### The Rule:

> **Source code dependencies must point only INWARD, toward higher-level policies.**

### Sơ đồ Dependencies:

```
┌─────────────────────────────────────────┐
│         Presentation Layer              │
│         (Controllers)                   │
└──────────────────┬──────────────────────┘
                   │
                   ↓ depends on
                   
┌──────────────────────────────────────────┐
│         Application Layer                │
│  - Defines INTERFACES                    │
│  - Commands, Queries                     │
└──────────────────┬──────────────────────┘
                   │
                   ↓ depends on
                   
┌──────────────────────────────────────────┐
│         Domain Layer                     │
│  - Entities                              │
│  - Business Rules                        │
└──────────────────────────────────────────┘
                   ↑
                   │
                   │ implements
                   
┌──────────────────┴──────────────────────┐
│         Infrastructure Layer            │
│  - Implements INTERFACES                │
│  - Database, External Services          │
└─────────────────────────────────────────┘
```

### ✅ ĐÚNG - Dependencies hợp lệ:

```csharp
// ✅ Presentation → Application
using BlogApi.Application.Features.Posts.Commands;

// ✅ Application → Domain
using BlogApi.Domain.Entities;

// ✅ Infrastructure → Application (implements interfaces)
using BlogApi.Application.Common.Interfaces;

// ✅ Infrastructure → Domain
using BlogApi.Domain.Entities;
```

### ❌ SAI - Dependencies vi phạm:

```csharp
// ❌ Application → Infrastructure (VI PHẠM!)
using BlogApi.Infrastructure.Repositories;

// ❌ Domain → Application (VI PHẠM!)
using BlogApi.Application.Common.Interfaces;

// ❌ Domain → Infrastructure (VI PHẠM!)
using BlogApi.Infrastructure.Data;
```

### Tại sao quan trọng?

**Khi tuân thủ Dependency Rule**:
- ✅ Business logic (Application, Domain) không bị ảnh hưởng khi thay đổi Infrastructure
- ✅ Có thể test Application layer mà không cần Database
- ✅ Có thể swap implementations dễ dàng

**Khi vi phạm Dependency Rule**:
- 🔴 Thay đổi Database → phải sửa Application layer
- 🔴 Không thể test mà không có Infrastructure
- 🔴 Tight coupling → khó bảo trì

---

## 💎 SOLID Principles

Clean Architecture được xây dựng dựa trên SOLID principles:

### S - Single Responsibility Principle

**Định nghĩa**: Một class chỉ nên có một lý do để thay đổi

```csharp
// ❌ SAI - Nhiều responsibilities
public class UserService
{
    public void CreateUser(User user) { }
    public void SendWelcomeEmail(User user) { }  // Email concern
    public void LogUserCreation(User user) { }   // Logging concern
}

// ✅ ĐÚNG - Tách biệt responsibilities
public class UserService
{
    private readonly IEmailService _emailService;
    private readonly ILogger _logger;
    
    public async Task CreateUser(User user)
    {
        // Only user creation logic
        await _repository.AddAsync(user);
        
        // Delegate to other services
        await _emailService.SendWelcomeEmail(user);
        _logger.LogInformation("User created: {UserId}", user.Id);
    }
}
```

### O - Open/Closed Principle

**Định nghĩa**: Open for extension, closed for modification

```csharp
// ❌ SAI - Phải sửa code khi thêm payment method mới
public class PaymentProcessor
{
    public void ProcessPayment(string type, decimal amount)
    {
        if (type == "CreditCard")
        {
            // Process credit card
        }
        else if (type == "PayPal")
        {
            // Process PayPal
        }
        // Phải sửa code khi thêm method mới
    }
}

// ✅ ĐÚNG - Extend bằng cách thêm implementation mới
public interface IPaymentMethod
{
    Task ProcessAsync(decimal amount);
}

public class CreditCardPayment : IPaymentMethod
{
    public async Task ProcessAsync(decimal amount) { }
}

public class PayPalPayment : IPaymentMethod
{
    public async Task ProcessAsync(decimal amount) { }
}

public class PaymentProcessor
{
    public async Task ProcessPayment(IPaymentMethod method, decimal amount)
    {
        await method.ProcessAsync(amount);
        // Không cần sửa code khi thêm payment method mới
    }
}
```

### L - Liskov Substitution Principle

**Định nghĩa**: Subclass phải có thể thay thế base class mà không làm hỏng chương trình

```csharp
// ✅ ĐÚNG - Mọi implementation đều tuân thủ contract
public interface IRepository<T>
{
    Task<T?> GetByIdAsync(Guid id);
}

public class SqlRepository<T> : IRepository<T>
{
    public async Task<T?> GetByIdAsync(Guid id)
    {
        // SQL implementation
    }
}

public class MongoRepository<T> : IRepository<T>
{
    public async Task<T?> GetByIdAsync(Guid id)
    {
        // MongoDB implementation
    }
}

// Có thể swap implementations
IRepository<Post> repo = new SqlRepository<Post>();
repo = new MongoRepository<Post>(); // ✅ Works perfectly
```

### I - Interface Segregation Principle

**Định nghĩa**: Không nên ép client implement interfaces mà nó không dùng

```csharp
// ❌ SAI - Fat interface
public interface IRepository
{
    Task Add(object entity);
    Task Update(object entity);
    Task Delete(object entity);
    Task<object> GetById(Guid id);
    Task<List<object>> Search(string query);
    Task<byte[]> ExportToCsv();  // Không phải mọi repo đều cần
    Task ImportFromCsv(byte[] data); // Không phải mọi repo đều cần
}

// ✅ ĐÚNG - Segregated interfaces
public interface IRepository<T>
{
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
    Task<T?> GetByIdAsync(Guid id);
}

public interface ISearchableRepository<T> : IRepository<T>
{
    Task<List<T>> SearchAsync(string query);
}

public interface ICsvExportable
{
    Task<byte[]> ExportToCsvAsync();
    Task ImportFromCsvAsync(byte[] data);
}
```

### D - Dependency Inversion Principle

**Định nghĩa**: High-level modules không nên phụ thuộc vào low-level modules. Cả hai nên phụ thuộc vào abstractions.

```csharp
// ❌ SAI - High-level phụ thuộc vào low-level
public class OrderService
{
    private SqlServerRepository _repository; // Concrete class
    
    public void CreateOrder(Order order)
    {
        _repository.Save(order);
    }
}

// ✅ ĐÚNG - Cả hai phụ thuộc vào abstraction
// High-level module
public class OrderService
{
    private readonly IOrderRepository _repository; // Abstraction
    
    public OrderService(IOrderRepository repository)
    {
        _repository = repository;
    }
    
    public async Task CreateOrder(Order order)
    {
        await _repository.SaveAsync(order);
    }
}

// Low-level module
public class SqlServerRepository : IOrderRepository
{
    public async Task SaveAsync(Order order) { }
}
```

---

## 🎭 Patterns thường dùng

### 1. Repository Pattern

**Mục đích**: Abstract data access logic

```csharp
// Interface trong Application
public interface IGenericRepository<TEntity, TKey> where TEntity : class
{
    Task<TEntity?> GetByIdAsync(TKey id);
    Task<IEnumerable<TEntity>> GetAllAsync();
    Task AddAsync(TEntity entity);
    Task UpdateAsync(TEntity entity);
    Task DeleteAsync(TEntity entity);
}

// Implementation trong Infrastructure
public class GenericRepository<TEntity, TKey> : IGenericRepository<TEntity, TKey>
    where TEntity : class
{
    protected readonly DbContext _context;
    protected readonly DbSet<TEntity> _dbSet;
    
    public GenericRepository(DbContext context)
    {
        _context = context;
        _dbSet = context.Set<TEntity>();
    }
    
    public virtual async Task<TEntity?> GetByIdAsync(TKey id)
    {
        return await _dbSet.FindAsync(id);
    }
    
    // ... other implementations
}
```

### 2. CQRS (Command Query Responsibility Segregation)

**Mục đích**: Tách biệt operations đọc và ghi

```csharp
// COMMAND - Ghi dữ liệu
public record CreatePostCommand(string Title, string Content) : IRequest<Guid>;

public class CreatePostHandler : IRequestHandler<CreatePostCommand, Guid>
{
    private readonly IPostRepository _repository;
    
    public async Task<Guid> Handle(CreatePostCommand request, CancellationToken ct)
    {
        var post = new Post { Title = request.Title, Content = request.Content };
        await _repository.AddAsync(post);
        return post.Id;
    }
}

// QUERY - Đọc dữ liệu
public record GetPostsQuery(int PageSize) : IRequest<List<PostDto>>;

public class GetPostsHandler : IRequestHandler<GetPostsQuery, List<PostDto>>
{
    private readonly IPostQueryService _queryService;
    
    public async Task<List<PostDto>> Handle(GetPostsQuery request, CancellationToken ct)
    {
        return await _queryService.GetPostsAsync(request.PageSize);
    }
}
```

**Lợi ích**:
- ✅ Tối ưu riêng cho read và write
- ✅ Scale độc lập
- ✅ Rõ ràng về intent

### 3. Mediator Pattern (với MediatR)

**Mục đích**: Giảm coupling giữa objects

```csharp
// Controller không biết về Handler
[ApiController]
[Route("api/[controller]")]
public class PostsController : ControllerBase
{
    private readonly IMediator _mediator;
    
    public PostsController(IMediator mediator) => _mediator = mediator;
    
    [HttpPost]
    public async Task<IActionResult> Create(CreatePostCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(id);
    }
}
```

### 4. Unit of Work Pattern

**Mục đích**: Quản lý transactions

```csharp
public interface IUnitOfWork : IDisposable
{
    IPostRepository Posts { get; }
    IProductRepository Products { get; }
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitAsync();
    Task RollbackAsync();
}

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    
    public IPostRepository Posts { get; }
    public IProductRepository Products { get; }
    
    public UnitOfWork(AppDbContext context)
    {
        _context = context;
        Posts = new PostRepository(context);
        Products = new ProductRepository(context);
    }
    
    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}
```

### 5. Specification Pattern

**Mục đích**: Encapsulate business rules cho queries

```csharp
public interface ISpecification<T>
{
    Expression<Func<T, bool>> Criteria { get; }
    List<Expression<Func<T, object>>> Includes { get; }
}

public class PostsByAuthorSpec : ISpecification<Post>
{
    public PostsByAuthorSpec(Guid authorId)
    {
        Criteria = p => p.AuthorId == authorId;
        Includes = new List<Expression<Func<Post, object>>>
        {
            p => p.Author,
            p => p.Category
        };
    }
    
    public Expression<Func<Post, bool>> Criteria { get; }
    public List<Expression<Func<Post, object>>> Includes { get; }
}

// Usage
var spec = new PostsByAuthorSpec(userId);
var posts = await _repository.FindAsync(spec);
```

---

## 📝 Ví dụ thực tế từ BlogApi

### Ví dụ 1: Tạo Post mới

**Flow**:
```
User Request
    ↓
Controller (Presentation)
    ↓
CreatePostCommand (Application)
    ↓
CreatePostHandler (Application)
    ↓
IPostRepository (Application - Interface)
    ↓
PostRepository (Infrastructure - Implementation)
    ↓
AppDbContext (Infrastructure)
    ↓
Database
```

**Code**:

```csharp
// 1. Controller (Presentation)
[HttpPost]
public async Task<IActionResult> Create(CreatePostCommand command)
{
    var id = await _mediator.Send(command);
    return Ok(id);
}

// 2. Command (Application)
public record CreatePostCommand(string Title, string Content, string? CategoryId) 
    : IRequest<Guid>;

// 3. Handler (Application)
public class CreatePostHandler : IRequestHandler<CreatePostCommand, Guid>
{
    private readonly IPostRepository _repository;
    private readonly ICurrentUserService _userService;
    
    public async Task<Guid> Handle(CreatePostCommand request, CancellationToken ct)
    {
        var post = new Post 
        { 
            Id = Uuid.NewDatabaseFriendly(Database.SqlServer),
            Title = request.Title,
            Content = request.Content,
            AuthorId = _userService.UserId!.Value,
            CategoryId = request.CategoryId
        };
        
        await _repository.AddAsync(post);
        return post.Id;
    }
}

// 4. Repository Implementation (Infrastructure)
public class PostRepository : GenericRepository<Post, Guid>, IPostRepository
{
    public PostRepository(AppDbContext context) : base(context) { }
    
    // Inherited AddAsync from GenericRepository
}
```

### Ví dụ 2: Gửi Chat Message với SignalR

**Vấn đề**: Application layer cần gửi real-time message nhưng không được phụ thuộc vào SignalR (Infrastructure)

**Giải pháp**: Tạo abstraction

```csharp
// 1. Interface (Application)
public interface IChatService
{
    Task SendMessageToUserAsync(Guid receiverId, Guid senderId, 
        string message, DateTime sentAt);
}

// 2. Command Handler (Application)
public class SendMessageHandler : IRequestHandler<SendMessageCommand, Guid>
{
    private readonly IGenericRepository<ChatMessage, Guid> _repository;
    private readonly IChatService _chatService; // ✅ Abstraction
    
    public async Task<Guid> Handle(SendMessageCommand request, CancellationToken ct)
    {
        var message = new ChatMessage { /* ... */ };
        await _repository.AddAsync(message);
        
        // ✅ Không biết về SignalR
        await _chatService.SendMessageToUserAsync(
            request.ReceiverId, senderId, request.Message, message.SentAt);
        
        return message.Id;
    }
}

// 3. Implementation (Infrastructure)
public class ChatService : IChatService
{
    private readonly IHubContext<ChatHub, IChatClient> _hubContext;
    
    public async Task SendMessageToUserAsync(
        Guid receiverId, Guid senderId, string message, DateTime sentAt)
    {
        await _hubContext.Clients
            .User(receiverId.ToString())
            .ReceiveMessage(senderId, message, sentAt);
    }
}
```

---

## ✅ Best Practices

### 1. Đặt tên rõ ràng

```csharp
// ❌ SAI
public class Manager { }
public class Helper { }
public class Util { }

// ✅ ĐÚNG
public class OrderService { }
public class EmailNotificationService { }
public class StringExtensions { }
```

### 2. Một file một class/interface

```
// ✅ ĐÚNG
IPostRepository.cs
PostRepository.cs
IEmailService.cs
EmailService.cs

// ❌ SAI
Repositories.cs (chứa nhiều classes)
```

### 3. Sử dụng Records cho DTOs

```csharp
// ✅ ĐÚNG - Immutable, concise
public record PostDto(
    Guid Id,
    string Title,
    string AuthorName,
    DateTime CreatedAt
);

// ❌ SAI - Mutable, verbose
public class PostDto
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string AuthorName { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### 4. Async/Await everywhere

```csharp
// ✅ ĐÚNG
public async Task<Post?> GetByIdAsync(Guid id)
{
    return await _dbSet.FindAsync(id);
}

// ❌ SAI
public Post? GetById(Guid id)
{
    return _dbSet.Find(id); // Blocking
}
```

### 5. Validation với FluentValidation

```csharp
public class CreatePostCommandValidator : AbstractValidator<CreatePostCommand>
{
    public CreatePostCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title too long");
            
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required")
            .MinimumLength(10).WithMessage("Content too short");
    }
}
```

### 6. Pipeline Behaviors

```csharp
// Logging Behavior
public class LoggingBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    
    public async Task<TResponse> Handle(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken ct)
    {
        _logger.LogInformation("Handling {RequestName}", typeof(TRequest).Name);
        var response = await next();
        _logger.LogInformation("Handled {RequestName}", typeof(TRequest).Name);
        return response;
    }
}
```

---

## ❌ Common Mistakes - Lỗi thường gặp

### 1. Application phụ thuộc vào Infrastructure

```csharp
// ❌ SAI
using BlogApi.Infrastructure.Repositories;

public class CreatePostHandler
{
    private readonly PostRepository _repository; // Concrete class
}

// ✅ ĐÚNG
using BlogApi.Application.Common.Interfaces;

public class CreatePostHandler
{
    private readonly IPostRepository _repository; // Interface
}
```

### 2. Interfaces trong Infrastructure

```csharp
// ❌ SAI
Infrastructure/
└── Repositories/
    ├── IPostRepository.cs      ❌
    └── PostRepository.cs

// ✅ ĐÚNG
Application/
└── Common/
    └── Interfaces/
        └── IPostRepository.cs  ✅

Infrastructure/
└── Repositories/
    └── PostRepository.cs       ✅
```

### 3. Business Logic trong Controllers

```csharp
// ❌ SAI
[HttpPost]
public async Task<IActionResult> Create(CreatePostDto dto)
{
    // Business logic trong controller
    if (string.IsNullOrEmpty(dto.Title))
        return BadRequest("Title required");
        
    var post = new Post { Title = dto.Title };
    await _repository.AddAsync(post);
    return Ok();
}

// ✅ ĐÚNG
[HttpPost]
public async Task<IActionResult> Create(CreatePostCommand command)
{
    // Delegate to Application layer
    var id = await _mediator.Send(command);
    return Ok(id);
}
```

### 4. Domain Entities có dependencies

```csharp
// ❌ SAI
public class Post
{
    private readonly IEmailService _emailService;
    
    public void Publish()
    {
        _emailService.SendEmail(); // Domain phụ thuộc Infrastructure
    }
}

// ✅ ĐÚNG
public class Post
{
    public void Publish()
    {
        // Chỉ business logic
        IsPublished = true;
        PublishedAt = DateTime.UtcNow;
    }
}

// Email được gửi trong Application layer
public class PublishPostHandler
{
    private readonly IEmailService _emailService;
    
    public async Task Handle(PublishPostCommand request)
    {
        post.Publish();
        await _repository.UpdateAsync(post);
        await _emailService.SendPublishedNotification(post);
    }
}
```

### 5. Không sử dụng Dependency Injection

```csharp
// ❌ SAI
public class OrderService
{
    private readonly OrderRepository _repository = new OrderRepository();
}

// ✅ ĐÚNG
public class OrderService
{
    private readonly IOrderRepository _repository;
    
    public OrderService(IOrderRepository repository)
    {
        _repository = repository;
    }
}
```

---

## 🧪 Testing Strategy

### 1. Unit Tests (Application Layer)

```csharp
public class CreatePostHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCreatePost()
    {
        // Arrange
        var mockRepo = new Mock<IPostRepository>();
        var mockUserService = new Mock<ICurrentUserService>();
        mockUserService.Setup(x => x.UserId).Returns(Guid.NewGuid());
        
        var handler = new CreatePostHandler(mockRepo.Object, mockUserService.Object);
        var command = new CreatePostCommand("Title", "Content");
        
        // Act
        var result = await handler.Handle(command, CancellationToken.None);
        
        // Assert
        Assert.NotEqual(Guid.Empty, result);
        mockRepo.Verify(x => x.AddAsync(It.IsAny<Post>()), Times.Once);
    }
}
```

### 2. Integration Tests

```csharp
public class PostsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    
    public PostsControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }
    
    [Fact]
    public async Task CreatePost_ShouldReturn201()
    {
        // Arrange
        var command = new { Title = "Test", Content = "Content" };
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/posts", command);
        
        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
```

### 3. Domain Tests

```csharp
public class PostTests
{
    [Fact]
    public void AddRating_ShouldCalculateAverage()
    {
        // Arrange
        var post = new Post();
        
        // Act
        post.AddRating(5);
        post.AddRating(3);
        
        // Assert
        Assert.Equal(4.0, post.AverageRating);
        Assert.Equal(2, post.TotalRatings);
    }
    
    [Fact]
    public void AddRating_InvalidScore_ShouldThrow()
    {
        // Arrange
        var post = new Post();
        
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => post.AddRating(6));
    }
}
```

---

## 🔄 Migration Guide

### Từ Layered Architecture sang Clean Architecture

#### Bước 1: Tạo cấu trúc folders

```bash
mkdir -p Domain/Entities
mkdir -p Application/Common/Interfaces
mkdir -p Application/Features
mkdir -p Infrastructure/Repositories
mkdir -p Infrastructure/Services
```

#### Bước 2: Di chuyển Entities

```bash
# Di chuyển từ Models/ sang Domain/Entities/
mv Models/*.cs Domain/Entities/
```

#### Bước 3: Tạo Interfaces

```csharp
// Tạo Application/Common/Interfaces/IRepository.cs
public interface IRepository<T>
{
    Task<T?> GetByIdAsync(Guid id);
    // ...
}
```

#### Bước 4: Di chuyển Repositories

```bash
# Di chuyển implementations sang Infrastructure
mv Repositories/*.cs Infrastructure/Repositories/
```

#### Bước 5: Cập nhật namespaces

```csharp
// Cũ
using MyApp.Models;
using MyApp.Repositories;

// Mới
using MyApp.Domain.Entities;
using MyApp.Application.Common.Interfaces;
```

#### Bước 6: Tạo Commands/Queries

```csharp
// Tách logic từ Controllers sang Application
public record CreatePostCommand(string Title, string Content) : IRequest<Guid>;

public class CreatePostHandler : IRequestHandler<CreatePostCommand, Guid>
{
    // Handler logic
}
```

#### Bước 7: Cập nhật DI

```csharp
// Program.cs
builder.Services.AddScoped<IPostRepository, PostRepository>();
```

---

## 📚 Tài liệu tham khảo

### Sách
1. **Clean Architecture** - Robert C. Martin
2. **Domain-Driven Design** - Eric Evans
3. **Implementing Domain-Driven Design** - Vaughn Vernon

### Articles
1. [The Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
2. [SOLID Principles](https://www.digitalocean.com/community/conceptual_articles/s-o-l-i-d-the-first-five-principles-of-object-oriented-design)

### Videos
1. [Clean Architecture with ASP.NET Core](https://www.youtube.com/watch?v=dK4Yb6-LxAk)
2. [CQRS and MediatR](https://www.youtube.com/watch?v=YzOBrVlthMk)

---

## 🎓 Tổng kết

### Checklist Clean Architecture

- [ ] Domain layer không có dependencies
- [ ] Application layer định nghĩa interfaces
- [ ] Infrastructure implements interfaces
- [ ] Presentation chỉ orchestrate
- [ ] Tuân thủ Dependency Rule
- [ ] Sử dụng CQRS pattern
- [ ] Dependency Injection everywhere
- [ ] Unit tests cho business logic
- [ ] Integration tests cho APIs

### Key Takeaways

1. **Dependency Rule là quan trọng nhất**: Dependencies chỉ point inward
2. **Interfaces trong Application**: Application định nghĩa contracts
3. **Business logic trong Domain/Application**: Không trong Controllers
4. **Infrastructure là pluggable**: Dễ swap implementations
5. **Testability first**: Design để dễ test

---

**Chúc bạn thành công với Clean Architecture!** 🚀

*Tài liệu này được tạo dựa trên kinh nghiệm thực tế từ dự án BlogApi*
