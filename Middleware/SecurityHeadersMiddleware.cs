namespace FloraCore.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 1. Ngăn chặn trình duyệt đoán định loại nội dung (MIME sniffing)
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

        // 2. Ngăn chặn trang web bị nhúng vào iframe (Chống Clickjacking)
        context.Response.Headers.Append("X-Frame-Options", "DENY");

        // 3. Kích hoạt bộ lọc XSS của trình duyệt
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");

        // 4. Kiểm soát thông tin referrer khi chuyển hướng link
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

        // 5. Chính sách bảo mật nội dung cơ bản (Có thể tùy chỉnh thêm tùy theo nhu cầu FE)
        // Bỏ qua CSP cho giao diện Scalar Docs vì nó cần sử dụng inline-script và inline-style
        if (!context.Request.Path.StartsWithSegments("/scalar"))
        {
            context.Response.Headers.Append("Content-Security-Policy", "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; font-src 'self' https://fonts.gstatic.com; img-src 'self' data: https:; frame-ancestors 'none';");
        }

        // 6. Ép buộc sử dụng HTTPS (HSTS) - Chỉ nên bật ở Production
        if (!context.Request.Host.Host.Contains("localhost"))
        {
            context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
        }

        await _next(context);
    }
}
