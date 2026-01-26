# 📚 GenericRepository - Hướng dẫn sử dụng Filter, Sort, Pagination

## 🎯 Tổng quan

GenericRepository giờ đây hỗ trợ đầy đủ các tính năng:
- ✅ **Filtering** - Lọc dữ liệu với predicates
- ✅ **Sorting** - Sắp xếp tăng/giảm dần
- ✅ **Pagination** - Phân trang với Skip/Take
- ✅ **Includes** - Eager loading navigation properties
- ✅ **Advanced Queries** - Count, Any, FirstOrDefault
- ✅ **Performance** - AsNoTracking, AsSplitQuery

---

## 📝 Các Methods có sẵn

### Basic CRUD
```csharp
Task<TEntity?> GetByIdAsync(TKey id)
Task<IEnumerable<TEntity>> GetAllAsync()
Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate)
Task AddAsync(TEntity entity)
Task UpdateAsync(TEntity entity)
Task DeleteAsync(TEntity entity)
IQueryable<TEntity> GetQueryable()
```

### Advanced Queries
```csharp
Task<IEnumerable<TEntity>> GetWithOptionsAsync(QueryOptions<TEntity> options)
Task<TEntity?> GetSingleWithOptionsAsync(QueryOptions<TEntity> options)
Task<PagedResult<TEntity>> GetPagedAsync(QueryOptions<TEntity> options)
Task<int> CountAsync(Expression<Func<TEntity, bool>>? filter = null)
Task<bool> AnyAsync(Expression<Func<TEntity, bool>> filter)
Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> filter)
```

---

## 💡 Ví dụ sử dụng

### 1. Simple Filter

```csharp
// Query: Get published posts
public class GetPublishedPostsQuery : IRequest<List<PostDto>>;

public class GetPublishedPostsHandler : IRequestHandler<GetPublishedPostsQuery, List<PostDto>>
{
    private readonly IGenericRepository<Post, Guid> _repository;
    
    public async Task<List<PostDto>> Handle(GetPublishedPostsQuery request, CancellationToken ct)
    {
        // Simple predicate filter
        var posts = await _repository.FindAsync(p => p.IsPublished);
        
        return posts.Select(p => new PostDto(
            p.Id,
            p.Title,
            p.Author.FullName,
            p.AverageRating,
            p.CreatedAt,
            p.CategoryId
        )).ToList();
    }
}
```

### 2. Filter + Sort

```csharp
// Query: Get posts by category, sorted by rating
public record GetPostsByCategoryQuery(string CategoryId) : IRequest<List<PostDto>>;

public class GetPostsByCategoryHandler : IRequestHandler<GetPostsByCategoryQuery, List<PostDto>>
{
    private readonly IGenericRepository<Post, Guid> _repository;
    
    public async Task<List<PostDto>> Handle(GetPostsByCategoryQuery request, CancellationToken ct)
    {
        var options = new QueryOptions<Post>
        {
            Filter = p => p.CategoryId == request.CategoryId,
            OrderByDescending = p => p.AverageRating,
            Includes = new List<Expression<Func<Post, object>>>
            {
                p => p.Author,
                p => p.Category
            },
            AsNoTracking = true
        };
        
        var posts = await _repository.GetWithOptionsAsync(options);
        
        return posts.Select(p => new PostDto(
            p.Id,
            p.Title,
            p.Author.FullName,
            p.AverageRating,
            p.CreatedAt,
            p.CategoryId
        )).ToList();
    }
}
```

### 3. Pagination với Fluent Builder

```csharp
// Query: Get paginated posts
public record GetPostsPagedQuery(int PageNumber, int PageSize) : IRequest<PagedResult<PostDto>>;

public class GetPostsPagedHandler : IRequestHandler<GetPostsPagedQuery, PagedResult<PostDto>>
{
    private readonly IGenericRepository<Post, Guid> _repository;
    
    public async Task<PagedResult<PostDto>> Handle(GetPostsPagedQuery request, CancellationToken ct)
    {
        // Using fluent builder
        var options = new QueryOptionsBuilder<Post>()
            .WithFilter(p => p.IsPublished)
            .WithOrderByDescending(p => p.CreatedAt)
            .WithInclude(p => p.Author)
            .WithInclude(p => p.Category)
            .WithPagination((request.PageNumber - 1) * request.PageSize, request.PageSize)
            .AsNoTracking()
            .Build();
        
        var pagedResult = await _repository.GetPagedAsync(options);
        
        return new PagedResult<PostDto>(
            pagedResult.Items.Select(p => new PostDto(
                p.Id,
                p.Title,
                p.Author.FullName,
                p.AverageRating,
                p.CreatedAt,
                p.CategoryId
            )).ToList(),
            pagedResult.TotalCount,
            pagedResult.PageNumber,
            pagedResult.PageSize
        );
    }
}
```

### 4. Complex Filter với Multiple Conditions

