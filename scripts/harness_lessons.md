# BÀI HỌC KINH NGHIỆM ĐÃ TỰ ĐÚC KẾT

Từ các phiên chạy và báo cáo kiểm duyệt, dưới đây là các bài học kinh nghiệm được đúc kết để cải thiện quy trình phát triển và chất lượng code cho AI Developer Agent:

## 🔄 Các bài học khôi phục tự động (Không được xóa)
- **Bảo vệ tính thuần khiết của Domain:** Domain Layer tuyệt đối không phụ thuộc vào Infrastructure hoặc Application. Định nghĩa Entities phải kế thừa từ `BaseEntity`.
- **Primary Constructor Check:** Khi dùng Primary Constructor (C# 12+), nếu có tham số, phải kiểm tra null bằng `ArgumentNullException.ThrowIfNull` trước khi sử dụng.
- **Constructor Signature Alignment:** Trước khi viết unit test, hãy đọc kỹ cấu trúc của lớp production thực tế để khai báo tham số mock khớp 100% với signature thực tế.
- **Sử dụng AutoFixture:** Tích hợp thư viện `AutoFixture` để tạo dữ liệu giả lập (mock data) một cách tự động và linh hoạt cho các unit/integration test, giúp giảm boilerplate code và tăng tốc độ phát triển test.
- **Tuân thủ nguyên tắc "Surgical Changes":** Bắt buộc chỉ sửa đổi chính xác những dòng code cần thiết cho nhiệm vụ được giao. Tuyệt đối không tự ý định dạng lại hoặc tái cấu trúc (refactor) code xung quanh đang chạy ổn định, đặc biệt là các file không thuộc phạm vi trực tiếp của nhiệm vụ. Việc thực hiện các thay đổi không được yêu cầu, dù có thể là cải tiến kỹ thuật, sẽ bị coi là vi phạm chính sách và ảnh hưởng đến điểm đánh giá.

## 📝 Bài học mới từ phiên chạy này:

*   **Vai trò và Ràng buộc Công cụ:**
    *   **Planner:** Chỉ được phép tạo kế hoạch và đặc tả, ghi vào thư mục `docs/` hoặc file `execution_plan.md`. Tuyệt đối không được sửa đổi trực tiếp mã nguồn production.
    *   **TestWriter:** Tập trung vào viết unit test và integration test, đảm bảo bao phủ đầy đủ các kịch bản.
*   **Tránh lặp lại thao tác thừa:**
    *   Khi gặp lỗi, đọc kỹ thông báo lỗi và các file liên quan (stack trace, test cases) để hiểu rõ nguyên nhân. Tránh lặp lại các thao tác đã thất bại trước đó (ví dụ: ghi đè cùng một nội dung file).
*   **Phân tích kỹ trước khi code:**
    *   Trước khi bắt đầu viết code, hãy dành thời gian phân tích kỹ yêu cầu, kiến trúc dự án và các file liên quan. Điều này giúp tránh các lỗi sai sót và đảm bảo code tuân thủ các nguyên tắc thiết kế.
*   **Tận dụng các test hiện có:**
    *   Khi viết test cho một controller hoặc handler mới, hãy tham khảo các test hiện có cho các controller/handler tương tự để hiểu cấu trúc, cách mock các dependency và viết assertions.
*   **Sử dụng các công cụ hỗ trợ:**
    *   Sử dụng các công cụ hỗ trợ như `view_source` để xem nội dung của các file hiện có, giúp hiểu rõ hơn về code và tránh các lỗi sai sót.

## 🧪 Bài học về kiểm thử (Tests):

*   **Đọc kỹ cấu trúc lớp production:** Trước khi viết unit test, hãy đọc kỹ cấu trúc của lớp production thực tế để khai báo tham số mock khớp 100% với signature thực tế.
*   **Sử dụng AutoFixture:** Tích hợp thư viện `AutoFixture` để tạo dữ liệu giả lập (mock data) một cách tự động và linh hoạt cho các unit/integration test, giúp giảm boilerplate code và tăng tốc độ phát triển test.

## 🏛️ Bài học về kiến trúc (Clean Architecture & CQRS):

*   **Bảo vệ tính thuần khiết của Domain:** Domain Layer tuyệt đối không phụ thuộc vào Infrastructure hoặc Application. Định nghĩa Entities phải kế thừa từ `BaseEntity`.

## 💻 Bài học về C# (.NET 9):

*   **Primary Constructor Check:** Khi dùng Primary Constructor (C# 12+), nếu có tham số, phải kiểm tra null bằng `ArgumentNullException.ThrowIfNull` trước khi sử dụng.
*   **Tuân thủ nguyên tắc "Surgical Changes":** Bắt buộc chỉ sửa đổi chính xác những dòng code cần thiết cho nhiệm vụ được giao. Tuyệt đối không tự ý định dạng lại hoặc tái cấu trúc (refactor) code xung quanh đang chạy ổn định, đặc biệt là các file không thuộc phạm vi trực tiếp của nhiệm vụ. Việc thực hiện các thay đổi không được yêu cầu, dù có thể là cải tiến kỹ thuật, sẽ bị coi là vi phạm chính sách và ảnh hưởng đến điểm đánh giá.
*   **Ràng buộc vai trò công cụ:** Các công cụ (Planner, TestWriter,...) cần tuân thủ nghiêm ngặt vai trò được giao. Ví dụ, Planner chỉ được tạo kế hoạch, không được phép sửa mã nguồn production.
*   **Thêm thư viện MediatR:** Khi sử dụng `INotification` và `INotificationHandler` cần đảm bảo đã thêm thư viện `MediatR` vào project.
*   **Sử dụng đúng namespace:** Kiểm tra và sử dụng đúng namespace cho các interface và class. Ví dụ: `MediatR.INotification` thay vì một namespace tự định nghĩa khác.
*   **Tuân thủ nghiêm ngặt vai trò công cụ:** AI Agent cần tuân thủ nghiêm ngặt vai trò được gán. Trong phiên chạy này, Planner đã vi phạm quy tắc khi cố gắng viết code test (thuộc về TestWriter).