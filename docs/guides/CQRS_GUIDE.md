# 📘 CQRS Pattern - Hướng dẫn Chi tiết

**CQRS** = **Command Query Responsibility Segregation**  
**Tác giả**: Greg Young, Udi Dahan  
**Mục đích**: Tách biệt operations đọc (Query) và ghi (Command)

---

## 📑 Mục lục

1. [CQRS là gì?](#-cqrs-là-gì)
2. [Tại sao cần CQRS?](#-tại-sao-cần-cqrs)
3. [Command vs Query](#-command-vs-query)
4. [Kiến trúc CQRS](#️-kiến-trúc-cqrs)
5. [Implementation với MediatR](#-implementation-với-mediatr)
6. [Ví dụ thực tế từ BlogApi](#-ví-dụ-thực-tế-từ-blogapi)
7. [CQRS Patterns](#-cqrs-patterns)
8. [Best Practices](#-best-practices)
9. [Common Mistakes](#-common-mistakes)
10. [Testing CQRS](#-testing-cqrs)
11. [Advanced Topics](#-advanced-topics)

---

## 🔄 Luồng Xử Lý Request từ Controller

### Tổng Quan Luồng Xử Lý

Khi một HTTP request được gửi đến API, nó sẽ đi qua các bước sau:

```
┌─────────────────────────────────────────────────────────────────────┐
│                    LUỒNG XỬ LÝ CQRS REQUEST                         │
└─────────────────────────────────────────────────────────────────────┘

1. HTTP Request
   │
   ↓
┌──────────────────────────────────────────────────────────────────┐
│  ASP.NET Core Pipeline                                           │
│  • Authentication Middleware                                     │
│  • Authorization Middleware                                      │
│  • Model Binding                                                 │
└──────────────────────────────┬───────────────────────────────────┘
                               │
                               ↓
┌──────────────────────────────────────────────────────────────────┐
│  2. CONTROLLER                                                   │
│  • Nhận request từ client                                        │
│  • Tạo Command/Query object                                      │
│  • Gọi _mediator.Send(command/query)                            │
└──────────────────────────────┬───────────────────────────────────┘
                               │
                               ↓
┌──────────────────────────────────────────────────────────────────┐
│  3. MEDIATR                                                      │
│  • Nhận Command/Query                                            │
│  • Tìm Handler tương ứng                                         │
│  • Khởi tạo Pipeline Behaviors                                   │
└──────────────────────────────┬───────────────────────────────────┘
                               │
                               ↓
┌──────────────────────────────────────────────────────────────────┐
│  4. PIPELINE BEHAVIORS (theo thứ tự)                             │
│                                                                  │
│  ┌────────────────────────────────────────────────────────┐    │
│  │  a) LoggingBehavior                                    │    │
│  │     • Log thông tin request                            │    │
│  │     • Log thời gian bắt đầu                            │    │
│  └────────────────────────────┬───────────────────────────┘    │
│                               │                                 │
│  ┌────────────────────────────▼───────────────────────────┐    │
│  │  b) ValidationBehavior                                 │    │
│  │     • Kiểm tra FluentValidation rules                  │    │
│  │     • Throw ValidationException nếu invalid            │    │
│  └────────────────────────────┬───────────────────────────┘    │
│                               │                                 │
│  ┌────────────────────────────▼───────────────────────────┐    │
│  │  c) OwnershipAuthorizationBehavior (cho Commands)      │    │
│  │     • Kiểm tra quyền sở hữu resource                   │    │
│  │     • Throw UnauthorizedException nếu không có quyền   │    │
│  └────────────────────────────┬───────────────────────────┘    │
│                               │                                 │
│  ┌────────────────────────────▼───────────────────────────┐    │
│  │  d) CachingBehavior (chỉ cho Queries)                  │    │
│  │     • Kiểm tra cache                                   │    │
│  │     • Return cached data nếu có                        │    │
│  │     • Nếu không có cache → tiếp tục                    │    │
│  └────────────────────────────┬───────────────────────────┘    │
└───────────────────────────────┼───────────────────────────────┘
                               │
                               ↓
┌──────────────────────────────────────────────────────────────────┐
│  5. HANDLER                                                      │
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  Command Handler                │  Query Handler        │   │
│  │  ────────────────                │  ─────────────        │   │
│  │  • Validation logic              │  • Đọc data           │   │
│  │  • Business logic                │  • Map to DTO         │   │
│  │  • Gọi Repository                │  • Return DTO         │   │
│  │  • Publish Domain Events         │                       │   │
│  │  • Return ID/Result              │                       │   │
│  └─────────────────────────────────────────────────────────┘   │
└──────────────────────────────┬───────────────────────────────────┘
                               │
                               ↓
┌──────────────────────────────────────────────────────────────────┐
│  6. REPOSITORY / DATA ACCESS                                     │
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  • EF Core (Commands)                                   │   │
│  │    - SaveChangesAsync()                                 │   │
│  │    - Transaction management                             │   │
│  │                                                         │   │
│  │  • Dapper (Queries - Optional)                          │   │
│  │    - Raw SQL queries                                    │   │
│  │    - Optimized reads                                    │   │
│  └─────────────────────────────────────────────────────────┘   │
└──────────────────────────────┬───────────────────────────────────┘
                               │
                               ↓
┌──────────────────────────────────────────────────────────────────┐
│  7. DATABASE                                                     │
│  • SQL Server                                                    │
│  • Execute query/command                                         │
│  • Return data                                                   │
└──────────────────────────────┬───────────────────────────────────┘
                               │
                               ↓
┌──────────────────────────────────────────────────────────────────┐
│  8. RESPONSE PIPELINE (ngược lại)                                │
│                                                                  │
│  Handler → Behaviors → MediatR → Controller → HTTP Response     │
│                                                                  │
│  • CachingBehavior: Lưu vào cache (nếu là Query)                │
│  • LoggingBehavior: Log kết quả và thời gian                    │
│  • Controller: Map to HTTP response                             │
└──────────────────────────────────────────────────────────────────┘
```

---

### Chi Tiết Từng Bước

#### **Bước 1: HTTP Request đến ASP.NET Core Pipeline**

```csharp
// Client gửi request
POST /api/posts
Content-Type: application/json
Authorization: Bearer eyJhbGc...

{
  "title": "My New Post",
  "content": "This is the content",
  "categoryId": "tech"
}
```

**ASP.NET Core Middleware xử lý:**
1. **Authentication Middleware**: Xác thực JWT token
2. **Authorization Middleware**: Kiểm tra [Authorize] attribute
3. **Model Binding**: Bind JSON → CreatePostCommand object

---

#### **Bước 2: Controller Nhận Request**

```csharp
[ApiController]
[Route("api/[controller]")]
public class PostsController : ControllerBase
{
    private readonly IMediator _mediator;
    
    public PostsController(IMediator mediator) => _mediator = mediator;
    
    [HttpPost]
    [Authorize] // ← Đã được verify ở middleware
    public async Task<IActionResult> Create([FromBody] CreatePostCommand command)
    {
        // ✅ Tại đây:
        // - User đã được authenticate
        // - command object đã được bind từ JSON
        // - Validation attributes đã được check (nếu có)
        
        // Gửi command đến MediatR
        var postId = await _mediator.Send(command);
        
        // Return response
        return CreatedAtAction(nameof(GetById), new { id = postId }, postId);
    }
}
```

**Điều gì xảy ra ở đây?**
- Controller **KHÔNG** chứa business logic
- Controller chỉ là "điểm vào" để nhận request
- `_mediator.Send(command)` là điểm chuyển giao cho CQRS pipeline

---

#### **Bước 3: MediatR Nhận Command/Query**

```csharp
// MediatR internal processing
public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
{
    // 1. Tìm Handler tương ứng
    var handlerType = typeof(IRequestHandler<,>)
        .MakeGenericType(request.GetType(), typeof(TResponse));
    
    var handler = _serviceProvider.GetService(handlerType);
    
    // 2. Tìm tất cả Pipeline Behaviors
    var behaviors = _serviceProvider
        .GetServices<IPipelineBehavior<IRequest<TResponse>, TResponse>>()
        .Reverse(); // Reverse để đúng thứ tự
    
    // 3. Build pipeline chain
    RequestHandlerDelegate<TResponse> pipeline = () => 
        handler.Handle(request, cancellationToken);
    
    // 4. Wrap handler với behaviors
    pipeline = behaviors.Aggregate(pipeline, (next, behavior) => 
        () => behavior.Handle(request, next, cancellationToken));
    
    // 5. Execute pipeline
    return await pipeline();
}
```

**MediatR làm gì?**
- Tự động tìm Handler phù hợp với Command/Query
- Tự động inject dependencies vào Handler
- Tạo pipeline với các Behaviors
- Execute pipeline theo thứ tự

---

### 🔍 Chi Tiết: Cách MediatR Tìm Handler Tương Ứng

#### **1. Registration Phase (Khi khởi động ứng dụng)**

Khi ứng dụng khởi động, MediatR scan tất cả assemblies để tìm và đăng ký handlers:

```csharp
// Program.cs
builder.Services.AddMediatR(cfg => 
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
```

**Điều gì xảy ra bên trong?**

```csharp
// MediatR internal registration process
public static IServiceCollection RegisterServicesFromAssembly(
    this IServiceCollection services, 
    Assembly assembly)
{
    // 1. Scan assembly để tìm tất cả types implement IRequestHandler
    var handlerTypes = assembly.GetTypes()
        .Where(t => t.GetInterfaces()
            .Any(i => i.IsGenericType && 
                      i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)))
        .ToList();
    
    // 2. Đăng ký từng handler vào DI container
    foreach (var handlerType in handlerTypes)
    {
        // Lấy interface mà handler implement
        var handlerInterface = handlerType.GetInterfaces()
            .First(i => i.IsGenericType && 
                       i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));
        
        // Đăng ký vào DI container
        services.AddTransient(handlerInterface, handlerType);
        
        Console.WriteLine($"Registered: {handlerInterface.Name} -> {handlerType.Name}");
    }
    
    return services;
}
```

**Output khi khởi động:**
```
Registered: IRequestHandler<CreatePostCommand, Guid> -> CreatePostHandler
Registered: IRequestHandler<UpdatePostCommand, bool> -> UpdatePostHandler
Registered: IRequestHandler<DeletePostCommand, bool> -> DeletePostHandler
Registered: IRequestHandler<GetPostsQuery, CursorPagedList<PostDto>> -> GetPostsHandler
Registered: IRequestHandler<GetPostByIdQuery, PostDto> -> GetPostByIdHandler
...
```

---

#### **2. Runtime Phase (Khi nhận request)**

Khi controller gọi `_mediator.Send(command)`, MediatR tìm handler theo các bước sau:

##### **Bước 2.1: Xác định Request Type**

```csharp
// Controller gửi command
var command = new CreatePostCommand("My Post", "Content", "tech");
var result = await _mediator.Send(command);

// MediatR nhận được:
// - request: CreatePostCommand instance
// - TRequest: CreatePostCommand (type)
// - TResponse: Guid (từ IRequest<Guid>)
```

##### **Bước 2.2: Build Handler Interface Type**

```csharp
public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
{
    // 1. Lấy type của request
    var requestType = request.GetType(); 
    // → CreatePostCommand
    
    // 2. Lấy response type
    var responseType = typeof(TResponse); 
    // → Guid
    
    // 3. Build handler interface type
    var handlerInterfaceType = typeof(IRequestHandler<,>)
        .MakeGenericType(requestType, responseType);
    // → IRequestHandler<CreatePostCommand, Guid>
    
    Console.WriteLine($"Looking for handler: {handlerInterfaceType.Name}");
}
```

**Giải thích `MakeGenericType`:**

```csharp
// Generic type definition (open generic)
typeof(IRequestHandler<,>)
// → IRequestHandler<TRequest, TResponse> (chưa có type cụ thể)

// Make generic type (closed generic)
typeof(IRequestHandler<,>).MakeGenericType(typeof(CreatePostCommand), typeof(Guid))
// → IRequestHandler<CreatePostCommand, Guid> (có type cụ thể)

// Tương tự như:
typeof(IRequestHandler<CreatePostCommand, Guid>)
```

##### **Bước 2.3: Resolve Handler từ DI Container**

```csharp
public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
{
    // ... (tiếp từ bước 2.2)
    
    // 4. Resolve handler từ DI container
    var handler = _serviceProvider.GetService(handlerInterfaceType);
    // → CreatePostHandler instance
    
    if (handler == null)
    {
        throw new InvalidOperationException(
            $"No handler registered for {handlerInterfaceType.Name}");
    }
    
    Console.WriteLine($"Found handler: {handler.GetType().Name}");
}
```

**DI Container làm gì?**

```csharp
// DI Container internal lookup
public object GetService(Type serviceType)
{
    // 1. Tìm registration trong dictionary
    // Key: IRequestHandler<CreatePostCommand, Guid>
    // Value: CreatePostHandler
    
    if (!_registrations.TryGetValue(serviceType, out var implementationType))
    {
        return null; // Không tìm thấy
    }
    
    // 2. Tạo instance của handler
    // Resolve dependencies của handler (IPostRepository, ICurrentUserService, etc.)
    var dependencies = ResolveDependencies(implementationType);
    
    // 3. Create instance với dependencies
    var instance = Activator.CreateInstance(implementationType, dependencies);
    
    return instance;
}
```

##### **Bước 2.4: Dependency Injection cho Handler**

```csharp
// Handler definition
public class CreatePostHandler : IRequestHandler<CreatePostCommand, Guid>
{
    private readonly IGenericRepository<Post, Guid> _repository;
    private readonly ICurrentUserService _userService;
    private readonly IEventPublisher _eventPublisher;
    
    // Constructor injection
    public CreatePostHandler(
        IGenericRepository<Post, Guid> repository,
        ICurrentUserService userService,
        IEventPublisher eventPublisher)
    {
        _repository = repository;
        _userService = userService;
        _eventPublisher = eventPublisher;
    }
}

// DI Container resolve process
var handler = new CreatePostHandler(
    serviceProvider.GetService<IGenericRepository<Post, Guid>>(),  // ← Resolve
    serviceProvider.GetService<ICurrentUserService>(),              // ← Resolve
    serviceProvider.GetService<IEventPublisher>()                   // ← Resolve
);
```

---

#### **3. Ví Dụ Hoàn Chỉnh: Từ Command → Handler**

```csharp
// ============================================
// STEP 1: Controller gửi command
// ============================================
[HttpPost]
public async Task<IActionResult> Create([FromBody] CreatePostCommand command)
{
    // command = CreatePostCommand { Title = "My Post", Content = "...", CategoryId = "tech" }
    var result = await _mediator.Send(command);
    return Ok(result);
}

// ============================================
// STEP 2: MediatR.Send() được gọi
// ============================================
public async Task<Guid> Send(CreatePostCommand request)
{
    // Type analysis:
    // - request.GetType() = CreatePostCommand
    // - TResponse = Guid (từ IRequest<Guid>)
    
    Console.WriteLine($"Request Type: {request.GetType().Name}");
    // Output: Request Type: CreatePostCommand
    
    Console.WriteLine($"Response Type: {typeof(Guid).Name}");
    // Output: Response Type: Guid
}

// ============================================
// STEP 3: Build Handler Interface Type
// ============================================
var handlerInterfaceType = typeof(IRequestHandler<,>)
    .MakeGenericType(typeof(CreatePostCommand), typeof(Guid));

Console.WriteLine($"Handler Interface: {handlerInterfaceType.Name}");
// Output: Handler Interface: IRequestHandler<CreatePostCommand, Guid>

// ============================================
// STEP 4: Resolve từ DI Container
// ============================================
var handler = _serviceProvider.GetService(handlerInterfaceType);

Console.WriteLine($"Handler Type: {handler.GetType().Name}");
// Output: Handler Type: CreatePostHandler

Console.WriteLine($"Handler Instance: {handler}");
// Output: Handler Instance: BlogApi.Application.Features.Posts.Commands.CreatePostHandler

// ============================================
// STEP 5: Verify Handler
// ============================================
if (handler is IRequestHandler<CreatePostCommand, Guid> typedHandler)
{
    Console.WriteLine("✅ Handler found and type-safe!");
    
    // Dependencies đã được inject:
    Console.WriteLine($"Repository: {typedHandler._repository != null}");
    Console.WriteLine($"UserService: {typedHandler._userService != null}");
    Console.WriteLine($"EventPublisher: {typedHandler._eventPublisher != null}");
}
```

---

#### **4. Type Matching Process**

MediatR sử dụng **exact type matching** để tìm handler:

```csharp
// ✅ MATCHING
Command: CreatePostCommand : IRequest<Guid>
Handler: CreatePostHandler : IRequestHandler<CreatePostCommand, Guid>
→ MATCH! (exact types)

// ❌ NOT MATCHING
Command: CreatePostCommand : IRequest<Guid>
Handler: CreatePostHandler : IRequestHandler<CreatePostCommand, bool>
→ NO MATCH (different response type: Guid vs bool)

// ❌ NOT MATCHING
Command: UpdatePostCommand : IRequest<bool>
Handler: CreatePostHandler : IRequestHandler<CreatePostCommand, Guid>
→ NO MATCH (different request type: UpdatePostCommand vs CreatePostCommand)
```

**Ví dụ lỗi khi không tìm thấy handler:**

```csharp
// Command
public record CreateProductCommand(string Name) : IRequest<Guid>;

// ❌ KHÔNG có handler
// public class CreateProductHandler : IRequestHandler<CreateProductCommand, Guid> { }

// Khi gọi:
await _mediator.Send(new CreateProductCommand("Product 1"));

// Exception:
// InvalidOperationException: Handler was not found for request of type CreateProductCommand.
// Container or service provider not configured properly or handlers not registered.
```

---

#### **5. Multiple Handlers (Không được phép)**

MediatR **KHÔNG** cho phép nhiều handlers cho cùng một request:

```csharp
// ❌ KHÔNG HỢP LỆ
public class CreatePostHandler1 : IRequestHandler<CreatePostCommand, Guid> { }
public class CreatePostHandler2 : IRequestHandler<CreatePostCommand, Guid> { }

// Khi register:
services.AddTransient<IRequestHandler<CreatePostCommand, Guid>, CreatePostHandler1>();
services.AddTransient<IRequestHandler<CreatePostCommand, Guid>, CreatePostHandler2>();

// Runtime error:
// InvalidOperationException: Multiple handlers registered for CreatePostCommand.
// Only one handler is allowed per request type.
```

**Giải pháp nếu cần multiple handlers:**
- Sử dụng **Notification** (INotification) thay vì Request
- Notifications cho phép nhiều handlers

```csharp
// ✅ HỢP LỆ với Notifications
public record PostCreatedNotification(Guid PostId) : INotification;

public class SendEmailHandler : INotificationHandler<PostCreatedNotification> { }
public class UpdateCacheHandler : INotificationHandler<PostCreatedNotification> { }
public class LogEventHandler : INotificationHandler<PostCreatedNotification> { }

// Tất cả 3 handlers sẽ được gọi
await _mediator.Publish(new PostCreatedNotification(postId));
```

---

#### **6. Handler Lifetime**

Handlers được đăng ký với **Transient** lifetime:

```csharp
// Registration
services.AddTransient<IRequestHandler<CreatePostCommand, Guid>, CreatePostHandler>();

// Meaning:
// - Mỗi request tạo một instance mới
// - Instance bị dispose sau khi request hoàn thành
// - Thread-safe (mỗi request có instance riêng)
```

**Lifecycle:**

```
Request 1: Create Post
  → CreatePostHandler instance #1 created
  → Handle() executed
  → Instance #1 disposed

Request 2: Create Post (concurrent)
  → CreatePostHandler instance #2 created
  → Handle() executed
  → Instance #2 disposed

Request 3: Create Post
  → CreatePostHandler instance #3 created
  → Handle() executed
  → Instance #3 disposed
```

---

#### **7. Debugging: Xem Handler Resolution**

Để debug quá trình tìm handler:

```csharp
// Custom MediatR wrapper
public class DebugMediator : IMediator
{
    private readonly IMediator _innerMediator;
    private readonly ILogger<DebugMediator> _logger;
    
    public async Task<TResponse> Send<TResponse>(
        IRequest<TResponse> request, 
        CancellationToken ct = default)
    {
        var requestType = request.GetType();
        var responseType = typeof(TResponse);
        
        _logger.LogInformation(
            "🔍 Looking for handler: IRequestHandler<{RequestType}, {ResponseType}>",
            requestType.Name,
            responseType.Name);
        
        try
        {
            var result = await _innerMediator.Send(request, ct);
            
            _logger.LogInformation(
                "✅ Handler found and executed successfully");
            
            return result;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Handler was not found"))
        {
            _logger.LogError(
                "❌ No handler registered for {RequestType}",
                requestType.Name);
            
            throw;
        }
    }
}
```

**Output:**
```
[INFO] 🔍 Looking for handler: IRequestHandler<CreatePostCommand, Guid>
[INFO] ✅ Handler found and executed successfully
```

---

#### **8. Tóm Tắt: Handler Resolution Flow**

```
┌─────────────────────────────────────────────────────────────┐
│                  HANDLER RESOLUTION FLOW                     │
└─────────────────────────────────────────────────────────────┘

1. REGISTRATION (Startup)
   ┌──────────────────────────────────────────────────────┐
   │ AddMediatR(typeof(Program).Assembly)                 │
   │   ↓                                                  │
   │ Scan assembly for IRequestHandler<,> implementations │
   │   ↓                                                  │
   │ Register each handler in DI container                │
   │   Key: IRequestHandler<CreatePostCommand, Guid>      │
   │   Value: CreatePostHandler                           │
   └──────────────────────────────────────────────────────┘

2. RUNTIME (Request)
   ┌──────────────────────────────────────────────────────┐
   │ _mediator.Send(command)                              │
   │   ↓                                                  │
   │ Get request type: CreatePostCommand                  │
   │   ↓                                                  │
   │ Get response type: Guid                              │
   │   ↓                                                  │
   │ Build interface: IRequestHandler<CreatePostCommand, Guid> │
   │   ↓                                                  │
   │ Resolve from DI: CreatePostHandler                   │
   │   ↓                                                  │
   │ Inject dependencies (Repository, UserService, etc.)  │
   │   ↓                                                  │
   │ Return handler instance                              │
   └──────────────────────────────────────────────────────┘

3. EXECUTION
   ┌──────────────────────────────────────────────────────┐
   │ handler.Handle(command, cancellationToken)           │
   │   ↓                                                  │
   │ Execute business logic                               │
   │   ↓                                                  │
   │ Return result                                        │
   └──────────────────────────────────────────────────────┘
```

---

#### **9. Key Points**

| Aspect | Detail |
|--------|--------|
| **Registration** | Automatic scan và register tất cả handlers |
| **Matching** | Exact type matching (TRequest, TResponse) |
| **Lifetime** | Transient (mỗi request = instance mới) |
| **DI** | Automatic dependency injection |
| **Multiple Handlers** | KHÔNG được phép (use Notifications thay vì) |
| **Thread Safety** | Mỗi request có instance riêng |
| **Performance** | Reflection chỉ chạy 1 lần (registration), runtime dùng dictionary lookup |

---



#### **Bước 4: Pipeline Behaviors**

##### **4a. LoggingBehavior (Đầu tiên)**

```csharp
public class LoggingBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        
        // ✅ Log TRƯỚC khi xử lý
        _logger.LogInformation(
            "Handling {RequestName}: {@Request}", 
            requestName, 
            request);
        
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // ➡️ Gọi behavior/handler tiếp theo
            var response = await next();
            
            stopwatch.Stop();
            
            // ✅ Log SAU khi xử lý thành công
            _logger.LogInformation(
                "Handled {RequestName} in {ElapsedMs}ms: {@Response}",
                requestName,
                stopwatch.ElapsedMilliseconds,
                response);
            
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            // ❌ Log lỗi
            _logger.LogError(
                ex,
                "Error handling {RequestName} after {ElapsedMs}ms",
                requestName,
                stopwatch.ElapsedMilliseconds);
            
            throw;
        }
    }
}
```

**Output log:**
```
[INFO] Handling CreatePostCommand: { "Title": "My New Post", "Content": "..." }
[INFO] Handled CreatePostCommand in 45ms: "3fa85f64-5717-4562-b3fc-2c963f66afa6"
```

---

##### **4b. ValidationBehavior**

```csharp
public class ValidationBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // ✅ Kiểm tra có validators không
        if (!_validators.Any())
            return await next(); // Không có validator → skip
        
        // ✅ Chạy tất cả validators
        var context = new ValidationContext<TRequest>(request);
        
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));
        
        // ✅ Collect tất cả errors
        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();
        
        // ❌ Nếu có lỗi → throw exception
        if (failures.Any())
        {
            throw new ValidationException(failures);
        }
        
        // ✅ Validation passed → tiếp tục
        return await next();
    }
}
```

**Validator Example:**
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

**Nếu validation fail:**
```json
{
  "type": "ValidationException",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Title": ["Title is required"],
    "Content": ["Content too short"]
  }
}
```

---

##### **4c. OwnershipAuthorizationBehavior (cho Commands)**

```csharp
public class OwnershipAuthorizationBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IOwnershipService _ownershipService;
    
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // ✅ Chỉ check ownership cho requests implement IOwnershipRequest
        if (request is not IOwnershipRequest ownershipRequest)
            return await next();
        
        var userId = _currentUserService.UserId;
        if (userId == null)
            throw new UnauthorizedAccessException("User not authenticated");
        
        // ✅ Kiểm tra quyền sở hữu
        var resourceId = ownershipRequest.GetResourceId();
        var resourceType = ownershipRequest.GetResourceType();
        
        var isOwner = await _ownershipService.IsOwnerAsync(
            userId.Value, 
            resourceType, 
            resourceId);
        
        // ❌ Không phải owner → throw exception
        if (!isOwner)
        {
            throw new ForbiddenAccessException(
                $"User {userId} does not own {resourceType} {resourceId}");
        }
        
        // ✅ Là owner → tiếp tục
        return await next();
    }
}
```

**Example:**
```csharp
// Command cần check ownership
public record UpdatePostCommand(Guid Id, string Title, string Content) 
    : IRequest<bool>, IOwnershipRequest
{
    public Guid GetResourceId() => Id;
    public string GetResourceType() => "Post";
}
```

---

##### **4d. CachingBehavior (chỉ cho Queries)**

```csharp
public class CachingBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;
    
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // ✅ Chỉ cache queries có [Cacheable] attribute
        var cacheableAttribute = typeof(TRequest)
            .GetCustomAttribute<CacheableAttribute>();
        
        if (cacheableAttribute == null)
            return await next(); // Không cache → skip
        
        // ✅ Tạo cache key
        var cacheKey = $"{typeof(TRequest).Name}_{GetRequestHash(request)}";
        
        // ✅ Kiểm tra cache
        if (_cache.TryGetValue(cacheKey, out TResponse cachedResponse))
        {
            _logger.LogInformation("Cache HIT for {CacheKey}", cacheKey);
            return cachedResponse;
        }
        
        _logger.LogInformation("Cache MISS for {CacheKey}", cacheKey);
        
        // ✅ Execute handler
        var response = await next();
        
        // ✅ Lưu vào cache
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = 
                TimeSpan.FromMinutes(cacheableAttribute.ExpirationMinutes)
        };
        
        _cache.Set(cacheKey, response, cacheOptions);
        
        return response;
    }
    
    private string GetRequestHash(TRequest request)
    {
        var json = JsonSerializer.Serialize(request);
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
        return Convert.ToBase64String(hash);
    }
}
```

**Example:**
```csharp
[Cacheable(ExpirationMinutes = 5)]
public record GetPostByIdQuery(Guid Id) : IRequest<PostDto>;
```

**Cache flow:**
```
Request 1: GetPostByIdQuery(Id=123)
  → Cache MISS
  → Execute Handler
  → Save to cache
  → Return data

Request 2: GetPostByIdQuery(Id=123) [trong vòng 5 phút]
  → Cache HIT
  → Return cached data (không execute handler)
```

---

#### **Bước 5: Handler Xử Lý**

##### **Command Handler Example:**

```csharp
public class CreatePostHandler : IRequestHandler<CreatePostCommand, Guid>
{
    private readonly IGenericRepository<Post, Guid> _repository;
    private readonly ICurrentUserService _userService;
    private readonly IEventPublisher _eventPublisher;
    
    public CreatePostHandler(
        IGenericRepository<Post, Guid> repository,
        ICurrentUserService userService,
        IEventPublisher eventPublisher)
    {
        _repository = repository;
        _userService = userService;
        _eventPublisher = eventPublisher;
    }
    
    public async Task<Guid> Handle(
        CreatePostCommand request, 
        CancellationToken cancellationToken)
    {
        // ✅ Bước 1: Lấy user hiện tại
        var userId = _userService.UserId 
            ?? throw new UnauthorizedAccessException();
        
        // ✅ Bước 2: Tạo entity
        var post = new Post 
        { 
            Id = Uuid.NewDatabaseFriendly(Database.SqlServer),
            Title = request.Title,
            Content = request.Content,
            AuthorId = userId,
            CategoryId = request.CategoryId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        // ✅ Bước 3: Business logic (nếu có)
        // Ví dụ: Kiểm tra duplicate title
        var existingPost = await _repository.GetQueryable()
            .FirstOrDefaultAsync(p => p.Title == request.Title, cancellationToken);
        
        if (existingPost != null)
            throw new BusinessException("Post with this title already exists");
        
        // ✅ Bước 4: Lưu vào database
        await _repository.AddAsync(post, cancellationToken);
        
        // ✅ Bước 5: Publish domain event
        await _eventPublisher.Publish(
            new PostCreatedEvent(post.Id, post.Title, userId),
            cancellationToken);
        
        // ✅ Bước 6: Return ID
        return post.Id;
    }
}
```

**Command Handler làm gì?**
1. **Validation**: Kiểm tra business rules
2. **Business Logic**: Xử lý logic nghiệp vụ
3. **Persistence**: Lưu vào database
4. **Events**: Publish domain events
5. **Return**: Trả về ID hoặc kết quả

---

##### **Query Handler Example:**

```csharp
public class GetPostsHandler 
    : IRequestHandler<GetPostsQuery, CursorPagedList<PostDto>>
{
    private readonly IGenericRepository<Post, Guid> _repository;
    
    public GetPostsHandler(IGenericRepository<Post, Guid> repository)
    {
        _repository = repository;
    }
    
    public async Task<CursorPagedList<PostDto>> Handle(
        GetPostsQuery request, 
        CancellationToken cancellationToken)
    {
        // ✅ Bước 1: Build query
        var query = _repository.GetQueryable()
            .Include(p => p.Author)
            .AsNoTracking(); // ← Quan trọng: không track changes
        
        // ✅ Bước 2: Apply filters
        if (!string.IsNullOrEmpty(request.CategoryId))
        {
            query = query.Where(p => p.CategoryId == request.CategoryId);
        }
        
        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            query = query.Where(p => 
                p.Title.Contains(request.SearchTerm) ||
                p.Content.Contains(request.SearchTerm));
        }
        
        // ✅ Bước 3: Apply cursor pagination
        if (request.Cursor.HasValue)
        {
            query = query.Where(p => p.Id.CompareTo(request.Cursor.Value) > 0);
        }
        
        // ✅ Bước 4: Order và limit
        query = query
            .OrderBy(p => p.Id)
            .Take(request.PageSize + 1); // +1 để check hasMore
        
        // ✅ Bước 5: Execute query
        var posts = await query.ToListAsync(cancellationToken);
        
        // ✅ Bước 6: Check hasMore
        var hasMore = posts.Count > request.PageSize;
        if (hasMore)
        {
            posts = posts.Take(request.PageSize).ToList();
        }
        
        // ✅ Bước 7: Map to DTOs
        var dtos = posts.Select(p => new PostDto(
            p.Id,
            p.Title,
            p.Author.UserName ?? "Unknown",
            p.AverageRating,
            p.CreatedAt,
            p.CategoryId
        )).ToList();
        
        // ✅ Bước 8: Return paged result
        var nextCursor = hasMore ? posts.Last().Id : (Guid?)null;
        
        return new CursorPagedList<PostDto>(dtos, nextCursor, hasMore);
    }
}
```

**Query Handler làm gì?**
1. **Build Query**: Tạo LINQ query
2. **Apply Filters**: Áp dụng filters, search
3. **Pagination**: Cursor-based pagination
4. **Execute**: Chạy query
5. **Map to DTO**: Chuyển entities → DTOs
6. **Return**: Trả về DTOs (KHÔNG phải entities)

---

#### **Bước 6: Repository / Data Access**

##### **Generic Repository:**

```csharp
public class GenericRepository<TEntity, TKey> : IGenericRepository<TEntity, TKey>
    where TEntity : class
{
    private readonly AppDbContext _context;
    private readonly DbSet<TEntity> _dbSet;
    
    public GenericRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<TEntity>();
    }
    
    // ✅ Add (cho Commands)
    public async Task AddAsync(TEntity entity, CancellationToken ct = default)
    {
        await _dbSet.AddAsync(entity, ct);
        await _context.SaveChangesAsync(ct); // ← Lưu ngay
    }
    
    // ✅ Update (cho Commands)
    public async Task UpdateAsync(TEntity entity, CancellationToken ct = default)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync(ct);
    }
    
    // ✅ Delete (cho Commands)
    public async Task DeleteAsync(TEntity entity, CancellationToken ct = default)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync(ct);
    }
    
    // ✅ GetQueryable (cho Queries)
    public IQueryable<TEntity> GetQueryable()
    {
        return _dbSet.AsQueryable();
    }
    
    // ✅ GetByIdAsync (cho cả Commands và Queries)
    public async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default)
    {
        return await _dbSet.FindAsync(new object[] { id }, ct);
    }
}
```

**Repository làm gì?**
- **Abstraction**: Che giấu chi tiết EF Core
- **Testability**: Dễ mock cho unit tests
- **Consistency**: API thống nhất
- **Transaction**: Quản lý SaveChangesAsync

---

#### **Bước 7: Database**

```sql
-- Command: CreatePost
BEGIN TRANSACTION;

INSERT INTO Posts (Id, Title, Content, AuthorId, CategoryId, CreatedAt, UpdatedAt)
VALUES (
    '3fa85f64-5717-4562-b3fc-2c963f66afa6',
    'My New Post',
    'This is the content',
    '7c9e6679-7425-40de-944b-e07fc1f90ae7',
    'tech',
    '2026-01-25 18:30:00',
    '2026-01-25 18:30:00'
);

COMMIT;

-- Query: GetPosts
SELECT 
    p.Id,
    p.Title,
    u.UserName AS AuthorName,
    p.AverageRating,
    p.CreatedAt,
    p.CategoryId
FROM Posts p
INNER JOIN AspNetUsers u ON p.AuthorId = u.Id
WHERE p.Id > @Cursor
ORDER BY p.Id
OFFSET 0 ROWS FETCH NEXT 11 ROWS ONLY;
```

---

#### **Bước 8: Response Pipeline (Ngược lại)**

```
Database
  ↓ (return data)
Repository
  ↓ (return entity/DTO)
Handler
  ↓ (return result)
CachingBehavior (lưu cache nếu là Query)
  ↓
LoggingBehavior (log kết quả)
  ↓
MediatR
  ↓ (return result)
Controller
  ↓ (map to HTTP response)
HTTP Response
```

**Controller trả về:**
```csharp
[HttpPost]
public async Task<IActionResult> Create([FromBody] CreatePostCommand command)
{
    var postId = await _mediator.Send(command);
    
    // ✅ Return 201 Created với Location header
    return CreatedAtAction(
        nameof(GetById), 
        new { id = postId }, 
        new { id = postId });
}
```

**HTTP Response:**
```http
HTTP/1.1 201 Created
Location: /api/posts/3fa85f64-5717-4562-b3fc-2c963f66afa6
Content-Type: application/json

{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

---

### Tóm Tắt Luồng Xử Lý

| Bước | Component | Nhiệm vụ | Command | Query |
|------|-----------|----------|---------|-------|
| 1 | ASP.NET Middleware | Authentication, Authorization | ✅ | ✅ |
| 2 | Controller | Nhận request, gọi MediatR | ✅ | ✅ |
| 3 | MediatR | Tìm Handler, build pipeline | ✅ | ✅ |
| 4a | LoggingBehavior | Log request/response | ✅ | ✅ |
| 4b | ValidationBehavior | Validate input | ✅ | ✅ |
| 4c | OwnershipBehavior | Check quyền sở hữu | ✅ | ❌ |
| 4d | CachingBehavior | Cache result | ❌ | ✅ |
| 5 | Handler | Business logic | ✅ | ✅ |
| 6 | Repository | Data access | ✅ | ✅ |
| 7 | Database | Execute SQL | ✅ | ✅ |
| 8 | Response | Return HTTP response | ✅ | ✅ |

---

### Ví Dụ Hoàn Chỉnh: Tạo Post

```csharp
// ============================================
// 1. CLIENT REQUEST
// ============================================
POST /api/posts HTTP/1.1
Authorization: Bearer eyJhbGc...
Content-Type: application/json

{
  "title": "CQRS Tutorial",
  "content": "Learn CQRS with MediatR",
  "categoryId": "tech"
}

// ============================================
// 2. CONTROLLER
// ============================================
[HttpPost]
[Authorize]
public async Task<IActionResult> Create([FromBody] CreatePostCommand command)
{
    var postId = await _mediator.Send(command);
    return CreatedAtAction(nameof(GetById), new { id = postId }, postId);
}

// ============================================
// 3. MEDIATR → PIPELINE BEHAVIORS
// ============================================

// 3a. LoggingBehavior
[INFO] Handling CreatePostCommand: { "Title": "CQRS Tutorial", ... }

// 3b. ValidationBehavior
✅ Title: "CQRS Tutorial" (valid)
✅ Content: "Learn CQRS with MediatR" (valid)

// 3c. OwnershipBehavior (skip - không phải IOwnershipRequest)

// ============================================
// 4. HANDLER
// ============================================
public async Task<Guid> Handle(CreatePostCommand request, CancellationToken ct)
{
    // 4a. Get user
    var userId = _userService.UserId; // "7c9e6679-7425-40de-944b-e07fc1f90ae7"
    
    // 4b. Create entity
    var post = new Post 
    { 
        Id = Uuid.NewDatabaseFriendly(Database.SqlServer),
        Title = "CQRS Tutorial",
        Content = "Learn CQRS with MediatR",
        AuthorId = userId,
        CategoryId = "tech",
        CreatedAt = DateTime.UtcNow
    };
    
    // 4c. Save
    await _repository.AddAsync(post);
    
    // 4d. Publish event
    await _eventPublisher.Publish(new PostCreatedEvent(post.Id, post.Title));
    
    // 4e. Return ID
    return post.Id; // "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}

// ============================================
// 5. REPOSITORY → DATABASE
// ============================================
INSERT INTO Posts (Id, Title, Content, AuthorId, CategoryId, CreatedAt)
VALUES ('3fa85f64...', 'CQRS Tutorial', 'Learn CQRS...', '7c9e6679...', 'tech', '2026-01-25...');

// ============================================
// 6. RESPONSE
// ============================================
HTTP/1.1 201 Created
Location: /api/posts/3fa85f64-5717-4562-b3fc-2c963f66afa6

{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}

// ============================================
// 7. LOGGING
// ============================================
[INFO] Handled CreatePostCommand in 45ms: "3fa85f64-5717-4562-b3fc-2c963f66afa6"
```

---

## 🎯 CQRS là gì?

### Định nghĩa

**CQRS** là một pattern tách biệt:
- **Commands**: Operations thay đổi state (Create, Update, Delete)
- **Queries**: Operations đọc data (Get, List, Search)

### Nguyên tắc cốt lõi

> **"A method should either change state or return data, but not both"**  
> — Bertrand Meyer (Command-Query Separation Principle)

### So sánh với Traditional Approach

#### ❌ Traditional (CRUD):

```csharp
public interface IPostService
{
    Task<Post> GetById(Guid id);           // Query
    Task<List<Post>> GetAll();             // Query
    Task<Post> Create(Post post);          // Command + Query (returns data)
    Task<Post> Update(Post post);          // Command + Query (returns data)
    Task Delete(Guid id);                  // Command
}

public class PostService : IPostService
{
    // Một class xử lý cả đọc và ghi
    // Khó tối ưu riêng cho từng operation
}
```

**Vấn đề**:
- 🔴 Một service xử lý cả đọc và ghi
- 🔴 Khó tối ưu riêng (read cần cache, write cần validation)
- 🔴 Model phức tạp (vừa đọc vừa ghi)
- 🔴 Khó scale riêng read và write

#### ✅ CQRS Approach:

```csharp
// COMMANDS - Chỉ thay đổi state
public record CreatePostCommand(string Title, string Content) : IRequest<Guid>;
public record UpdatePostCommand(Guid Id, string Title, string Content) : IRequest<bool>;
public record DeletePostCommand(Guid Id) : IRequest<bool>;

// QUERIES - Chỉ đọc data
public record GetPostByIdQuery(Guid Id) : IRequest<PostDto>;
public record GetPostsQuery(int PageSize) : IRequest<List<PostDto>>;

// Handlers riêng biệt
public class CreatePostHandler : IRequestHandler<CreatePostCommand, Guid> { }
public class GetPostsHandler : IRequestHandler<GetPostsQuery, List<PostDto>> { }
```

**Lợi ích**:
- ✅ Tách biệt rõ ràng read và write
- ✅ Tối ưu riêng cho từng operation
- ✅ Models đơn giản hơn
- ✅ Dễ scale

---

## 💡 Tại sao cần CQRS?

### 1. **Tối ưu hóa Performance**

#### Vấn đề với CRUD:

```csharp
// ❌ Một model cho cả đọc và ghi
public class Post
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public Guid AuthorId { get; set; }
    public AppUser Author { get; set; }        // Eager loading
    public List<Comment> Comments { get; set; } // Eager loading
    public List<Tag> Tags { get; set; }        // Eager loading
    
    // Khi CREATE: không cần Author, Comments, Tags
    // Khi READ: cần tất cả
    // → Không tối ưu!
}
```

#### Giải pháp với CQRS:

```csharp
// ✅ Command Model - Chỉ cần data để ghi
public record CreatePostCommand(string Title, string Content);

// ✅ Query Model - Chỉ trả về data cần thiết
public record PostDto(
    Guid Id,
    string Title,
    string AuthorName,      // Flat, denormalized
    int CommentCount,       // Aggregated
    List<string> TagNames   // Simplified
);
```

### 2. **Scalability**

```
┌─────────────────────────────────────┐
│         Traditional CRUD            │
│                                     │
│  Read: 90% traffic                 │
│  Write: 10% traffic                │
│                                     │
│  → Cả hai dùng chung resources     │
│  → Không scale riêng được          │
└─────────────────────────────────────┘

┌─────────────────────────────────────┐
│              CQRS                   │
│                                     │
│  ┌──────────┐      ┌──────────┐   │
│  │  Queries │      │ Commands │   │
│  │  (90%)   │      │  (10%)   │   │
│  │          │      │          │   │
│  │ Read DB  │      │ Write DB │   │
│  │ (Cached) │      │ (Master) │   │
│  └──────────┘      └──────────┘   │
│                                     │
│  → Scale riêng cho read và write   │
└─────────────────────────────────────┘
```

### 3. **Separation of Concerns**

```csharp
// ✅ Command Handler - Focus on business logic
public class CreatePostHandler
{
    public async Task<Guid> Handle(CreatePostCommand request)
    {
        // Validation
        if (string.IsNullOrEmpty(request.Title))
            throw new ValidationException();
        
        // Business logic
        var post = new Post { /* ... */ };
        
        // Persistence
        await _repository.AddAsync(post);
        
        // Side effects
        await _eventPublisher.Publish(new PostCreatedEvent(post.Id));
        
        return post.Id;
    }
}

// ✅ Query Handler - Focus on data retrieval
public class GetPostsHandler
{
    public async Task<List<PostDto>> Handle(GetPostsQuery request)
    {
        // Optimized query with Dapper
        var sql = "SELECT Id, Title, AuthorName FROM PostsView";
        return await _connection.QueryAsync<PostDto>(sql);
    }
}
```

### 4. **Flexibility**

```csharp
// Commands có thể dùng EF Core
public class CreatePostHandler
{
    private readonly AppDbContext _context;
    
    public async Task Handle(CreatePostCommand request)
    {
        _context.Posts.Add(new Post { /* ... */ });
        await _context.SaveChangesAsync();
    }
}

// Queries có thể dùng Dapper (nhanh hơn)
public class GetPostsHandler
{
    private readonly IDbConnection _connection;
    
    public async Task<List<PostDto>> Handle(GetPostsQuery request)
    {
        return await _connection.QueryAsync<PostDto>(
            "SELECT * FROM PostsView WHERE ...");
    }
}

// Hoặc thậm chí dùng database khác
public class GetPostsHandler
{
    private readonly IElasticClient _elasticClient; // Elasticsearch
    
    public async Task<List<PostDto>> Handle(GetPostsQuery request)
    {
        return await _elasticClient.SearchAsync<PostDto>(/* ... */);
    }
}
```

---

## 🔍 Command vs Query

### Command (Write Operations)

**Đặc điểm**:
- ✅ Thay đổi state của hệ thống
- ✅ Không trả về data (hoặc chỉ trả về ID)
- ✅ Có side effects
- ✅ Cần validation nghiêm ngặt
- ✅ Có thể trigger events

**Ví dụ**:
```csharp
// Command
public record CreatePostCommand(string Title, string Content) : IRequest<Guid>;

// Handler
public class CreatePostHandler : IRequestHandler<CreatePostCommand, Guid>
{
    private readonly IPostRepository _repository;
    private readonly IEventPublisher _eventPublisher;
    
    public async Task<Guid> Handle(CreatePostCommand request, CancellationToken ct)
    {
        // 1. Validation
        if (string.IsNullOrEmpty(request.Title))
            throw new ValidationException("Title is required");
        
        // 2. Create entity
        var post = new Post 
        { 
            Id = Guid.NewGuid(),
            Title = request.Title,
            Content = request.Content,
            CreatedAt = DateTime.UtcNow
        };
        
        // 3. Persist
        await _repository.AddAsync(post);
        
        // 4. Publish event (side effect)
        await _eventPublisher.Publish(new PostCreatedEvent(post.Id));
        
        // 5. Return only ID (not full entity)
        return post.Id;
    }
}
```

**Naming Convention**:
- `CreateXxxCommand`
- `UpdateXxxCommand`
- `DeleteXxxCommand`
- `PublishXxxCommand`
- `ArchiveXxxCommand`

### Query (Read Operations)

**Đặc điểm**:
- ✅ Chỉ đọc data
- ✅ Không thay đổi state
- ✅ Không có side effects
- ✅ Có thể cache
- ✅ Trả về DTOs (không phải entities)

**Ví dụ**:
```csharp
// Query
public record GetPostsQuery(int PageSize = 10) : IRequest<List<PostDto>>;

// Handler
public class GetPostsHandler : IRequestHandler<GetPostsQuery, List<PostDto>>
{
    private readonly IPostQueryService _queryService;
    private readonly IMemoryCache _cache;
    
    public async Task<List<PostDto>> Handle(GetPostsQuery request, CancellationToken ct)
    {
        // 1. Check cache
        var cacheKey = $"posts_{request.PageSize}";
        if (_cache.TryGetValue(cacheKey, out List<PostDto> cachedPosts))
            return cachedPosts;
        
        // 2. Query database (optimized)
        var posts = await _queryService.GetPostsAsync(request.PageSize);
        
        // 3. Cache result
        _cache.Set(cacheKey, posts, TimeSpan.FromMinutes(5));
        
        // 4. Return DTOs
        return posts;
    }
}
```

**Naming Convention**:
- `GetXxxQuery`
- `GetXxxByIdQuery`
- `ListXxxQuery`
- `SearchXxxQuery`
- `FindXxxQuery`

### So sánh Command vs Query

| Aspect | Command | Query |
|--------|---------|-------|
| **Purpose** | Thay đổi state | Đọc data |
| **Return** | void hoặc ID | DTOs |
| **Side Effects** | Có | Không |
| **Validation** | Nghiêm ngặt | Minimal |
| **Caching** | Không | Có thể |
| **Events** | Có thể publish | Không |
| **Database** | Write DB | Read DB (có thể khác) |
| **Optimization** | Transaction, locking | Indexing, caching |

---

## 🏗️ Kiến trúc CQRS

### 1. Simple CQRS (Shared Database)

```
┌─────────────────────────────────────────────────────┐
│                  Presentation                        │
│                  (Controllers)                       │
└──────────────┬──────────────────┬───────────────────┘
               │                  │
               ↓                  ↓
┌──────────────────────┐  ┌──────────────────────┐
│      Commands        │  │       Queries        │
│  (Write Operations)  │  │  (Read Operations)   │
└──────────┬───────────┘  └───────────┬──────────┘
           │                          │
           ↓                          ↓
┌──────────────────────┐  ┌──────────────────────┐
│   Command Handlers   │  │   Query Handlers     │
│  - Validation        │  │  - Optimized reads   │
│  - Business logic    │  │  - Caching           │
│  - Events            │  │  - Projections       │
└──────────┬───────────┘  └───────────┬──────────┘
           │                          │
           ↓                          ↓
┌──────────────────────────────────────────────────┐
│              Shared Database                      │
│  - Posts table                                    │
│  - Products table                                 │
│  - Users table                                    │
└──────────────────────────────────────────────────┘
```

**Đặc điểm**:
- ✅ Đơn giản, dễ implement
- ✅ Một database cho cả read và write
- ✅ Phù hợp cho hầu hết ứng dụng
- ⚠️ Không scale riêng read và write

**Ví dụ**:
```csharp
// Command Handler - Ghi vào database
public class CreatePostHandler
{
    private readonly AppDbContext _context;
    
    public async Task<Guid> Handle(CreatePostCommand request)
    {
        var post = new Post { /* ... */ };
        _context.Posts.Add(post);
        await _context.SaveChangesAsync();
        return post.Id;
    }
}

// Query Handler - Đọc từ cùng database
public class GetPostsHandler
{
    private readonly AppDbContext _context;
    
    public async Task<List<PostDto>> Handle(GetPostsQuery request)
    {
        return await _context.Posts
            .Select(p => new PostDto(p.Id, p.Title, p.AuthorName))
            .ToListAsync();
    }
}
```

### 2. CQRS with Read Models

```
┌─────────────────────────────────────────────────────┐
│                  Presentation                        │
└──────────────┬──────────────────┬───────────────────┘
               │                  │
               ↓                  ↓
┌──────────────────────┐  ┌──────────────────────┐
│      Commands        │  │       Queries        │
└──────────┬───────────┘  └───────────┬──────────┘
           │                          │
           ↓                          ↓
┌──────────────────────┐  ┌──────────────────────┐
│   Command Handlers   │  │   Query Handlers     │
└──────────┬───────────┘  └───────────┬──────────┘
           │                          │
           ↓                          ↓
┌──────────────────┐      ┌──────────────────────┐
│   Write Model    │      │    Read Models       │
│  (Normalized)    │      │  (Denormalized)      │
│                  │      │                      │
│  Posts           │      │  PostsView           │
│  Users           │      │  PostDetailsView     │
│  Comments        │      │  PostSummaryView     │
└────────┬─────────┘      └──────────────────────┘
         │                         ↑
         │  Sync/Update            │
         └─────────────────────────┘
```

**Đặc điểm**:
- ✅ Write model normalized (3NF)
- ✅ Read models denormalized (optimized cho queries)
- ✅ Sync giữa write và read models
- ✅ Queries cực nhanh

**Ví dụ**:
```csharp
// Write Model (Normalized)
public class Post
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public Guid AuthorId { get; set; }
    public AppUser Author { get; set; }
}

// Read Model (Denormalized)
public class PostView
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string AuthorName { get; set; }      // Denormalized
    public string AuthorEmail { get; set; }     // Denormalized
    public int CommentCount { get; set; }       // Aggregated
    public double AverageRating { get; set; }   // Aggregated
}

// Command Handler - Ghi vào Write Model
public class CreatePostHandler
{
    public async Task<Guid> Handle(CreatePostCommand request)
    {
        var post = new Post { /* ... */ };
        await _writeRepository.AddAsync(post);
        
        // Update Read Model
        await UpdateReadModel(post);
        
        return post.Id;
    }
    
    private async Task UpdateReadModel(Post post)
    {
        var postView = new PostView
        {
            Id = post.Id,
            Title = post.Title,
            AuthorName = post.Author.FullName,
            AuthorEmail = post.Author.Email,
            CommentCount = 0,
            AverageRating = 0
        };
        
        await _readRepository.UpsertAsync(postView);
    }
}

// Query Handler - Đọc từ Read Model
public class GetPostsHandler
{
    public async Task<List<PostDto>> Handle(GetPostsQuery request)
    {
        // Query từ denormalized view - cực nhanh
        return await _readRepository.GetAllAsync();
    }
}
```

### 3. Event-Sourced CQRS (Advanced)

```
┌─────────────────────────────────────────────────────┐
│                  Presentation                        │
└──────────────┬──────────────────┬───────────────────┘
               │                  │
               ↓                  ↓
┌──────────────────────┐  ┌──────────────────────┐
│      Commands        │  │       Queries        │
└──────────┬───────────┘  └───────────┬──────────┘
           │                          │
           ↓                          ↓
┌──────────────────────┐  ┌──────────────────────┐
│   Command Handlers   │  │   Query Handlers     │
│  - Publish Events    │  │  - Read from Views   │
└──────────┬───────────┘  └───────────┬──────────┘
           │                          │
           ↓                          │
┌──────────────────────┐             │
│    Event Store       │             │
│  - PostCreated       │             │
│  - PostUpdated       │             │
│  - PostDeleted       │             │
└──────────┬───────────┘             │
           │                         │
           │  Replay Events          │
           ↓                         │
┌──────────────────────┐             │
│  Event Handlers      │             │
│  - Update Read Model │             │
└──────────┬───────────┘             │
           │                         │
           ↓                         ↓
┌──────────────────────────────────────────────────┐
│              Read Models (Projections)            │
│  - PostsView                                      │
│  - PostDetailsView                                │
└──────────────────────────────────────────────────┘
```

**Đặc điểm**:
- ✅ Không lưu state, chỉ lưu events
- ✅ Có thể replay events để rebuild state
- ✅ Audit trail hoàn hảo
- ✅ Time travel (xem state tại thời điểm bất kỳ)
- ⚠️ Phức tạp, chỉ dùng khi thực sự cần

---

## 🔧 Implementation với MediatR

### 1. Setup MediatR

```bash
dotnet add package MediatR
dotnet add package MediatR.Extensions.Microsoft.DependencyInjection
```

```csharp
// Program.cs
builder.Services.AddMediatR(cfg => 
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
```

### 2. Tạo Command

```csharp
// Command Definition
public record CreatePostCommand(string Title, string Content, string? CategoryId) 
    : IRequest<Guid>;

// Command Handler
public class CreatePostHandler : IRequestHandler<CreatePostCommand, Guid>
{
    private readonly IPostRepository _repository;
    private readonly ICurrentUserService _userService;
    
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
            AuthorId = userId,
            CategoryId = request.CategoryId,
            CreatedAt = DateTime.UtcNow
        };
        
        await _repository.AddAsync(post);
        
        return post.Id;
    }
}
```

### 3. Tạo Query

```csharp
// Query Definition
public record GetPostsQuery(int PageSize = 10) : IRequest<List<PostDto>>;

// Query Handler
public class GetPostsHandler : IRequestHandler<GetPostsQuery, List<PostDto>>
{
    private readonly IPostQueryService _queryService;
    
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

### 4. Sử dụng trong Controller

```csharp
[ApiController]
[Route("api/[controller]")]
public class PostsController : ControllerBase
{
    private readonly IMediator _mediator;
    
    public PostsController(IMediator mediator) => _mediator = mediator;
    
    // Command endpoint
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreatePostCommand command)
    {
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }
    
    // Query endpoint
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int pageSize = 10)
    {
        var query = new GetPostsQuery(pageSize);
        var result = await _mediator.Send(query);
        return Ok(result);
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

### 5. Pipeline Behaviors

MediatR cho phép thêm behaviors vào pipeline:

```csharp
// Validation Behavior
public class ValidationBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    
    public async Task<TResponse> Handle(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken ct)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);
            var results = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, ct)));
            
            var failures = results
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .ToList();
            
            if (failures.Any())
                throw new ValidationException(failures);
        }
        
        return await next();
    }
}

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

// Caching Behavior (chỉ cho Queries)
public class CachingBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IMemoryCache _cache;
    
    public async Task<TResponse> Handle(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken ct)
    {
        // Chỉ cache queries
        if (request is not IQuery)
            return await next();
        
        var cacheKey = $"{typeof(TRequest).Name}_{request.GetHashCode()}";
        
        if (_cache.TryGetValue(cacheKey, out TResponse cachedResponse))
            return cachedResponse;
        
        var response = await next();
        
        _cache.Set(cacheKey, response, TimeSpan.FromMinutes(5));
        
        return response;
    }
}

// Register behaviors
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
});
```

---

## 📝 Ví dụ thực tế từ BlogApi

### Ví dụ 1: Quản lý Posts

#### Commands:

```csharp
// 1. Create Post Command
public record CreatePostCommand(string Title, string Content, string? CategoryId) 
    : IRequest<Guid>;

