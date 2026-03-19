# 🔍 Search API Guide - Unified Search

This guide covers the advanced search capabilities of the Blog API. The search endpoint is unified, meaning it supports multiple approaches using a single route.

## 📍 Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/api/posts/search` | Search using query parameters |
| `POST` | `/api/posts/search` | Search using a JSON body (supports advanced filters) |
| `ANY` | `/api/posts/unified` | Alias for backward compatibility |

---

## 🛠️ Approach 1: Simple Search (GET)

Best for simple filters, link sharing, and browser testing.

### Query Parameters
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `searchTerm` | `string` | `null` | Search in Title and Content |
| `categoryId` | `string` | `null` | Filter by category ID |
| `minRating` | `double` | `null` | Minimum rating (0-5) |
| `fromDate` | `DateTime` | `null` | Filter from date |
| `toDate` | `DateTime` | `null` | Filter to date |
| `sortBy` | `string` | `"CreatedAt"` | Sort by: `Title`, `Rating`, `CreatedAt` |
| `sortDescending` | `bool` | `true` | Sort direction |
| `page` | `int` | `1` | Page number (1-based) |
| `pageSize` | `int` | `10` | Items per page |

### Example
```http
GET /api/posts/search?searchTerm=react&minRating=4.0&page=1
```

---

## 📮 Approach 2: Body Search (POST)

Best for complex filters, avoiding long URLs, and type-safe client integration.

### Request Body (JSON)
```json
{
  "searchTerm": "react",
  "categoryId": "web-dev",
  "minRating": 4.0,
  "sortBy": "Rating",
  "sortDescending": true,
  "page": 1,
  "pageSize": 10
}
```

---

## 🎛️ Approach 3: Advanced FilterModel (POST)

Best for Data Grids (AG-Grid, MUI DataGrid) and complex dashboards.

### Request Body (JSON)
```json
{
  "filters": {
    "title": { "filterType": "text", "type": "contains", "filter": "react" },
    "averageRating": { "filterType": "number", "type": "greaterThan", "filter": 4.0 }
  },
  "sort": [
    { "colId": "averageRating", "sort": "desc" }
  ],
  "page": 0,
  "pageSize": 10
}
```
*Note: When using `filters`, the `page` parameter becomes 0-based to match standard grid libraries.*

---

## 📊 Response Format

All search approaches return a consistent `PagedResult<PostDto>` object:

```json
{
  "items": [
    {
      "id": "guid",
      "title": "Post Title",
      "authorName": "John Doe",
      "averageRating": 4.5,
      "createdAt": "2026-03-19T00:00:00Z",
      "categoryId": "string"
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

## 💡 Client-Side (JavaScript)

```javascript
const search = async (params) => {
  const isAdvanced = params.filters || Object.keys(params).length > 3;
  
  const response = await fetch('/api/posts/search', {
    method: isAdvanced ? 'POST' : 'GET',
    headers: isAdvanced ? { 'Content-Type': 'application/json' } : {},
    body: isAdvanced ? JSON.stringify(params) : null
  });
  
  return await response.json();
};
```
