# 🎛️ FilterModel API - Documentation

## 🎯 Tổng quan

**FilterModel** là một cách tiếp cận mạnh mẽ và linh hoạt để xử lý dynamic filtering, được sử dụng phổ biến trong các data grid libraries như:
- ✅ **AG-Grid** (React, Angular, Vue)
- ✅ **MUI DataGrid** (Material-UI)
- ✅ **DevExtreme DataGrid**
- ✅ **Kendo UI Grid**
- ✅ **Syncfusion Grid**

---

## 📍 API Endpoint

```
POST /api/posts/filter
Content-Type: application/json
```

---

## 📝 Request Body Schema

### FilterModel Structure

```json
{
  "filters": {
    "columnName": {
      "filterType": "text | number | date | boolean | set",
      "type": "operator",
      "filter": "value",
      "values": ["value1", "value2"],
      "dateFrom": "2026-01-01",
      "dateTo": "2026-01-31"
    }
  },
  "sort": [
    {
      "colId": "columnName",
      "sort": "asc | desc"
    }
  ],
  "page": 0,
  "pageSize": 10
}
```

### Filter Types & Operators

#### Text Filters
```json
{
  "title": {
    "filterType": "text",
    "type": "contains | equals | notEqual | startsWith | endsWith | blank | notBlank",
    "filter": "react"
  }
}
```

**Operators**:
- `contains` - Contains substring
- `equals` - Exact match
- `notEqual` - Not equal
- `startsWith` - Starts with
- `endsWith` - Ends with
- `blank` - Is null or empty
- `notBlank` - Is not null or empty

#### Number Filters
```json
{
  "averageRating": {
    "filterType": "number",
    "type": "equals | notEqual | lessThan | lessThanOrEqual | greaterThan | greaterThanOrEqual | inRange",
    "filter": 4.0
  }
}
```

**Operators**:
- `equals` - Equal to
- `notEqual` - Not equal to
- `lessThan` - Less than
- `lessThanOrEqual` - Less than or equal
- `greaterThan` - Greater than
- `greaterThanOrEqual` - Greater than or equal
- `inRange` - Between two values

#### Date Filters
```json
{
  "createdAt": {
    "filterType": "date",
    "type": "equals | greaterThan | lessThan | inRange",
    "filter": "2026-01-15T00:00:00Z",
    "dateFrom": "2026-01-01T00:00:00Z",
    "dateTo": "2026-01-31T23:59:59Z"
  }
}
```

**Operators**:
- `equals` - Exact date
- `greaterThan` - After date
- `lessThan` - Before date
- `inRange` - Between two dates

#### Boolean Filters
```json
{
  "isPublished": {
    "filterType": "boolean",
    "type": "equals",
    "filter": true
  }
}
```

#### Set Filters (IN clause)
```json
{
  "categoryId": {
    "filterType": "set",
    "values": ["tech", "web-dev", "mobile"]
  }
}
```

---

## 🔍 Example Requests

### 1. Simple Text Filter

Search posts containing "react" in title:

```http
POST /api/posts/filter
Content-Type: application/json

{
  "filters": {
    "title": {
      "filterType": "text",
      "type": "contains",
      "filter": "react"
    }
  },
  "page": 0,
  "pageSize": 10
}
```

**cURL**:
```bash
curl -X POST "https://localhost:7001/api/posts/filter" \
  -H "Content-Type: application/json" \
  -d '{
    "filters": {
      "title": {
        "filterType": "text",
        "type": "contains",
        "filter": "react"
      }
    },
    "page": 0,
    "pageSize": 10
  }'
```

---

### 2. Number Filter (Rating >= 4.0)

```http
POST /api/posts/filter
Content-Type: application/json

{
  "filters": {
    "averageRating": {
      "filterType": "number",
      "type": "greaterThanOrEqual",
      "filter": 4.0
    }
  },
  "sort": [
    {
      "colId": "averageRating",
      "sort": "desc"
    }
  ],
  "page": 0,
  "pageSize": 20
}
```

---

### 3. Date Range Filter

Posts created in January 2026:

```http
POST /api/posts/filter
Content-Type: application/json

{
  "filters": {
    "createdAt": {
      "filterType": "date",
      "type": "inRange",
      "dateFrom": "2026-01-01T00:00:00Z",
      "dateTo": "2026-01-31T23:59:59Z"
    }
  },
  "sort": [
    {
      "colId": "createdAt",
      "sort": "desc"
    }
  ],
  "page": 0,
  "pageSize": 10
}
```

---

### 4. Multiple Filters (AND logic)

Posts with:
- Title contains "react"
- Rating >= 4.0
- Created in 2026

