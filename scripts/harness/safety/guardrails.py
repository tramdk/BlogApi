import os
import re
import ast

def is_path_safe(file_path: str, root_dir: str) -> bool:
    """Kiểm tra xem đường dẫn file có nằm hoàn toàn trong thư mục gốc dự án hay không (tránh Path Traversal)."""
    try:
        abs_target = os.path.abspath(file_path)
        abs_root = os.path.abspath(root_dir)
        return os.path.commonpath([abs_root, abs_target]) == abs_root
    except Exception:
        return False

def strip_wrapping_quotes(s: str) -> str:
    """Loại bỏ các ký tự bao bọc chuỗi như nháy đơn, nháy kép, triple quotes, hoặc prefix verbatim (@, $, r, f)."""
    s = s.strip()
    
    # Loại bỏ prefix của C# hoặc Python nếu có (ví dụ: @", $", r", f")
    prefix_match = re.match(r'^([@$rfRF]+)?(["\'])', s)
    if prefix_match:
        prefix = prefix_match.group(0)  # Ví dụ: @" hoặc "
        quote_char = prefix_match.group(2)
        
        # Kiểm tra triple quotes trước
        triple_quote = quote_char * 3
        if s.startswith(prefix_match.group(1) + triple_quote if prefix_match.group(1) else triple_quote) and s.endswith(triple_quote):
            start_len = (len(prefix_match.group(1)) if prefix_match.group(1) else 0) + 3
            return s[start_len:-3]
            
        if s.endswith(quote_char):
            start_len = len(prefix)
            return s[start_len:-1]
            
    return s

def safe_parse_action_arguments(action_args: str) -> tuple:
    """Parse đối số của công cụ một cách an toàn bằng Python ast module để tránh lỗi dấu phẩy/ngoặc lồng nhau."""
    action_args = action_args.strip()
    try:
        parsed = ast.literal_eval(f"({action_args})")
        if isinstance(parsed, tuple):
            return parsed
        return (parsed,)
    except Exception:
        # Fallback về split theo dấu phẩy đầu tiên nếu ast parse thất bại
        parts = action_args.split(",", 1)
        p1 = strip_wrapping_quotes(parts[0])
        p2 = parts[1].strip() if len(parts) > 1 else ""
        if p2.endswith(')'):
            p2 = p2[:-1].strip()
        p2 = strip_wrapping_quotes(p2)
        
        # Decode các ký tự escape nếu dùng phương án fallback
        try:
            p2 = p2.encode('utf-8').decode('unicode_escape')
        except Exception:
            p2 = p2.replace("\\n", "\n").replace("\\t", "\t").replace('\\"', '"').replace("\\'", "'").replace("\\\\", "\\")
            
        return (p1, p2) if len(parts) > 1 else (p1,)

def format_observation(status: str, summary: str, details: str, next_actions: list[str] = None, artifacts: list[str] = None) -> str:
    """Tiêu chuẩn hóa định dạng quan sát (Observation) trả về cho AI Agent."""
    obs = "=== OBSERVATION ===\n"
    obs += f"STATUS: {status}\n"
    obs += f"SUMMARY: {summary}\n"
    if artifacts:
        obs += f"ARTIFACTS: {', '.join(artifacts)}\n"
    if next_actions:
        obs += "SUGGESTED NEXT ACTIONS:\n"
        for act in next_actions:
            obs += f"  - {act}\n"
    obs += f"DETAILS:\n{details}\n"
    obs += "==================="
    return obs
