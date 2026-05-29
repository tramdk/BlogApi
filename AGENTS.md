# AGENTS.md — Rules rút ra từ failure history

## Rules

1. TRƯỚC KHI viết code, dùng `view_source` đọc file test tương ứng trong FloraCore.Tests/ để biết method signature, expected behavior.
2. Viết 1 file .cs production → BUILD NGAY (`dotnet build FloraCore.csproj`). KHÔNG viết nhiều file cùng lúc.
3. Nếu gặp lỗi CS1503/C1061, dùng `view_source` đọc interface/class gốc, copy đúng method signature.
4. KHÔNG gọi `git commit`. Chỉ dùng `git status`, `git diff`, `git add`.
5. Nếu `dotnet test --filter` không tìm thấy test nào, chạy không filter trước (`dotnet test`).
6. KHÔNG sửa file .csproj, .sln, Program.cs, startup.cs, appsettings.
7. TestWriter CHỈ được sửa file trong FloraCore.Tests/. KHÔNG đụng production code.
8. Nếu cùng lỗi compiler xuất hiện 3+ lần, ĐỔI CHIẾN THUẬT — đọc file test để biết signature đúng.
