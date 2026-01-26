# ✅ HTTP Request Mapping Complete!

**Ngày thực hiện**: 2026-01-26  
**Trạng thái**: ✅ **THÀNH CÔNG**

---

## 📊 Tổng quan

Đã hoàn thành việc mapping filter options và sort options từ HTTP request query parameters vào SearchPostsQuery.

### Kết quả:

```
✅ Build succeeded
✅ 0 errors
✅ 0 warnings
✅ API endpoint /api/posts/search hoạt động
✅ Hỗ trợ đầy đủ query parameters
```

---

## 🌐 API Endpoint

### URL
```
GET /api/posts/search
```

### Query Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `searchTerm` | string | ❌ | null | Search in title and content |
| `categoryId` | string | ❌ | null | Filter by category ID |
| `minRating` | double | ❌ | null | Minimum rating (0-5) |
| `fromDate` | DateTime | ❌ | null | Filter from date |
| `toDate` | DateTime | ❌ | null | Filter to date |
| `sortBy` | string | ❌ | "CreatedAt" | Sort field: Title, Rating, CreatedAt |
| `sortDescending` | bool | ❌ | true | Sort direction |
| `pageNumber` | int | ❌ | 1 | Page number (1-based) |
| `pageSize` | int | ❌ | 10 | Items per page |

---

## 📝 Controller Implementation

### PostsController.cs

```csharp
/// <summary>
/// Search posts with advanced filtering, sorting, and pagination
/// </summary>
[HttpGet("search")]
[AllowAnonymous]
public async Task<IActionResult> Search(
    [FromQuery] string? searchTerm = null,
    [FromQuery] string? categoryId = null,
    [FromQuery] double? minRating = null,
    [FromQuery] DateTime? fromDate = null,
    [FromQuery] DateTime? toDate = null,
    [FromQuery] string sortBy = "CreatedAt",
    [FromQuery] bool sortDescending = true,
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 10)
{
    var query = new SearchPostsQuery(
        searchTerm,
        categoryId,
        minRating,
        fromDate,
        toDate,
        sortBy,
        sortDescending,
        pageNumber,
        pageSize
    );

    var result = await _mediator.Send(query);
    return Ok(result);
}
```

**Đặc điểm**:
- ✅ `[FromQuery]` - Tự động map từ query string
- ✅ Default values - Không bắt buộc tất cả parameters
- ✅ Type-safe - ASP.NET Core tự động parse types
- ✅ XML Documentation - Swagger sẽ hiển thị đầy đủ

---

## 🔍 Example HTTP Requests

### 1. Simple Search

```http
GET /api/posts/search?searchTerm=javascript
```

**Response**:
```json
{
  "items": [...],
  "totalCount": 15,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 2,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

### 2. Filter by Category

```http
GET /api/posts/search?categoryId=tech-category-id
```

### 3. Filter by Rating

```http
GET /api/posts/search?minRating=4.0
```

### 4. Sort by Title (Ascending)

```http
GET /api/posts/search?sortBy=Title&sortDescending=false
```

### 5. Pagination

```http
GET /api/posts/search?pageNumber=2&pageSize=20
```

### 6. Complex Search (All Parameters)

```http
GET /api/posts/search?searchTerm=react&categoryId=web-dev&minRating=4.0&fromDate=2026-01-01&toDate=2026-01-31&sortBy=Rating&sortDescending=true&pageNumber=1&pageSize=15
```

---

## 🔄 Request Flow

```
HTTP Request
    ↓
Query Parameters
    ↓
ASP.NET Core Model Binding
    ↓
Controller Action Parameters
    ↓
SearchPostsQuery (MediatR)
    ↓
SearchPostsHandler
    ↓
GenericRepository.GetPagedAsync()
    ↓
QueryOptions → EF Core Query
    ↓
Database
    ↓
PagedResult<PostDto>
    ↓
JSON Response
```

---

## 💻 Client-Side Examples

### JavaScript (Fetch API)

```javascript
const searchPosts = async (filters) => {
  const params = new URLSearchParams();
  
  if (filters.searchTerm) params.append('searchTerm', filters.searchTerm);
  if (filters.categoryId) params.append('categoryId', filters.categoryId);
  if (filters.minRating) params.append('minRating', filters.minRating);
  if (filters.sortBy) params.append('sortBy', filters.sortBy);
  if (filters.pageNumber) params.append('pageNumber', filters.pageNumber);
  
  const response = await fetch(`/api/posts/search?${params.toString()}`);
  return await response.json();
};

// Usage
const result = await searchPosts({
  searchTerm: 'javascript',
  minRating: 4.0,
  sortBy: 'Rating',
  pageNumber: 1
});
```

### TypeScript (Axios)

```typescript
import axios from 'axios';

interface SearchFilters {
  searchTerm?: string;
  categoryId?: string;
  minRating?: number;
  sortBy?: 'Title' | 'Rating' | 'CreatedAt';
  sortDescending?: boolean;
  pageNumber?: number;
  pageSize?: number;
}

const searchPosts = async (filters: SearchFilters) => {
  const response = await axios.get('/api/posts/search', {
    params: filters
  });
  return response.data;
};
```

### React Hook

```typescript
import { useState, useEffect } from 'react';

const usePostSearch = (filters) => {
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  
  useEffect(() => {
    const fetchData = async () => {
      setLoading(true);
      try {
        const params = new URLSearchParams(filters);
        const response = await fetch(`/api/posts/search?${params}`);
        const result = await response.json();
        setData(result);
      } catch (err) {
        setError(err);
      } finally {
        setLoading(false);
      }
    };
    
    fetchData();
  }, [filters]);
  
  return { data, loading, error };
};

