# 🐳 Hướng dẫn Docker Toàn tập (Blog API)

Chào mừng bạn đến với hướng dẫn chi tiết về Docker cho dự án Blog API. Tài liệu này bao gồm cả kiến thức cơ bản cho người mới bắt đầu và các kỹ thuật nâng cao dành cho chuyên gia.

---

## 🟢 PHẦN 1: DÀNH CHO NGƯỜI MỚI BẮT ĐẦU

### 1. Docker là gì? (Ví dụ dễ hiểu)
Hãy tưởng tượng bạn đang chuyển nhà:
- **Ứng dụng của bạn** là đồ đạc.
- **Docker Image** giống như một cái bản thiết kế hoặc một cái thùng carton đã được đóng gói sẵn mọi thứ (code, thư viện, cài đặt).
- **Docker Container** là một ngôi nhà thực tế được xây từ bản thiết kế đó, hoặc là cái thùng đã được mở ra và đồ đạc đang hoạt động bên trong.

### 2. Các khái niệm cốt lõi trong dự án
- **Dockerfile**: Cái "Bản thiết kế" để tạo ra Image.
- **docker-compose.yml**: Người "Điều phối viên" để chạy nhiều dịch vụ cùng lúc (API, Database, Redis).

### 3. Cách sử dụng nhanh
| Lệnh | Ý nghĩa |
| :--- | :--- |
| `docker-compose up -d` | **Khởi động mọi thứ** (chạy ngầm). |
| `docker-compose down` | **Dừng và dọn dẹp** mọi thứ. |
| `docker-compose up -d --build` | **Cập nhật code mới** khi bạn vừa sửa code C#. |
| `docker logs -f blog-api-service` | Theo dõi hoạt động của API theo thời gian thực. |

---

## 🟡 PHẦN 2: KIẾN THỨC NÂNG CAO

### 1. Multi-stage Builds (Tối ưu Image)
Dự án sử dụng 2 giai đoạn (stage) trong `Dockerfile`:
- **Stage 1 (Build):** Dùng SDK đầy đủ để biên dịch code.
- **Stage 2 (Runtime):** Chỉ dùng Runtime gọn nhẹ để chạy app.
**Kết quả:** Image cuối cùng cực kỳ nhỏ gọn (~200MB thay vì >800MB) và bảo mật hơn.

### 2. Docker Layer Caching
Docker lưu trữ kết quả của mỗi lệnh thành một "Layer". Bằng cách copy file `.csproj` và chạy `dotnet restore` riêng biệt, Docker sẽ cache lại các thư viện NuGet. Nếu bạn chỉ sửa code, quá trình build lại sẽ cực nhanh vì không cần tải lại thư viện.

### 3. Networking & Hostname
Trong mạng nội bộ của Docker, các dịch vụ gọi nhau qua tên service:
- API kết nối DB qua hostname: `db`.
- API kết nối Redis qua hostname: `redis`.
Docker tự xử lý việc phân giải IP bên trong Bridge Network.

### 4. Volumes: Persistence vs Bind Mounts
- **Named Volume (`postgres_data`):** Dùng cho DB để đảm bảo dữ liệu không mất khi Container bị xóa.
- **Bind Mount (`./uploads:/app/uploads`):** Liên kết thư mục trên máy bạn với Container để bạn có thể xem file trực tiếp từ Windows Explorer.

### 5. Bảo mật: Non-root Users
Trong `Dockerfile`, chúng ta sử dụng `USER app`. Mặc định Docker chạy với quyền `root`, việc chuyển sang user `app` (quyền hạn thấp) giúp ngăn chặn hacker chiếm quyền điều khiển hệ thống nếu ứng dụng bị leak lỗ hổng.

---

## 💡 Luồng làm việc khuyến nghị
1. Mở **Docker Desktop** để quản lý giao diện trực quan.
2. Chạy `docker-compose up -d` khi bắt đầu làm việc.
3. Sử dụng [http://localhost:8080/scalar/v1](http://localhost:8080/scalar/v1) để kiểm tra API.
4. Khi sửa code C#, hãy dùng lệnh `--build` để cập nhật container.

---
> **Lưu ý:** Nếu bạn gặp lỗi "Port is already allocated", hãy kiểm tra xem có ứng dụng nào khác đang chiếm cổng 8080, 5432 hoặc 6379 trên máy không nhé!