```http
POST /api/posts/filter
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
      "type": "greaterThan",
      "filter": "2026-01-01T00:00:00Z"
    }
  },
  "sort": [
    {
      "colId": "averageRating",
      "sort": "desc"
    }
  ],
  "page": 0,
  "pageSize": 15
}
```

---

### 5. Set Filter (IN clause)

Posts in specific categories:

```http
POST /api/posts/filter
Content-Type: application/json

{
  "filters": {
    "categoryId": {
      "filterType": "set",
      "values": ["tech", "web-dev", "mobile"]
    }
  },
  "sort": [
    {
      "colId": "createdAt",
      "sort": "desc"
    }
  ],
  "page": 0,
  "pageSize": 10
}
```

---

### 6. Complex Filter with Multiple Sorts

```http
POST /api/posts/filter
Content-Type: application/json

{
  "filters": {
    "title": {
      "filterType": "text",
      "type": "contains",
      "filter": "javascript"
    },
    "averageRating": {
      "filterType": "number",
      "type": "greaterThan",
      "filter": 3.5
    },
    "categoryId": {
      "filterType": "set",
      "values": ["tech", "web-dev"]
    }
  },
  "sort": [
    {
      "colId": "averageRating",
      "sort": "desc"
    }
  ],
  "page": 0,
  "pageSize": 20
}
```

---

## 💻 Client-Side Integration

### AG-Grid (React)

```typescript
import { AgGridReact } from 'ag-grid-react';
import { useState } from 'react';
import axios from 'axios';

const PostsGrid = () => {
  const [rowData, setRowData] = useState([]);
  const [totalRows, setTotalRows] = useState(0);
  
  const columnDefs = [
    { field: 'title', filter: 'agTextColumnFilter' },
    { field: 'authorName', filter: 'agTextColumnFilter' },
    { field: 'averageRating', filter: 'agNumberColumnFilter' },
    { field: 'createdAt', filter: 'agDateColumnFilter' },
    { field: 'categoryId', filter: 'agSetColumnFilter' }
  ];
  
  const onFilterChanged = async (params) => {
    const filterModel = params.api.getFilterModel();
    const sortModel = params.api.getSortModel();
    
    const requestBody = {
      filters: filterModel,
      sort: sortModel,
      page: 0,
      pageSize: 100
    };
    
    const response = await axios.post('/api/posts/filter', requestBody);
    
    setRowData(response.data.items);
    setTotalRows(response.data.totalCount);
  };
  
  return (
    <div className="ag-theme-alpine" style={{ height: 600 }}>
      <AgGridReact
        columnDefs={columnDefs}
        rowData={rowData}
        onFilterChanged={onFilterChanged}
        onSortChanged={onFilterChanged}
        pagination={true}
        paginationPageSize={100}
      />
    </div>
  );
};
```

---

### MUI DataGrid (React)

```typescript
import { DataGrid, GridFilterModel, GridSortModel } from '@mui/x-data-grid';
import { useState, useEffect } from 'react';
import axios from 'axios';

const PostsDataGrid = () => {
  const [rows, setRows] = useState([]);
  const [totalRows, setTotalRows] = useState(0);
  const [filterModel, setFilterModel] = useState<GridFilterModel>({ items: [] });
  const [sortModel, setSortModel] = useState<GridSortModel>([]);
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(10);
  
  const columns = [
    { field: 'title', headerName: 'Title', width: 300, filterable: true },
    { field: 'authorName', headerName: 'Author', width: 200, filterable: true },
    { field: 'averageRating', headerName: 'Rating', width: 120, type: 'number', filterable: true },
    { field: 'createdAt', headerName: 'Created', width: 180, type: 'date', filterable: true },
    { field: 'categoryId', headerName: 'Category', width: 150, filterable: true }
  ];
  
  useEffect(() => {
    fetchData();
  }, [filterModel, sortModel, page, pageSize]);
  
  const fetchData = async () => {
    // Convert MUI filter model to API filter model
    const filters = {};
    filterModel.items.forEach(item => {
      filters[item.columnField] = {
        filterType: getFilterType(item.columnField),
        type: item.operatorValue,
        filter: item.value
      };
    });
    
    // Convert MUI sort model to API sort model
    const sort = sortModel.map(item => ({
      colId: item.field,
      sort: item.sort
    }));
    
    const requestBody = {
      filters,
      sort,
      page,
      pageSize
    };
    
    const response = await axios.post('/api/posts/filter', requestBody);
    
    setRows(response.data.items);
    setTotalRows(response.data.totalCount);
  };
  
  const getFilterType = (field: string) => {
    const column = columns.find(c => c.field === field);
    return column?.type === 'number' ? 'number' : 
           column?.type === 'date' ? 'date' : 'text';
  };
  
  return (
    <DataGrid
      rows={rows}
      columns={columns}
      rowCount={totalRows}
      filterModel={filterModel}
      onFilterModelChange={setFilterModel}
      sortModel={sortModel}
      onSortModelChange={setSortModel}
      page={page}
      pageSize={pageSize}
      onPageChange={setPage}
      onPageSizeChange={setPageSize}
      paginationMode="server"
      filterMode="server"
      sortingMode="server"
    />
  );
};
```

