# 🔍 Clean Architecture Audit Report - BlogApi

**Ngày kiểm tra**: 2026-01-26  
**Người kiểm tra**: Antigravity AI  
**Phiên bản dự án**: BlogApi v1.0

---

## 📊 Tổng quan đánh giá

| Tiêu chí | Điểm | Ghi chú |
|----------|------|---------|
| **Tổng thể** | 6.5/10 | Có cấu trúc tốt nhưng vi phạm nghiêm trọng Dependency Rule |
| Phân tách Layer | 8/10 | Cấu trúc folder rõ ràng |
| Dependency Rule | 3/10 | **VI PHẠM NGHIÊM TRỌNG** |
| CQRS Pattern | 9/10 | Triển khai tốt với MediatR |
| Repository Pattern | 7/10 | Có vấn đề về vị trí interface |
| Testability | 8/10 | Integration tests tốt |

---

## ✅ Những điểm làm tốt

### 1. **Cấu trúc thư mục rõ ràng**
```
BlogApi/
├── Domain/Entities/          ✅ Tốt
├── Application/
│   ├── Features/            ✅ Vertical Slice Architecture
│   ├── Common/
│   │   ├── Behaviors/       ✅ Pipeline Behaviors
│   │   ├── Interfaces/      ✅ Abstractions
│   │   └── Services/        
├── Infrastructure/          ✅ Implementations
└── Controllers/             ✅ Presentation Layer
```

### 2. **CQRS Pattern với MediatR**
- ✅ Tách biệt rõ ràng Commands và Queries
- ✅ Sử dụng MediatR hiệu quả
- ✅ Pipeline Behaviors: Validation, Logging, Authorization, Caching

**Ví dụ tốt**:
```csharp
// Application/Features/Posts/Commands/CreatePostCommand.cs
public record CreatePostCommand(string Title, string Content, string? CategoryId = null) 
    : IRequest<Guid>;

public class CreatePostHandler : IRequestHandler<CreatePostCommand, Guid>
{
    // Handler implementation
}
```

### 3. **Domain Layer thuần túy**
- ✅ Entities không phụ thuộc vào bất kỳ layer nào
- ✅ Business logic trong Domain (ví dụ: `Post.AddRating()`)
- ✅ Sử dụng Value Objects pattern

**Ví dụ tốt**:
```csharp
// Domain/Entities/Post.cs
public void AddRating(int score)
{
    if (score < 1 || score > 5) 
        throw new ArgumentOutOfRangeException(nameof(score));
    
    double currentTotalScore = AverageRating * TotalRatings;
    TotalRatings++;
    AverageRating = (currentTotalScore + score) / TotalRatings;
}
```

### 4. **Dependency Injection tốt**
- ✅ DI Container trong `Program.cs`
- ✅ Đăng ký services theo interface
- ✅ Scoped lifetime phù hợp

### 5. **Testing Infrastructure**
- ✅ Integration tests với `BaseIntegrationTest`
- ✅ In-memory database cho testing
- ✅ Test coverage tốt

---

## ❌ Vi phạm nghiêm trọng Clean Architecture

### **🚨 VI PHẠM #1: Application Layer phụ thuộc vào Infrastructure Layer**

**Mức độ nghiêm trọng**: 🔴 **CRITICAL**

**Vấn đề**: Application layer đang import trực tiếp từ Infrastructure layer, vi phạm **Dependency Rule** cơ bản của Clean Architecture.

**Số lượng vi phạm**: **35+ files**

#### Danh sách vi phạm:

```csharp
// ❌ SAI - Application layer không được phụ thuộc vào Infrastructure
using BlogApi.Infrastructure.Repositories;
using BlogApi.Infrastructure.Hubs;
```

**Các file vi phạm**:

