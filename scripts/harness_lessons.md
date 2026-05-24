# BÀI HỌC KINH NGHIỆM ĐÃ TỰ ĐÚC KẾT

## ⚙️ 1. Quản lý Lớp Domain & Application
- **Bảo vệ tính thuần khiết của Domain:** Domain Layer tuyệt đối không phụ thuộc vào Infrastructure hoặc Application. Định nghĩa Entities phải kế thừa từ `BaseEntity`.

## 🛠️ 2. Ép buộc Quy tắc C# 12+
- **Primary Constructor Check:** Khi dùng Primary Constructor (C# 12+), nếu có tham số, phải kiểm tra null bằng `ArgumentNullException.ThrowIfNull` trước khi sử dụng.

## 🧪 3. Viết Test Cases
- **Constructor Signature Alignment:** Trước khi viết unit test, hãy đọc kỹ cấu trúc của lớp production thực tế để khai báo tham số mock khớp 100% với signature thực tế.