```csharp
// Query: Search posts
public record SearchPostsQuery(
    string? SearchTerm,
    string? CategoryId,
    double? MinRating,
    DateTime? FromDate,
    int PageNumber = 1,
    int PageSize = 10
) : IRequest<PagedResult<PostDto>>;

public class SearchPostsHandler : IRequestHandler<SearchPostsQuery, PagedResult<PostDto>>
{
    private readonly IGenericRepository<Post, Guid> _repository;
    
    public async Task<PagedResult<PostDto>> Handle(SearchPostsQuery request, CancellationToken ct)
    {
        // Build complex filter
        Expression<Func<Post, bool>> filter = p => true; // Start with always true
        
        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            filter = filter.And(p => 
                p.Title.ToLower().Contains(searchTerm) || 
                p.Content.ToLower().Contains(searchTerm));
        }
        
        if (!string.IsNullOrEmpty(request.CategoryId))
        {
            filter = filter.And(p => p.CategoryId == request.CategoryId);
        }
        
        if (request.MinRating.HasValue)
        {
            filter = filter.And(p => p.AverageRating >= request.MinRating.Value);
        }
        
        if (request.FromDate.HasValue)
        {
            filter = filter.And(p => p.CreatedAt >= request.FromDate.Value);
        }
        
        var options = new QueryOptionsBuilder<Post>()
            .WithFilter(filter)
            .WithOrderByDescending(p => p.CreatedAt)
            .WithInclude(p => p.Author)
            .WithPagination((request.PageNumber - 1) * request.PageSize, request.PageSize)
            .AsNoTracking()
            .Build();
        
        var pagedResult = await _repository.GetPagedAsync(options);
        
        return new PagedResult<PostDto>(
            pagedResult.Items.Select(p => new PostDto(
                p.Id,
                p.Title,
                p.Author.FullName,
                p.AverageRating,
                p.CreatedAt,
                p.CategoryId
            )).ToList(),
            pagedResult.TotalCount,
            pagedResult.PageNumber,
            pagedResult.PageSize
        );
    }
}
```

### 5. Nested Includes

```csharp
// Query: Get post with comments and comment authors
public record GetPostWithCommentsQuery(Guid PostId) : IRequest<PostDetailDto>;

public class GetPostWithCommentsHandler : IRequestHandler<GetPostWithCommentsQuery, PostDetailDto>
{
    private readonly IGenericRepository<Post, Guid> _repository;
    
    public async Task<PostDetailDto> Handle(GetPostWithCommentsQuery request, CancellationToken ct)
    {
        var options = new QueryOptions<Post>
        {
            Filter = p => p.Id == request.PostId,
            IncludeStrings = new List<string>
            {
                "Author",
                "Category",
                "Comments.Author"  // Nested include
            },
            AsNoTracking = true
        };
        
        var post = await _repository.GetSingleWithOptionsAsync(options);
        
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

### 6. Count và Any

```csharp
// Check if user has posts
public record HasUserPostsQuery(Guid UserId) : IRequest<bool>;

public class HasUserPostsHandler : IRequestHandler<HasUserPostsQuery, bool>
{
    private readonly IGenericRepository<Post, Guid> _repository;
    
    public async Task<bool> Handle(HasUserPostsQuery request, CancellationToken ct)
    {
        return await _repository.AnyAsync(p => p.AuthorId == request.UserId);
    }
}

// Get post count by category
public record GetPostCountByCategoryQuery(string CategoryId) : IRequest<int>;

public class GetPostCountByCategoryHandler : IRequestHandler<GetPostCountByCategoryQuery, int>
{
    private readonly IGenericRepository<Post, Guid> _repository;
    
    public async Task<int> Handle(GetPostCountByCategoryQuery request, CancellationToken ct)
    {
        return await _repository.CountAsync(p => p.CategoryId == request.CategoryId);
    }
}
```

### 7. FirstOrDefault

```csharp
// Get latest post by author
public record GetLatestPostByAuthorQuery(Guid AuthorId) : IRequest<PostDto?>;

public class GetLatestPostByAuthorHandler : IRequestHandler<GetLatestPostByAuthorQuery, PostDto?>
{
    private readonly IGenericRepository<Post, Guid> _repository;
    
    public async Task<PostDto?> Handle(GetLatestPostByAuthorQuery request, CancellationToken ct)
    {
        var options = new QueryOptions<Post>
        {
            Filter = p => p.AuthorId == request.AuthorId,
            OrderByDescending = p => p.CreatedAt,
            Take = 1,
            Includes = new List<Expression<Func<Post, object>>>
            {
                p => p.Author,
                p => p.Category
            },
            AsNoTracking = true
        };
        
        var post = await _repository.GetSingleWithOptionsAsync(options);
        
        if (post == null) return null;
        
        return new PostDto(
            post.Id,
            post.Title,
            post.Author.FullName,
            post.AverageRating,
            post.CreatedAt,
            post.CategoryId
        );
    }
}
```

### 8. Multiple Sorting

```csharp
// Get posts sorted by rating then by date
public record GetTopPostsQuery(int Count) : IRequest<List<PostDto>>;

public class GetTopPostsHandler : IRequestHandler<GetTopPostsQuery, List<PostDto>>
{
    private readonly IGenericRepository<Post, Guid> _repository;
    
