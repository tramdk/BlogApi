# BÀI HỌC KINH NGHIỆM ĐÃ TỰ ĐÚC KẾT

## ⚙️ 1. Quản lý Lớp Domain & Application
- **Bảo vệ tính thuần khiết của Domain:** Domain Layer tuyệt đối không phụ thuộc vào Infrastructure hoặc Application. Định nghĩa Entities phải kế thừa từ `BaseEntity`.

## 🛠️ 2. Ép buộc Quy tắc C# 12+
- **Primary Constructor Check:** Khi dùng Primary Constructor (C# 12+), nếu có tham số, phải kiểm tra null bằng `ArgumentNullException.ThrowIfNull` trước khi sử dụng.

## 🧪 3. Viết Test Cases
- **Constructor Signature Alignment:** Trước khi viết unit test, hãy đọc kỹ cấu trúc của lớp production thực tế để khai báo tham số mock khớp 100% với signature thực tế.

## 🧪 4. Mock IQueryable với async support
- Handler dùng `ToListAsync()` trên `IQueryable<T>`, nhưng `List<T>.AsQueryable()` không implement `IAsyncEnumerable<T>`.
- **Fix:** Dùng `source.AsAsyncQueryable()` thay vì `source.AsQueryable()`. Extension method này trả về `TestAsyncEnumerable<T>` wrapper implement cả `IQueryable<T>` lẫn `IAsyncEnumerable<T>`.
- Pattern:
  ```csharp
  mock.Setup(repo => repo.GetQueryable()).Returns(orders.AsAsyncQueryable());
  ```

## 🧪 5. Integration Test — ApiResponse<T> unwrapping
- Controller trả về `ApiResponse<T>` envelope (qua `ControllerBaseExtensions.OkOrError`).
- Test KHÔNG được assert trực tiếp response body — PHẢI unwrap `.Data`:
  ```csharp
  var response = await client.PostAsJsonAsync("/api/v1/orders", payload);
  var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<OrderDto>>();
  apiResponse.Data.Should().NotBeNull();
  // Assert trên apiResponse.Data, không phải response
  ```
- Dùng `System.Net.Http.Json` (`ReadFromJsonAsync`, `PostAsJsonAsync`).

## 🧪 6. Date timing drift — KHÔNG dùng DateTime.Now trực tiếp
- `DateTime.Now` trong seed data và `DateTime.Now` trong filter query lệch nhau micro-giây → sai kết quả.
- **Fix:** Dùng `DateTime.Today` (midnight) hoặc một biến `var today = DateTime.Today;` dùng chung cho cả seed và filter.
- Pattern:
  ```csharp
  var today = DateTime.Today;
  var orders = new List<Order> {
      new() { OrderDate = today.AddDays(-1), ... },
      new() { OrderDate = today, ... }
  };
  var query = new GetOrderStatisticsQuery { StartDate = today };
  ```

## 🧪 7. Route prefix — PHẢI có /api/v1/
- Controller route là `[Route("api/v1/[controller]")]`.
- Integration test URL PHẢI dùng `/api/v1/...` (VD: `/api/v1/auth/login`, không phải `/api/auth/login`).

## 🧪 8. Auth setup trong Integration Test
- User PHẢI được register trước khi login:
  ```csharp
  var registerPayload = new RegisterCommand { Email = "admin@test.com", Password = "Test@123", ... };
  await client.PostAsJsonAsync("/api/v1/auth/register", registerPayload);
  var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", loginPayload);
  ```
- Sau login, extract token từ response (có thể trong ApiResponse.Data.Token hoặc ApiResponse.Token).
- Gắn token: `client.DefaultRequestHeaders.Authorization = new("Bearer", token);`

## 🧪 9. Seed data — Foreign Key constraint
- Khi seed Order, Order.UserId PHẢI là Guid của user đã seed trong DB (không được random Guid mới).
- Pattern: tạo User trước, dùng userId của user đó cho Order.UserId.
- ShippingAddress NOT NULL: seed Order PHẢI có `ShippingAddress = new Address { ... }`.

## 🧪 10. Dùng CustomWebApplicationFactory
- KHÔNG dùng `WebApplicationFactory<Program>` trực tiếp — dùng `CustomWebApplicationFactory` (trong FloraCore.Tests) để seed DB + config.
- Factory fixture: implement `IClassFixture<CustomWebApplicationFactory>`.
