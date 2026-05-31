# BÀI HỌC KINH NGHIỆM ĐÃ TỰ ĐÚC KẾT

Từ các phiên chạy và báo cáo kiểm duyệt, dưới đây là các bài học kinh nghiệm được hệ thống đúc kết nhằm tối ưu quy trình phát triển và bảo đảm chất lượng cho AI Agent:

## 🏛️ 1. Kiến trúc & Thiết kế (Clean Architecture & Domain)
*   **Bảo vệ tính thuần khiết của Domain**: Domain Layer tuyệt đối không được phụ thuộc vào Infrastructure hoặc Application. Định nghĩa các Entities phải kế thừa từ `BaseEntity`.
*   **Tập trung hóa Quy tắc Nghiệp vụ (SSOT)**: Logic nghiệp vụ thuộc về Domain Entity hoặc Domain Service. Tuyệt đối không sao chép logic tại nhiều Handlers/Controllers.

## 💻 2. Tiêu chuẩn C# (.NET 9) & Coding Policy
*   **Primary Constructor Check (C# 12+)**: Khi sử dụng Primary Constructor, nếu có tham số, phải kiểm tra null bằng `ArgumentNullException.ThrowIfNull` hoặc toán tử gán trước khi sử dụng.
*   **Bất đồng bộ (Async/Await)**: Luôn sử dụng `async/await` kèm `CancellationToken` cho các tác vụ I/O. Hàm async phải có hậu tố `Async`. Không sử dụng `.Result` hoặc `.Wait()`.
*   **Sử dụng đúng Namespace**: Kiểm tra và sử dụng đúng namespace cho các thư viện bên ngoài (ví dụ: `MediatR.INotification` và `MediatR.INotificationHandler`). Đảm bảo thêm thư viện `MediatR` vào dự án khi cần dùng.

## 🧪 3. Quy trình Kiểm thử & Mocking (Testing)
*   **Đọc kỹ cấu trúc lớp production**: Trước khi viết unit test, hãy đọc kỹ cấu trúc (method signature, constructors) của lớp production thực tế để khai báo tham số mock khớp 100% với signature thực tế.
*   **Tận dụng AutoFixture**: Tích hợp thư viện `AutoFixture` để tự động tạo dữ liệu giả lập (mock data) cho unit/integration tests nhằm giảm thiểu boilerplate code và tăng tốc độ phát triển.
*   **Tham khảo các test hiện có**: Khi viết test cho controller hoặc handler mới, hãy tham khảo các test case tương tự đã có sẵn để hiểu cách mock dependencies và viết assertions hợp lệ.

## 🛡️ 4. Quy tắc vận hành & Ràng buộc Công cụ (Agent Workflows)
*   **Tuân thủ nguyên tắc "Surgical Changes"**: Chỉ sửa đổi chính xác những dòng code cần thiết cho nhiệm vụ được giao. Tuyệt đối không tự ý định dạng lại hoặc tái cấu trúc (refactor) code xung quanh đang chạy ổn định để tránh vi phạm chính sách kiểm duyệt tĩnh.
*   **Ràng buộc nghiêm ngặt Vai trò Công cụ**:
    *   **Planner**: Chỉ được phép khảo sát, lập kế hoạch thực thi chi tiết (`docs/plans/execution_plan.md`) và tạo file stub rỗng. Tuyệt đối không được viết code logic hoặc chạy lệnh biên dịch/test.
    *   **TestWriter**: Chỉ được phép tạo và sửa đổi các file kiểm thử trong thư mục `FloraCore.Tests/`. Tuyệt đối không chạm vào production code.
*   **Tránh lặp lại thao tác thừa**: Khi gặp lỗi biên dịch hoặc lỗi test, hãy dùng `view_source` đọc kỹ stack trace để hiểu nguyên nhân gốc rễ. Tuyệt đối không lặp lại nguyên văn thao tác/nội dung ghi đè đã thất bại trước đó.