public class CreatePostHandler : IRequestHandler<CreatePostCommand, Guid>
{
    private readonly IPostRepository _repository;
    private readonly ICurrentUserService _userService;
    private readonly IEventPublisher _eventPublisher;
    
    public async Task<Guid> Handle(CreatePostCommand request, CancellationToken ct)
    {
        var userId = _userService.UserId ?? throw new UnauthorizedAccessException();
        
        var post = new Post 
        { 
            Id = Uuid.NewDatabaseFriendly(Database.SqlServer),
            Title = request.Title,
            Content = request.Content,
            AuthorId = userId,
            CategoryId = request.CategoryId,
            CreatedAt = DateTime.UtcNow
        };
        
        await _repository.AddAsync(post);
        
        // Publish event
        await _eventPublisher.Publish(new PostCreatedEvent(post.Id, post.Title));
        
        return post.Id;
    }
}

// 2. Update Post Command
public record UpdatePostCommand(Guid Id, string Title, string Content, string? CategoryId) 
    : IRequest<bool>, IOwnershipRequest;

public class UpdatePostHandler : IRequestHandler<UpdatePostCommand, bool>
{
    private readonly IPostRepository _repository;
    
    public async Task<bool> Handle(UpdatePostCommand request, CancellationToken ct)
    {
        var post = await _repository.GetByIdAsync(request.Id);
        if (post == null) return false;
        
        post.Title = request.Title;
        post.Content = request.Content;
        post.CategoryId = request.CategoryId;
        post.UpdatedAt = DateTime.UtcNow;
        
        await _repository.UpdateAsync(post);
        
        return true;
    }
}

