import os
import re
import subprocess
from harness.tools.sandbox import run_dotnet_command

def extract_compiler_errors(output: str) -> list[str]:
    """Trích xuất các lỗi biên dịch C# (CSxxxx) từ console output của dotnet build."""
    errors = []
    # Pattern: file_path(line,col): error CSxxxx: description
    pattern = r"([^(]+)\((\d+),(\d+)\):\s*error\s+(CS\d+):\s*([^\[\n\r]+)"
    matches = re.findall(pattern, output)
    for match in matches:
        file_path, line, col, code, message = match
        errors.append(f"❌ Lỗi {code} tại {os.path.basename(file_path.strip())}:{line}:{col} - {message.strip()}")
    return errors

def extract_test_errors(output: str) -> list[str]:
    """Trích xuất chi tiết lỗi test case (error message + stack trace) từ console output của dotnet test."""
    errors = []
    # Pattern: Failed TestName [duration]
    fail_pattern = r"^\s*Failed\s+(\S+)\s*\["
    # Find each failure block position
    matches = list(re.finditer(fail_pattern, output, re.MULTILINE))
    for i, m in enumerate(matches):
        test_name = m.group(1)
        block_start = m.end()
        # Next failure or end of string
        block_end = matches[i + 1].start() if i + 1 < len(matches) else len(output)
        block = output[block_start:block_end]

        # Extract Error Message
        err_msg = ""
        err_match = re.search(r"Error Message:\s*\n\s*(.+?)(?=\n\s*Stack Trace:)", block, re.DOTALL)
        if err_match:
            err_msg = err_match.group(1).strip()

        # Extract Stack Trace (first meaningful frames)
        stack = ""
        stack_match = re.search(r"Stack Trace:\s*\n(.+)", block, re.DOTALL)
        if stack_match:
            lines = stack_match.group(1).strip().splitlines()
            short_lines = []
            for line in lines:
                line = line.strip()
                line = re.sub(r'[a-zA-Z]:\\(?:[^\\]+\\)+([^\\]+\.cs)', r'.../\1', line)
                short_lines.append(line)
                if len(short_lines) >= 5:
                    short_lines.append("    ... (stack trace truncated)")
                    break
            stack = "\n".join(short_lines)

        # Classify error type and extract source file from stack trace
        error_class = classify_test_error(err_msg, stack)

        entry = f"⚠️  {test_name}"
        if err_msg:
            entry += f"\n   📄 {err_msg}"
        if error_class:
            entry += f"\n   🏷️  {error_class}"
        if stack:
            entry += f"\n   🔍 {stack}"
        errors.append(entry)
    return errors

def classify_test_error(err_msg: str, stack: str) -> str:
    """Phân loại lỗi test để agent biết hướng sửa mà không cần đọc stack trace."""
    if not err_msg:
        return ""
    # Lỗi DB
    if "FOREIGN KEY" in err_msg or "foreign key" in err_msg:
        return "DB: FOREIGN KEY constraint — seed data thiếu hoặc UserId không tồn tại"
    if "NOT NULL" in err_msg or "not null" in err_msg:
        return "DB: NOT NULL constraint — thiếu required field khi seed data"
    if "DbUpdateException" in err_msg or "SqliteException" in err_msg:
        m = re.search(r"SQLite Error \d+: (.+)", err_msg)
        return f"DB: SQLite — {m.group(1) if m else err_msg[:80]}"
    # Lỗi HTTP
    if "404 (Not Found)" in err_msg or "responded 404" in err_msg:
        return "HTTP 404: Sai route — thiếu api version prefix (v1/) hoặc sai URL"
    if "401 (Unauthorized)" in err_msg:
        return "HTTP 401: Auth fail — sai token, credential, hoặc user chưa register"
    if "403 (Forbidden)" in err_msg:
        return "HTTP 403: Role fail — user không có quyền Admin"
    if "400 (Bad Request)" in err_msg:
        return "HTTP 400: Request sai format — date format, missing field, validation"
    # Lỗi JSON
    if "JsonException" in err_msg or "GetProperty" in err_msg or "KeyNotFoundException" in err_msg:
        return "JSON: Sai cấu trúc response — ApiResponse wrapper chưa được unwrap (dùng .Data)"
    if "JsonSerializer" in err_msg or "Deserialize" in err_msg:
        return "JSON: Deserialize fail — response format không khớp model"
    # Lỗi assertion
    if "Assert." in err_msg or "Expected" in err_msg:
        return "ASSERT: Kỳ vọng sai — kiểm tra expected value hoặc seed data"
    # Lỗi auth/login
    if "Invalid credentials" in err_msg or "UnauthorizedAccessException" in err_msg:
        return "AUTH: Sai email/password — user chưa được seed hoặc register"
    # Lỗi System
    if "NullReferenceException" in err_msg:
        return "NULL: Object reference not set — dependency hoặc service chưa được mock/DI"
    if "InvalidOperationException" in err_msg:
        return "SYSTEM: Invalid operation — sequence, collection, hoặc state sai"
    return ""

def check_csharp_linting() -> str:
    """Kiểm tra linter (dotnet format) đối với các tệp C# đã bị thay đổi trong nhánh hiện tại."""
    try:
        # Lấy danh sách các file thay đổi từ git (Loại bỏ shell=True)
        result = subprocess.run(
            ["git", "status", "--porcelain"],
            shell=False,
            capture_output=True,
            encoding='utf-8',
            errors='replace'
        )
        if result.returncode != 0:
            return ""
            
        csharp_files = []
        for line in result.stdout.splitlines():
            # git status porcelain format: XY path
            parts = line.strip().split(maxsplit=1)
            if len(parts) < 2:
                continue
            path = parts[1].strip()
            if path.endswith(".cs") and os.path.exists(path):
                csharp_files.append(path)
                
        if not csharp_files:
            return ""
            
        print(f"🧹 [Harness Linter]: Đang kiểm tra định dạng (dotnet format) cho {len(csharp_files)} tệp C# thay đổi...")
        # Ghép các tệp cách nhau bằng dấu cách
        files_str = " ".join(f'"{f}"' for f in csharp_files)
        # Chạy dotnet format với bộ lọc chỉ quét các file thay đổi
        lint_code, lint_out = run_dotnet_command(f"dotnet format --include {files_str} --verify-no-changes")
        
        if lint_code != 0:
            # Lọc ra các dòng báo lỗi linter WHITESPACE hoặc STYLE
            lint_errors = []
            for line in lint_out.splitlines():
                if "error WHITESPACE" in line or "error STYLE" in line or "error analyzer" in line:
                    lint_errors.append(f"Format Violation: {line.strip()}")
            if lint_errors:
                return "\n🧹 CÁC LỖI ĐỊNH DẠNG CODE (LINT VIOLATIONS):\n" + "\n".join(lint_errors[:15]) + ("\n...[còn nhiều lỗi khác]..." if len(lint_errors) > 15 else "") + "\n"
        return ""
    except Exception as e:
        return f"\nLỗi khi chạy Harness Linter: {str(e)}\n"