1. **Commands** (23 files):
   - `Application/Features/Posts/Commands/CreatePostCommand.cs`
   - `Application/Features/Posts/Commands/UpdatePostCommand.cs`
   - `Application/Features/Posts/Commands/DeletePostCommand.cs`
   - `Application/Features/Posts/Commands/RatePostCommand.cs`
   - `Application/Features/Products/Commands/*.cs` (5 files)
   - `Application/Features/ProductCategories/Commands/*.cs` (3 files)
   - `Application/Features/PostCategories/Commands/*.cs` (3 files)
   - `Application/Features/Cart/Commands/*.cs` (3 files)
   - `Application/Features/Auth/Commands/*.cs` (3 files)
   - `Application/Features/Notifications/Commands/*.cs`
   - `Application/Features/Favorites/Commands/*.cs`
   - `Application/Features/Reviews/Commands/*.cs`

2. **Queries** (10 files):
   - `Application/Features/Posts/Queries/*.cs` (2 files)
   - `Application/Features/Products/Queries/*.cs` (2 files)
   - `Application/Features/ProductCategories/Queries/*.cs` (2 files)
   - `Application/Features/PostCategories/Queries/*.cs` (2 files)
   - `Application/Features/Cart/Queries/*.cs`
   - `Application/Features/Chat/Queries/*.cs`
   - `Application/Features/Notifications/Queries/*.cs`

3. **Behaviors** (1 file):
   - `Application/Common/Behaviors/AuthorizationBehavior.cs`

**Ví dụ cụ thể**:

```csharp
// ❌ File: Application/Features/Posts/Commands/CreatePostCommand.cs
using BlogApi.Application.Common.Interfaces;
using BlogApi.Domain.Entities;
using BlogApi.Infrastructure.Repositories;  // ❌ VI PHẠM!
using MediatR;

public class CreatePostHandler : IRequestHandler<CreatePostCommand, Guid>
{
    private readonly IPostRepository _postRepository;  // ❌ Interface từ Infrastructure
    // ...
}
```

```csharp
// ❌ File: Application/Common/Behaviors/AuthorizationBehavior.cs
using BlogApi.Application.Common.Interfaces;
using BlogApi.Infrastructure.Repositories;  // ❌ VI PHẠM!
using MediatR;

public class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly IPostRepository _postRepository;  // ❌ Interface từ Infrastructure
    // ...
}
```

```csharp
// ❌ File: Application/Features/Chat/Commands/SendMessage/SendMessageCommand.cs
using BlogApi.Domain.Entities;
using BlogApi.Infrastructure.Hubs;         // ❌ VI PHẠM!
using BlogApi.Infrastructure.Repositories; // ❌ VI PHẠM!
using Microsoft.AspNetCore.SignalR;
```

---

### **🚨 VI PHẠM #2: Interface đặt sai vị trí**

**Mức độ nghiêm trọng**: 🔴 **CRITICAL**

**Vấn đề**: Các Repository Interfaces đang nằm trong Infrastructure layer thay vì Application layer.

**Vị trí hiện tại** (❌ SAI):
```
Infrastructure/
└── Repositories/
    ├── IGenericRepository.cs      ❌ SAI
    ├── IPostRepository.cs         ❌ SAI
    └── PostQueryService.cs
```

**Vị trí đúng** (✅ ĐÚNG):
```
Application/
└── Common/
    └── Interfaces/
        ├── IGenericRepository.cs  ✅ ĐÚNG
        └── IPostRepository.cs     ✅ ĐÚNG

Infrastructure/
└── Repositories/
    ├── GenericRepository.cs       ✅ Implementation
    └── PostRepository.cs          ✅ Implementation
```

**Giải thích**:
- Theo Clean Architecture, **Application layer định nghĩa interfaces** (contracts)
- **Infrastructure layer triển khai** các interfaces đó
- Infrastructure phụ thuộc vào Application, **KHÔNG PHẢI NGƯỢC LẠI**

---

### **🚨 VI PHẠM #3: Application Services chứa implementation**

**Mức độ nghiêm trọng**: 🟡 **MEDIUM**

**Vấn đề**: `JwtService` và `TokenBlacklistService` có cả interface và implementation trong Application layer.

**Vị trí hiện tại**:
```
Application/Common/Services/
├── IJwtService.cs              ✅ OK
├── JwtService.cs               ⚠️ Nên chuyển sang Infrastructure
├── ITokenBlacklistService.cs  ✅ OK
└── TokenBlacklistService.cs   ⚠️ Nên chuyển sang Infrastructure
```