// 3. Delete Post Command
public record DeletePostCommand(Guid Id) : IRequest<bool>, IOwnershipRequest;

public class DeletePostHandler : IRequestHandler<DeletePostCommand, bool>
{
    private readonly IPostRepository _repository;
    
    public async Task<bool> Handle(DeletePostCommand request, CancellationToken ct)
    {
        var post = await _repository.GetByIdAsync(request.Id);
        if (post == null) return false;
        
        await _repository.DeleteAsync(post);
        
        return true;
    }
}

// 4. Rate Post Command
public record RatePostCommand(Guid PostId, int Score) : IRequest<bool>;

public class RatePostHandler : IRequestHandler<RatePostCommand, bool>
{
    private readonly IPostRepository _repository;
    
    public async Task<bool> Handle(RatePostCommand request, CancellationToken ct)
    {
        var post = await _repository.GetByIdAsync(request.PostId);
        if (post == null) return false;
        
        // Business logic trong Domain
        post.AddRating(request.Score);
        
        await _repository.UpdateAsync(post);
        
        return true;
    }
}
```

#### Queries:

```csharp
// 1. Get Posts Query (với pagination)
public record GetPostsQuery(Guid? Cursor = null, int PageSize = 10) 
    : IRequest<CursorPagedList<PostDto>>;

