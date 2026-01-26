# 🚀 C# Advanced Tips & Tricks for Professional Developers

Tài liệu này đi sâu vào **Extension Methods** và các kỹ thuật C# hiện đại (Modern C#) giúp bạn viết code sạch, ngắn gọn và mạnh mẽ hơn.

---

## 🏗️ Phần 1: Extension Methods - "Vũ khí" của Clean Code

Extension Methods cho phép bạn "cấy ghép" thêm phương thức vào các class có sẵn (kể cả class của .NET như `string`, `int`, `List<T>`) mà không cần tạo class kế thừa hay chỉnh sửa code gốc.

### 1.1. Cú pháp chuẩn
```csharp
public static class StringExtensions // 1. Phải là static class
{
    // 2. Phải là static method
    // 3. Tham số đầu tiên dùng 'this'
    public static bool IsValidEmail(this string email)
    {
        return email.Contains("@") && email.Contains(".");
    }
}
```

### 1.2. Cách dùng (Usage)
```csharp
string myEmail = "test@example.com";

// Cách cũ (Gọi Static Method)
bool isValid1 = StringExtensions.IsValidEmail(myEmail);

// Cách "Pro" (Extension Method)
bool isValid2 = myEmail.IsValidEmail(); // Nhìn như phương thức có sẵn của string!
```

### 1.3. Ví dụ thực tế: Generic Extension
Bạn muốn mọi object đều có thể chuyển thành JSON? Đừng viết Helper class, hãy viết Extension!

```csharp
public static class ObjectExtensions
{
    public static string ToJson<T>(this T obj)
    {
        return System.Text.Json.JsonSerializer.Serialize(obj);
    }
}

// Sử dụng
var user = new { Name = "T", Age = 25 };
string json = user.ToJson(); // Quá tiện!
```

---

## 🔥 Phần 2: Modern C# Tricks (C# 9, 10, 11, 12)

### 2.1. Pattern Matching & Switch Expression
Quên cấu trúc `switch-case` dài dòng ngày xưa đi. Hãy xem sức mạnh của C# hiện đại.

**Bài toán:** Tính giá vé dựa trên loại khách hàng.

**Cách cũ:**
```csharp
public decimal GetTicketPrice(Visitor visitor)
{
    if (visitor == null) throw new ArgumentNullException();
    if (visitor.Age < 12) return 5.0m;
    if (visitor.Age > 60) return 7.0m;
    if (visitor.Description.StartsWith("VIP")) return 20.0m;
    return 10.0m;
}
```

**Cách Modern C# (Property Pattern & Relational Pattern):**
```csharp
public decimal GetTicketPrice(Visitor visitor) => visitor switch
{
    null => throw new ArgumentNullException(nameof(visitor)),
    { Age: < 12 } => 5.0m,                    // Trẻ em
    { Age: > 60 } => 7.0m,                    // Người già
    { Description: "VIP" } => 20.0m,          // Khách VIP (đúng chuỗi)
    { Description: var d } when d.StartsWith("V") => 15.0m, // Logic phức tạp hơn
    _ => 10.0m                                // Mặc định (Discard pattern)
};
```

### 2.2. Records (Immutable Data Types)
Bạn hay dùng `class` DTO (Data Transfer Object) chỉ để chứa dữ liệu? Hãy dùng `record`.

**Class truyền thống:**
```csharp
public class UserDto
{
    public string Name { get; set; }
    public int Age { get; set; }
    
    // Phải tự viết Equals, GetHashCode nếu muốn so sánh giá trị
}
```

**Record (Ngắn gọn, So sánh theo giá trị, Bất biến):**
```csharp
public record UserDto(string Name, int Age); 
// Xong! Tự động có Constructor, Properties (init-only), Equals, ToString() xịn.
```

Trường hợp sửa dữ liệu trên record (Bất biến nhưng linh hoạt với `with`):
```csharp
var user1 = new UserDto("T", 25);
var user2 = user1 with { Age = 26 }; // Tạo user mới copy từ user1, chỉ thay đổi Age
```

