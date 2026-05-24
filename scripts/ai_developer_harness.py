import os
import sys

# Đảm bảo Python có thể import từ thư mục scripts hiện tại
script_dir = os.path.dirname(os.path.abspath(__file__))
if script_dir not in sys.path:
    sys.path.insert(0, script_dir)

# Khắc phục lỗi mã hóa Unicode trên terminal Windows
if sys.platform.startswith('win'):
    import io
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
    sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8', errors='replace')

try:
    from harness import AIDeveloperHarness
except ImportError as e:
    print(f"Lỗi: Không thể import package harness: {e}")
    sys.exit(1)

if __name__ == "__main__":
    task = "Hãy kiểm tra xem dự án hiện tại build và chạy thử nghiệm tests thành công không."
    auto_approve = False
    
    # Phân tích các đối số dòng lệnh
    args = sys.argv[1:]
    if "--auto-approve" in args:
        auto_approve = True
        args.remove("--auto-approve")
        
    mock_mode_flag = False
    if "--mock" in args:
        mock_mode_flag = True
        args.remove("--mock")
        
    if len(args) > 0:
        task = args[0]
        
    harness = AIDeveloperHarness(auto_approve=auto_approve)
    if mock_mode_flag:
        harness.mock_mode = True
        harness.llm_router.mock_mode = True
    harness.execute_pipeline(task)
