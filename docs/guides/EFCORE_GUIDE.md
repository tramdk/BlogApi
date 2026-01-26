# Hướng dẫn chi tiết về Entity Framework Core (EF Core) trong dự án BlogApi

## 1. Tổng quan
**Entity Framework Core (EF Core)** là một bộ ánh xạ đối tượng-quan hệ (ORM) hiện đại cho .NET. Nó cho phép các nhà phát triển làm việc với cơ sở dữ liệu bằng cách sử dụng các đối tượng .NET, giúp loại bỏ hầu hết các mã truy cập dữ liệu phải viết thủ công.

Trong dự án này, EF Core đóng vai trò là "xương sống" cho tất cả các thao tác thay đổi dữ liệu (Write operations).

---

## 2. Các thành phần chính

### A. Thực thể (Entities) - `Domain/Entities/`
Thực thể là các lớp C# (POCO - Plain Old CLR Objects) đại diện cho các bảng trong cơ sở dữ liệu.
- **Đặc điểm**: Mỗi thuộc tính trong lớp tương ứng với một cột trong bảng.
- **Ví dụ**: Lớp `Post.cs` đại diện cho bảng `Posts`.

### B. DbContext - `Infrastructure/Data/AppDbContext.cs`
Đây là thành phần quan trọng nhất, đóng vai trò là cầu nối giữa code C# và Database.
- **Nhiệm vụ**: Quản lý các kết nối, định nghĩa các bảng (`DbSet`), và cấu hình các mối quan hệ giữa các bảng.
- **Các phương thức quan trọng**:
    - `OnModelCreating`: Nơi cấu hình chi tiết (Fluent API) mà các thuộc tính (Attributes) không thể hiện được.

### C. DbSet - `DbSet<T>`
Đại diện cho một bộ sưu tập các thực thể trong cơ sở dữ liệu.
- **Cách dùng**: `_context.Posts.Add(post)` - `DbSet` cho phép bạn thực hiện các thao tác LINQ như `Where`, `OrderBy`, và các lệnh `Add`, `Remove`, `Update`.

### D. Fluent API - `OnModelCreating`
Chúng ta sử dụng Fluent API thay vì Data Annotations để giữ cho các lớp Entity ở tầng Domain được "sạch" (không phụ thuộc vào thư viện EF Core).
- **Ví dụ cấu hình quan hệ**:
  ```csharp
  builder.Entity<Post>()
         .HasOne(p => p.Author)
         .WithMany(u => u.Posts)
         .HasForeignKey(p => p.AuthorId);
  ```

---

## 3. Luồng làm việc với Cơ sở dữ liệu

### Bước 1: Thay đổi Model (Code First)
Bạn tạo mới hoặc sửa đổi các lớp trong thư mục `Domain/Entities`.

### Bước 2: Tạo Migration
Migration là cách EF Core ghi lại các thay đổi của schema database theo thời gian.
```bash
dotnet ef migrations add <TenMigration>
```
Lệnh này sẽ tạo ra các file C# trong thư mục `Migrations/` mô tả cách chuyển đổi DB từ phiên bản cũ sang phiên bản mới.

### Bước 3: Cập nhật Database
```bash
dotnet ef database update
```
EF Core sẽ thực thi các câu lệnh SQL tương ứng để thay đổi cấu trúc bảng thực tế.

---

## 4. Repository Pattern & Unit of Work

Dự án sử dụng Repository Pattern để trừu tượng hóa các thao tác dữ liệu:

### IGenericRepository & GenericRepository
Nằm tại `Infrastructure/Repositories/IGenericRepository.cs`, lớp này cung cấp các phương thức dùng chung cho mọi thực thể:
- `GetByIdAsync`: Lấy 1 bản ghi theo ID.
- `AddAsync`: Thêm bản ghi mới và tự động gọi `SaveChangesAsync()`.
- `UpdateAsync` & `DeleteAsync`: Cập nhật và xóa.

### Lợi ích:
1. **Dễ Test**: Bạn có thể dễ dàng thay thế database thật bằng database ảo (InMemory) trong khi test.
2. ** DRY (Don't Repeat Yourself)**: Không cần viết lại các hàm cơ bản cho từng bảng.

---

## 5. Các cơ chế nâng cao được sử dụng

### Change Tracking (Theo dõi thay đổi)
Khi bạn lấy một thực thể từ `DbContext`, EF Core sẽ "theo dõi" nó. Mọi thay đổi bạn thực hiện trên đối tượng đó sẽ được ghi nhận. Khi bạn gọi `SaveChangesAsync()`, EF Core sẽ tự động tạo ra câu lệnh SQL UPDATE chỉ cho những cột bị thay đổi.

### Lazy Loading vs Eager Loading
- **Eager Loading**: Sử dụng phương thức `.Include(x => x.Relation)` để lấy dữ liệu liên quan ngay lập tức (ví dụ lấy Post kèm theo Author). Dự án ưu tiên cách này để kiểm soát hiệu suất.
- **Lazy Loading**: Tự động lấy dữ liệu liên quan khi truy cập vào thuộc tính navigation (không được sử dụng trong dự án này để tránh lỗi N+1).

### LINQ to Entities
Nơi bạn viết code C# (như `.Where(p => p.IsPublished)`) và EF Core sẽ dịch nó thành câu lệnh SQL thuần túy để thực thi dưới Database.

---

## 6. Lưu ý quan trọng
- **SaveChangesAsync()**: Luôn phải gọi phương thức này để các thay đổi được lưu xuống đĩa. Trong dự án này, nó đã được tích hợp sẵn vào các phương thức `Add/Update/Delete` của `GenericRepository`.
- **Performance**: Đối với các truy vấn chỉ đọc (Read-only) cực lớn, chúng ta chuyển sang dùng **Dapper** (xem [DAPPER_GUIDE.md](./DAPPER_GUIDE.md)) để bỏ qua cơ chế theo dõi thay đổi của EF Core, giúp tăng tốc độ.
