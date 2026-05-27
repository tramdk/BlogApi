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
        # Tự động tạo thư mục cha nếu chưa có và path không rỗng
        dir_name = os.path.dirname(file_path)
        if dir_name:
            os.makedirs(dir_name, exist_ok=True)
        with open(file_path, 'w', encoding='utf-8') as f:
            f.write(content)
        return "Ghi file thành công."
    except Exception as e:
        return f"Lỗi ghi file: {str(e)}"
