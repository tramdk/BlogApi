# 🌐 API Endpoints - Filter, Sort & Pagination

## 📍 Search Posts Endpoint

### Endpoint
```
GET /api/posts/search
```

### Description
Search and filter posts with advanced options including filtering, sorting, and pagination.

---

## 📝 Query Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `searchTerm` | string | No | null | Search in title and content |
| `categoryId` | string | No | null | Filter by category ID |
| `minRating` | double | No | null | Minimum rating (0-5) |
| `fromDate` | DateTime | No | null | Filter posts from this date |
| `toDate` | DateTime | No | null | Filter posts to this date |
| `sortBy` | string | No | "CreatedAt" | Sort field: `Title`, `Rating`, `CreatedAt` |
| `sortDescending` | bool | No | true | Sort direction: `true` (desc) or `false` (asc) |
| `pageNumber` | int | No | 1 | Page number (1-based) |
| `pageSize` | int | No | 10 | Number of items per page |

---

## 📊 Response Format

```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "title": "Post Title",
      "authorName": "John Doe",
      "averageRating": 4.5,
      "createdAt": "2026-01-26T00:00:00Z",
      "categoryId": "category-id"
    }
  ],
  "totalCount": 100,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 10,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

---

## 🔍 Example Requests

### 1. Simple Search

Search posts containing "javascript":

```http
GET /api/posts/search?searchTerm=javascript
```

**cURL**:
```bash
curl -X GET "https://localhost:7001/api/posts/search?searchTerm=javascript"
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

---

### 2. Filter by Category

Get posts in specific category:

```http
GET /api/posts/search?categoryId=tech-category-id
```

**cURL**:
```bash
curl -X GET "https://localhost:7001/api/posts/search?categoryId=tech-category-id"
```

---

### 3. Filter by Rating

Get posts with rating >= 4.0:

```http
GET /api/posts/search?minRating=4.0
```

**cURL**:
```bash
curl -X GET "https://localhost:7001/api/posts/search?minRating=4.0"
```

---

### 4. Filter by Date Range

Get posts created in January 2026:

```http
GET /api/posts/search?fromDate=2026-01-01&toDate=2026-01-31
```

**cURL**:
```bash
curl -X GET "https://localhost:7001/api/posts/search?fromDate=2026-01-01&toDate=2026-01-31"
```

---

### 5. Sort by Title (Ascending)

```http
GET /api/posts/search?sortBy=Title&sortDescending=false
```

**cURL**:
```bash
curl -X GET "https://localhost:7001/api/posts/search?sortBy=Title&sortDescending=false"
```

---

### 6. Sort by Rating (Descending)

```http
GET /api/posts/search?sortBy=Rating&sortDescending=true
```

**cURL**:
```bash
curl -X GET "https://localhost:7001/api/posts/search?sortBy=Rating&sortDescending=true"
```

---

### 7. Pagination

Get page 2 with 20 items per page:

```http
GET /api/posts/search?pageNumber=2&pageSize=20
```

**cURL**:
```bash
curl -X GET "https://localhost:7001/api/posts/search?pageNumber=2&pageSize=20"
```

---

### 8. Complex Search (Multiple Filters)

Search posts with:
- Contains "react" in title/content
- In category "web-dev"
- Rating >= 4.0
- Created after 2026-01-01
- Sorted by rating (descending)
- Page 1, 15 items per page

```http
GET /api/posts/search?searchTerm=react&categoryId=web-dev&minRating=4.0&fromDate=2026-01-01&sortBy=Rating&sortDescending=true&pageNumber=1&pageSize=15
```

**cURL**:
```bash
curl -X GET "https://localhost:7001/api/posts/search?searchTerm=react&categoryId=web-dev&minRating=4.0&fromDate=2026-01-01&sortBy=Rating&sortDescending=true&pageNumber=1&pageSize=15"
```

**Response**:
```json
{
  "items": [
    {
      "id": "post-1-id",
      "title": "Getting Started with React",
      "authorName": "Jane Smith",
      "averageRating": 4.8,
      "createdAt": "2026-01-15T10:30:00Z",
      "categoryId": "web-dev"
    },
    {
      "id": "post-2-id",
      "title": "React Hooks Deep Dive",
      "authorName": "John Doe",
      "averageRating": 4.5,
      "createdAt": "2026-01-20T14:20:00Z",
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

## 🔧 JavaScript/TypeScript Examples

### Using Fetch API

```javascript
// Simple search
const searchPosts = async (searchTerm) => {
  const response = await fetch(
    `/api/posts/search?searchTerm=${encodeURIComponent(searchTerm)}`
  );
  const data = await response.json();
  return data;
};

