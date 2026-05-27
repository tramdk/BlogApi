# BÀI HỌC KINH NGHIỆM ĐÃ TỰ ĐÚC KẾT

## ⚙️ 1. Quản lý Lớp Domain & Application
- **Bảo vệ tính thuần khiết của Domain:** Domain Layer tuyệt đối không phụ thuộc vào Infrastructure hoặc Application. Định nghĩa Entities phải kế thừa từ `BaseEntity`.

## 🛠️ 2. Ép buộc Quy tắc C# 12+
- **Primary Constructor Check:** Khi dùng Primary Constructor (C# 12+), nếu có tham số, phải kiểm tra null bằng `ArgumentNullException.ThrowIfNull` trước khi sử dụng.

## 🧪 3. Viết Test Cases
- **Constructor Signature Alignment:** Trước khi viết unit test, hãy đọc kỹ cấu trúc của lớp production thực tế để khai báo tham số mock khớp 100% với signature thực tế.
- **Sử dụng AutoFixture:** Tích hợp thư viện `AutoFixture` để tạo dữ liệu giả lập (mock data) một cách tự động và linh hoạt cho các unit/integration test, giúp giảm boilerplate code và tăng tốc độ phát triển test.

## 🚀 4. Quy trình làm việc & Tuân thủ chính sách
- **Tuân thủ nguyên tắc "Surgical Changes":** Bắt buộc chỉ sửa đổi chính xác những dòng code cần thiết cho nhiệm vụ được giao. Tuyệt đối không tự ý định dạng lại hoặc tái cấu trúc (refactor) code xung quanh đang chạy ổn định, đặc biệt là các file không thuộc phạm vi trực tiếp của nhiệm vụ. Việc thực hiện các thay đổi không được yêu cầu, dù có thể là cải tiến kỹ thuật, sẽ bị coi là vi phạm chính sách và ảnh hưởng đến điểm đánh giá.