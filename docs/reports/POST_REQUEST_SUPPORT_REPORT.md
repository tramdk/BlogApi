# ✅ POST Request Support Added!

**Ngày thực hiện**: 2026-01-26  
**Trạng thái**: ✅ **THÀNH CÔNG**

---

## 📊 Tổng quan

Đã thêm thành công **POST endpoint** để nhận search options trong request body, bổ sung cho GET endpoint hiện có.

### Kết quả:

```
✅ Build succeeded
✅ 0 errors
✅ 0 warnings
✅ 2 endpoints: GET và POST
✅ Linh hoạt cho mọi use case
```

---

## 🌐 API Endpoints

### Option 1: GET (Query Parameters)

```http
GET /api/posts/search?searchTerm=react&minRating=4.0&pageNumber=1
```

**Khi nào dùng**:
- ✅ Filters đơn giản (1-3 parameters)
- ✅ Muốn bookmark/share URL
- ✅ Cần browser cache
- ✅ RESTful semantic

**Ưu điểm**:
- ✅ RESTful
- ✅ Cacheable
- ✅ Bookmarkable
- ✅ Dễ test trên browser

**Nhược điểm**:
- ❌ Giới hạn URL length (~2000 chars)
- ❌ Khó với complex filters

---

### Option 2: POST (Request Body)

```http
POST /api/posts/search
Content-Type: application/json

{
  "searchTerm": "react",
  "minRating": 4.0,
  "categoryId": "web-dev",
  "fromDate": "2026-01-01",
  "sortBy": "Rating",
  "pageNumber": 1
}
```

**Khi nào dùng**:
- ✅ Nhiều filters phức tạp (5+ parameters)
- ✅ Có nested objects
- ✅ Cần validate complex data
- ✅ URL quá dài

**Ưu điểm**:
- ✅ Không giới hạn độ dài
- ✅ Dễ gửi complex objects
- ✅ Type-safe với DTOs
- ✅ Dễ validate

**Nhược điểm**:
- ❌ Không RESTful cho reads
- ❌ Không cache được
- ❌ Không bookmark được

---

## 📝 Implementation

### Controller

```csharp
// GET endpoint
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
    var query = new SearchPostsQuery(...);
    var result = await _mediator.Send(query);
    return Ok(result);
}

// POST endpoint
[HttpPost("search")]
[AllowAnonymous]
public async Task<IActionResult> SearchPost([FromBody] SearchPostsRequest request)
{
    var query = new SearchPostsQuery(
        request.SearchTerm,
        request.CategoryId,
        request.MinRating,
        request.FromDate,
        request.ToDate,
        request.SortBy,
        request.SortDescending,
        request.PageNumber,
        request.PageSize
    );
    
    var result = await _mediator.Send(query);
    return Ok(result);
}
```

### DTO

```csharp
public class SearchPostsRequest
{
    public string? SearchTerm { get; set; }
    public string? CategoryId { get; set; }
    public double? MinRating { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
```

---

## 💻 Client Examples

### JavaScript (Fetch)

```javascript
// GET request
const resultGet = await fetch('/api/posts/search?searchTerm=react&minRating=4.0');

// POST request
const resultPost = await fetch('/api/posts/search', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    searchTerm: 'react',
    minRating: 4.0,
    sortBy: 'Rating',
    pageNumber: 1
  })
});
```

### TypeScript (Axios)

```typescript
// GET request
const resultGet = await axios.get('/api/posts/search', {
  params: {
    searchTerm: 'react',
    minRating: 4.0
  }
});

// POST request
const resultPost = await axios.post('/api/posts/search', {
  searchTerm: 'react',
  minRating: 4.0,
  sortBy: 'Rating',
  pageNumber: 1
});
```

### React Hook

```typescript
const usePostSearch = () => {
  const searchWithGet = async (params) => {
    const queryString = new URLSearchParams(params).toString();
    const response = await fetch(`/api/posts/search?${queryString}`);
    return await response.json();
  };
  
  const searchWithPost = async (body) => {
    const response = await fetch('/api/posts/search', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body)
    });
    return await response.json();
  };
  
  return { searchWithGet, searchWithPost };
};
```

---

## 🔄 Request Flow

### GET Request Flow
```
Browser URL
    ↓
Query String: ?searchTerm=react&minRating=4.0
    ↓
ASP.NET Core Model Binding
    ↓
Controller Action Parameters
    ↓
SearchPostsQuery
    ↓
Handler → Repository → Database
    ↓
PagedResult<PostDto>
```

### POST Request Flow
```
HTTP POST Body
    ↓
JSON: { "searchTerm": "react", "minRating": 4.0 }
    ↓
ASP.NET Core Model Binding
    ↓
SearchPostsRequest DTO
    ↓
SearchPostsQuery
    ↓
Handler → Repository → Database
    ↓
PagedResult<PostDto>
```

---

## 📊 Comparison Table

