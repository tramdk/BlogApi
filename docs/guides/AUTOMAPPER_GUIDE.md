# 🗺️ AutoMapper Ultimate Guide for .NET Developers

**AutoMapper** là thư viện giúp "tiêu diệt" code chuyển đổi dữ liệu (mapping) nhàm chán giữa các object. Thay vì viết thủ công `A.Prop = B.Prop`, AutoMapper tự động làm việc này dựa trên quy ước tên (convention).

---

## 🛑 Vấn đề (The Pain)

Hãy tưởng tượng bạn có `User` (Entity) và `UserDto` (Data Transfer Object).

**Entity:**
```csharp
public class User {
    public Guid Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public DateTime CreatedAt { get; set; }
    // ... 20 properties khác
}
```

**DTO:**
```csharp
public class UserDto {
    public Guid Id { get; set; }
    public string FullName { get; set; } // Ghép từ First + Last
}
```

**Code thủ công (Manual Mapping):**
```csharp
// Controller
var user = await _repo.GetUser(id);
var dto = new UserDto {
    Id = user.Id,
    FullName = $"{user.FirstName} {user.LastName}",
    // ... Lặp lại 20 dòng nếu có 20 props giống nhau
};
```
❌ **Nhược điểm:** Code dài, dễ quên map field mới, khó bảo trì.

---

## ✅ Giải pháp AutoMapper (The Cure)

### 1. Cài đặt
```bash
dotnet add package AutoMapper
dotnet add package AutoMapper.Extensions.Microsoft.DependencyInjection
```

### 2. Định nghĩa Profile (Luật chơi)
Tạo `MappingProfile.cs`:
```csharp
public class MappingProfile : Profile {
    public MappingProfile() {
        // Map 2 chiều nếu cần (ReverseMap)
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"));
    }
}
```

### 3. Đăng ký & Sử dụng
```csharp
// Program.cs / ServiceCollectionExtensions
services.AddAutoMapper(typeof(Program)); 

// Controller
public class UserController : ControllerBase {
    private readonly IMapper _mapper;
    
    // Inject Mapper
    public UserController(IMapper mapper) => _mapper = mapper;

    public IActionResult Get(Guid id) {
        var user = _repo.GetUser(id);
        
        // ✨ Magic happens here!
        var dto = _mapper.Map<UserDto>(user); 
        
        return Ok(dto);
    }
}
```

---

## 🚀 Kỹ thuật Nâng cao (Ninja Skills)

### 1. Flattening (Làm phẳng)
Nếu Entity có object con, nhưng DTO muốn property "phẳng".
**Entity:** `Order.Customer.Name`
**DTO:** `OrderDto.CustomerName`
=> AutoMapper tự động map `Customer.Name` vào `CustomerName`! Không cần config gì cả.

### 2. Projection (Hiệu năng cao với EF Core) ⚡
Đừng bao giờ query hết DB rồi mới map! Hãy map ngay trong câu SQL.

**Sai (Tốn RAM):**
```csharp
// Select * from Users (Load hết data thừa)
var users = await _dbContext.Users.ToListAsync();
// Map in memory
var dtos = _mapper.Map<List<UserDto>>(users);
```

**Đúng (Tối ưu SQL):**
```csharp
using AutoMapper.QueryableExtensions;

// Select Id, FirstName, LastName from Users (Chỉ lấy cột cần thiết)
var dtos = await _dbContext.Users
    .ProjectTo<UserDto>(_mapper.ConfigurationProvider)
    .ToListAsync();
```

### 3. Custom Value Resolver
Khi logic quá phức tạp (như tạo URL từ HttpContext).
```csharp
CreateMap<File, FileDto>()
    .ForMember(d => d.Url, opt => opt.MapFrom<FileUrlResolver>());
```
Xem ví dụ chi tiết trong `BlogApi.Application/Common/Mappings/MappingProfile.cs` của dự án này.

---

## 🛡️ Best Practices Checklist

1.  **Unit Test Mapping:** Luôn viết test để đảm bảo config đúng.
    ```csharp
    [Fact]
    public void AutoMapper_Configuration_IsValid() {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        config.AssertConfigurationIsValid(); // Throws exception nếu map sai/thiếu
    }
    ```
2.  **DTO for Input/Output:** Luôn dùng DTO cho API, không trả Entity trực tiếp.
3.  **ProjectTo:** Luôn dùng `ProjectTo` cho các query danh sách lớn (List/Get All).
