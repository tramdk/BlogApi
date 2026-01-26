# Hướng dẫn triển khai và cách SignalR hoạt động trong dự án BlogApi

## 1. Tổng quan về SignalR trong dự án
SignalR được sử dụng để cung cấp khả năng giao tiếp thời gian thực (real-time) giữa Server và Client. Trong dự án này, chúng ta triển khai hai tính năng chính:
- **Chat**: Cho phép người dùng gửi tin nhắn trực tiếp cho nhau.
- **Push Notifications**: Gửi thông báo tức thời (ví dụ: bài viết mới, lượt thích, tin nhắn mới) đến người dùng.

## 2. Cấu trúc triển khai

### Hubs (`Infrastructure/Hubs`)
- `ChatHub`: Quản lý các kết nối liên quan đến chat.
- `NotificationHub`: Quản lý các kết nối liên quan đến thông báo.
- `UserIdProvider`: Một thành phần tùy chỉnh giúp SignalR xác định `UserId` từ JWT Token (sử dụng claim `sub`).

### Interfaces (`Application/Common/Interfaces`)
- `IChatClient`: Định nghĩa các phương thức mà Server có thể gọi ở phía Client (ví dụ: `ReceiveMessage`).
- `INotificationClient`: Định nghĩa các phương thức cho thông báo (ví dụ: `ReceiveNotification`).
- `INotificationService`: Interface cho phép các lớp khác trong hệ thống gửi thông báo mà không cần biết chi tiết về SignalR.

### Services (`Infrastructure/Services`)
- `NotificationService`: Triển khai gửi thông báo, vừa lưu vào Database để xem lại, vừa đẩy qua SignalR để hiển thị tức thời.

## 3. Cách SignalR hoạt động trong dự án

### Luồng kết nối (Connection Flow)
1. **Authentication**: Client phải gửi kèm JWT Token trong query string khi kết nối:
   `wss://domain/hubs/chat?access_token={token}`
2. **Authorization**: `Program.cs` đã được cấu hình để trích xuất token từ query string cho các yêu cầu bắt đầu bằng `/hubs`.
3. **User Identification**: `UserIdProvider` trích xuất `sub` từ token để SignalR biết kết nối này thuộc về người dùng nào. Điều này cho phép chúng ta gửi tin nhắn đến một người dùng cụ thể bằng `Clients.User(userId)`.

### Luồng Chat (Chat Flow)
1. User A gọi API `POST /api/chat/send` kèm theo `ReceiverId` và `Message`.
2. `SendMessageCommandHandler` xử lý:
   - Lưu tin nhắn vào bảng `ChatMessages`.
   - Sử dụng `IHubContext<ChatHub, IChatClient>` để gọi `ReceiveMessage` cho `ReceiverId`.
3. User B (nếu đang kết nối) sẽ nhận được sự kiện `ReceiveMessage` ngay lập tức.

### Luồng Thông báo (Notification Flow)
1. Một sự kiện xảy ra trong hệ thống (ví dụ: Admin gửi thông báo hệ thống).
2. Code gọi `_notificationService.SendNotificationToUser(...)`.
3. `NotificationService`:
   - Lưu thông báo vào bảng `Notifications` (để User xem lại sau này).
   - Đẩy thông báo qua `NotificationHub` tới Client của User đó.

## 4. Hướng dẫn tích hợp phía Client (JavaScript)

### Khởi tạo kết nối
```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/chat", {
        accessTokenFactory: () => "YOUR_JWT_TOKEN"
    })
    .build();

connection.on("ReceiveMessage", (senderId, message, sentAt) => {
    console.log(`Nhận tin nhắn từ ${senderId}: ${message}`);
});

await connection.start();
```

### Các Endpoint Hub
- Chat: `/hubs/chat`
- Notifications: `/hubs/notifications`

### Các phương thức Client cần lắng nghe
- **Chat**: `ReceiveMessage(senderId, message, sentAt)`
- **Notifications**: `ReceiveNotification(title, message, type, relatedId)`
