# 🚀 Redis Implementation Guide

Dự án này sử dụng **Redis** làm giải pháp lưu trữ dữ liệu phân tán (Distributed Storage) để tối ưu hóa hiệu năng và khả năng mở rộng. Redis được tích hợp cho hai mục đích chính: **Distributed Caching** và **Distributed Rate Limiting**.

---

## 📑 Mục lục
1. [Tổng quan](#-tổng-quan)
2. [Cấu hình Infrastructure (Docker)](#-cấu-hình-infrastructure-docker)
3. [Cấu hình Ứng dụng (.NET)](#-cấu-hình-ứng-dụng-net)
4. [Tính năng: Distributed Caching](#-tính-năng-distributed-caching)
5. [Tính năng: Distributed Rate Limiting](#-tính-năng-distributed-rate-limiting)
6. [Kiểm tra & Giám sát](#-kiểm-tra--giám-sát)

---

## 🎯 Tổng quan

Trong môi trường container hóa (Docker) hoặc khi triển khai đa máy chủ (Multi-instance), việc lưu trữ cache hoặc đếm số lượng yêu cầu (Rate Limit) trong bộ nhớ cục bộ (In-Memory) sẽ không hiệu quả vì dữ liệu không được đồng bộ. 

**Redis giải quyết vấn đề này bằng cách:**
- **Đồng bộ hóa Cache:** Tất cả các instance của API đều truy cập chung một kho dữ liệu.
- **Rate Limiting chính xác:** Đảm bảo một Client không thể vượt quá giới hạn bằng cách gửi yêu cầu tới các instance khác nhau.

---

## 🐳 Cấu hình Infrastructure (Docker)

Redis được định nghĩa trong file `docker-compose.yml`:

```yaml
redis:
  image: redis:alpine
  container_name: blog-api-redis
  ports:
    - "6379:6379"
```

Khi chạy trong Docker network, API sẽ kết nối tới Redis thông qua hostname `redis`.

---

## ⚙ Cấu hình Ứng dụng (.NET)

### 1. Connection String
Trong `appsettings.json` hoặc biến môi trường:
```json
"ConnectionStrings": {
  "Redis": "redis:6379" // Local: "localhost:6379"
}
```

### 2. Đăng ký Dịch vụ (DI)
Các dịch vụ được đăng ký trong `ServiceCollectionExtensions.cs`:

```csharp
public static IServiceCollection AddRedisCache(this IServiceCollection services, IConfiguration configuration)
{
    var redisConnectionString = configuration.GetConnectionString("Redis");
    if (string.IsNullOrEmpty(redisConnectionString))
    {
        services.AddDistributedMemoryCache(); // Fallback nếu không có Redis
    }
    else
    {
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "BlogApi_";
        });
    }
    return services;
}
```

---

## ⚡ Tính năng: Distributed Caching

Dự án sử dụng **MediatR Pipeline Behavior** để tự động xử lý cache cho các Query.

### Cách sử dụng:
Để một Query được lưu vào Redis, chỉ cần thêm attribute `[Cacheable]` vào class Query đó.

**Ví dụ (`GetPostsQuery.cs`):**
```csharp
[Cacheable(ExpirationMinutes = 5)]
public record GetPostsQuery(string? Cursor, int PageSize = 10) : IRequest<PagedResult<PostDto>>;
```

### Luồng hoạt động:
1. `CachingBehavior` chặn yêu cầu trước khi đến Handler.
2. Kiểm tra trong Redis với key: `Cache_GetPostsQuery_{"PageSize":10,...}`.
3. **Cache Hit:** Trả về dữ liệu từ Redis ngay lập tức (Thời gian phản hồi < 5ms).
4. **Cache Miss:** Thực hiện logic (truy vấn DB), sau đó lưu kết quả vào Redis trước khi trả về.

---

## 🛡 Tính năng: Distributed Rate Limiting

Dự án tích hợp `AspNetCoreRateLimit.Redis` để quản lý giới hạn yêu cầu.

```csharp
if (!string.IsNullOrEmpty(redisConnectionString))
{
    // Cấu hình lưu trữ bộ đếm và policy vào Redis
    services.AddDistributedRateLimiting();
}
```

**Lợi ích:**
Nếu bạn giới hạn 100 yêu cầu/phút, dù client gọi đến Container A hay Container B, Redis sẽ cộng dồn bộ đếm chính xác, ngăn chặn việc "lách luật" bằng cách đổi server.

---

## 🔍 Kiểm tra & Giám sát

### 1. Kiểm tra qua Logs
Khi Redis hoạt động, bạn sẽ thấy các dòng log sau trong console:
- `[INF] Cache missed for Cache_... added to cache` (Lần đầu truy cập).
- `[INF] Cache hit for Cache_...` (Các lần truy cập sau).

### 2. Kiểm tra trực tiếp trong Container
Bạn có thể truy cập vào container Redis để xem dữ liệu thực tế:
```bash
docker exec -it blog-api-redis redis-cli
127.0.0.1:6379> KEYS *
127.0.0.1:6379> GET "BlogApi_Cache_GetPostsQuery_..."
```

---

> **Lưu ý:** Nếu Redis gặp sự cố, hệ thống sẽ log lỗi nhưng vẫn cho phép ứng dụng chạy (thông qua cơ chế fallback hoặc error handling trong behavior) để đảm bảo tính sẵn sàng cao.