// Complex search with multiple filters
const advancedSearch = async (filters) => {
  const params = new URLSearchParams();
  
  if (filters.searchTerm) params.append('searchTerm', filters.searchTerm);
  if (filters.categoryId) params.append('categoryId', filters.categoryId);
  if (filters.minRating) params.append('minRating', filters.minRating);
  if (filters.fromDate) params.append('fromDate', filters.fromDate.toISOString());
  if (filters.toDate) params.append('toDate', filters.toDate.toISOString());
  if (filters.sortBy) params.append('sortBy', filters.sortBy);
  if (filters.sortDescending !== undefined) params.append('sortDescending', filters.sortDescending);
  if (filters.pageNumber) params.append('pageNumber', filters.pageNumber);
  if (filters.pageSize) params.append('pageSize', filters.pageSize);
  
  const response = await fetch(`/api/posts/search?${params.toString()}`);
  const data = await response.json();
  return data;
};

// Usage
const result = await advancedSearch({
  searchTerm: 'javascript',
  categoryId: 'web-dev',
  minRating: 4.0,
  sortBy: 'Rating',
  sortDescending: true,
  pageNumber: 1,
  pageSize: 10
});

console.log(`Found ${result.totalCount} posts`);
console.log(`Page ${result.pageNumber} of ${result.totalPages}`);
console.log(result.items);
```

### Using Axios

```typescript
import axios from 'axios';

interface SearchFilters {
  searchTerm?: string;
  categoryId?: string;
  minRating?: number;
  fromDate?: Date;
  toDate?: Date;
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
  createdAt: Date;
  categoryId: string | null;
}

const searchPosts = async (filters: SearchFilters): Promise<PagedResult<PostDto>> => {
  const response = await axios.get<PagedResult<PostDto>>('/api/posts/search', {
    params: filters
  });
  return response.data;
};

// Usage
const result = await searchPosts({
  searchTerm: 'react',
  minRating: 4.0,
  sortBy: 'Rating',
  pageNumber: 1,
  pageSize: 20
});
```

---

## 🎨 React Example

```tsx
import React, { useState, useEffect } from 'react';

interface SearchFilters {
  searchTerm: string;
  categoryId: string;
  minRating: number | null;
  sortBy: string;
  sortDescending: boolean;
  pageNumber: number;
  pageSize: number;
}

const PostSearchComponent: React.FC = () => {
  const [filters, setFilters] = useState<SearchFilters>({
    searchTerm: '',
    categoryId: '',
    minRating: null,
    sortBy: 'CreatedAt',
    sortDescending: true,
    pageNumber: 1,
    pageSize: 10
  });
  
  const [results, setResults] = useState<PagedResult<PostDto> | null>(null);
  const [loading, setLoading] = useState(false);
  
  const searchPosts = async () => {
    setLoading(true);
    try {
      const params = new URLSearchParams();
      if (filters.searchTerm) params.append('searchTerm', filters.searchTerm);
      if (filters.categoryId) params.append('categoryId', filters.categoryId);
      if (filters.minRating) params.append('minRating', filters.minRating.toString());
      params.append('sortBy', filters.sortBy);
      params.append('sortDescending', filters.sortDescending.toString());
      params.append('pageNumber', filters.pageNumber.toString());
      params.append('pageSize', filters.pageSize.toString());
      
      const response = await fetch(`/api/posts/search?${params.toString()}`);
      const data = await response.json();
      setResults(data);
    } catch (error) {
      console.error('Search failed:', error);
    } finally {
      setLoading(false);
    }
  };
  
  useEffect(() => {
    searchPosts();
  }, [filters]);
  
  return (
    <div>
      <div className="filters">
        <input
          type="text"
          placeholder="Search..."
          value={filters.searchTerm}
          onChange={(e) => setFilters({ ...filters, searchTerm: e.target.value })}
        />
        
        <select
          value={filters.sortBy}
          onChange={(e) => setFilters({ ...filters, sortBy: e.target.value })}
        >
          <option value="CreatedAt">Date</option>
          <option value="Title">Title</option>
          <option value="Rating">Rating</option>
        </select>
        
        <input
          type="number"
          placeholder="Min Rating"
          min="0"
          max="5"
          step="0.1"
          value={filters.minRating || ''}
          onChange={(e) => setFilters({ 
            ...filters, 
            minRating: e.target.value ? parseFloat(e.target.value) : null 
          })}
        />
      </div>
      
      {loading && <div>Loading...</div>}
      
      {results && (
        <>
          <div className="results">
            {results.items.map(post => (
              <div key={post.id} className="post-card">
                <h3>{post.title}</h3>
                <p>By {post.authorName}</p>
                <p>Rating: {post.averageRating} ⭐</p>
              </div>
            ))}
          </div>
          
          <div className="pagination">
            <button 
              disabled={!results.hasPreviousPage}
              onClick={() => setFilters({ ...filters, pageNumber: filters.pageNumber - 1 })}
            >
              Previous
            </button>
            
            <span>Page {results.pageNumber} of {results.totalPages}</span>
            
            <button 
              disabled={!results.hasNextPage}
              onClick={() => setFilters({ ...filters, pageNumber: filters.pageNumber + 1 })}
            >
              Next
            </button>
          </div>
        </>
      )}
    </div>
  );
};
```

---

## 📱 Mobile (React Native) Example

```typescript
import React, { useState } from 'react';
import { View, TextInput, FlatList, Text, TouchableOpacity } from 'react-native';