public class GetPostsHandler : IRequestHandler<GetPostsQuery, CursorPagedList<PostDto>>
{
    private readonly IPostQueryService _queryService;
    
    public async Task<CursorPagedList<PostDto>> Handle(GetPostsQuery request, CancellationToken ct)
    {
        // Sử dụng Dapper cho performance
        return await _queryService.GetPostsAsync(request.Cursor, request.PageSize);
    }
}

// 2. Get Post Detail Query
[Cacheable(ExpirationMinutes = 5)]
public record GetPostDetailQuery(Guid Id) : IRequest<PostDetailDto>;

public class GetPostDetailHandler : IRequestHandler<GetPostDetailQuery, PostDetailDto>
{
    private readonly IPostRepository _repository;
    
    public async Task<PostDetailDto> Handle(GetPostDetailQuery request, CancellationToken ct)
    {
        var post = await _repository.GetByIdAsync(request.Id);
        if (post == null) return null!;
        
        return new PostDetailDto(
            post.Id,
            post.Title,
            post.Content,
            post.AverageRating,
            post.TotalRatings,
            post.CategoryId
        );
    }
}
```

#### DTOs:

```csharp
// DTO cho list view
public record PostDto(
    Guid Id,
    string Title,
    string AuthorName,
    double AverageRating,
    DateTime CreatedAt,
    string? CategoryId
);

