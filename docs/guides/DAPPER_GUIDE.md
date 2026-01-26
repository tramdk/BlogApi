# Hướng dẫn sử dụng Dapper trong dự án BlogApi

## 1. Tại sao sử dụng Dapper?
Trong dự án này, chúng ta sử dụng kết hợp giữa **Entity Framework Core (EF Core)** và **Dapper**:
- **EF Core**: Dùng cho các tác vụ ghi (Commands), quản lý quan hệ phức tạp và Migrations. EF Core giúp đảm bảo tính nhất quán của dữ liệu.
- **Dapper**: Dùng cho các tác vụ đọc (Queries). Dapper là một Micro-ORM nhẹ, cho phép viết SQL thuần túy, giúp tối ưu hóa hiệu suất tối đa và dễ dàng kiểm soát các câu lệnh JOIN phức tạp.

## 2. Cách triển khai trong dự án

### Khởi tạo Connection
Chúng ta tận dụng kết nối từ `AppDbContext` để không phải quản lý Connection String ở nhiều nơi:

```csharp
public class MyQueryService
{
    private readonly AppDbContext _context;
    public MyQueryService(AppDbContext context) => _context = context;

    public async Task ExecuteAsync()
    {
        // Lấy DbConnection từ EF Context
        using var connection = _context.Database.GetDbConnection();
        // ... thực hiện query với Dapper
    }
}
```

### Viết Query với Dapper
Dapper tự động ánh xạ (map) các cột trong kết quả SQL vào các thuộc tính của C# class (DTO).

```csharp
var sql = @"
    SELECT p.Id, p.Title, u.FullName as AuthorName
    FROM Posts p
    INNER JOIN AspNetUsers u ON p.AuthorId = u.Id
    WHERE p.Id = @PostId";

var parameters = new { PostId = someId };

// QueryAsync<T> trả về danh sách các đối tượng kiểu T
var post = await connection.QueryFirstOrDefaultAsync<PostDto>(sql, parameters);
```

## 3. Cursor-based Pagination với Dapper
Đây là kỹ thuật chính chúng ta sử dụng để lấy danh sách dữ liệu lớn.

### Ưu điểm so với Skip/Take:
- Hiệu suất không bị giảm khi trang càng xa.
- Không bị lặp hoặc mất bản ghi khi dữ liệu bị thay đổi trong lúc phân trang.

### Code mẫu:
```csharp
public async Task<CursorPagedList<PostDto>> GetPostsAsync(Guid? cursor, int pageSize)
{
    using var connection = _context.Database.GetDbConnection();
    
    // Lấy thêm 1 bản ghi để kiểm tra xem còn trang sau không (pageSize + 1)
    var sql = @"
        SELECT TOP (@PageSizePlusOne) 
            p.Id, p.Title, u.FullName as AuthorName, p.CreatedAt
        FROM Posts p
        INNER JOIN AspNetUsers u ON p.AuthorId = u.Id
        WHERE (@Cursor IS NULL OR p.Id < @Cursor)
        ORDER BY p.Id DESC";

    var parameters = new { Cursor = cursor, PageSizePlusOne = pageSize + 1 };
    var posts = (await connection.QueryAsync<PostDto>(sql, parameters)).ToList();

    // Logic xử lý Cursor...
}
```

## 4. Nguyên tắc và Best Practices
1. **Đặt tên cột phù hợp**: Sử dụng `AS` trong SQL để khớp tên cột với thuộc tính của DTO (ví dụ: `u.FullName AS AuthorName`).
2. **Sử dụng Parameters**: Luôn sử dụng object tham số (ví dụ: `new { Id = 1 }`) để chống tấn công SQL Injection. Tuyệt đối không cộng chuỗi SQL.
3. **Chỉ đọc (Read-only)**: Chỉ nên dùng Dapper cho mục đích Query (SELECT). Đối với INSERT/UPDATE/DELETE, hãy ưu tiên dùng EF Core để tận dụng Change Tracker và Validation.
4. **Viết SQL chuẩn**: Đảm bảo câu lệnh SQL tương thích với hệ quản trị cơ sở dữ liệu đang dùng (SQL Server sử dụng `TOP`, SQLite sử dụng `LIMIT`).

## 5. Cấu trúc file
Các logic liên quan đến Dapper nên được đặt trong thư mục:
- `Infrastructure/Repositories/` (Các lớp QueryService)
- Định nghĩa Interface tại `Application/Common/Interfaces/`