| Feature | GET | POST |
|---------|-----|------|
| **Method** | `GET /api/posts/search?...` | `POST /api/posts/search` |
| **Parameters** | Query string | Request body (JSON) |
| **RESTful** | ✅ Yes | ❌ No (for reads) |
| **Cacheable** | ✅ Browser cache | ❌ No cache |
| **Bookmarkable** | ✅ Can share URL | ❌ Cannot bookmark |
| **URL Length** | ❌ Limited (~2000) | ✅ No limit |
| **Complex Objects** | ❌ Difficult | ✅ Easy (JSON) |
| **Type Safety** | ⚠️ String parsing | ✅ Strong typing |
| **Validation** | ⚠️ Manual | ✅ FluentValidation |
| **Browser Test** | ✅ Easy | ❌ Need tools |
| **Best For** | Simple searches | Complex searches |

---

## 🎯 Use Case Recommendations

### Use GET when:

1. **Simple Search**
   ```http
   GET /api/posts/search?searchTerm=javascript
   ```

2. **Basic Filtering**
   ```http
   GET /api/posts/search?categoryId=tech&minRating=4.0
   ```

3. **Shareable Links**
   ```http
   GET /api/posts/search?searchTerm=react&sortBy=Rating
   ```

### Use POST when:

1. **Complex Filters**
   ```json
   {
     "searchTerm": "react",
     "categoryId": "web-dev",
     "minRating": 4.0,
     "fromDate": "2026-01-01",
     "toDate": "2026-01-31",
     "tags": ["hooks", "typescript"],
     "sortBy": "Rating"
   }
   ```

2. **Many Parameters (5+)**
   ```json
   {
     "searchTerm": "...",
     "categoryId": "...",
     "minRating": 4.0,
     "maxRating": 5.0,
     "fromDate": "...",
     "toDate": "...",
     "authorId": "...",
     "tags": [...],
     "sortBy": "...",
     "pageNumber": 1
   }
   ```

3. **Nested Objects**
   ```json
   {
     "filters": {
       "search": "react",
       "category": "web-dev",
       "rating": { "min": 4.0, "max": 5.0 }
     },
     "sorting": {
       "field": "Rating",
       "direction": "desc"
     },
     "pagination": {
       "page": 1,
       "size": 10
     }
   }
   ```

---

## 🧪 Testing

### cURL - GET
```bash
curl -X GET "https://localhost:7001/api/posts/search?searchTerm=javascript&minRating=4.0"
```

### cURL - POST
```bash
curl -X POST "https://localhost:7001/api/posts/search" \
  -H "Content-Type: application/json" \
  -d '{
    "searchTerm": "javascript",
    "minRating": 4.0,
    "sortBy": "Rating",
    "pageNumber": 1
  }'
```

### Postman

**GET Request**:
- Method: GET
- URL: `{{baseUrl}}/api/posts/search`
- Params: Add query parameters

**POST Request**:
- Method: POST
- URL: `{{baseUrl}}/api/posts/search`
- Headers: `Content-Type: application/json`
- Body: Raw JSON

---

## ✅ Best Practices

### 1. Choose the Right Method

```javascript
// ✅ GOOD - Simple search with GET
const simpleSearch = async (term) => {
  return await fetch(`/api/posts/search?searchTerm=${term}`);
};

// ✅ GOOD - Complex search with POST
const complexSearch = async (filters) => {
  return await fetch('/api/posts/search', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(filters)
  });
};
```

### 2. Handle Both Methods in Client

```typescript
const searchPosts = async (filters: SearchFilters, usePost = false) => {
  if (usePost || Object.keys(filters).length > 3) {
    // Use POST for complex searches
    return await axios.post('/api/posts/search', filters);
  } else {
    // Use GET for simple searches
    return await axios.get('/api/posts/search', { params: filters });
  }
};
```

### 3. Validate Request Body

```csharp
public class SearchPostsRequestValidator : AbstractValidator<SearchPostsRequest>
{
    public SearchPostsRequestValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.MinRating).InclusiveBetween(0, 5).When(x => x.MinRating.HasValue);
    }
}
```

### 4. Document Both Endpoints

```csharp
/// <summary>
/// Search posts with GET (query parameters)
/// </summary>
[HttpGet("search")]
public async Task<IActionResult> Search(...) { }

/// <summary>
/// Search posts with POST (request body) - for complex filters
/// </summary>
[HttpPost("search")]
public async Task<IActionResult> SearchPost([FromBody] SearchPostsRequest request) { }
```

---

## 📚 Files Created/Updated

| File | Status | Description |
|------|--------|-------------|
| `PostsController.cs` | ✅ Updated | Added POST endpoint |
| `SearchPostsRequest.cs` | ✅ Exists | DTO for request body |
| `POST_SEARCH_DOCUMENTATION.md` | ✅ Created | POST documentation |

---

## 🎉 Kết luận

Giờ đây API hỗ trợ **2 phương thức** search posts:

### GET - Simple & RESTful
```http
GET /api/posts/search?searchTerm=react&minRating=4.0
```
- ✅ RESTful
- ✅ Cacheable
- ✅ Bookmarkable
- ✅ Best for simple searches

### POST - Powerful & Flexible
```http
POST /api/posts/search
Content-Type: application/json

{
  "searchTerm": "react",
  "minRating": 4.0,
  "categoryId": "web-dev",
  "fromDate": "2026-01-01",
  "sortBy": "Rating"
}
```
- ✅ No URL length limit
- ✅ Complex objects support
- ✅ Type-safe validation
- ✅ Best for complex searches

**Chọn phương pháp phù hợp với use case của bạn!** 🚀