const PostSearchScreen = () => {
  const [searchTerm, setSearchTerm] = useState('');
  const [posts, setPosts] = useState([]);
  const [page, setPage] = useState(1);
  const [hasMore, setHasMore] = useState(true);
  
  const searchPosts = async (pageNum = 1) => {
    const response = await fetch(
      `/api/posts/search?searchTerm=${searchTerm}&pageNumber=${pageNum}&pageSize=20`
    );
    const data = await response.json();
    
    if (pageNum === 1) {
      setPosts(data.items);
    } else {
      setPosts([...posts, ...data.items]);
    }
    
    setHasMore(data.hasNextPage);
    setPage(pageNum);
  };
  
  const loadMore = () => {
    if (hasMore) {
      searchPosts(page + 1);
    }
  };
  
  return (
    <View>
      <TextInput
        placeholder="Search posts..."
        value={searchTerm}
        onChangeText={setSearchTerm}
        onSubmitEditing={() => searchPosts(1)}
      />
      
      <FlatList
        data={posts}
        keyExtractor={(item) => item.id}
        renderItem={({ item }) => (
          <View>
            <Text>{item.title}</Text>
            <Text>By {item.authorName}</Text>
            <Text>⭐ {item.averageRating}</Text>
          </View>
        )}
        onEndReached={loadMore}
        onEndReachedThreshold={0.5}
      />
    </View>
  );
};
```

---

## 🧪 Testing with Postman

### Collection Setup

1. **Create new request**: `Search Posts`
2. **Method**: `GET`
3. **URL**: `{{baseUrl}}/api/posts/search`
4. **Params**:
   - `searchTerm`: `javascript`
   - `minRating`: `4.0`
   - `sortBy`: `Rating`
   - `pageNumber`: `1`
   - `pageSize`: `10`

### Environment Variables
```json
{
  "baseUrl": "https://localhost:7001"
}
```

---

## ✅ Validation

### Query Parameter Validation

The API automatically validates:
- ✅ `pageNumber` must be >= 1
- ✅ `pageSize` must be >= 1 and <= 100
- ✅ `minRating` must be between 0 and 5
- ✅ `sortBy` must be one of: `Title`, `Rating`, `CreatedAt`
- ✅ Date formats must be valid ISO 8601

---

## 🎯 Best Practices

1. **Always use pagination** - Don't request all items at once
2. **Cache results** - Cache search results on client side
3. **Debounce search input** - Wait for user to stop typing
4. **Show loading states** - Provide feedback during search
5. **Handle errors gracefully** - Show user-friendly error messages
6. **Use appropriate page sizes** - 10-20 for lists, 50-100 for tables

---

## 📊 Performance Tips

1. **Use `AsNoTracking`** - Already enabled in SearchPostsQuery
2. **Limit page size** - Max 100 items per page
3. **Index database columns** - Index on `CreatedAt`, `AverageRating`, `CategoryId`
4. **Cache frequently searched queries** - Use Redis for popular searches
5. **Use CDN** - Cache API responses on CDN edge servers

---

**API Documentation Complete!** 🎉