    public async Task<List<PostDto>> Handle(GetTopPostsQuery request, CancellationToken ct)
    {
        // For multiple sorting, use GetQueryable
        var posts = await _repository.GetQueryable()
            .Include(p => p.Author)
            .Include(p => p.Category)
            .Where(p => p.IsPublished)
            .OrderByDescending(p => p.AverageRating)
            .ThenByDescending(p => p.CreatedAt)
            .Take(request.Count)
            .AsNoTracking()
            .ToListAsync();
        
        return posts.Select(p => new PostDto(
            p.Id,
            p.Title,
            p.Author.FullName,
            p.AverageRating,
            p.CreatedAt,
            p.CategoryId
        )).ToList();
    }
}
```

---

## 🔧 Helper Extension Methods

Để làm việc với complex filters dễ dàng hơn, tạo extension methods:

```csharp
// Application/Common/Extensions/ExpressionExtensions.cs
public static class ExpressionExtensions
{
    public static Expression<Func<T, bool>> And<T>(
        this Expression<Func<T, bool>> first,
        Expression<Func<T, bool>> second)
    {
        var parameter = Expression.Parameter(typeof(T));
        
        var leftVisitor = new ReplaceExpressionVisitor(first.Parameters[0], parameter);
        var left = leftVisitor.Visit(first.Body);
        
        var rightVisitor = new ReplaceExpressionVisitor(second.Parameters[0], parameter);
        var right = rightVisitor.Visit(second.Body);
        
        return Expression.Lambda<Func<T, bool>>(
            Expression.AndAlso(left!, right!), parameter);
    }
    
    public static Expression<Func<T, bool>> Or<T>(
        this Expression<Func<T, bool>> first,
        Expression<Func<T, bool>> second)
    {
        var parameter = Expression.Parameter(typeof(T));
        
        var leftVisitor = new ReplaceExpressionVisitor(first.Parameters[0], parameter);
        var left = leftVisitor.Visit(first.Body);
        
        var rightVisitor = new ReplaceExpressionVisitor(second.Parameters[0], parameter);
        var right = rightVisitor.Visit(second.Body);
        
        return Expression.Lambda<Func<T, bool>>(
            Expression.OrElse(left!, right!), parameter);
    }
    
    private class ReplaceExpressionVisitor : ExpressionVisitor
    {
        private readonly Expression _oldValue;
        private readonly Expression _newValue;
        
        public ReplaceExpressionVisitor(Expression oldValue, Expression newValue)
        {
            _oldValue = oldValue;
            _newValue = newValue;
        }
        
        public override Expression? Visit(Expression? node)
        {
            return node == _oldValue ? _newValue : base.Visit(node);
        }
    }
}
```

---

## 📊 Performance Tips

### 1. AsNoTracking cho Read-Only Queries
```csharp
var options = new QueryOptionsBuilder<Post>()
    .AsNoTracking()  // ✅ Faster for read-only
    .Build();
```

### 2. AsSplitQuery cho Multiple Includes
```csharp
var options = new QueryOptionsBuilder<Post>()
    .WithInclude(p => p.Author)
    .WithInclude(p => p.Category)
    .WithInclude(p => p.Comments)
    .AsSplitQuery()  // ✅ Avoid cartesian explosion
    .Build();
```

### 3. Projection thay vì Select All
```csharp
// ❌ BAD - Loads all columns
var posts = await _repository.GetAllAsync();

// ✅ GOOD - Only load needed columns
var posts = await _repository.GetQueryable()
    .Select(p => new PostDto(p.Id, p.Title, p.Author.FullName))
    .ToListAsync();
```

### 4. Count trước khi Load Data
```csharp
// Check count first
var count = await _repository.CountAsync(p => p.CategoryId == categoryId);
if (count == 0) return new List<PostDto>();

// Then load data
var posts = await _repository.FindAsync(p => p.CategoryId == categoryId);
```

---

## ✅ Best Practices

1. **Sử dụng QueryOptions cho complex queries**
   - Dễ đọc, dễ maintain
   - Tái sử dụng được

2. **AsNoTracking cho read-only queries**
   - Tăng performance
   - Giảm memory usage

3. **Projection (Select) thay vì load full entities**
   - Chỉ load columns cần thiết
   - Trả về DTOs

4. **AsSplitQuery khi có nhiều includes**
   - Tránh cartesian explosion
   - Tăng performance

5. **Pagination cho large datasets**
   - Luôn dùng Skip/Take
   - Trả về PagedResult với metadata

---

## 🎯 Kết luận

GenericRepository giờ đây rất mạnh mẽ với:
- ✅ Filtering linh hoạt
- ✅ Sorting đa dạng
- ✅ Pagination đầy đủ
- ✅ Includes cho eager loading
- ✅ Performance optimizations

Sử dụng đúng cách sẽ giúp code:
- 🚀 Nhanh hơn
- 📝 Dễ đọc hơn
- 🔧 Dễ maintain hơn
- ✨ Linh hoạt hơn