---

### Vanilla JavaScript

```javascript
const searchWithFilterModel = async (filterModel) => {
  const response = await fetch('/api/posts/filter', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(filterModel)
  });
  
  return await response.json();
};

// Example usage
const result = await searchWithFilterModel({
  filters: {
    title: {
      filterType: 'text',
      type: 'contains',
      filter: 'react'
    },
    averageRating: {
      filterType: 'number',
      type: 'greaterThanOrEqual',
      filter: 4.0
    }
  },
  sort: [
    { colId: 'averageRating', sort: 'desc' }
  ],
  page: 0,
  pageSize: 10
});

console.log(`Found ${result.totalCount} posts`);
console.log(result.items);
```

---

## 📊 Response Format

Same as other endpoints:

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
  "totalCount": 42,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 5,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

---

## 🎯 Supported Column Names

| Column | Type | Filter Type | Example |
|--------|------|-------------|---------|
| `title` | string | text | `contains`, `equals` |
| `content` | string | text | `contains`, `startsWith` |
| `authorName` | string | text | `contains`, `equals` |
| `averageRating` | number | number | `greaterThan`, `inRange` |
| `createdAt` | DateTime | date | `inRange`, `greaterThan` |
| `categoryId` | string | text/set | `equals`, `values` |

---

## ✅ Best Practices

### 1. Use Appropriate Filter Types

```json
// ✅ GOOD - Correct filter types
{
  "filters": {
    "title": { "filterType": "text", "type": "contains", "filter": "react" },
    "averageRating": { "filterType": "number", "type": "greaterThan", "filter": 4.0 },
    "createdAt": { "filterType": "date", "type": "inRange", "dateFrom": "...", "dateTo": "..." }
  }
}

// ❌ BAD - Wrong filter types
{
  "filters": {
    "averageRating": { "filterType": "text", "type": "contains", "filter": "4.0" }
  }
}
```

### 2. Use Set Filters for Multiple Values

```json
// ✅ GOOD - Set filter for IN clause
{
  "categoryId": {
    "filterType": "set",
    "values": ["tech", "web-dev", "mobile"]
  }
}

// ❌ BAD - Multiple separate filters (won't work as OR)
{
  "categoryId1": { "filterType": "text", "type": "equals", "filter": "tech" },
  "categoryId2": { "filterType": "text", "type": "equals", "filter": "web-dev" }
}
```

### 3. Page Numbering

```json
// Page is 0-based
{
  "page": 0,      // First page
  "pageSize": 10
}

{
  "page": 1,      // Second page
  "pageSize": 10
}
```

---

## 🔄 Comparison with Other Approaches

| Approach | Use Case | Complexity | Flexibility |
|----------|----------|------------|-------------|
| **Query Parameters** | Simple searches | Low | Low |
| **Request Body (DTO)** | Medium complexity | Medium | Medium |
| **FilterModel** | Data grids, complex UI | High | Very High |

### When to use FilterModel:

✅ **Use FilterModel when**:
- Integrating with AG-Grid, MUI DataGrid, etc.
- Need dynamic, user-driven filtering
- Complex UI with many filter options
- Building admin panels or dashboards

❌ **Don't use FilterModel when**:
- Simple search (use query parameters)
- Fixed, predefined filters (use DTO)
- Mobile apps with simple UI

---

## 🧪 Testing

### Postman

1. **Create new request**: `POST`
2. **URL**: `{{baseUrl}}/api/posts/filter`
3. **Headers**: `Content-Type: application/json`
4. **Body** (raw JSON):

```json
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
    }
  },
  "sort": [
    {
      "colId": "averageRating",
      "sort": "desc"
    }
  ],
  "page": 0,
  "pageSize": 10
}
```

---

## 🎉 Kết luận

FilterModel API cung cấp:
- ✅ **Dynamic Filtering** - User-driven filters
- ✅ **Multiple Filter Types** - Text, Number, Date, Boolean, Set
- ✅ **Flexible Operators** - Contains, Equals, GreaterThan, InRange, etc.
- ✅ **Sorting Support** - Multiple columns
- ✅ **Pagination** - Server-side paging
- ✅ **Data Grid Integration** - AG-Grid, MUI DataGrid ready

**Perfect for building powerful data tables and admin panels!** 🚀
