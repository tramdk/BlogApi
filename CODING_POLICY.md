# CODING POLICY & AGENT INSTRUCTIONS

Chào mừng bạn đến với Coding Policy của dự án. AI Agent **BẮT BUỘC** phải đọc và tuân thủ các quy tắc này trước khi thực thi bất kỳ yêu cầu mới nào.

## 1. Nguyên Tắc Cốt Lõi (Core Principles)
*   **Clean Code & Nguyên lý SOLID:** Code phải dễ đọc, dễ bảo trì, có tính mở rộng cao và tuân thủ chặt chẽ các nguyên lý SOLID.
*   **Không phá vỡ code hiện tại:** Trước khi thêm/sửa, phải tìm hiểu ngữ cảnh và các cấu trúc hiện có. Hãy kế thừa design pattern hiện tại thay vì tạo ra style mới một cách không cần thiết.
*   **Đảm bảo tính chính xác:** Luôn kiểm tra (build/test) lại code sau khi sửa.
*   **Bảo mật:** Không lưu trữ hard-code mật khẩu, connection strings trong code.

## 2. Quy Tắc Cho Backend (.NET C# - BlogApi)
*   **Kiến trúc:** Dự án tuân theo **Clean Architecture** gồm các tầng cơ bản (Domain, Application, Infrastructure, Web/API).
    *   **Domain:** Chứa Entities (`Product`, `Post`, v.v.). Đây là lõi của ứng dụng, KHÔNG phụ thuộc vào bất kỳ framework bên ngoài nào.
    *   **Application:** Chứa interfaces, DTOs, logic nghiệp vụ, và extensions (`ServiceCollectionExtensions.cs`).
    *   **Infrastructure:** Nơi giao tiếp với DB, chứa `AppDbContext`, repository implementations và migrations. Sử dụng Entity Framework Core.
    *   **API:** Chỉ chứa Controllers/Endpoints. Controllers phải mỏng (thin controllers), đẩy mọi logic xuống tầng Application.
*   **Database & Dapper / EF Core:** 
    *   Sử dụng LINQ & EF Core cho các thao tác thông thường.
    *   Đảm bảo cấu hình quan hệ Entities bằng Fluent API trong `OnModelCreating`.
*   **Naming Conventions:**
    *   `PascalCase` cho Tên lớp (Class), Phương thức (Method), Thuộc tính (Property).
    *   `camelCase` cho Tham số (Parameter) và Biến cục bộ (Local Variable).
    *   `_camelCase` cho private readonly fields.
    *   Tiền tố `I` cho Interfaces (e.g., `IRepository`).
*   **Asynchronous Programming (Bất đồng bộ):**
    *   Luôn dùng `async/await` cho các tác vụ I/O (Database, Network, File).
    *   Các hàm bất đồng bộ phải có hậu tố `Async` (ví dụ: `GetProductByIdAsync`).
    *   **Tuyệt đối không dùng** `.Result` hoặc `.Wait()` để tránh Deadlock. Nên dùng `CancellationToken` nếu có thể.
*   **Modern C# & .NET 8 Features:**
    *   Khuyến khích dùng `record` thay cho `class` đối với DTOs hoặc CQRS Commands/Queries vì chúng gọn nhẹ và immutable (bất biến).
    *   Dùng **File-scoped namespaces** thay cho block-scoped để code đỡ bị thụt lề quá nhiều.
    *   Bật và xử lý triệt để Nullable Reference Types (`<Nullable>enable</Nullable>`).
*   **Dependency Injection & Configurations:**
    *   Nên dùng `IOptions<T>` hoặc `IOptionsSnapshot<T>` để inject settings từ `appsettings.json`, KHÔNG inject `IConfiguration` trực tiếp rải rác khắp nơi.
    *   Phân định rõ vòng đời: `Scoped` cho DbContext/Repository, `Singleton` cho Cache/State, `Transient` cho logic nhẹ.

## 3. Quy Tắc Cho Frontend (TypeScript - cái-tiệm-hoa-của-chin)
*   **TypeScript:** **Tuyệt đối không sử dụng `any`**. Phải định nghĩa Interfaces/Types đầy đủ, chính xác.
*   **Components:** Sử dụng Hàm (Functional Components) + Hooks.
*   **Tên file & Thư mục:**
    *   File React components sử dụng `PascalCase.tsx`.
    *   File logic / utils sử dụng `camelCase.ts` hoặc `kebab-case.ts`.
