# 📮 POST Request for Search - Documentation

## 🎯 Tổng quan

Ngoài GET request với query parameters, API cũng hỗ trợ **POST request** với options trong body. Điều này hữu ích khi:
- ✅ Có nhiều filter options phức tạp
- ✅ Muốn gửi nested objects
- ✅ Cần validate complex data
- ✅ Tránh giới hạn độ dài URL

---

## 📍 Endpoints

### Option 1: GET (Query Parameters)
```
GET /api/posts/search?searchTerm=react&minRating=4.0&pageNumber=1
```

**Ưu điểm**:
- ✅ RESTful, semantic đúng
- ✅ Có thể bookmark URL
- ✅ Browser cache
- ✅ Dễ test trên browser

**Nhược điểm**:
- ❌ Giới hạn độ dài URL (~2000 chars)
- ❌ Khó với complex filters
- ❌ Không gửi được nested objects

### Option 2: POST (Request Body)
```
POST /api/posts/search
Content-Type: application/json

{
  "searchTerm": "react",
  "minRating": 4.0,
  "pageNumber": 1
}
```

**Ưu điểm**:
- ✅ Không giới hạn độ dài
- ✅ Dễ gửi complex objects
- ✅ Dễ validate
- ✅ Type-safe với DTOs

**Nhược điểm**:
- ❌ Không RESTful cho read operations
- ❌ Không cache được
- ❌ Không bookmark được

---

## 📝 Request Body Schema

### SearchPostsRequest

```json
{
  "searchTerm": "string | null",
  "categoryId": "string | null",
  "minRating": "number | null",
  "fromDate": "string (ISO 8601) | null",
  "toDate": "string (ISO 8601) | null",
  "sortBy": "string",
  "sortDescending": "boolean",
  "pageNumber": "number",
  "pageSize": "number"
}
```

### C# DTO

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

## 🔍 Example Requests

### 1. Simple Search

```http
POST /api/posts/search
Content-Type: application/json

{
  "searchTerm": "javascript",
  "pageNumber": 1,
  "pageSize": 10
}
```

**cURL**:
```bash
curl -X POST "https://localhost:7001/api/posts/search" \
  -H "Content-Type: application/json" \
  -d '{
    "searchTerm": "javascript",
    "pageNumber": 1,
    "pageSize": 10
  }'
```

---

### 2. Filter by Category

```http
POST /api/posts/search
Content-Type: application/json

{
  "categoryId": "tech-category-id",
  "sortBy": "Rating",
  "sortDescending": true,
  "pageNumber": 1,
  "pageSize": 20
}
```

**cURL**:
```bash
curl -X POST "https://localhost:7001/api/posts/search" \
  -H "Content-Type: application/json" \
  -d '{
    "categoryId": "tech-category-id",
    "sortBy": "Rating",
    "sortDescending": true,
    "pageNumber": 1,
    "pageSize": 20
  }'
```

---

### 3. Complex Search with All Filters

```http
POST /api/posts/search
Content-Type: application/json

{
  "searchTerm": "react",
  "categoryId": "web-dev",
  "minRating": 4.0,
  "fromDate": "2026-01-01T00:00:00Z",
  "toDate": "2026-01-31T23:59:59Z",
  "sortBy": "Rating",
  "sortDescending": true,
  "pageNumber": 1,
  "pageSize": 15
}
```

**cURL**:
```bash
curl -X POST "https://localhost:7001/api/posts/search" \
  -H "Content-Type: application/json" \
  -d '{
    "searchTerm": "react",
    "categoryId": "web-dev",
    "minRating": 4.0,
    "fromDate": "2026-01-01T00:00:00Z",
    "toDate": "2026-01-31T23:59:59Z",
    "sortBy": "Rating",
    "sortDescending": true,
    "pageNumber": 1,
    "pageSize": 15
  }'
```

---

### 4. Only Pagination (No Filters)

```http
POST /api/posts/search
Content-Type: application/json

{
  "pageNumber": 2,
  "pageSize": 20
}
```

---

## 💻 Client-Side Examples

### JavaScript (Fetch API)

```javascript
const searchPosts = async (filters) => {
  const response = await fetch('/api/posts/search', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(filters)
  });
  
  return await response.json();
};

// Usage
const result = await searchPosts({
  searchTerm: 'javascript',
  minRating: 4.0,
  sortBy: 'Rating',
  sortDescending: true,
  pageNumber: 1,
  pageSize: 10
});

console.log(`Found ${result.totalCount} posts`);
console.log(result.items);
```

---

### TypeScript (Axios)