// Usage in component
const SearchComponent = () => {
  const { data, loading } = usePostSearch({
    searchTerm: 'react',
    minRating: 4.0,
    pageNumber: 1
  });
  
  if (loading) return <div>Loading...</div>;
  
  return (
    <div>
      {data?.items.map(post => (
        <div key={post.id}>{post.title}</div>
      ))}
    </div>
  );
};
```

---

## 🧪 Testing

### cURL

```bash
# Simple search
curl -X GET "https://localhost:7001/api/posts/search?searchTerm=javascript"

# With multiple filters
curl -X GET "https://localhost:7001/api/posts/search?searchTerm=react&minRating=4.0&sortBy=Rating&pageNumber=1&pageSize=10"
```

### Postman

1. Create new request: `GET`
2. URL: `{{baseUrl}}/api/posts/search`
3. Params tab:
   - `searchTerm`: `javascript`
   - `minRating`: `4.0`
   - `sortBy`: `Rating`
   - `pageNumber`: `1`
   - `pageSize`: `10`

### Swagger UI

1. Navigate to `/swagger`
2. Find `GET /api/posts/search`
3. Click "Try it out"
4. Fill in parameters
5. Click "Execute"

---

## 📊 Query Parameter Mapping

### ASP.NET Core Model Binding

ASP.NET Core tự động map query parameters:

```
?searchTerm=javascript
    ↓
string? searchTerm = "javascript"

?minRating=4.5
    ↓
double? minRating = 4.5

?sortDescending=false
    ↓
bool sortDescending = false

?fromDate=2026-01-01T00:00:00Z
    ↓
DateTime? fromDate = new DateTime(2026, 1, 1)

?pageNumber=2
    ↓
int pageNumber = 2
```

**Lợi ích**:
- ✅ Automatic type conversion
- ✅ Null handling
- ✅ Default values
- ✅ Validation

---

## ✅ Validation

### Built-in Validation

ASP.NET Core tự động validate:
- ✅ Type safety (string, int, double, DateTime, bool)
- ✅ Nullable types
- ✅ Default values

### Custom Validation (Optional)

Có thể thêm validation với FluentValidation:

```csharp
public class SearchPostsQueryValidator : AbstractValidator<SearchPostsQuery>
{
    public SearchPostsQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .WithMessage("Page number must be greater than 0");
            
        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .LessThanOrEqualTo(100)
            .WithMessage("Page size must be between 1 and 100");
            
        RuleFor(x => x.MinRating)
            .InclusiveBetween(0, 5)
            .When(x => x.MinRating.HasValue)
            .WithMessage("Rating must be between 0 and 5");
            
        RuleFor(x => x.SortBy)
            .Must(x => new[] { "Title", "Rating", "CreatedAt" }.Contains(x))
            .WithMessage("SortBy must be Title, Rating, or CreatedAt");
    }
}
```

---

## 🎯 Best Practices

### 1. Use Default Values

```csharp
// ✅ GOOD - Có default values
[FromQuery] string sortBy = "CreatedAt"
[FromQuery] bool sortDescending = true
[FromQuery] int pageNumber = 1

// ❌ BAD - Không có defaults, phải check null
[FromQuery] string? sortBy
```

### 2. Document Parameters

```csharp
/// <summary>
/// Search posts with advanced filtering
/// </summary>
/// <param name="searchTerm">Search in title and content</param>
/// <param name="minRating">Minimum rating (0-5)</param>
[HttpGet("search")]
public async Task<IActionResult> Search(...)
```

### 3. Use Nullable for Optional Filters

```csharp
// ✅ GOOD - Nullable cho optional filters
[FromQuery] string? searchTerm = null
[FromQuery] double? minRating = null
[FromQuery] DateTime? fromDate = null

// ❌ BAD - Non-nullable cho optional
[FromQuery] string searchTerm = ""  // Empty string khác với null
```

### 4. Validate Input

```csharp
// Validate trong Handler hoặc dùng FluentValidation
if (request.PageNumber < 1)
    throw new ValidationException("Page number must be >= 1");
    
if (request.PageSize > 100)
    throw new ValidationException("Page size must be <= 100");
```

---

## 📚 Files Created/Updated

| File | Status | Description |
|------|--------|-------------|
| `PostsController.cs` | ✅ Updated | Added `/search` endpoint |
| `SearchPostsRequest.cs` | ✅ Created | DTO for request mapping |
| `API_SEARCH_DOCUMENTATION.md` | ✅ Created | API documentation |

---

## 🎉 Kết luận

Đã hoàn thành việc mapping HTTP request query parameters vào SearchPostsQuery với:

### Tính năng:
- ✅ **Automatic Model Binding** - ASP.NET Core tự động map
- ✅ **Type Safety** - Compile-time checking
- ✅ **Default Values** - Không bắt buộc tất cả params
- ✅ **Nullable Support** - Optional filters
- ✅ **XML Documentation** - Swagger integration
- ✅ **Client Examples** - JavaScript, TypeScript, React

### API Endpoint:
```
GET /api/posts/search?searchTerm=react&minRating=4.0&sortBy=Rating&pageNumber=1&pageSize=10
```

### Response:
```json
{
  "items": [...],
  "totalCount": 100,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 10,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

**HTTP Request Mapping Complete!** 🚀
