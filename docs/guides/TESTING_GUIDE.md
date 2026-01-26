# 🧪 Testing Guide - Hướng dẫn Chi tiết về Testing trong .NET

**Framework**: xUnit, NUnit, MSTest  
**Mocking**: Moq, NSubstitute  
**Assertion**: FluentAssertions  
**Coverage**: Coverlet

---

## 📑 Mục lục

1. [Testing Fundamentals](#-testing-fundamentals)
2. [Unit Testing](#-unit-testing)
3. [Integration Testing](#-integration-testing)
4. [Testing CQRS Handlers](#-testing-cqrs-handlers)
5. [Mocking Dependencies](#-mocking-dependencies)
6. [Testing API Controllers](#-testing-api-controllers)
7. [Testing Database Operations](#-testing-database-operations)
8. [Test Data Builders](#-test-data-builders)
9. [Code Coverage](#-code-coverage)
10. [Best Practices](#-best-practices)

---

## 🎯 Testing Fundamentals

### 1. Types of Tests

```
┌─────────────────────────────────────────────────────────────┐
│                    TEST PYRAMID                              │
└─────────────────────────────────────────────────────────────┘

                    ▲
                   ╱ ╲
                  ╱   ╲
                 ╱ E2E ╲          ← Slow, Expensive, Few
                ╱───────╲
               ╱         ╲
              ╱Integration╲       ← Medium Speed, Medium Cost
             ╱─────────────╲
            ╱               ╲
           ╱  Unit Tests     ╲    ← Fast, Cheap, Many
          ╱___________________╲
```

**Unit Tests**:
- Test individual components in isolation
- Fast execution
- No external dependencies
- 70-80% of total tests

**Integration Tests**:
- Test multiple components together
- May use real database (in-memory)
- Test API endpoints
- 15-25% of total tests

**End-to-End Tests**:
- Test complete user workflows
- Use real infrastructure
- Slowest, most expensive
- 5-10% of total tests

---

### 2. Test Project Setup

#### **Step 1: Create Test Project**

```bash
# Create test project
dotnet new xunit -n BlogApi.Tests

# Add reference to main project
cd BlogApi.Tests
dotnet add reference ../BlogApi/BlogApi.csproj

# Add required packages
dotnet add package Moq
dotnet add package FluentAssertions
dotnet add package Microsoft.EntityFrameworkCore.InMemory
dotnet add package Microsoft.AspNetCore.Mvc.Testing
```

#### **Step 2: Project Structure**

```
BlogApi.Tests/
├── Unit/
│   ├── Commands/
│   │   ├── CreatePostHandlerTests.cs
│   │   ├── UpdatePostHandlerTests.cs
│   │   └── DeletePostHandlerTests.cs
│   ├── Queries/
│   │   ├── GetPostsHandlerTests.cs
│   │   └── GetPostByIdHandlerTests.cs
│   └── Services/
│       └── EmailServiceTests.cs
├── Integration/
│   ├── Controllers/
│   │   ├── PostsControllerTests.cs
│   │   └── UsersControllerTests.cs
│   └── Database/
│       └── RepositoryTests.cs
├── Helpers/
│   ├── TestDataBuilder.cs
│   └── InMemoryDbContextFactory.cs
└── Fixtures/
    └── WebApplicationFactory.cs
```

---

## 🔬 Unit Testing

### 1. Basic Unit Test Structure

```csharp
using Xunit;
using FluentAssertions;
using Moq;

namespace BlogApi.Tests.Unit.Commands
{
    public class CreatePostHandlerTests
    {
        // AAA Pattern: Arrange, Act, Assert
        
        [Fact]
        public async Task Handle_ValidCommand_ReturnsPostId()
        {
            // Arrange (Setup)
            var mockRepository = new Mock<IGenericRepository<Post, Guid>>();
            var mockUserService = new Mock<ICurrentUserService>();
            
            mockUserService.Setup(x => x.UserId)
                .Returns(Guid.NewGuid());
            
            var handler = new CreatePostHandler(
                mockRepository.Object,
                mockUserService.Object);
            
            var command = new CreatePostCommand(
                "Test Title",
                "Test Content",
                "tech");
            
            // Act (Execute)
            var result = await handler.Handle(command, CancellationToken.None);
            
            // Assert (Verify)
            result.Should().NotBeEmpty();
            mockRepository.Verify(
                x => x.AddAsync(It.IsAny<Post>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }
        
        [Fact]
        public async Task Handle_NoUserId_ThrowsUnauthorizedException()
        {
            // Arrange
            var mockRepository = new Mock<IGenericRepository<Post, Guid>>();
            var mockUserService = new Mock<ICurrentUserService>();
            
            mockUserService.Setup(x => x.UserId).Returns((Guid?)null);
            
            var handler = new CreatePostHandler(
                mockRepository.Object,
                mockUserService.Object);
            
            var command = new CreatePostCommand("Title", "Content", "tech");
            
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => handler.Handle(command, CancellationToken.None));
        }
    }
}
```

### 2. Testing with Theory (Parameterized Tests)

```csharp
public class ValidationTests
{
    [Theory]
    [InlineData("", "Content", false)] // Empty title
    [InlineData("Title", "", false)]   // Empty content
    [InlineData("Title", "Content", true)] // Valid
    public void Validate_DifferentInputs_ReturnsExpectedResult(
        string title, 
        string content, 
        bool expectedValid)
    {
        // Arrange
        var validator = new CreatePostCommandValidator();
        var command = new CreatePostCommand(title, content, "tech");
        
        // Act
        var result = validator.Validate(command);
        
        // Assert
        result.IsValid.Should().Be(expectedValid);
    }
    
    [Theory]
    [MemberData(nameof(GetTestData))]
    public void Validate_ComplexScenarios_WorksCorrectly(
        CreatePostCommand command, 
        bool expectedValid)
    {
        // Arrange
        var validator = new CreatePostCommandValidator();
        
        // Act
        var result = validator.Validate(command);
        
        // Assert
        result.IsValid.Should().Be(expectedValid);
    }
    
    public static IEnumerable<object[]> GetTestData()
    {
        yield return new object[] 
        { 
            new CreatePostCommand("Title", "Content", "tech"), 
            true 
        };
        yield return new object[] 
        { 
            new CreatePostCommand("", "Content", "tech"), 
            false 
        };
    }
}
```

### 3. Testing Async Methods

```csharp
public class AsyncTests
{
    [Fact]
    public async Task GetDataAsync_ReturnsData()
    {
        // Arrange
        var mockService = new Mock<IDataService>();
        mockService.Setup(x => x.GetDataAsync())
            .ReturnsAsync("Test Data");
        
        // Act
        var result = await mockService.Object.GetDataAsync();
        
        // Assert
        result.Should().Be("Test Data");
    }
    
    [Fact]
    public async Task ProcessAsync_ThrowsException_HandlesGracefully()
    {
        // Arrange
        var mockService = new Mock<IDataService>();
        mockService.Setup(x => x.ProcessAsync())
            .ThrowsAsync(new InvalidOperationException("Error"));
        
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => mockService.Object.ProcessAsync());
    }
}
```

---

## 🧩 Integration Testing

### 1. WebApplicationFactory Setup

```csharp
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BlogApi.Tests.Integration
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove real DbContext
                services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
                
                // Add in-memory database
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb");
                });
                
                // Build service provider
                var sp = services.BuildServiceProvider();
                
                // Create scope and seed database
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                
                db.Database.EnsureCreated();
                SeedDatabase(db);
            });
        }
        
        private void SeedDatabase(AppDbContext db)
        {
            db.Posts.AddRange(
                new Post 
                { 
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Title = "Test Post 1",
                    Content = "Content 1",
                    AuthorId = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow
                },
                new Post 
                { 
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Title = "Test Post 2",
                    Content = "Content 2",
                    AuthorId = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow
                }
            );
            
            db.SaveChanges();
        }
    }
}
```

### 2. Testing API Endpoints

```csharp
public class PostsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;
    
    public PostsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }
    
    [Fact]
    public async Task GetPosts_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/api/posts");
        
        // Assert
        response.EnsureSuccessStatusCode();
        response.Content.Headers.ContentType.ToString()
            .Should().Contain("application/json");
    }
    
    [Fact]
    public async Task GetPostById_ExistingId_ReturnsPost()
    {
        // Arrange
        var postId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        
        // Act
        var response = await _client.GetAsync($"/api/posts/{postId}");
        
        // Assert
        response.EnsureSuccessStatusCode();
        
        var post = await response.Content.ReadFromJsonAsync<PostDto>();
        post.Should().NotBeNull();
        post!.Id.Should().Be(postId);
        post.Title.Should().Be("Test Post 1");
    }
    
    [Fact]
    public async Task CreatePost_ValidData_ReturnsCreated()
    {
        // Arrange
        var command = new CreatePostCommand(
            "New Post",
            "New Content",
            "tech");
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/posts", command);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var location = response.Headers.Location;
        location.Should().NotBeNull();
    }
    
    [Fact]
    public async Task CreatePost_InvalidData_ReturnsBadRequest()
    {
        // Arrange
        var command = new CreatePostCommand("", "", ""); // Invalid
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/posts", command);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
```

### 3. Testing with Authentication

```csharp
public class AuthenticatedTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    
    public AuthenticatedTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }
    
    [Fact]
    public async Task CreatePost_WithAuth_ReturnsCreated()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", token);
        
        var command = new CreatePostCommand("Title", "Content", "tech");
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/posts", command);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
    
    [Fact]
    public async Task CreatePost_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var command = new CreatePostCommand("Title", "Content", "tech");
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/posts", command);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
    
    private async Task<string> GetAuthTokenAsync()
    {
        var loginDto = new LoginDto 
        { 
            Email = "test@example.com", 
            Password = "Test123!" 
        };
        
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        
        return result!.Token;
    }
}
```

---

## 🎯 Testing CQRS Handlers

### 1. Testing Command Handlers

```csharp
public class CreatePostHandlerTests
{
    private readonly Mock<IGenericRepository<Post, Guid>> _mockRepository;
    private readonly Mock<ICurrentUserService> _mockUserService;
    private readonly Mock<IEventPublisher> _mockEventPublisher;
    private readonly CreatePostHandler _handler;
    
    public CreatePostHandlerTests()
    {
        _mockRepository = new Mock<IGenericRepository<Post, Guid>>();
        _mockUserService = new Mock<ICurrentUserService>();
        _mockEventPublisher = new Mock<IEventPublisher>();
        
        _handler = new CreatePostHandler(
            _mockRepository.Object,
            _mockUserService.Object,
            _mockEventPublisher.Object);
    }
    
    [Fact]
    public async Task Handle_ValidCommand_CreatesPost()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockUserService.Setup(x => x.UserId).Returns(userId);
        
        var command = new CreatePostCommand(
            "Test Title",
            "Test Content",
            "tech");
        
        // Act
        var result = await _handler.Handle(command, CancellationToken.None);
        
        // Assert
        result.Should().NotBeEmpty();
        
        _mockRepository.Verify(
            x => x.AddAsync(
                It.Is<Post>(p => 
                    p.Title == "Test Title" &&
                    p.Content == "Test Content" &&
                    p.AuthorId == userId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Fact]
    public async Task Handle_ValidCommand_PublishesEvent()
    {
        // Arrange
        _mockUserService.Setup(x => x.UserId).Returns(Guid.NewGuid());
        
        var command = new CreatePostCommand("Title", "Content", "tech");
        
        // Act
        await _handler.Handle(command, CancellationToken.None);
        
        // Assert
        _mockEventPublisher.Verify(
            x => x.Publish(
                It.IsAny<PostCreatedEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Fact]
    public async Task Handle_NoUserId_ThrowsException()
    {
        // Arrange
        _mockUserService.Setup(x => x.UserId).Returns((Guid?)null);
        
        var command = new CreatePostCommand("Title", "Content", "tech");
        
        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _handler.Handle(command, CancellationToken.None));
        
        _mockRepository.Verify(
            x => x.AddAsync(It.IsAny<Post>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
```

### 2. Testing Query Handlers

```csharp
public class GetPostsHandlerTests
{
    private readonly Mock<IGenericRepository<Post, Guid>> _mockRepository;
    private readonly GetPostsHandler _handler;
    
    public GetPostsHandlerTests()
    {
        _mockRepository = new Mock<IGenericRepository<Post, Guid>>();
        _handler = new GetPostsHandler(_mockRepository.Object);
    }
    
    [Fact]
    public async Task Handle_ReturnsPagedPosts()
    {
        // Arrange
        var posts = new List<Post>
        {
            new Post 
            { 
                Id = Guid.NewGuid(), 
                Title = "Post 1",
                Author = new AppUser { UserName = "user1" }
            },
            new Post 
            { 
                Id = Guid.NewGuid(), 
                Title = "Post 2",
                Author = new AppUser { UserName = "user2" }
            }
        };
        
        var queryable = posts.AsQueryable().BuildMock();
        _mockRepository.Setup(x => x.GetQueryable()).Returns(queryable);
        
        var query = new GetPostsQuery(null, 10);
        
        // Act
        var result = await _handler.Handle(query, CancellationToken.None);
        
        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.Items[0].Title.Should().Be("Post 1");
    }
    
    [Fact]
    public async Task Handle_WithCursor_ReturnsPaginatedResults()
    {
        // Arrange
        var cursor = Guid.Parse("11111111-1111-1111-1111-111111111111");
        
        var posts = new List<Post>
        {
            new Post { Id = cursor, Title = "Post 1" },
            new Post 
            { 
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), 
                Title = "Post 2" 
            },
            new Post 
            { 
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), 
                Title = "Post 3" 
            }
        };
        
        var queryable = posts.AsQueryable().BuildMock();
        _mockRepository.Setup(x => x.GetQueryable()).Returns(queryable);
        
        var query = new GetPostsQuery(cursor, 10);
        
        // Act
        var result = await _handler.Handle(query, CancellationToken.None);
        
        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().NotContain(p => p.Id == cursor);
    }
}
```

---

## 🎭 Mocking Dependencies

### 1. Basic Mocking with Moq

```csharp
// Setup method to return value
var mock = new Mock<IEmailService>();
mock.Setup(x => x.SendEmailAsync(
    It.IsAny<string>(),
    It.IsAny<string>(),
    It.IsAny<string>()))
    .ReturnsAsync(true);

// Setup method to throw exception
mock.Setup(x => x.SendEmailAsync(
    It.IsAny<string>(),
    It.IsAny<string>(),
    It.IsAny<string>()))
    .ThrowsAsync(new InvalidOperationException("Email failed"));

// Setup property
mock.Setup(x => x.IsEnabled).Returns(true);

// Verify method was called
mock.Verify(
    x => x.SendEmailAsync(
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<string>()),
    Times.Once);

// Verify method was never called
mock.Verify(
    x => x.SendEmailAsync(
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<string>()),
    Times.Never);
```

### 2. Advanced Mocking

```csharp
public class AdvancedMockingTests
{
    [Fact]
    public async Task MockWithCallback()
    {
        // Arrange
        var mock = new Mock<IRepository<Post>>();
        Post capturedPost = null;
        
        mock.Setup(x => x.AddAsync(It.IsAny<Post>()))
            .Callback<Post>(p => capturedPost = p)
            .ReturnsAsync(true);
        
        // Act
        await mock.Object.AddAsync(new Post { Title = "Test" });
        
        // Assert
        capturedPost.Should().NotBeNull();
        capturedPost.Title.Should().Be("Test");
    }
    
    [Fact]
    public async Task MockWithSequence()
    {
        // Arrange
        var mock = new Mock<IDataService>();
        
        mock.SetupSequence(x => x.GetDataAsync())
            .ReturnsAsync("First")
            .ReturnsAsync("Second")
            .ReturnsAsync("Third");
        
        // Act & Assert
        (await mock.Object.GetDataAsync()).Should().Be("First");
        (await mock.Object.GetDataAsync()).Should().Be("Second");
        (await mock.Object.GetDataAsync()).Should().Be("Third");
    }
    
    [Fact]
    public void MockWithConditionalSetup()
    {
        // Arrange
        var mock = new Mock<IUserService>();
        
        mock.Setup(x => x.GetUser(It.Is<Guid>(id => id != Guid.Empty)))
            .Returns(new User { Name = "John" });
        
        mock.Setup(x => x.GetUser(Guid.Empty))
            .Returns((User)null);
        
        // Act & Assert
        mock.Object.GetUser(Guid.NewGuid()).Should().NotBeNull();
        mock.Object.GetUser(Guid.Empty).Should().BeNull();
    }
}
```

### 3. Mocking IQueryable

```csharp
using MockQueryable.Moq;

public class QueryableMockingTests
{
    [Fact]
    public async Task MockIQueryable_WorksWithLinq()
    {
        // Arrange
        var posts = new List<Post>
        {
            new Post { Id = Guid.NewGuid(), Title = "Post 1" },
            new Post { Id = Guid.NewGuid(), Title = "Post 2" },
            new Post { Id = Guid.NewGuid(), Title = "Post 3" }
        };
        
        var mockQueryable = posts.AsQueryable().BuildMock();
        
        var mockRepository = new Mock<IGenericRepository<Post, Guid>>();
        mockRepository.Setup(x => x.GetQueryable())
            .Returns(mockQueryable);
        
        // Act
        var result = await mockRepository.Object.GetQueryable()
            .Where(p => p.Title.Contains("Post"))
            .ToListAsync();
        
        // Assert
        result.Should().HaveCount(3);
    }
}
```

---

## 🌐 Testing API Controllers

### 1. Controller Unit Tests

```csharp
public class PostsControllerUnitTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly PostsController _controller;
    
    public PostsControllerUnitTests()
    {
        _mockMediator = new Mock<IMediator>();
        _controller = new PostsController(_mockMediator.Object);
    }
    
    [Fact]
    public async Task Create_ValidCommand_ReturnsCreatedResult()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var command = new CreatePostCommand("Title", "Content", "tech");
        
        _mockMediator.Setup(x => x.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(postId);
        
        // Act
        var result = await _controller.Create(command);
        
        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(PostsController.GetById));
        createdResult.RouteValues["id"].Should().Be(postId);
    }
    
    [Fact]
    public async Task GetById_ExistingId_ReturnsOkResult()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var postDto = new PostDto(postId, "Title", "Author", 4.5, DateTime.UtcNow, "tech");
        
        _mockMediator.Setup(x => x.Send(
            It.Is<GetPostByIdQuery>(q => q.Id == postId),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(postDto);
        
        // Act
        var result = await _controller.GetById(postId);
        
        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedPost = okResult.Value.Should().BeOfType<PostDto>().Subject;
        returnedPost.Id.Should().Be(postId);
    }
    
    [Fact]
    public async Task GetById_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        var postId = Guid.NewGuid();
        
        _mockMediator.Setup(x => x.Send(
            It.IsAny<GetPostByIdQuery>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostDto)null);
        
        // Act
        var result = await _controller.GetById(postId);
        
        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }
}
```

---

## 💾 Testing Database Operations

### 1. In-Memory Database Tests

```csharp
public class RepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly GenericRepository<Post, Guid> _repository;
    
    public RepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _context = new AppDbContext(options);
        _repository = new GenericRepository<Post, Guid>(_context);
        
        SeedDatabase();
    }
    
    private void SeedDatabase()
    {
        var posts = new List<Post>
        {
            new Post 
            { 
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Title = "Post 1",
                Content = "Content 1",
                AuthorId = Guid.NewGuid()
            },
            new Post 
            { 
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Title = "Post 2",
                Content = "Content 2",
                AuthorId = Guid.NewGuid()
            }
        };
        
        _context.Posts.AddRange(posts);
        _context.SaveChanges();
    }
    
    [Fact]
    public async Task AddAsync_ValidPost_AddsToDatabase()
    {
        // Arrange
        var post = new Post
        {
            Id = Guid.NewGuid(),
            Title = "New Post",
            Content = "New Content",
            AuthorId = Guid.NewGuid()
        };
        
        // Act
        await _repository.AddAsync(post);
        
        // Assert
        var savedPost = await _context.Posts.FindAsync(post.Id);
        savedPost.Should().NotBeNull();
        savedPost.Title.Should().Be("New Post");
    }
    
    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsPost()
    {
        // Arrange
        var postId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        
        // Act
        var post = await _repository.GetByIdAsync(postId);
        
        // Assert
        post.Should().NotBeNull();
        post.Title.Should().Be("Post 1");
    }
    
    [Fact]
    public async Task UpdateAsync_ExistingPost_UpdatesDatabase()
    {
        // Arrange
        var postId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var post = await _repository.GetByIdAsync(postId);
        post.Title = "Updated Title";
        
        // Act
        await _repository.UpdateAsync(post);
        
        // Assert
        var updatedPost = await _context.Posts.FindAsync(postId);
        updatedPost.Title.Should().Be("Updated Title");
    }
    
    [Fact]
    public async Task DeleteAsync_ExistingPost_RemovesFromDatabase()
    {
        // Arrange
        var postId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var post = await _repository.GetByIdAsync(postId);
        
        // Act
        await _repository.DeleteAsync(post);
        
        // Assert
        var deletedPost = await _context.Posts.FindAsync(postId);
        deletedPost.Should().BeNull();
    }
    
    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
```

### 2. Testing with Real Database (SQLite)

```csharp
public class SqliteRepositoryTests : IDisposable
{
    private readonly DbConnection _connection;
    private readonly AppDbContext _context;
    private readonly GenericRepository<Post, Guid> _repository;
    
    public SqliteRepositoryTests()
    {
        // Create in-memory SQLite database
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;
        
        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();
        
        _repository = new GenericRepository<Post, Guid>(_context);
    }
    
    [Fact]
    public async Task ComplexQuery_WorksCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _context.Posts.AddRange(
            new Post { Title = "Post 1", AuthorId = userId },
            new Post { Title = "Post 2", AuthorId = userId },
            new Post { Title = "Post 3", AuthorId = Guid.NewGuid() }
        );
        await _context.SaveChangesAsync();
        
        // Act
        var userPosts = await _repository.GetQueryable()
            .Where(p => p.AuthorId == userId)
            .OrderBy(p => p.Title)
            .ToListAsync();
        
        // Assert
        userPosts.Should().HaveCount(2);
        userPosts[0].Title.Should().Be("Post 1");
    }
    
    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
```

---

## 🏗️ Test Data Builders

### 1. Builder Pattern for Test Data

```csharp
public class PostBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _title = "Default Title";
    private string _content = "Default Content";
    private Guid _authorId = Guid.NewGuid();
    private string _categoryId = "tech";
    private DateTime _createdAt = DateTime.UtcNow;
    
    public PostBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }
    
    public PostBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }
    
    public PostBuilder WithContent(string content)
    {
        _content = content;
        return this;
    }
    
    public PostBuilder WithAuthorId(Guid authorId)
    {
        _authorId = authorId;
        return this;
    }
    
    public PostBuilder WithCategoryId(string categoryId)
    {
        _categoryId = categoryId;
        return this;
    }
    
    public PostBuilder CreatedAt(DateTime createdAt)
    {
        _createdAt = createdAt;
        return this;
    }
    
    public Post Build()
    {
        return new Post
        {
            Id = _id,
            Title = _title,
            Content = _content,
            AuthorId = _authorId,
            CategoryId = _categoryId,
            CreatedAt = _createdAt,
            UpdatedAt = _createdAt
        };
    }
    
    public static implicit operator Post(PostBuilder builder) => builder.Build();
}

// Usage
public class BuilderTests
{
    [Fact]
    public void UsingBuilder_CreatesPost()
    {
        // Arrange
        var post = new PostBuilder()
            .WithTitle("Custom Title")
            .WithContent("Custom Content")
            .WithCategoryId("news")
            .Build();
        
        // Assert
        post.Title.Should().Be("Custom Title");
        post.Content.Should().Be("Custom Content");
        post.CategoryId.Should().Be("news");
    }
    
    [Fact]
    public void UsingBuilder_WithDefaults()
    {
        // Arrange
        var post = new PostBuilder().Build();
        
        // Assert
        post.Title.Should().Be("Default Title");
        post.Content.Should().Be("Default Content");
    }
}
```

### 2. Object Mother Pattern

```csharp
public static class TestData
{
    public static class Posts
    {
        public static Post CreateDefault() => new Post
        {
            Id = Guid.NewGuid(),
            Title = "Test Post",
            Content = "Test Content",
            AuthorId = Guid.NewGuid(),
            CategoryId = "tech",
            CreatedAt = DateTime.UtcNow
        };
        
        public static Post CreateWithTitle(string title)
        {
            var post = CreateDefault();
            post.Title = title;
            return post;
        }
        
        public static Post CreateWithAuthor(Guid authorId)
        {
            var post = CreateDefault();
            post.AuthorId = authorId;
            return post;
        }
        
        public static List<Post> CreateMany(int count)
        {
            return Enumerable.Range(1, count)
                .Select(i => new Post
                {
                    Id = Guid.NewGuid(),
                    Title = $"Post {i}",
                    Content = $"Content {i}",
                    AuthorId = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow
                })
                .ToList();
        }
    }
    
    public static class Users
    {
        public static AppUser CreateDefault() => new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = "testuser",
            Email = "test@example.com"
        };
        
        public static AppUser CreateAdmin() => new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = "admin",
            Email = "admin@example.com",
            // Set admin role
        };
    }
}

// Usage
public class ObjectMotherTests
{
    [Fact]
    public void UsingObjectMother_CreatesPost()
    {
        // Arrange
        var post = TestData.Posts.CreateDefault();
        
        // Assert
        post.Should().NotBeNull();
        post.Title.Should().Be("Test Post");
    }
    
    [Fact]
    public void UsingObjectMother_CreatesManyPosts()
    {
        // Arrange
        var posts = TestData.Posts.CreateMany(5);
        
        // Assert
        posts.Should().HaveCount(5);
    }
}
```

---

## 📊 Code Coverage

### 1. Setup Coverlet

```bash
# Install coverlet
dotnet add package coverlet.collector
dotnet add package coverlet.msbuild

# Run tests with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Generate HTML report (install ReportGenerator first)
dotnet tool install -g dotnet-reportgenerator-globaltool

reportgenerator -reports:coverage.opencover.xml -targetdir:coveragereport -reporttypes:Html

# Open report
start coveragereport/index.html
```

### 2. Coverage Configuration

```xml
<!-- Directory.Build.props -->
<Project>
  <PropertyGroup>
    <CollectCoverage>true</CollectCoverage>
    <CoverletOutputFormat>opencover</CoverletOutputFormat>
    <Exclude>[*]*.Migrations.*,[*]*.Program,[*]*.Startup</Exclude>
    <ExcludeByAttribute>Obsolete,GeneratedCode,CompilerGenerated</ExcludeByAttribute>
  </PropertyGroup>
</Project>
```

### 3. Coverage Goals

```
┌─────────────────────────────────────────────────────────────┐
│                  COVERAGE TARGETS                            │
└─────────────────────────────────────────────────────────────┘

Component               Target      Minimum
─────────────────────────────────────────────
Domain Logic            90-100%     80%
Command Handlers        80-90%      70%
Query Handlers          70-80%      60%
Controllers             60-70%      50%
Infrastructure          50-60%      40%

Overall Project         70-80%      60%
```

---

## ✅ Best Practices

### 1. Test Naming

```csharp
// ✅ Good: Clear, descriptive names
[Fact]
public async Task Handle_ValidCommand_CreatesPost()

[Fact]
public async Task Handle_InvalidCommand_ThrowsValidationException()

[Fact]
public async Task Handle_UnauthorizedUser_ThrowsUnauthorizedException()

// ❌ Bad: Unclear names
[Fact]
public async Task Test1()

[Fact]
public async Task TestCreatePost()
```

### 2. AAA Pattern (Arrange, Act, Assert)

```csharp
[Fact]
public async Task Example_Test()
{
    // Arrange - Setup test data and dependencies
    var mockService = new Mock<IService>();
    mockService.Setup(x => x.GetData()).ReturnsAsync("data");
    
    var handler = new MyHandler(mockService.Object);
    var command = new MyCommand("test");
    
    // Act - Execute the code under test
    var result = await handler.Handle(command, CancellationToken.None);
    
    // Assert - Verify the results
    result.Should().NotBeNull();
    result.Should().Be("expected");
    
    mockService.Verify(x => x.GetData(), Times.Once);
}
```

### 3. One Assert Per Test (When Possible)

```csharp
// ✅ Good: Focus on one behavior
[Fact]
public void CreatePost_SetsTitle()
{
    var post = new Post { Title = "Test" };
    post.Title.Should().Be("Test");
}

[Fact]
public void CreatePost_SetsContent()
{
    var post = new Post { Content = "Content" };
    post.Content.Should().Be("Content");
}

// ⚠️ Acceptable: Related assertions
[Fact]
public void CreatePost_SetsAllProperties()
{
    var post = new Post 
    { 
        Title = "Test", 
        Content = "Content" 
    };
    
    post.Title.Should().Be("Test");
    post.Content.Should().Be("Content");
}
```

### 4. Test Independence

```csharp
// ✅ Good: Each test is independent
public class IndependentTests
{
    [Fact]
    public void Test1()
    {
        var service = new MyService();
        // Test logic
    }
    
    [Fact]
    public void Test2()
    {
        var service = new MyService();
        // Test logic
    }
}

// ❌ Bad: Tests depend on each other
public class DependentTests
{
    private static MyService _service = new MyService();
    
    [Fact]
    public void Test1()
    {
        _service.DoSomething(); // Modifies shared state
    }
    
    [Fact]
    public void Test2()
    {
        // Depends on Test1 running first!
        _service.DoSomethingElse();
    }
}
```

### 5. Don't Test Framework Code

```csharp
// ❌ Bad: Testing EF Core
[Fact]
public async Task DbContext_SavesChanges()
{
    var options = new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase("test")
        .Options;
    
    using var context = new AppDbContext(options);
    context.Posts.Add(new Post { Title = "Test" });
    await context.SaveChangesAsync();
    
    var post = await context.Posts.FirstAsync();
    post.Title.Should().Be("Test");
}

// ✅ Good: Test your business logic
[Fact]
public async Task Repository_AddsPost()
{
    var mockContext = new Mock<AppDbContext>();
    var repository = new PostRepository(mockContext.Object);
    
    await repository.AddAsync(new Post { Title = "Test" });
    
    // Verify your code, not EF Core
    mockContext.Verify(x => x.SaveChangesAsync(default), Times.Once);
}
```

### 6. Fast Tests

```csharp
// ✅ Good: Fast, isolated unit test
[Fact]
public async Task FastTest()
{
    var mock = new Mock<IService>();
    mock.Setup(x => x.GetData()).ReturnsAsync("data");
    
    var result = await mock.Object.GetData();
    
    result.Should().Be("data");
}

// ⚠️ Slower: Integration test (acceptable when needed)
[Fact]
public async Task SlowerIntegrationTest()
{
    using var factory = new WebApplicationFactory<Program>();
    var client = factory.CreateClient();
    
    var response = await client.GetAsync("/api/posts");
    
    response.EnsureSuccessStatusCode();
}

// ❌ Bad: Unnecessarily slow
[Fact]
public async Task UnnecessarilySlow()
{
    await Task.Delay(1000); // Don't do this!
    // Test logic
}
```

---

## 🎯 Summary

### Test Checklist:

- [ ] Unit tests for all handlers
- [ ] Integration tests for API endpoints
- [ ] Tests for validation logic
- [ ] Tests for business rules
- [ ] Tests for error scenarios
- [ ] Mocks for external dependencies
- [ ] In-memory database for integration tests
- [ ] Code coverage > 70%
- [ ] All tests pass
- [ ] Tests run fast (< 1 second per test)

### Common Patterns:

1. **AAA Pattern**: Arrange, Act, Assert
2. **Builder Pattern**: For test data creation
3. **Object Mother**: For common test objects
4. **Test Fixtures**: For shared setup
5. **Mocking**: For external dependencies

### Tools:

- **xUnit**: Test framework
- **Moq**: Mocking framework
- **FluentAssertions**: Readable assertions
- **Coverlet**: Code coverage
- **ReportGenerator**: Coverage reports

---

**Happy Testing! 🧪**
