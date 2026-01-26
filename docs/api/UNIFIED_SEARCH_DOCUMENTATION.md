# 🎯 Unified Search API - One Endpoint to Rule Them All

## 📍 Endpoint

```
GET  /api/posts/unified
POST /api/posts/unified
```

**One endpoint, multiple approaches!**

---

## 🎯 Tổng quan

Unified Search API là một endpoint duy nhất có thể xử lý **3 approaches** khác nhau:

1. ✅ **GET with Query Parameters** - Simple searches
2. ✅ **POST with Simple Body** - Medium complexity
3. ✅ **POST with FilterModel** - Advanced data grids

API tự động **detect** approach nào đang được sử dụng và xử lý phù hợp!

---

## 🔍 Approach 1: GET with Query Parameters

### Simple Search

```http
GET /api/posts/unified?searchTerm=react&minRating=4.0&sortBy=Rating&page=1&pageSize=10
```

**cURL**:
```bash
curl -X GET "https://localhost:7001/api/posts/unified?searchTerm=react&minRating=4.0"
```

**JavaScript**:
```javascript
const result = await fetch('/api/posts/unified?searchTerm=react&minRating=4.0');
const data = await result.json();
```

**Khi nào dùng**:
- ✅ Simple searches (1-3 parameters)
- ✅ Bookmarkable URLs
- ✅ Quick testing in browser

---

## 📮 Approach 2: POST with Simple Body

### Medium Complexity Search

```http
POST /api/posts/unified
Content-Type: application/json

{
  "searchTerm": "react",
  "categoryId": "web-dev",
  "minRating": 4.0,
  "fromDate": "2026-01-01",
  "toDate": "2026-01-31",
  "sortBy": "Rating",
  "sortDescending": true,
  "page": 1,
  "pageSize": 15
}
```

**cURL**:
```bash
curl -X POST "https://localhost:7001/api/posts/unified" \
  -H "Content-Type: application/json" \
  -d '{
    "searchTerm": "react",
    "categoryId": "web-dev",
    "minRating": 4.0,
    "sortBy": "Rating",
    "page": 1,
    "pageSize": 15
  }'
```

**JavaScript**:
```javascript
const result = await fetch('/api/posts/unified', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    searchTerm: 'react',
    categoryId: 'web-dev',
    minRating: 4.0,
    sortBy: 'Rating',
    page: 1,
    pageSize: 15
  })
});
```

**Khi nào dùng**:
- ✅ Complex searches (3-5 parameters)
- ✅ API integrations
- ✅ Mobile apps

---

## 🎛️ Approach 3: POST with FilterModel

### Advanced Data Grid Search

```http
POST /api/posts/unified
Content-Type: application/json

{
  "filters": {
    "title": {
      "filterType": "text",
      "type": "contains",
      "filter": "react"
    },
    "averageRating": {
      "filterType": "number",
      "type": "greaterThanOrEqual",
      "filter": 4.0
    },
    "createdAt": {
      "filterType": "date",
      "type": "inRange",
      "dateFrom": "2026-01-01T00:00:00Z",
      "dateTo": "2026-01-31T23:59:59Z"
    },
    "categoryId": {
      "filterType": "set",
      "values": ["tech", "web-dev", "mobile"]
    }
  },
  "sort": [
    { "colId": "averageRating", "sort": "desc" }
  ],
  "page": 0,
  "pageSize": 10
}
```

**JavaScript (AG-Grid)**:
```javascript
const onFilterChanged = async (params) => {
  const filterModel = params.api.getFilterModel();
  const sortModel = params.api.getSortModel();
  
  const result = await fetch('/api/posts/unified', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      filters: filterModel,
      sort: sortModel,
      page: 0,
      pageSize: 100
    })
  });
  
  const data = await result.json();
  setRowData(data.items);
};
```

**Khi nào dùng**:
- ✅ Data grids (AG-Grid, MUI DataGrid)
- ✅ Admin panels
- ✅ Complex dashboards

---

## 🔄 Auto-Detection Logic

API tự động detect approach:

```csharp
// 1. Check if FilterModel is used
if (request.Filters != null && request.Filters.Any())
{
    // Use FilterModel approach
    // Parse filters using FilterModelParser
}
// 2. Check if simple search parameters are used
else if (request.SearchTerm != null || request.CategoryId != null || ...)
{
    // Use simple search approach
    // Build Expression predicates
}
// 3. Otherwise
else
{
    // Return all posts (with pagination)
}
```

---

## 📊 Request Parameters

### Simple Search Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `searchTerm` | string | No | null | Search in title/content |
| `categoryId` | string | No | null | Filter by category |
| `minRating` | double | No | null | Minimum rating (0-5) |
| `fromDate` | DateTime | No | null | From date |
| `toDate` | DateTime | No | null | To date |
| `sortBy` | string | No | "CreatedAt" | Sort field |
| `sortDescending` | bool | No | true | Sort direction |
| `page` | int | No | 1 | Page number (1-based) |
| `pageSize` | int | No | 10 | Items per page |

### FilterModel Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `filters` | Dictionary | No | Column filters |
| `sort` | List | No | Sort model |
| `page` | int | No | Page number (0-based) |
| `pageSize` | int | No | Items per page |

---

## 💡 Examples

### Example 1: Simple GET Request

```http
GET /api/posts/unified?searchTerm=javascript
```

**Response**:
```json
{
  "items": [...],
  "totalCount": 42,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 5,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

---

### Example 2: POST with Simple Parameters

```http
POST /api/posts/unified
Content-Type: application/json

{
  "searchTerm": "react",
  "minRating": 4.0,
  "sortBy": "Rating",
  "sortDescending": true,
  "page": 1,
  "pageSize": 20
}
```

---

### Example 3: POST with FilterModel

```http
POST /api/posts/unified
Content-Type: application/json

{
  "filters": {
    "title": {
      "filterType": "text",
      "type": "contains",
      "filter": "react"
    },
    "averageRating": {
      "filterType": "number",
      "type": "greaterThan",
      "filter": 4.0
    }
  },
  "sort": [
    { "colId": "averageRating", "sort": "desc" }
  ],
  "page": 0,
  "pageSize": 10
}
```

---

### Example 4: Mixed Approach (Query + Body)

```http
POST /api/posts/unified?pageSize=20
Content-Type: application/json

{
  "searchTerm": "react",
  "minRating": 4.0
}
```

**Note**: Body parameters take precedence over query parameters when both are provided.

---

## 🎨 Client-Side Integration

### Vanilla JavaScript

```javascript
// Simple search with GET
const simpleSearch = async (term) => {
  const response = await fetch(`/api/posts/unified?searchTerm=${term}`);
  return await response.json();
};

// Complex search with POST
const complexSearch = async (filters) => {
  const response = await fetch('/api/posts/unified', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(filters)
  });
  return await response.json();
};

// FilterModel search
const filterModelSearch = async (filterModel) => {
  const response = await fetch('/api/posts/unified', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      filters: filterModel.filters,
      sort: filterModel.sort,
      page: filterModel.page,
      pageSize: filterModel.pageSize
    })
  });
  return await response.json();
};
```

---

### React Hook

```typescript
import { useState } from 'react';

const useUnifiedSearch = () => {
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(false);
  
  const search = async (request: any, usePost = false) => {
    setLoading(true);
    
    try {
      let response;
      
      if (usePost || request.filters) {
        // Use POST
        response = await fetch('/api/posts/unified', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(request)
        });
      } else {
        // Use GET
        const params = new URLSearchParams(request);
        response = await fetch(`/api/posts/unified?${params}`);
      }
      
      const result = await response.json();
      setData(result);
    } finally {
      setLoading(false);
    }
  };
  
  return { data, loading, search };
};