```typescript
import axios from 'axios';

interface SearchPostsRequest {
  searchTerm?: string;
  categoryId?: string;
  minRating?: number;
  fromDate?: string;
  toDate?: string;
  sortBy?: 'Title' | 'Rating' | 'CreatedAt';
  sortDescending?: boolean;
  pageNumber?: number;
  pageSize?: number;
}

interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

interface PostDto {
  id: string;
  title: string;
  authorName: string;
  averageRating: number;
  createdAt: string;
  categoryId: string | null;
}

const searchPosts = async (
  request: SearchPostsRequest
): Promise<PagedResult<PostDto>> => {
  const response = await axios.post<PagedResult<PostDto>>(
    '/api/posts/search',
    request
  );
  return response.data;
};

// Usage
const result = await searchPosts({
  searchTerm: 'react',
  categoryId: 'web-dev',
  minRating: 4.0,
  sortBy: 'Rating',
  sortDescending: true,
  pageNumber: 1,
  pageSize: 10
});
```

---

### React Hook

```typescript
import { useState } from 'react';
import axios from 'axios';

interface SearchFilters {
  searchTerm?: string;
  categoryId?: string;
  minRating?: number;
  sortBy?: string;
  sortDescending?: boolean;
  pageNumber?: number;
  pageSize?: number;
}

const usePostSearch = () => {
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  
  const search = async (filters: SearchFilters) => {
    setLoading(true);
    setError(null);
    
    try {
      const response = await axios.post('/api/posts/search', filters);
      setData(response.data);
    } catch (err) {
      setError(err);
    } finally {
      setLoading(false);
    }
  };
  
  return { data, loading, error, search };
};

// Usage in component
const SearchComponent = () => {
  const { data, loading, error, search } = usePostSearch();
  const [filters, setFilters] = useState({
    searchTerm: '',
    minRating: null,
    sortBy: 'CreatedAt',
    pageNumber: 1,
    pageSize: 10
  });
  
  const handleSearch = () => {
    search(filters);
  };
  
  return (
    <div>
      <input
        value={filters.searchTerm}
        onChange={(e) => setFilters({ ...filters, searchTerm: e.target.value })}
      />
      <button onClick={handleSearch}>Search</button>
      
      {loading && <div>Loading...</div>}
      {error && <div>Error: {error.message}</div>}
      {data && (
        <div>
          {data.items.map(post => (
            <div key={post.id}>{post.title}</div>
          ))}
        </div>
      )}
    </div>
  );
};
```

---

### React with Form

```typescript
import React, { useState } from 'react';

const PostSearchForm: React.FC = () => {
  const [formData, setFormData] = useState({
    searchTerm: '',
    categoryId: '',
    minRating: '',
    sortBy: 'CreatedAt',
    sortDescending: true,
    pageNumber: 1,
    pageSize: 10
  });
  
  const [results, setResults] = useState(null);
  
  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    // Build request body (only include non-empty values)
    const requestBody = {
      searchTerm: formData.searchTerm || undefined,
      categoryId: formData.categoryId || undefined,
      minRating: formData.minRating ? parseFloat(formData.minRating) : undefined,
      sortBy: formData.sortBy,
      sortDescending: formData.sortDescending,
      pageNumber: formData.pageNumber,
      pageSize: formData.pageSize
    };
    
    const response = await fetch('/api/posts/search', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(requestBody)
    });
    
    const data = await response.json();
    setResults(data);
  };
  
  return (
    <form onSubmit={handleSubmit}>
      <input
        type="text"
        placeholder="Search..."
        value={formData.searchTerm}
        onChange={(e) => setFormData({ ...formData, searchTerm: e.target.value })}
      />
      
      <select
        value={formData.categoryId}
        onChange={(e) => setFormData({ ...formData, categoryId: e.target.value })}
      >
        <option value="">All Categories</option>
        <option value="tech">Technology</option>
        <option value="web-dev">Web Development</option>
      </select>
      
      <input
        type="number"
        placeholder="Min Rating"
        min="0"
        max="5"
        step="0.1"
        value={formData.minRating}
        onChange={(e) => setFormData({ ...formData, minRating: e.target.value })}
      />
      
      <select
        value={formData.sortBy}
        onChange={(e) => setFormData({ ...formData, sortBy: e.target.value })}
      >
        <option value="CreatedAt">Date</option>
        <option value="Title">Title</option>
        <option value="Rating">Rating</option>
      </select>
      
      <label>
        <input
          type="checkbox"
          checked={formData.sortDescending}
          onChange={(e) => setFormData({ ...formData, sortDescending: e.target.checked })}
        />
        Descending
      </label>
      
      <button type="submit">Search</button>
      
      {results && (
        <div>
          <p>Found {results.totalCount} posts</p>
          {results.items.map(post => (
            <div key={post.id}>
              <h3>{post.title}</h3>
              <p>By {post.authorName} - ⭐ {post.averageRating}</p>
            </div>
          ))}
        </div>
      )}
    </form>
  );
};
```

---

## 🧪 Testing

### Postman

1. **Create new request**:
   - Method: `POST`
   - URL: `{{baseUrl}}/api/posts/search`