**Lý do**: 
- `JwtService` sử dụng `System.IdentityModel.Tokens.Jwt` - external dependency
- `TokenBlacklistService` sử dụng `IDistributedCache` - infrastructure concern

**Nên chuyển sang**:
```
Infrastructure/Services/
├── JwtService.cs
└── TokenBlacklistService.cs
```

---

### **🚨 VI PHẠM #4: Domain Entities sử dụng trong Application**

**Mức độ nghiêm trọng**: 🟢 **LOW** (Đây thực ra là OK)

**Ghi chú**: Application layer **ĐƯỢC PHÉP** sử dụng Domain Entities. Đây KHÔNG phải là vi phạm.

✅ **Đúng theo Clean Architecture**:
```
Domain (innermost)
   ↑
Application (depends on Domain)
   ↑
Infrastructure (depends on Application & Domain)
   ↑
Presentation (depends on all)
```

---

## 🔧 Hướng dẫn sửa chữa chi tiết

### **Bước 1: Di chuyển Interfaces sang Application Layer**

#### 1.1. Tạo interfaces trong Application
```bash
# Tạo file mới
Application/Common/Interfaces/IGenericRepository.cs
Application/Common/Interfaces/IPostRepository.cs
Application/Common/Interfaces/IPostQueryService.cs
```

#### 1.2. Di chuyển interface definitions
```csharp
// ✅ Application/Common/Interfaces/IGenericRepository.cs
namespace BlogApi.Application.Common.Interfaces;

public interface IGenericRepository<TEntity, TKey> where TEntity : class
{
    Task<TEntity?> GetByIdAsync(TKey id);
    Task<IEnumerable<TEntity>> GetAllAsync();
    Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate);
    Task AddAsync(TEntity entity);
    Task UpdateAsync(TEntity entity);
    Task DeleteAsync(TEntity entity);
    IQueryable<TEntity> GetQueryable();
}
```

```csharp
// ✅ Application/Common/Interfaces/IPostRepository.cs
namespace BlogApi.Application.Common.Interfaces;

public interface IPostRepository : IGenericRepository<Post, Guid>
{
    Task<Post?> GetByIdWithAuthorAsync(Guid id);
}
```

```csharp
// ✅ Application/Common/Interfaces/IPostQueryService.cs
namespace BlogApi.Application.Common.Interfaces;

public interface IPostQueryService
{
    Task<CursorPagedList<PostDto>> GetPostsAsync(Guid? cursor, int pageSize);
}
```

#### 1.3. Cập nhật implementations trong Infrastructure
```csharp
// ✅ Infrastructure/Repositories/GenericRepository.cs
using BlogApi.Application.Common.Interfaces;  // ✅ ĐÚNG
using BlogApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BlogApi.Infrastructure.Repositories;

public class GenericRepository<TEntity, TKey> : IGenericRepository<TEntity, TKey> 
    where TEntity : class
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<TEntity> _dbSet;

    public GenericRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<TEntity>();
    }

    // Implementation...
}
```

```csharp
// ✅ Infrastructure/Repositories/PostRepository.cs
using BlogApi.Application.Common.Interfaces;  // ✅ ĐÚNG
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
}
```

```csharp
// ✅ Infrastructure/Repositories/PostQueryService.cs
using BlogApi.Application.Common.Interfaces;  // ✅ ĐÚNG
using BlogApi.Application.Common.Models;
using BlogApi.Application.Features.Posts.DTOs;
using BlogApi.Infrastructure.Data;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace BlogApi.Infrastructure.Repositories;

public class PostQueryService : IPostQueryService
{
    private readonly AppDbContext _context;

    public PostQueryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CursorPagedList<PostDto>> GetPostsAsync(Guid? cursor, int pageSize)
    {
        // Implementation using Dapper...
    }
}
```

---

### **Bước 2: Cập nhật tất cả Commands và Queries**