// DTO cho detail view
public record PostDetailDto(
    Guid Id,
    string Title,
    string Content,
    double AverageRating,
    int TotalRatings,
    string? CategoryId
);
```

### Ví dụ 2: Shopping Cart

#### Commands:

```csharp
// 1. Add to Cart Command
public record AddToCartCommand(Guid ProductId, int Quantity) : IRequest<bool>;

public class AddToCartHandler : IRequestHandler<AddToCartCommand, bool>
{
    private readonly IGenericRepository<Cart, Guid> _cartRepository;
    private readonly IGenericRepository<CartItem, Guid> _cartItemRepository;
    private readonly ICurrentUserService _userService;
    
    public async Task<bool> Handle(AddToCartCommand request, CancellationToken ct)
    {
        var userId = _userService.UserId ?? throw new UnauthorizedAccessException();
        
        // Get or create cart
        var cart = await GetOrCreateCart(userId);
        
        // Check if item exists
        var existingItem = cart.Items
            .FirstOrDefault(i => i.ProductId == request.ProductId);
        
        if (existingItem != null)
        {
            // Update quantity
            existingItem.Quantity += request.Quantity;
            
            if (existingItem.Quantity <= 0)
            {
                // Remove if quantity <= 0
                await _cartItemRepository.DeleteAsync(existingItem);
            }
            else
            {
                await _cartItemRepository.UpdateAsync(existingItem);
            }
        }
        else if (request.Quantity > 0)
        {
            // Add new item
            var newItem = new CartItem
            {
                Id = Guid.NewGuid(),
                CartId = cart.Id,
                ProductId = request.ProductId,
                Quantity = request.Quantity
            };
            
            await _cartItemRepository.AddAsync(newItem);
        }
        
        return true;
    }
}

