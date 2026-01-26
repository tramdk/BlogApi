# Hướng dẫn Quản lý File trong dự án BlogApi

## 1. Tổng quan
Tính năng Quản lý File cho phép tải lên, lưu trữ metadata và liên kết file với các đối tượng khác nhau trong hệ thống (như Sản phẩm, Bài viết, Người dùng) một cách linh hoạt.

## 2. Cấu trúc Database (`FileMetadata`)
Bảng `FileMetadata` lưu trữ các thông tin chi tiết về file:
- **FileName**: Tên file gốc người dùng tải lên.
- **StoredName**: Tên file duy nhất được lưu trên đĩa (tránh trùng lặp).
- **FilePath**: Đường dẫn vật lý đến file.
- **ObjectId**: ID của đối tượng liên quan (ví dụ: ID của bài viết).
- **ObjectType**: Loại đối tượng (ví dụ: "Post", "Product", "Avatar").
- **UploadedById**: ID của người dùng đã tải lên.

## 3. Cách sử dụng API

### Tải lên File (Upload)
- **Endpoint**: `POST /api/files/upload`
- **Body**: `multipart/form-data` (chọn file)
- **Query parameters (Tùy chọn)**:
    - `objectId`: ID của đối tượng liên quan.
    - `objectType`: Phân loại đối tượng.
- **Ví dụ**: Đính kèm ảnh cho sản phẩm có ID 123.
  `POST /api/files/upload?objectId=123&objectType=Product`

### Lấy danh sách File của một đối tượng
- **Endpoint**: `GET /api/files/object/{objectType}/{objectId}`
- **Ví dụ**: Lấy tất cả ảnh của sản phẩm ID 123.
  `GET /api/files/object/Product/123`

### Tải file về (Download)
- **Endpoint**: `GET /api/files/download/{id}`
- **Ghi chú**: Endpoint này không yêu cầu đăng nhập (AllowAnonymous) để có thể hiển thị ảnh trên web dễ dàng.

### Xóa File
- **Endpoint**: `DELETE /api/files/{id}`
- **Hành động**: Xóa cả metadata trong database và file vật lý trên đĩa.

## 4. Cấu hình kỹ thuật
- **Thư mục lưu trữ**: Mặc định lưu tại thư mục `uploads/` trong thư mục gốc của project API.
- **Dịch vụ**: `IFileService` được đăng ký trong DI container.
- **Quan hệ**: Metadata liên kết với `AppUser` (người tải lên). Nếu xóa user, thông tin người tải lên sẽ được set về `null` (OnDelete.SetNull).