#### 2.1. Xóa imports từ Infrastructure
```csharp
// ❌ TRƯỚC
using BlogApi.Infrastructure.Repositories;

// ✅ SAU
using BlogApi.Application.Common.Interfaces;
```

#### 2.2. Ví dụ cụ thể - CreatePostCommand
```csharp
// ✅ Application/Features/Posts/Commands/CreatePostCommand.cs
using BlogApi.Application.Common.Interfaces;  // ✅ ĐÚNG
using BlogApi.Domain.Entities;
using MediatR;
using UUIDNext;

namespace BlogApi.Application.Features.Posts.Commands;

public record CreatePostCommand(string Title, string Content, string? CategoryId = null) 
    : IRequest<Guid>;

public class CreatePostHandler : IRequestHandler<CreatePostCommand, Guid>
{
    private readonly IPostRepository _postRepository;  // ✅ Interface từ Application
    private readonly ICurrentUserService _currentUserService;

    public CreatePostHandler(
        IPostRepository postRepository, 
        ICurrentUserService currentUserService)
    {
        _postRepository = postRepository;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(CreatePostCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId 
            ?? throw new UnauthorizedAccessException();

        var post = new Post 
        { 
            Id = Uuid.NewDatabaseFriendly(Database.SqlServer), 
            Title = request.Title, 
            Content = request.Content, 
            AuthorId = userId,
            CategoryId = request.CategoryId
        };
        
        await _postRepository.AddAsync(post);
        return post.Id;
    }
}
```

#### 2.3. Cập nhật AuthorizationBehavior
```csharp
// ✅ Application/Common/Behaviors/AuthorizationBehavior.cs
using BlogApi.Application.Common.Interfaces;  // ✅ ĐÚNG
using MediatR;

namespace BlogApi.Application.Common.Behaviors;

public class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IPostRepository _postRepository;  // ✅ Interface từ Application

    public AuthorizationBehavior(
        ICurrentUserService currentUserService, 
        IPostRepository postRepository)
    {
        _currentUserService = currentUserService;
        _postRepository = postRepository;
    }

    // Implementation...
}
```

---

### **Bước 3: Di chuyển Application Services sang Infrastructure**

#### 3.1. Di chuyển JwtService
```bash
# Di chuyển file
Application/Common/Services/JwtService.cs 
  → Infrastructure/Services/JwtService.cs
```

```csharp
// ✅ Infrastructure/Services/JwtService.cs
using BlogApi.Application.Common.Interfaces;  // ✅ Interface từ Application
using BlogApi.Application.Common.Services;    // ✅ Interface từ Application
using BlogApi.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace BlogApi.Infrastructure.Services;

public class JwtService : IJwtService
{
    private readonly IConfiguration _config;

    public JwtService(IConfiguration config) => _config = config;

    // Implementation...
}
```

#### 3.2. Di chuyển TokenBlacklistService
```bash
# Di chuyển file
Application/Common/Services/TokenBlacklistService.cs 
  → Infrastructure/Services/TokenBlacklistService.cs
```

```csharp
// ✅ Infrastructure/Services/TokenBlacklistService.cs
using BlogApi.Application.Common.Services;  // ✅ Interface từ Application
using Microsoft.Extensions.Caching.Distributed;

namespace BlogApi.Infrastructure.Services;

public class TokenBlacklistService : ITokenBlacklistService
{
    private readonly IDistributedCache _cache;

    public TokenBlacklistService(IDistributedCache cache)
    {
        _cache = cache;
    }

    // Implementation...
}
```

#### 3.3. Giữ lại Interfaces trong Application
```csharp
// ✅ Application/Common/Services/IJwtService.cs - GIỮ NGUYÊN
namespace BlogApi.Application.Common.Services;

public interface IJwtService
{
    (string Token, string Jti) GenerateAccessToken(AppUser user, IEnumerable<string> roles);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
```

```csharp
// ✅ Application/Common/Services/ITokenBlacklistService.cs - GIỮ NGUYÊN
namespace BlogApi.Application.Common.Services;

public interface ITokenBlacklistService
{
    Task BlacklistTokenAsync(string jti, TimeSpan expiry);
    Task<bool> IsTokenBlacklistedAsync(string jti);
}
```

