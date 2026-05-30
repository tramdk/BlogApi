# AGENTS.md — Rules rút ra từ failure history

## Rules

1. TRƯỚC KHI viết code, dùng `view_source` đọc file test tương ứng trong FloraCore.Tests/ để biết method signature, expected behavior.
2. Viết 1 file .cs production → BUILD NGAY (`dotnet build FloraCore.csproj`). KHÔNG viết nhiều file cùng lúc.
3. Nếu gặp lỗi CS1503/C1061, dùng `view_source` đọc interface/class gốc, copy đúng method signature.
4. KHÔNG gọi `git commit`. Chỉ dùng `git status`, `git diff`, `git add`.
5. Nếu `dotnet test --filter` không tìm thấy test nào, chạy không filter trước (`dotnet test`).
6. TestWriter CHỈ được sửa file trong FloraCore.Tests/. KHÔNG đụng production code.
7. Nếu cùng lỗi compiler xuất hiện 3+ lần, ĐỔI CHIẾN THUẬT — đọc file test để biết signature đúng.


## Quy định Cốt lõi của Project (từ CODING_POLICY.md)

9. **Dependency Injection & Primary Constructors**:
   - BẮT BUỘC sử dụng cú pháp Primary Constructor của C# 12+ cho các lớp DI (ví dụ: `public class OrdersController(IMediator mediator)`).
   - Phải thực hiện kiểm tra null ngay khi khởi tạo bằng `ArgumentNullException.ThrowIfNull(dependency)` hoặc toán tử gán `?? throw new ArgumentNullException(nameof(dependency))`.
10. **CQRS & MediatR**:
    - Mỗi Command/Query phải là một `record` bất biến (immutable) kế thừa `IRequest<T>`.
    - Triển khai xử lý thông qua Handler độc lập kế thừa `IRequestHandler<TRequest, TResponse>`.
    - Bắt buộc viết XML comments đầy đủ cho Command/Query và Handler.
11. **Phân trang & Lọc danh sách**:
    - Tất cả các API trả về danh sách lớn (`GET /api/orders`, `/api/products`) mặc định phải hỗ trợ phân trang (`PageNumber`/`PageSize`) và các bộ lọc (như `OrderStatus`, `SearchTerm`, v.v.).
    - Sử dụng `QueryOptionsBuilder<TEntity>` để áp dụng phân trang và `.AsNoTracking()` cho các truy vấn chỉ đọc (Read-only Queries).
12. **Bất đồng bộ (Async/Await)**:
    - Luôn sử dụng `async/await` cho các tác vụ I/O (Database, Network, File). Tên phương thức async phải có hậu tố `Async`.
    - Tuyệt đối không dùng `.Result` hoặc `.Wait()` để tránh gây Deadlock. Luôn luôn truyền `CancellationToken`.
13. **EF Core & Dapper Hybrid**:
    - Sử dụng EF Core (LINQ) cho các lệnh ghi/sửa đổi (Commands).
    - Sử dụng Dapper hoặc các phương thức `.Select()` / `.ProjectTo()` tối ưu hóa cho các truy vấn đọc hiệu năng cao (Read-only) và thống kê để tránh lỗi truy vấn N+1 và giảm thiểu lượng tải dữ liệu (Over-fetching).
14. **Quản lý Cấu hình & Lỗi**:
    - Sử dụng Strongly-typed Configuration thông qua **`IOptions<T>`** hoặc **`IOptionsSnapshot<T>`** thay vì inject trực tiếp `IConfiguration` rải rác.
    - Không viết cứng chuỗi thông báo lỗi hoặc log trong code; sử dụng `ResourceManager` để đọc từ tệp `.resx` tương ứng.

15. LUÔN LUÔN chạy static check bằng `./scripts/final-check.ps1 validate-all` (hoặc `validate-policy`) và đảm bảo đạt 100% trước khi báo cáo hoàn thành công việc.
16. LUÔN LUÔN quét danh sách `<skills>` có sẵn ở đầu phiên làm việc. Nếu phát hiện skill nào liên quan đến nghiệp vụ hiện tại (ví dụ: `optimizing-ef-core-queries`), bắt buộc dùng `view_file` đọc tệp `SKILL.md` tương ứng để áp dụng chính xác các chỉ dẫn kỹ thuật trước khi thiết kế hoặc viết code.