// 2. Update Cart Item Quantity Command
public record UpdateCartItemQuantityCommand(Guid ProductId, int Quantity) : IRequest<bool>;

public class UpdateCartItemQuantityHandler 
    : IRequestHandler<UpdateCartItemQuantityCommand, bool>
{
    public async Task<bool> Handle(UpdateCartItemQuantityCommand request, CancellationToken ct)
    {
        var userId = _userService.UserId ?? throw new UnauthorizedAccessException();
        var cart = await GetCart(userId);
        
        var item = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
        if (item == null) return false;
        
        if (request.Quantity <= 0)
        {
            await _cartItemRepository.DeleteAsync(item);
        }
        else
        {
            item.Quantity = request.Quantity;
            await _cartItemRepository.UpdateAsync(item);
        }
        
        return true;
    }
}

// 3. Remove from Cart Command
public record RemoveFromCartCommand(Guid ProductId) : IRequest<bool>;

public class RemoveFromCartHandler : IRequestHandler<RemoveFromCartCommand, bool>
{
    public async Task<bool> Handle(RemoveFromCartCommand request, CancellationToken ct)
    {
        var userId = _userService.UserId ?? throw new UnauthorizedAccessException();
        var cart = await GetCart(userId);
        
        var item = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
        if (item == null) return false;
        
        await _cartItemRepository.DeleteAsync(item);
        
        return true;
    }
}
```

#### Queries:

```csharp
// Get Cart Query
public record GetCartQuery : IRequest<CartDto>;