---

### **Bước 4: Cập nhật Program.cs**

```csharp
// ✅ Program.cs
using BlogApi.Application.Common.Interfaces;
using BlogApi.Application.Common.Services;
using BlogApi.Infrastructure.Repositories;
using BlogApi.Infrastructure.Services;

// ...

// Repository registrations
builder.Services.AddScoped(typeof(IGenericRepository<,>), typeof(GenericRepository<,>));
builder.Services.AddScoped<IPostRepository, PostRepository>();
builder.Services.AddScoped<IPostQueryService, PostQueryService>();

// Service registrations
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<ITokenBlacklistService, TokenBlacklistService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IFileService, FileService>();
```

---

### **Bước 5: Xử lý SignalR Hub dependency**

**Vấn đề**: `SendMessageCommand` đang inject `IHubContext` trực tiếp

```csharp
// ❌ SAI - Application/Features/Chat/Commands/SendMessage/SendMessageCommand.cs
using BlogApi.Infrastructure.Hubs;  // ❌ VI PHẠM
using Microsoft.AspNetCore.SignalR;

public class SendMessageHandler : IRequestHandler<SendMessageCommand, Guid>
{
    private readonly IHubContext<ChatHub, IChatClient> _hubContext;  // ❌ VI PHẠM
}
```

**Giải pháp**: Tạo abstraction layer

#### 5.1. Tạo interface trong Application
```csharp
// ✅ Application/Common/Interfaces/IChatService.cs
namespace BlogApi.Application.Common.Interfaces;

public interface IChatService
{
    Task SendMessageToUserAsync(Guid receiverId, Guid senderId, string message, DateTime sentAt);
}
```

#### 5.2. Implement trong Infrastructure
```csharp
// ✅ Infrastructure/Services/ChatService.cs
using BlogApi.Application.Common.Interfaces;
using BlogApi.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace BlogApi.Infrastructure.Services;

public class ChatService : IChatService
{
    private readonly IHubContext<ChatHub, IChatClient> _hubContext;

    public ChatService(IHubContext<ChatHub, IChatClient> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendMessageToUserAsync(
        Guid receiverId, 
        Guid senderId, 
        string message, 
        DateTime sentAt)
    {
        await _hubContext.Clients
            .User(receiverId.ToString())
            .ReceiveMessage(senderId, message, sentAt);
    }
}
```

#### 5.3. Cập nhật Command Handler
```csharp
// ✅ Application/Features/Chat/Commands/SendMessage/SendMessageCommand.cs
using BlogApi.Application.Common.Interfaces;  // ✅ ĐÚNG
using BlogApi.Domain.Entities;
using MediatR;
using UUIDNext;

namespace BlogApi.Application.Features.Chat.Commands.SendMessage;

public class SendMessageHandler : IRequestHandler<SendMessageCommand, Guid>
{
    private readonly IGenericRepository<ChatMessage, Guid> _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IChatService _chatService;  // ✅ ĐÚNG

    public SendMessageHandler(
        IGenericRepository<ChatMessage, Guid> repository,
        ICurrentUserService currentUserService,
        IChatService chatService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _chatService = chatService;
    }

    public async Task<Guid> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        var senderId = _currentUserService.UserId 
            ?? throw new UnauthorizedAccessException();

        var message = new ChatMessage
        {
            Id = Uuid.NewDatabaseFriendly(Database.SqlServer),
            SenderId = senderId,
            ReceiverId = request.ReceiverId,
            Message = request.Message,
            SentAt = DateTime.UtcNow
        };

        await _repository.AddAsync(message);

        // ✅ Sử dụng abstraction thay vì HubContext trực tiếp
        await _chatService.SendMessageToUserAsync(
            request.ReceiverId, 
            senderId, 
            request.Message, 
            message.SentAt);

        return message.Id;
    }
}
```

#### 5.4. Đăng ký service
```csharp
// ✅ Program.cs
builder.Services.AddScoped<IChatService, ChatService>();
```

---

## 📋 Checklist sửa chữa