*   **Giao diện & UI:**
    *   Hiệu ứng mượt mà, typography tối ưu, phối màu phù hợp và theo hướng Premium / Aesthetic.

## 4. Workflows Của Agent Khi Nhận Task Mới
Mỗi khi nhận một yêu cầu phát triển tính năng mới, AI Agent phải tuân theo lộ trình sau:
1.  **Phân tích (Analyze):** Đọc các file liên quan để hiểu ngữ cảnh. (VD: Nếu yêu cầu thêm field mới cho `Product`, phải xem xét cả `Product.cs`, `AppDbContext.cs`, DTOs,...).
2.  **Lên kế hoạch (Plan):** Đưa ra một tóm tắt ngắn để người dùng biết sẽ sửa những file nào.
3.  **Thực thi (Execute):** Viết code hoặc sửa code một cách chi tiết.
4.  **Kiểm tra (Verify):** Khuyến khích sử dụng các lệnh Terminal (`dotnet build` cho hệ Backend) để đảm bảo không có lỗi cú pháp.

## 5. Thiết Kế API (RESTful & GraphQL)
*   **Routing & Naming:** Luôn dùng danh từ số nhiều cho RESTful API endpoints (VD: `/api/products` thay vì `/api/getProduct`).
*   **HTTP Methods:** Phân định rõ ràng: `GET` (đọc), `POST` (tạo mới), `PUT/PATCH` (cập nhật), `DELETE` (xóa).
*   **Standard Response Format:**
    *   Với dữ liệu thành công: Có thể trả về trực tiếp Data hoặc dùng một wrapper thống nhất (ví dụ: `ApiResponse<T> { data, message, isSuccess }`) tùy theo convention của team Frontend.
    *   Với dữ liệu lỗi: Bắt buộc tuân thủ chuẩn **RFC 7807 (`ProblemDetails`)** vốn là chuẩn mặc định của .NET Core hiện tại, tránh việc tự chế format lỗi lộn xộn.
*   **Phân trang & Lọc:** Các API trả về danh sách lớn (`GET /api/products`) MẶC ĐỊNH phải có tham số phân trang (`pageIndex`, `pageSize`).

## 6. Xử Lý Lỗi (Error Handling)
*   **Backend:** TUYỆT ĐỐI không dùng `try-catch` bừa bãi trong Controller. Phải có một **Global Exception Handler** (Middleware/Filter) để hứng toàn bộ Exception không lường trước và trả ra JSON đúng định dạng chuẩn (`ProblemDetails` hoặc tự custom).
*   **Frontend:** Khi gọi API thât bại, phải hiển thị Toast/thông báo người dùng thân thiện. Hạn chế tối đa thông báo chung chung kiểu "Lỗi hệ thống". Bắt buộc log lỗi ra Console với chi tiết trong môi trường Dev, nhưng ẩn chi tiết đi trong môi trường Production.
*   **Validation:** Thực hiện ở CẢ 2 bên. Frontend validate sớm để tăng trải nghiệm UX, Backend (sử dụng FluentValidation) để đảm bảo toàn vẹn dữ liệu.

## 7. Git Workflow & Version Control
*   **Tên nhánh (Branching):** Định dạng tên nhánh phải phản ánh loại task: `feature/tên-tính-năng`, `bugfix/tên-lỗi`, `hotfix/tên-lỗi-nghiêm-trọng`.
*   **Luận điểm Commit (Commit Messages):** Viết theo chuẩn **Conventional Commits** (ví dụ: `feat: add product review functionality`, `fix: resolve cors issue in production`, `chore: update dependencies`).

## 8. Bảo Mật & Xác Thực (Security & Authentication)
*   **Authentication:** Sử dụng JWT cho xác thực. Access Token có thời hạn ngắn, kết hợp chức năng cấp lại token (Refresh Token) nếu cần thiết.
*   **Authorization:** Phân quyền theo Role-Based (RBAC) cho Backend (`[Authorize(Roles="Admin")]`). Kiểm tra kỹ quyền truy cập vào dữ liệu (Data-level authorization) - người dùng A không được phép sửa/xóa bài viết của người dùng B.
*   **CORS:** Chỉ cho phép các domain được chỉ định (frontend deployment url) trong cấu hình CORS Production.