public class GetCartHandler : IRequestHandler<GetCartQuery, CartDto>
{
    private readonly IGenericRepository<Cart, Guid> _repository;
    private readonly ICurrentUserService _userService;
    
    public async Task<CartDto> Handle(GetCartQuery request, CancellationToken ct)
    {
        var userId = _userService.UserId ?? throw new UnauthorizedAccessException();
        
        var cart = await _repository.GetQueryable()
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId, ct);
        
        if (cart == null)
            return new CartDto(new List<CartItemDto>(), 0);
        
        var items = cart.Items.Select(i => new CartItemDto(
            i.ProductId,
            i.Product.Name,
            i.Product.Price,
            i.Quantity,
            i.Product.Price * i.Quantity
        )).ToList();
        
        var total = items.Sum(i => i.Subtotal);
        
        return new CartDto(items, total);
    }
}
```

---

## 🎭 CQRS Patterns

### 1. Command Pattern Variations

#### a) Simple Command (void return)

```csharp
public record DeletePostCommand(Guid Id) : IRequest;

public class DeletePostHandler : IRequestHandler<DeletePostCommand>
{
    public async Task Handle(DeletePostCommand request, CancellationToken ct)
    {
        await _repository.DeleteAsync(request.Id);
        // No return value
    }
}
```

#### b) Command with Result

```csharp
public record CreatePostCommand(string Title, string Content) : IRequest<Guid>;

public class CreatePostHandler : IRequestHandler<CreatePostCommand, Guid>
{
    public async Task<Guid> Handle(CreatePostCommand request, CancellationToken ct)
    {
        var post = new Post { /* ... */ };
        await _repository.AddAsync(post);
        return post.Id; // Return ID
    }
}
```

#### c) Command with Result Object

```csharp
public record CreateOrderCommand(List<OrderItem> Items) : IRequest<CreateOrderResult>;

public record CreateOrderResult(Guid OrderId, decimal TotalAmount, string OrderNumber);

public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, CreateOrderResult>
{
    public async Task<CreateOrderResult> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        var order = new Order { /* ... */ };
        await _repository.AddAsync(order);
        
        return new CreateOrderResult(
            order.Id,
            order.TotalAmount,
            order.OrderNumber
        );
    }
}
```

### 2. Query Pattern Variations

#### a) Simple Query

```csharp
public record GetPostByIdQuery(Guid Id) : IRequest<PostDto>;
```

#### b) Query with Parameters

```csharp
public record GetPostsQuery(
    int PageSize = 10,
    Guid? Cursor = null,
    string? CategoryId = null,
    string? SearchTerm = null
) : IRequest<CursorPagedList<PostDto>>;
```

#### c) Query with Filtering

```csharp
public record SearchPostsQuery : IRequest<List<PostDto>>
{
    public string? SearchTerm { get; init; }
    public string? CategoryId { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public int? MinRating { get; init; }
}
```

### 3. Composite Commands

```csharp
// Composite command that executes multiple operations
public record PublishPostCommand(Guid PostId) : IRequest<bool>;

public class PublishPostHandler : IRequestHandler<PublishPostCommand, bool>
{
    private readonly IMediator _mediator;
    
    public async Task<bool> Handle(PublishPostCommand request, CancellationToken ct)
    {
        // 1. Update post status
        await _mediator.Send(new UpdatePostStatusCommand(request.PostId, "Published"));
        
        // 2. Send notifications
        await _mediator.Send(new SendPostPublishedNotificationCommand(request.PostId));
        
        // 3. Update search index
        await _mediator.Send(new UpdateSearchIndexCommand(request.PostId));
        
        return true;
    }
}
```

---

## ✅ Best Practices

### 1. Naming Conventions

```csharp
// ✅ ĐÚNG - Rõ ràng về intent
CreatePostCommand
UpdatePostCommand
DeletePostCommand
PublishPostCommand

GetPostsQuery
GetPostByIdQuery
SearchPostsQuery

// ❌ SAI - Không rõ ràng
PostCommand
ManagePostCommand
PostQuery
```

### 2. Command/Query Separation

```csharp
// ✅ ĐÚNG - Command không trả về data
public record CreatePostCommand(string Title) : IRequest<Guid>; // Chỉ trả ID

// ❌ SAI - Command trả về full entity
public record CreatePostCommand(string Title) : IRequest<Post>; // Trả full entity
```

### 3. DTOs cho Queries

```csharp
// ✅ ĐÚNG - Query trả về DTO
public record GetPostsQuery : IRequest<List<PostDto>>;

public record PostDto(Guid Id, string Title, string AuthorName);

// ❌ SAI - Query trả về Entity
public record GetPostsQuery : IRequest<List<Post>>; // Entity có navigation properties
```

### 4. Validation

```csharp
// ✅ ĐÚNG - Validation trong Validator
public class CreatePostCommandValidator : AbstractValidator<CreatePostCommand>
{
    public CreatePostCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200);
            
        RuleFor(x => x.Content)
            .NotEmpty()
            .MinimumLength(10);
    }
}

// ❌ SAI - Validation trong Handler
public class CreatePostHandler
{
    public async Task<Guid> Handle(CreatePostCommand request)
    {
        if (string.IsNullOrEmpty(request.Title)) // ❌
            throw new Exception("Title required");
        // ...
    }
}
```

### 5. Single Responsibility

```csharp
// ✅ ĐÚNG - Mỗi handler một responsibility
public class CreatePostHandler : IRequestHandler<CreatePostCommand, Guid>
{
    public async Task<Guid> Handle(CreatePostCommand request)
    {
        // Chỉ tạo post
        var post = new Post { /* ... */ };
        await _repository.AddAsync(post);
        return post.Id;
    }
}

// ❌ SAI - Handler làm quá nhiều việc
public class CreatePostHandler
{
    public async Task<Guid> Handle(CreatePostCommand request)
    {
        // Tạo post
        var post = new Post { /* ... */ };
        await _repository.AddAsync(post);
        
        // Send email ❌
        await _emailService.SendEmail(/* ... */);
        
        // Update cache ❌
        _cache.Remove("posts");
        
        // Log ❌
        _logger.Log("Post created");
        
        return post.Id;
    }
}

// ✅ ĐÚNG - Dùng events hoặc behaviors
public class CreatePostHandler
{
    public async Task<Guid> Handle(CreatePostCommand request)
    {
        var post = new Post { /* ... */ };
        await _repository.AddAsync(post);
        
        // Publish event - other handlers will react
        await _eventPublisher.Publish(new PostCreatedEvent(post.Id));
        
        return post.Id;
    }
}
```

### 6. Immutability

```csharp
// ✅ ĐÚNG - Commands và Queries immutable (records)
public record CreatePostCommand(string Title, string Content) : IRequest<Guid>;

