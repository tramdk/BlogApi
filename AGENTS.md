# AGENTS.md — Quy trình & Quy tắc tối ưu cho Agent

## Giai đoạn 1: Khám phá & Định vị (Discovery)
1. **Đồng bộ & Ưu tiên CodeGraph**: 
   - LUÔN LUÔN chạy lệnh `codegraph sync` ở đầu mỗi phiên làm việc (hoặc sau khi pull code mới về) để đảm bảo bản đồ chỉ mục luôn phản ánh chính xác trạng thái hiện tại của codebase.
   - Luôn luôn sử dụng các tool của MCP Server `codegraph` đầu tiên để tìm kiếm symbol, truy vết mối quan hệ (references/calls) giữa các class/interface nhằm định vị nhanh tệp cần xử lý và tối ưu lượng token sử dụng.
2. **Đọc tệp Kiểm thử trước**: Dựa vào kết quả từ CodeGraph, sử dụng công cụ đọc file để xem file test tương ứng trong `FloraCore.Tests/` trước khi viết code để hiểu rõ Method Signature và Expected Behavior mong muốn.
3. **Quét tài liệu Skill**: Quét danh sách `<skills>` có sẵn ở đầu phiên làm việc. Nếu phát hiện skill liên quan đến nghiệp vụ hiện tại (ví dụ: `optimizing-ef-core-queries`), bắt buộc đọc tệp `SKILL.md` tương ứng để áp dụng chính xác chỉ dẫn kỹ thuật.

## Giai đoạn 2: Thiết kế & Viết code (Coding & Design Rules)
4. **Viết đơn lẻ & Biên dịch ngay**: Chỉ viết hoặc sửa đổi 1 file production `.cs` tại một thời điểm. Ngay sau đó phải chạy lệnh Build ngay (`dotnet build FloraCore.csproj`) để kiểm tra lỗi cú pháp trước khi chuyển sang file tiếp theo.
5. **Quy định Cốt lõi của Project (từ CODING_POLICY.md)**:
   - **Dependency Injection & Primary Constructors**: BẮT BUỘC sử dụng cú pháp Primary Constructor của C# 12+ cho các lớp DI. Kiểm tra null ngay khi khởi tạo bằng `ArgumentNullException.ThrowIfNull` hoặc toán tử gán `?? throw new ArgumentNullException`.
   - **CQRS & MediatR**: Mỗi Command/Query phải là một `record` bất biến (immutable) kế thừa `IRequest<T>`. Triển khai xử lý qua Handler độc lập kế thừa `IRequestHandler<TRequest, TResponse>` kèm đầy đủ XML comments.
   - **Phân trang & Lọc danh sách**: Tất cả các API trả về danh sách lớn mặc định phải hỗ trợ phân trang (`PageNumber`/`PageSize`) và bộ lọc. Sử dụng `QueryOptionsBuilder<TEntity>` và `.AsNoTracking()` cho truy vấn Read-only.
   - **Bất đồng bộ (Async/Await)**: Luôn sử dụng `async/await` cho các tác vụ I/O (Database, Network, File) với hậu tố `Async`. Tuyệt đối không dùng `.Result` hoặc `.Wait()`. Truyền đầy đủ `CancellationToken`.
   - **EF Core & Dapper Hybrid**: Sử dụng EF Core (LINQ) cho các lệnh ghi/sửa đổi (Commands). Sử dụng Dapper hoặc các phương thức `.Select()` / `.ProjectTo()` cho các truy vấn đọc hiệu năng cao (Read-only) nhằm tránh lỗi N+1 và Over-fetching.
   - **Quản lý Cấu hình & Lỗi**: Sử dụng Strongly-typed Configuration thông qua `IOptions<T>` hoặc `IOptionsSnapshot<T>`. Không viết cứng chuỗi thông báo lỗi; sử dụng `ResourceManager` để đọc từ tệp `.resx` tương ứng.

## Giai đoạn 3: Khắc phục lỗi & Kiểm thử (Verification & Troubleshooting)
6. **Xử lý lỗi Compiler**: Nếu gặp lỗi compiler (CS1503/CS1061), hãy mở interface hoặc class gốc để đối chiếu và copy chính xác method signature đúng. Nếu cùng một lỗi compiler xuất hiện quá 3 lần, bắt buộc phải ĐỔI CHIẾN THUẬT — đọc kỹ file test tương ứng để lấy signature chuẩn xác.
7. **Phạm vi tác động của TestWriter**: Đối với vai trò TestWriter, CHỈ được chỉnh sửa các file nằm trong thư mục `FloraCore.Tests/`. Tuyệt đối KHÔNG chạm vào production code.
8. **Chạy thử nghiệm đơn vị**: Chạy lệnh test. Nếu chạy `dotnet test --filter` không tìm thấy test nào, hãy chạy kiểm thử toàn bộ dự án (`dotnet test`) trước để kiểm tra tính hợp lệ.

## Giai đoạn 4: Nghiệm thu (Final Checks)
9. **Kiểm soát phiên bản**: KHÔNG gọi lệnh `git commit`. Chỉ sử dụng các lệnh trạng thái và chuẩn bị như `git status`, `git diff`, `git add`.
10. **Chạy Static Check**: Trước khi hoàn thành và báo cáo công việc, LUÔN LUÔN chạy công cụ static check thông qua `./scripts/final-check.ps1 validate-all` (hoặc `validate-policy`) và đảm bảo đạt độ chính xác 100%.
11. **Đồng bộ hóa Chỉ mục Cuối phiên**: Ngay trước khi hoàn thành công việc và báo cáo kết quả, luôn luôn chạy `codegraph sync` để chỉ mục CodeGraph của dự án luôn ở trạng thái mới nhất cho các phiên làm việc tiếp theo.