## 9. Kiểm Thử (Testing)
*   **Unit Tests:** Mọi logic nghiệp vụ quan trọng (trong Application layer hoặc các hàm Utility/Helper) đều phải có Unit Test.
*   **Frameworks:** Sử dụng `xUnit`, `Moq`, và `FluentAssertions` cho Backend. Đối với Frontend, sử dụng `Jest` và `React Testing Library` (hoặc Vitest).
*   **Quy tắc AAA:** Luôn viết test theo chuẩn Arrange - Act - Assert.
*   **Integration Tests:** Khuyến khích viết Integration Tests cho các luồng API quan trọng (như Thanh toán, Đặt hàng, Tạo bài viết) để đảm bảo Controller - Service - Database hoạt động liền mạch.

## 10. Bộ Nhớ Đệm & Tối Ưu Hiệu Năng (Caching & Performance)
*   **Backend Caching:** Sử dụng `MemoryCache` cho các dữ liệu nhỏ, cấu hình tĩnh hoặc ít thay đổi (Ví dụ: danh sách Categories). Cân nhắc dùng `Distributed Cache` (như Redis) cho dữ liệu cần share giữa các server hoặc session user.
*   **Frontend Caching:** Tận dụng lợi thế của State Management, `localStorage` hoặc thư viện query data (như React Query / SWR). Không vứt request vô tội vạ để làm ngập gầm Server nếu data chưa quá hạn (Stale Time).
*   **Tối ưu Truy vấn DB (Lỗi N+1):** Cẩn trọng khi dùng Entity Framework để thao tác dữ liệu quan hệ. Phải chủ động sử dụng `.Include()` đúng lúc để load Eager. Khuyến khích dùng projection `.Select()` để chỉ lấy các trường thiết yếu, tránh việc memory bloat hoặc sinh ra quá nhiều câu lệnh SQL rời rạc.

---
**Ghi chú:** Khi Agent nhìn thấy file này, đồng nghĩa với việc Agent đã ký cam kết tuân thủ 100% các điều khoản trên trong suốt quá trình làm việc.


Behavioral guidelines to reduce common LLM coding mistakes. Merge with project-specific instructions as needed.

**Tradeoff:** These guidelines bias toward caution over speed. For trivial tasks, use judgment.

## 1. Think Before Coding

**Don't assume. Don't hide confusion. Surface tradeoffs.**

Before implementing:
- State your assumptions explicitly. If uncertain, ask.
- If multiple interpretations exist, present them - don't pick silently.
- If a simpler approach exists, say so. Push back when warranted.
- If something is unclear, stop. Name what's confusing. Ask.

## 2. Simplicity First

**Minimum code that solves the problem. Nothing speculative.**

- No features beyond what was asked.
- No abstractions for single-use code.
- No "flexibility" or "configurability" that wasn't requested.
- No error handling for impossible scenarios.
- If you write 200 lines and it could be 50, rewrite it.

Ask yourself: "Would a senior engineer say this is overcomplicated?" If yes, simplify.

## 3. Surgical Changes

**Touch only what you must. Clean up only your own mess.**

When editing existing code:
- Don't "improve" adjacent code, comments, or formatting.
- Don't refactor things that aren't broken.
- Match existing style, even if you'd do it differently.
- If you notice unrelated dead code, mention it - don't delete it.

When your changes create orphans:
- Remove imports/variables/functions that YOUR changes made unused.
- Don't remove pre-existing dead code unless asked.

The test: Every changed line should trace directly to the user's request.

## 4. Goal-Driven Execution

**Define success criteria. Loop until verified.**

Transform tasks into verifiable goals:
- "Add validation" → "Write tests for invalid inputs, then make them pass"
- "Fix the bug" → "Write a test that reproduces it, then make it pass"
- "Refactor X" → "Ensure tests pass before and after"

For multi-step tasks, state a brief plan:
```
1. [Step] → verify: [check]
2. [Step] → verify: [check]
3. [Step] → verify: [check]
```

Strong success criteria let you loop independently. Weak criteria ("make it work") require constant clarification.

---

**These guidelines are working if:** fewer unnecessary changes in diffs, fewer rewrites due to overcomplication, and clarifying questions come before implementation rather than after mistakes.