// ❌ SAI - Mutable class
public class CreatePostCommand : IRequest<Guid>
{
    public string Title { get; set; } // Mutable
    public string Content { get; set; } // Mutable
}
```

---

## ❌ Common Mistakes

### 1. Returning Entities from Commands

```csharp
// ❌ SAI
public record CreatePostCommand(string Title) : IRequest<Post>;

public class CreatePostHandler : IRequestHandler<CreatePostCommand, Post>
{
    public async Task<Post> Handle(CreatePostCommand request)
    {
        var post = new Post { /* ... */ };
        await _repository.AddAsync(post);
        return post; // ❌ Trả về entity
    }
}

// ✅ ĐÚNG
public record CreatePostCommand(string Title) : IRequest<Guid>;

public class CreatePostHandler : IRequestHandler<CreatePostCommand, Guid>
{
    public async Task<Guid> Handle(CreatePostCommand request)
    {
        var post = new Post { /* ... */ };
        await _repository.AddAsync(post);
        return post.Id; // ✅ Chỉ trả về ID
    }
}
```

### 2. Queries Modifying State

```csharp
// ❌ SAI - Query thay đổi state
public class GetPostsHandler : IRequestHandler<GetPostsQuery, List<PostDto>>
{
    public async Task<List<PostDto>> Handle(GetPostsQuery request)
    {
        // ❌ Logging view count
        await _analyticsService.IncrementViewCount();
        
        return await _repository.GetAllAsync();
    }
}

// ✅ ĐÚNG - Query chỉ đọc
public class GetPostsHandler : IRequestHandler<GetPostsQuery, List<PostDto>>
{
    public async Task<List<PostDto>> Handle(GetPostsQuery request)
    {
        // ✅ Chỉ đọc data
        return await _repository.GetAllAsync();
    }
}
```

### 3. Business Logic trong Controllers

```csharp
// ❌ SAI
[HttpPost]
public async Task<IActionResult> Create(CreatePostDto dto)
{
    // ❌ Business logic trong controller
    if (string.IsNullOrEmpty(dto.Title))
        return BadRequest();
    
    var post = new Post { Title = dto.Title };
    await _repository.AddAsync(post);
    
    return Ok();
}

// ✅ ĐÚNG
[HttpPost]
public async Task<IActionResult> Create(CreatePostCommand command)
{
    // ✅ Delegate to handler
    var id = await _mediator.Send(command);
    return Ok(id);
}
```

### 4. Fat Commands/Queries

```csharp
// ❌ SAI - Quá nhiều parameters
public record CreatePostCommand(
    string Title,
    string Content,
    string CategoryId,
    List<string> Tags,
    bool IsPublished,
    DateTime? PublishDate,
    string MetaTitle,
    string MetaDescription,
    string MetaKeywords,
    bool AllowComments,
    bool IsFeatured
) : IRequest<Guid>;

// ✅ ĐÚNG - Nhóm related data
public record CreatePostCommand(
    string Title,
    string Content,
    string CategoryId,
    PostMetadata Metadata,
    PostSettings Settings
) : IRequest<Guid>;

public record PostMetadata(string Title, string Description, string Keywords);
public record PostSettings(bool IsPublished, bool AllowComments, bool IsFeatured);
```

---

## 🧪 Testing CQRS

### 1. Unit Testing Commands

```csharp
public class CreatePostHandlerTests
{
    [Fact]
    public async Task Handle_ValidCommand_ShouldCreatePost()
    {
        // Arrange
        var mockRepo = new Mock<IPostRepository>();
        var mockUserService = new Mock<ICurrentUserService>();
        mockUserService.Setup(x => x.UserId).Returns(Guid.NewGuid());
        
        var handler = new CreatePostHandler(mockRepo.Object, mockUserService.Object);
        var command = new CreatePostCommand("Test Title", "Test Content");
        
        // Act
        var result = await handler.Handle(command, CancellationToken.None);
        
        // Assert
        Assert.NotEqual(Guid.Empty, result);
        mockRepo.Verify(x => x.AddAsync(It.IsAny<Post>()), Times.Once);
    }
    
    [Fact]
    public async Task Handle_UnauthorizedUser_ShouldThrow()
    {
        // Arrange
        var mockRepo = new Mock<IPostRepository>();
        var mockUserService = new Mock<ICurrentUserService>();
        mockUserService.Setup(x => x.UserId).Returns((Guid?)null);
        
        var handler = new CreatePostHandler(mockRepo.Object, mockUserService.Object);
        var command = new CreatePostCommand("Test Title", "Test Content");
        
        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));
    }
}
```

### 2. Unit Testing Queries

```csharp
public class GetPostsHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnPosts()
    {
        // Arrange
        var mockQueryService = new Mock<IPostQueryService>();
        var expectedPosts = new List<PostDto>
        {
            new PostDto(Guid.NewGuid(), "Title 1", "Author 1", 4.5, DateTime.UtcNow, null),
            new PostDto(Guid.NewGuid(), "Title 2", "Author 2", 3.5, DateTime.UtcNow, null)
        };
        
        mockQueryService
            .Setup(x => x.GetPostsAsync(It.IsAny<int>()))
            .ReturnsAsync(expectedPosts);
        
        var handler = new GetPostsHandler(mockQueryService.Object);
        var query = new GetPostsQuery(10);
        
        // Act
        var result = await handler.Handle(query, CancellationToken.None);
        
        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Title 1", result[0].Title);
    }
}
```

### 3. Integration Testing

```csharp
public class PostsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    
    public PostsControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }
    
    [Fact]
    public async Task CreatePost_ValidCommand_ShouldReturn201()
    {
        // Arrange
        var command = new { Title = "Test Post", Content = "Test Content" };
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/posts", command);
        
        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        
        var postId = await response.Content.ReadFromJsonAsync<Guid>();
        Assert.NotEqual(Guid.Empty, postId);
    }
    
    [Fact]
    public async Task GetPosts_ShouldReturnPosts()
    {
        // Act
        var response = await _client.GetAsync("/api/posts?pageSize=10");
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var posts = await response.Content.ReadFromJsonAsync<List<PostDto>>();
        Assert.NotNull(posts);
    }
}
```

---

## 🚀 Advanced Topics

### 1. Event-Driven CQRS

```csharp
// Domain Event
public record PostCreatedEvent(Guid PostId, string Title);

// Command Handler publishes event
public class CreatePostHandler : IRequestHandler<CreatePostCommand, Guid>
{
    private readonly IEventPublisher _eventPublisher;
    
    public async Task<Guid> Handle(CreatePostCommand request, CancellationToken ct)
    {
        var post = new Post { /* ... */ };
        await _repository.AddAsync(post);
        
        // Publish event
        await _eventPublisher.Publish(new PostCreatedEvent(post.Id, post.Title));
        
        return post.Id;
    }
}

// Event Handlers react to events
public class SendEmailOnPostCreatedHandler : INotificationHandler<PostCreatedEvent>
{
    public async Task Handle(PostCreatedEvent notification, CancellationToken ct)
    {
        await _emailService.SendNewPostNotification(notification.PostId);
    }
}

public class UpdateSearchIndexHandler : INotificationHandler<PostCreatedEvent>
{
    public async Task Handle(PostCreatedEvent notification, CancellationToken ct)
    {
        await _searchService.IndexPost(notification.PostId);
    }
}
```

### 2. Optimistic Concurrency

```csharp
public class Post
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public byte[] RowVersion { get; set; } // Concurrency token
}

public class UpdatePostHandler : IRequestHandler<UpdatePostCommand, bool>
{
    public async Task<bool> Handle(UpdatePostCommand request, CancellationToken ct)
    {
        try
        {
            var post = await _repository.GetByIdAsync(request.Id);
            post.Title = request.Title;
            
            await _repository.UpdateAsync(post);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            // Handle concurrency conflict
            throw new ConcurrencyException("Post was modified by another user");
        }
    }
}
```

### 3. Saga Pattern

```csharp
// Saga orchestrates multiple commands
public class CreateOrderSaga
{
    private readonly IMediator _mediator;
    
    public async Task<Guid> Execute(CreateOrderCommand command)
    {
        try
        {
            // 1. Create order
            var orderId = await _mediator.Send(command);
            
            // 2. Reserve inventory
            await _mediator.Send(new ReserveInventoryCommand(orderId));
            
            // 3. Process payment
            await _mediator.Send(new ProcessPaymentCommand(orderId));
            
            // 4. Send confirmation
            await _mediator.Send(new SendOrderConfirmationCommand(orderId));
            
            return orderId;
        }
        catch (Exception)
        {
            // Compensating transactions
            await _mediator.Send(new CancelOrderCommand(orderId));
            throw;
        }
    }
}
```

---

## 📚 Tổng kết

### Key Takeaways

1. **CQRS tách biệt read và write** - Mỗi operation có model riêng
2. **Commands thay đổi state** - Không trả về data (hoặc chỉ ID)
3. **Queries đọc data** - Không thay đổi state
4. **MediatR simplifies implementation** - Decoupling, pipeline behaviors
5. **Optimize riêng cho read và write** - Cache queries, validate commands
6. **DTOs cho queries** - Không trả về entities
7. **Events cho side effects** - Decouple business logic

### Khi nào dùng CQRS?

✅ **NÊN dùng khi**:
- Hệ thống phức tạp với nhiều business rules
- Read và write có requirements khác nhau
- Cần scale riêng read và write
- Cần audit trail đầy đủ
- Team lớn, cần boundaries rõ ràng

❌ **KHÔNG NÊN dùng khi**:
- CRUD đơn giản
- Hệ thống nhỏ
- Team nhỏ, ít kinh nghiệm
- Không cần scale

### Resources

- [CQRS by Martin Fowler](https://martinfowler.com/bliki/CQRS.html)
- [MediatR Documentation](https://github.com/jbogard/MediatR)
- [CQRS Journey by Microsoft](https://docs.microsoft.com/en-us/previous-versions/msp-n-p/jj554200(v=pandp.10))

---

**Chúc bạn thành công với CQRS!** 🚀

*Tài liệu này được tạo dựa trên implementation thực tế trong BlogApi*