### Phase 1: Di chuyển Interfaces (Ưu tiên cao)
- [ ] Tạo `Application/Common/Interfaces/IGenericRepository.cs`
- [ ] Tạo `Application/Common/Interfaces/IPostRepository.cs`
- [ ] Tạo `Application/Common/Interfaces/IPostQueryService.cs`
- [ ] Cập nhật `Infrastructure/Repositories/GenericRepository.cs`
- [ ] Cập nhật `Infrastructure/Repositories/PostRepository.cs`
- [ ] Cập nhật `Infrastructure/Repositories/PostQueryService.cs`
- [ ] Xóa interfaces cũ trong Infrastructure

### Phase 2: Cập nhật Commands (35+ files)
- [ ] Cập nhật tất cả files trong `Application/Features/Posts/Commands/`
- [ ] Cập nhật tất cả files trong `Application/Features/Products/Commands/`
- [ ] Cập nhật tất cả files trong `Application/Features/ProductCategories/Commands/`
- [ ] Cập nhật tất cả files trong `Application/Features/PostCategories/Commands/`
- [ ] Cập nhật tất cả files trong `Application/Features/Cart/Commands/`
- [ ] Cập nhật tất cả files trong `Application/Features/Auth/Commands/`
- [ ] Cập nhật tất cả files trong `Application/Features/Notifications/Commands/`
- [ ] Cập nhật tất cả files trong `Application/Features/Favorites/Commands/`
- [ ] Cập nhật tất cả files trong `Application/Features/Reviews/Commands/`

### Phase 3: Cập nhật Queries (10+ files)
- [ ] Cập nhật tất cả files trong `Application/Features/Posts/Queries/`
- [ ] Cập nhật tất cả files trong `Application/Features/Products/Queries/`
- [ ] Cập nhật tất cả files trong `Application/Features/ProductCategories/Queries/`
- [ ] Cập nhật tất cả files trong `Application/Features/PostCategories/Queries/`
- [ ] Cập nhật tất cả files trong `Application/Features/Cart/Queries/`
- [ ] Cập nhật tất cả files trong `Application/Features/Chat/Queries/`
- [ ] Cập nhật tất cả files trong `Application/Features/Notifications/Queries/`

### Phase 4: Cập nhật Behaviors
- [ ] Cập nhật `Application/Common/Behaviors/AuthorizationBehavior.cs`

### Phase 5: Di chuyển Services
- [ ] Di chuyển `JwtService` sang Infrastructure
- [ ] Di chuyển `TokenBlacklistService` sang Infrastructure
- [ ] Tạo `IChatService` interface
- [ ] Implement `ChatService` trong Infrastructure
- [ ] Cập nhật `SendMessageCommand`

### Phase 6: Cập nhật DI Registration
- [ ] Cập nhật `Program.cs` với registrations mới

### Phase 7: Testing
- [ ] Chạy tất cả unit tests
- [ ] Chạy tất cả integration tests
- [ ] Kiểm tra build thành công
- [ ] Test các endpoints chính

---

## 🎯 Kết quả sau khi sửa

### Dependency Graph đúng:
```
┌─────────────────────────────────────────┐
│         Presentation Layer              │
│         (Controllers, Program.cs)       │
└──────────────────┬──────────────────────┘
                   │ depends on
                   ↓
┌─────────────────────────────────────────┐
│         Application Layer               │
│  - Features (Commands, Queries)         │
│  - Interfaces (IRepository, IService)   │  ← Defines contracts
│  - Behaviors, DTOs                      │
└──────────────────┬──────────────────────┘
                   │ depends on
                   ↓
┌─────────────────────────────────────────┐
│         Domain Layer                    │
│         (Entities, Value Objects)       │
└─────────────────────────────────────────┘
                   ↑
                   │ implements
┌──────────────────┴──────────────────────┐
│         Infrastructure Layer            │
│  - Repositories (Implementations)       │  ← Implements contracts
│  - Services (Implementations)           │
│  - DbContext, Hubs                      │
└─────────────────────────────────────────┘
```

