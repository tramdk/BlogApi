import os

def generate_directory_tree(root_dir: str) -> str:
    """Tự động quét cấu trúc thư mục dự án và tạo sơ đồ dạng cây để Agent hiểu kiến trúc và tránh sai đường dẫn."""
    exclude_dirs = {
        ".git", ".vs", "bin", "obj", ".claude", "uploads", 
        "Logs", "node_modules", ".github", "telemetry", "Properties"
    }
    
    lines = []
    
    def walk(directory, prefix="", depth=0):
        if depth > 4:
            return
        try:
            entries = sorted(os.listdir(directory))
        except Exception:
            return
            
        dirs = []
        files = []
        for entry in entries:
            if entry in exclude_dirs:
                continue
            path = os.path.join(directory, entry)
            if os.path.isdir(path):
                dirs.append(entry)
            else:
                if entry.endswith(('.exe', '.dll', '.pdb', '.user', '.suo')):
                    continue
                files.append(entry)
                
        all_entries = dirs + files
        for i, entry in enumerate(all_entries):
            is_last = (i == len(all_entries) - 1)
            connector = "└── " if is_last else "├── "
            path = os.path.join(directory, entry)
            
            if os.path.isdir(path):
                lines.append(f"{prefix}{connector}{entry}/")
                new_prefix = prefix + ("    " if is_last else "│   ")
                walk(path, new_prefix, depth + 1)
            else:
                lines.append(f"{prefix}{connector}{entry}")
                
    walk(root_dir)
    return "\n".join(lines)

def build_cache_contents(root_dir: str, policy_content: str, lessons_content: str, skills_contents: list = None) -> list[str]:
    """Tạo nội dung tĩnh lớn (mã nguồn, cây thư mục, coding policy) để kích hoạt Gemini Context Cache (>32k tokens)."""
    contents = []
    
    # 1. Coding Policy
    if policy_content:
        contents.append(f"--- CODING POLICY (BẮT BUỘC TUÂN THỦ) ---\n{policy_content}")
        
    # 1b. Skills & Guides
    if skills_contents:
        for s_file, s_content in skills_contents:
            title = os.path.basename(s_file).replace(".md", "").upper().replace("_", " ")
            contents.append(f"--- {title} REFERATIVE GUIDELINES ({s_file}) ---\n{s_content}")
    else:
        # Fallback cho DDD Guide nếu không truyền skills_contents
        ddd_guide_path = os.path.join(root_dir, "docs", "guides", "DDD_GUIDE.md")
        if os.path.exists(ddd_guide_path):
            try:
                with open(ddd_guide_path, "r", encoding="utf-8") as df:
                    contents.append(f"--- DDD ARCHITECTURE & DESIGN GUIDELINES ---\n{df.read()}")
            except Exception:
                pass
        
    # 1c. Harness Lessons Learned
    if lessons_content:
        contents.append(f"--- BÀI HỌC KINH NGHIỆM ĐÃ TỰ ĐÚC KẾT (BẮT BUỘC TRÁNH LỖI SAU) ---\n{lessons_content}")
        
    # 2. Cây thư mục dự án
    dir_tree = generate_directory_tree(root_dir)
    contents.append(f"--- CẤU TRÚC THƯ MỤC DỰ ÁN ---\n{dir_tree}")
    
    # 3. Quét và đọc các tệp tin mã nguồn chính trong Domain, Application, Infrastructure, Controllers
    core_files_content = []
    exclude_dirs = {
        ".git", ".vs", "bin", "obj", ".claude", "uploads", 
        "Logs", "node_modules", ".github", "telemetry", "Properties",
        "Tests", "FloraCore.Tests"
    }
    
    for root, dirs, files in os.walk(root_dir):
        dirs[:] = [d for d in dirs if d not in exclude_dirs]
        for file in files:
            if file.endswith(".cs") and not file.lower().endswith("test.cs") and not file.lower().endswith("tests.cs"):
                file_path = os.path.join(root, file)
                rel_path = os.path.relpath(file_path, root_dir)
                try:
                    with open(file_path, 'r', encoding='utf-8') as f:
                        core_files_content.append(f"FILE: {rel_path}\n{f.read()}")
                except Exception:
                    pass
                    
    if core_files_content:
        contents.append("--- MÃ NGUỒN CỐT LÕI DỰ ÁN ---\n" + "\n\n".join(core_files_content))
        
    # 4. Tính toán số lượng ký tự để đảm bảo vượt ngưỡng 32,768 tokens (khoảng 130,000 ký tự)
    total_chars = sum(len(c) for c in contents)
    target_chars = 135000  # Đảm bảo chắc chắn vượt 32,768 tokens
    if total_chars < target_chars:
        padding_needed = target_chars - total_chars
        smart_guides = [
            "// ===========================================================================",
            "// C# 12+ / .NET 9 ENTERPRISE CODING GUIDELINES & BEST PRACTICES",
            "// ===========================================================================",
            "// 1. PRIMARY CONSTRUCTORS (C# 12+):",
            "//    - Bắt buộc dùng Primary Constructor cho tất cả dependencies injection.",
            "//    - Luôn viết ThrowIfNull check tại vị trí khởi tạo thuộc tính readonly private.",
            "//    - Ví dụ: public class ProductService(IProductRepository repository) {",
            "//          private readonly IProductRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));",
            "//      }",
            "// 2. CLEAN ARCHITECTURE PURITY:",
            "//    - Domain layer không có bất kỳ dependency nào khác ngoài System.",
            "//    - Application layer chỉ chứa logic nghiệp vụ và interfaces, không phụ thuộc Infrastructure.",
            "//    - Infrastructure layer chứa EF Core DbContext, Repositories implementations.",
            "// 3. CQRS PATTERN WITH MEDIATR:",
            "//    - Đảm bảo Commands và Queries là immutable records.",
            "//    - Command handler độc lập, viết chung file với Command definition.",
            "// 4. RESOURCE MANAGER & LOCALIZATION:",
            "//    - Không hardcode chuỗi thông báo lỗi. Sử dụng ResourceManager để đọc từ file .resx.",
            "// 5. ASYNCHRONOUS PROGRAMMING:",
            "//    - Luôn dùng async/await cho I/O tasks. Luôn truyền CancellationToken.",
            "//    - Không bao giờ dùng .Result hoặc .Wait() để tránh deadlock.",
            "// 6. OUTBOX PATTERN FOR RELIABILITY:",
            "//    - Ghi nhận OutboxMessage trong cùng một db transaction với thực thể chính.",
            "//    - Background processor sẽ xử lý OutboxMessage bất đồng bộ đáng tin cậy.",
            "// 7. REQUEST IDEMPOTENCY & INBOX PATTERN:",
            "//    - Sử dụng IdempotencyKey qua IDistributedCache để ngăn chặn trùng lặp API requests.",
            "//    - Sử dụng InboxMessage trong event consumer để ngăn chặn trùng lặp event processing.",
            "// ==========================================================================="
        ]
        padding_str = "\n" + "\n".join(smart_guides) + "\n"
        while total_chars + len(padding_str) < target_chars:
            padding_str += "\n" + "\n".join(smart_guides) + "\n"
        
        # Cắt bớt phần dư thừa để khớp chính xác target_chars
        if total_chars + len(padding_str) > target_chars:
            excess = (total_chars + len(padding_str)) - target_chars
            padding_str = padding_str[:-excess] if excess < len(padding_str) else ""
        contents.append(padding_str)
        
    return contents