2. **Headers**:
   - `Content-Type`: `application/json`

3. **Body** (raw JSON):
```json
{
  "searchTerm": "javascript",
  "categoryId": "tech",
  "minRating": 4.0,
  "sortBy": "Rating",
  "sortDescending": true,
  "pageNumber": 1,
  "pageSize": 10
}
```

4. **Click Send**

---

### Swagger UI

1. Navigate to `/swagger`
2. Find `POST /api/posts/search`
3. Click "Try it out"
4. Edit request body JSON
5. Click "Execute"

---

### cURL Examples

#### Minimal Request
```bash
curl -X POST "https://localhost:7001/api/posts/search" \
  -H "Content-Type: application/json" \
  -d '{}'
```

#### With Search Term
```bash
curl -X POST "https://localhost:7001/api/posts/search" \
  -H "Content-Type: application/json" \
  -d '{
    "searchTerm": "javascript"
  }'
```

#### Full Request
```bash
curl -X POST "https://localhost:7001/api/posts/search" \
  -H "Content-Type: application/json" \
  -d '{
    "searchTerm": "react",
    "categoryId": "web-dev",
    "minRating": 4.0,
    "fromDate": "2026-01-01T00:00:00Z",
    "toDate": "2026-01-31T23:59:59Z",
    "sortBy": "Rating",
    "sortDescending": true,
    "pageNumber": 1,
    "pageSize": 15
  }'
```

---

## 📊 Response Format

Giống như GET request:

```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "title": "Getting Started with React",
      "authorName": "Jane Smith",
      "averageRating": 4.8,
      "createdAt": "2026-01-15T10:30:00Z",
      "categoryId": "web-dev"
    }
  ],
  "totalCount": 8,
  "pageNumber": 1,
  "pageSize": 15,
  "totalPages": 1,
  "hasPreviousPage": false,
  "hasNextPage": false
}
```

---

## 🎯 Khi nào dùng GET vs POST?

### Dùng GET khi:
- ✅ Filters đơn giản (1-3 parameters)
- ✅ Muốn bookmark/share URL
- ✅ Cần browser cache
- ✅ RESTful semantic quan trọng

**Example**: Simple search, basic filtering

### Dùng POST khi:
- ✅ Nhiều filters phức tạp (5+ parameters)
- ✅ Có nested objects
- ✅ Cần validate complex data
- ✅ URL quá dài

**Example**: Advanced search, complex filters, reporting

---

## ✅ Best Practices

### 1. Validate Request Body

```csharp
public class SearchPostsRequestValidator : AbstractValidator<SearchPostsRequest>
{
    public SearchPostsRequestValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0);
            
        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .LessThanOrEqualTo(100);
            
        RuleFor(x => x.MinRating)
            .InclusiveBetween(0, 5)
            .When(x => x.MinRating.HasValue);
    }
}
```

### 2. Handle Null/Empty Values

```csharp
// Client-side: Don't send null/empty values
const requestBody = {
  searchTerm: searchTerm || undefined,  // undefined won't be serialized
  categoryId: categoryId || undefined,
  minRating: minRating || undefined
};
```

### 3. Use Default Values

```csharp
public class SearchPostsRequest
{
    public string SortBy { get; set; } = "CreatedAt";  // Default
    public bool SortDescending { get; set; } = true;   // Default
    public int PageNumber { get; set; } = 1;           // Default
    public int PageSize { get; set; } = 10;            // Default
}
```

### 4. Document with XML Comments

```csharp
/// <summary>
/// Search posts with POST request (for complex filters in body)
/// </summary>
/// <param name="request">Search request with filters</param>
/// <returns>Paged result with posts</returns>
[HttpPost("search")]
public async Task<IActionResult> SearchPost([FromBody] SearchPostsRequest request)
```

---

## 🔄 Comparison Table

| Feature | GET (Query Params) | POST (Body) |
|---------|-------------------|-------------|
| **RESTful** | ✅ Yes | ❌ No (for reads) |
| **Cacheable** | ✅ Yes | ❌ No |
| **Bookmarkable** | ✅ Yes | ❌ No |
| **URL Length Limit** | ❌ ~2000 chars | ✅ No limit |
| **Complex Objects** | ❌ Difficult | ✅ Easy |
| **Validation** | ⚠️ Manual | ✅ FluentValidation |
| **Type Safety** | ⚠️ String parsing | ✅ Strong typing |
| **Browser Testing** | ✅ Easy | ❌ Need tools |

---

## 🎉 Kết luận

Bạn giờ có **2 options** để search posts:

### Option 1: GET (Recommended cho simple searches)
```http
GET /api/posts/search?searchTerm=react&minRating=4.0
```

### Option 2: POST (Recommended cho complex searches)
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

**Chọn phương pháp phù hợp với use case của bạn!** 🚀