### Lợi ích:
1. ✅ **Tuân thủ Dependency Rule**: Application không phụ thuộc Infrastructure
2. ✅ **Testability**: Dễ dàng mock interfaces trong unit tests
3. ✅ **Flexibility**: Dễ thay đổi implementation mà không ảnh hưởng Application
4. ✅ **Maintainability**: Code rõ ràng, dễ hiểu, dễ bảo trì
5. ✅ **Scalability**: Dễ mở rộng với implementations mới

---

## 📚 Tài liệu tham khảo

1. **Clean Architecture** - Robert C. Martin
   - [Clean Architecture Blog](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)

2. **Dependency Rule**:
   > "Source code dependencies must point only inward, toward higher-level policies."

3. **Best Practices**:
   - Application layer định nghĩa **interfaces** (contracts)
   - Infrastructure layer cung cấp **implementations**
   - Domain layer **không phụ thuộc** vào bất kỳ layer nào
   - Presentation layer **orchestrates** và gọi Application layer

---

## 🔍 Script kiểm tra tự động

Tạo script PowerShell để kiểm tra vi phạm:

```powershell
# check-clean-architecture.ps1

Write-Host "🔍 Checking Clean Architecture violations..." -ForegroundColor Cyan

$violations = @()

# Check Application layer dependencies
$appFiles = Get-ChildItem -Path "Application" -Recurse -Filter "*.cs"
foreach ($file in $appFiles) {
    $content = Get-Content $file.FullName
    if ($content -match "using BlogApi\.Infrastructure") {
        $violations += "❌ $($file.FullName) imports Infrastructure"
    }
}

# Check Interface locations
$infraInterfaces = Get-ChildItem -Path "Infrastructure" -Recurse -Filter "I*.cs"
foreach ($file in $infraInterfaces) {
    if ($file.Name -match "^I[A-Z].*\.cs$") {
        $violations += "⚠️  $($file.FullName) - Interface should be in Application"
    }
}

if ($violations.Count -eq 0) {
    Write-Host "✅ No Clean Architecture violations found!" -ForegroundColor Green
} else {
    Write-Host "❌ Found $($violations.Count) violations:" -ForegroundColor Red
    $violations | ForEach-Object { Write-Host $_ -ForegroundColor Yellow }
    exit 1
}
```

**Sử dụng**:
```bash
powershell -ExecutionPolicy Bypass -File check-clean-architecture.ps1
```

---

## 📊 Tổng kết

### Điểm mạnh của dự án:
1. ✅ Cấu trúc folder rõ ràng
2. ✅ CQRS pattern tốt với MediatR
3. ✅ Domain layer thuần túy
4. ✅ Pipeline behaviors hiệu quả
5. ✅ Testing infrastructure tốt

### Điểm cần cải thiện:
1. 🔴 **CRITICAL**: Application phụ thuộc vào Infrastructure (35+ files)
2. 🔴 **CRITICAL**: Interfaces đặt sai vị trí
3. 🟡 **MEDIUM**: Application Services chứa implementations
4. 🟡 **MEDIUM**: SignalR Hub dependency trực tiếp

### Ước tính thời gian sửa chữa:
- **Phase 1-2**: 2-3 giờ (Di chuyển interfaces + cập nhật Commands)
- **Phase 3-4**: 1-2 giờ (Cập nhật Queries + Behaviors)
- **Phase 5-6**: 1-2 giờ (Di chuyển Services + DI)
- **Phase 7**: 1 giờ (Testing)
- **Tổng**: **5-8 giờ** làm việc

### Mức độ ưu tiên:
1. 🔴 **HIGH**: Di chuyển Repository interfaces (Phase 1)
2. 🔴 **HIGH**: Cập nhật tất cả Commands/Queries (Phase 2-3)
3. 🟡 **MEDIUM**: Di chuyển Application Services (Phase 5)
4. 🟢 **LOW**: Tạo ChatService abstraction (Phase 5)

---

**Kết luận**: Dự án có nền tảng tốt nhưng cần refactor nghiêm túc để tuân thủ đúng Clean Architecture. Việc sửa chữa sẽ mang lại lợi ích lớn về maintainability và testability trong dài hạn.