### 2.3. Null Coalescing Assignment (`??=`)
Gán giá trị nếu biến đang null.

**Cách cũ:**
```csharp
if (myList == null)
{
    myList = new List<int>();
}
```

**Cách mới:**
```csharp
myList ??= new List<int>(); // Nếu myList null thì new, ko thì thôi.
```

### 2.4. Khai báo Using ngắn gọn (Using Declaration)
Giảm bớt ngoặc nhọn `{}` trôi nổi.

**Cách cũ:**
```csharp
using (var stream = new FileStream("file.txt", FileMode.Open))
{
    // Làm gì đó...
} // stream.Dispose() gọi ở đây
```

**Cách mới:**
```csharp
using var stream = new FileStream("file.txt", FileMode.Open);
// Làm gì đó...
// stream.Dispose() tự động gọi khi hết hàm (out of scope)
```

### 2.5. Primary Constructors (C# 12)
Giảm bớt code lặp đi lặp lại khi Inject Dependency.

**Cách cũ:**
```csharp
public class UserService
{
    private readonly IRepository _repo;
    public UserService(IRepository repo)
    {
        _repo = repo;
    }
}
```

**Cách mới (Primary Constructor):**
```csharp
public class UserService(IRepository _repo) // Inject thẳng vào tên class
{
    public void DoWork()
    {
        _repo.Save(); // Dùng luôn tham số
    }
}
```

---

## ⚡ Phần 3: LINQ Ninja Tricks

### 3.1. `Chunk()` - Chia nhỏ list
Bạn muốn xử lý 1000 items, nhưng mỗi lần chỉ gửi 100 items (Batch processing)?

```csharp
var numbers = Enumerable.Range(1, 1000);
var batches = numbers.Chunk(100); // Trả về List<int[]> mỗi mảng 100 phần tử

foreach (var batch in batches)
{
    ProcessBatch(batch);
}
```

### 3.2. `TryGetNonEnumeratedCount()`
Đếm phần tử mà không muốn kích hoạt Query DB hay duyệt In-Memory List?

```csharp
IEnumerable<int> data = GetData();

// Kiểm tra xem đếm nhanh được không (ví dụ data là List hoặc Array)
if (data.TryGetNonEnumeratedCount(out int count))
{
    Console.WriteLine($"Count is known fast: {count}");
}
else
{
    Console.WriteLine("Need to iterate to count...");
}
```

### 3.3. `Index` và `Range` (`^` và `..`)
Lấy phần tử từ cuối mảng siêu dễ.

```csharp
var arr = new[] { 1, 2, 3, 4, 5 };

var lastItem = arr[^1];      // 5 (Phần tử cuối cùng)
var subArray = arr[1..^1];   // { 2, 3, 4 } (Từ index 1 đến sát cuối)
```

---

## 💡 Phần 4: Dependency Injection (DI) Tricks

### 4.1. Keyed Services (C# 12 / .NET 8)
Bạn có 2 implementation của interface `INotificationService` (Email và SMS), làm sao chọn?

**Đăng ký:**
```csharp
builder.Services.AddKeyedScoped<INotificationService, EmailService>("email");
builder.Services.AddKeyedScoped<INotificationService, SmsService>("sms");
```

**Sử dụng (Inject):**
```csharp
public class MyController([FromKeyedServices("sms")] INotificationService smsService)
{
    // smsService ở đây là SmsService
}
```

---

## 🛡️ Best Practices Checklist

1.  **Async All The Way:** Đừng bao giờ dùng `.Result` hoặc `.Wait()` trong code async. Hãy dùng `await`. Nếu không sẽ bị Deadlock.
2.  **String Interpolation:** Dùng `$"Hello {name}"` thay vì `String.Format`.
3.  **Global Usings:** Tạo file `GlobalUsings.cs` để gom các using phổ biến (`System`, `System.Linq`, `Microsoft.EntityFrameworkCore`...) để File code gọn hơn.