// Usage
const SearchComponent = () => {
  const { data, loading, search } = useUnifiedSearch();
  
  // Simple search
  const handleSimpleSearch = () => {
    search({ searchTerm: 'react', minRating: 4.0 });
  };
  
  // FilterModel search
  const handleFilterModelSearch = () => {
    search({
      filters: {
        title: { filterType: 'text', type: 'contains', filter: 'react' }
      },
      page: 0,
      pageSize: 10
    }, true);
  };
  
  return (
    <div>
      <button onClick={handleSimpleSearch}>Simple Search</button>
      <button onClick={handleFilterModelSearch}>Advanced Search</button>
      {loading && <div>Loading...</div>}
      {data && <div>{data.items.length} results</div>}
    </div>
  );
};
```

---

### AG-Grid Integration

```typescript
const PostsGrid = () => {
  const [rowData, setRowData] = useState([]);
  
  const onFilterChanged = async (params) => {
    const filterModel = params.api.getFilterModel();
    const sortModel = params.api.getSortModel();
    
    // Automatically use FilterModel approach
    const response = await fetch('/api/posts/unified', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        filters: filterModel,
        sort: sortModel,
        page: 0,
        pageSize: 100
      })
    });
    
    const data = await response.json();
    setRowData(data.items);
  };
  
  return (
    <AgGridReact
      columnDefs={columnDefs}
      rowData={rowData}
      onFilterChanged={onFilterChanged}
      onSortChanged={onFilterChanged}
    />
  );
};
```

---

## ✅ Benefits

### For Developers:
- ✅ **One Endpoint** - Easier to maintain
- ✅ **Flexible** - Supports multiple approaches
- ✅ **Auto-Detection** - No need to choose endpoint
- ✅ **Backward Compatible** - Works with existing clients
- ✅ **Future-Proof** - Easy to add new features

### For Clients:
- ✅ **Simple** - Use GET for simple searches
- ✅ **Powerful** - Use POST for complex searches
- ✅ **Consistent** - Same response format
- ✅ **Flexible** - Choose the best approach for your use case

---

## 🔄 Migration Guide

### From Separate Endpoints

**Before** (3 separate endpoints):
```javascript
// Simple search
fetch('/api/posts/search?searchTerm=react')

// POST search
fetch('/api/posts/search', { method: 'POST', body: {...} })

// FilterModel
fetch('/api/posts/filter', { method: 'POST', body: {...} })
```

**After** (1 unified endpoint):
```javascript
// All use the same endpoint!
fetch('/api/posts/unified?searchTerm=react')
fetch('/api/posts/unified', { method: 'POST', body: {...} })
fetch('/api/posts/unified', { method: 'POST', body: { filters: {...} } })
```

**Note**: Old endpoints still work! Unified endpoint is an addition, not a replacement.

---

## 🎯 Best Practices

### 1. Choose the Right Approach

```javascript
// ✅ GOOD - Use GET for simple searches
fetch('/api/posts/unified?searchTerm=react')

// ✅ GOOD - Use POST for complex searches
fetch('/api/posts/unified', {
  method: 'POST',
  body: JSON.stringify({ searchTerm: 'react', minRating: 4.0, ... })
})

// ✅ GOOD - Use FilterModel for data grids
fetch('/api/posts/unified', {
  method: 'POST',
  body: JSON.stringify({ filters: {...}, sort: [...] })
})
```

### 2. Let the API Auto-Detect

```javascript
// ✅ GOOD - API will auto-detect the approach
const search = async (request) => {
  const usePost = request.filters || Object.keys(request).length > 3;
  
  if (usePost) {
    return await fetch('/api/posts/unified', {
      method: 'POST',
      body: JSON.stringify(request)
    });
  } else {
    const params = new URLSearchParams(request);
    return await fetch(`/api/posts/unified?${params}`);
  }
};
```

### 3. Consistent Error Handling

```javascript
const search = async (request) => {
  try {
    const response = await fetch('/api/posts/unified', {
      method: 'POST',
      body: JSON.stringify(request)
    });
    
    if (!response.ok) {
      throw new Error(`HTTP ${response.status}`);
    }
    
    return await response.json();
  } catch (error) {
    console.error('Search failed:', error);
    throw error;
  }
};
```

---

## 🎉 Kết luận

Unified Search API cung cấp:
- ✅ **One Endpoint** - `/api/posts/unified`
- ✅ **Multiple Approaches** - GET, POST simple, POST FilterModel
- ✅ **Auto-Detection** - API tự động detect approach
- ✅ **Flexible** - Choose the best approach for your use case
- ✅ **Consistent** - Same response format
- ✅ **Backward Compatible** - Old endpoints still work

**One endpoint to rule them all!** 🚀
