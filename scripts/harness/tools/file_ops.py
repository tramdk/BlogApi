import os

def read_source_file(file_path: str) -> str:
    """Đọc nội dung một file nguồn trong dự án."""
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            return f.read()
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
