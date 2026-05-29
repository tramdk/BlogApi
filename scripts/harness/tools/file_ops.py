import os

def read_source_file(file_path: str, start_line: int = None, end_line: int = None) -> str:
    """Đọc nội dung một file nguồn trong dự án (hỗ trợ phân trang)."""
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            lines = f.readlines()
            
            if start_line is not None and end_line is not None:
                # 1-indexed for user input
                start = max(0, start_line - 1)
                end = min(len(lines), end_line)
                lines = lines[start:end]
                
            return "".join(lines)
    except Exception as e:
        return f"Lỗi đọc file: {str(e)}"

def write_source_file(file_path: str, content: str) -> str:
    """Ghi nội dung mới vào một file nguồn."""
    try:
        # Resolve về absolute path để tránh CWD-dependent
        abs_path = os.path.abspath(file_path)
        dir_name = os.path.dirname(abs_path)
        if dir_name:
            os.makedirs(dir_name, exist_ok=True)
            
        # Xử lý thuộc tính Read-Only trên Windows/Linux trước khi ghi
        if os.path.exists(abs_path):
            try:
                import stat
                current_mode = os.stat(abs_path).st_mode
                # Nếu file bị đánh dấu read-only, xóa cờ này đi
                if not (current_mode & stat.S_IWRITE):
                    os.chmod(abs_path, current_mode | stat.S_IWRITE)
            except Exception as stat_err:
                # Bỏ qua nếu không có quyền set chmod, thử ghi tiếp
                pass
                
        with open(abs_path, 'w', encoding='utf-8') as f:
            f.write(content)
        if not os.path.exists(abs_path):
            return f"Lỗi ghi file: {abs_path} không tồn tại sau khi ghi (có thể bị redirect/block)."
        return "Ghi file thành công."
    except PermissionError as pe:
        return f"Lỗi ghi file: Truy cập bị từ chối (Permission Denied). File có thể đang bị khóa (lock) bởi tiến trình khác (như dotnet build, IDE, hoặc Git) hoặc thuộc tính bảo mật của hệ điều hành. Chi tiết: {str(pe)}"
    except Exception as e:
        return f"Lỗi ghi file: {str(e)}"

