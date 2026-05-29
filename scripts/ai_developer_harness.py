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

    skip_enricher_flag = False
    if "--skip-enricher" in args:
        skip_enricher_flag = True
        args.remove("--skip-enricher")
        
    # Xử lý tham số kích hoạt Skill
    skill_name = None
    if "--skill" in args:
        try:
            idx = args.index("--skill")
            if idx + 1 < len(args) and not args[idx + 1].startswith("-"):
                skill_name = args[idx + 1]
                args.pop(idx + 1)
            args.pop(idx)
        except ValueError:
            pass

        # Quét các skill có sẵn trong thư mục skills/
        root_dir = os.path.dirname(script_dir)
        skills_path = os.path.join(root_dir, "skills")
        available_skills = []
        if os.path.exists(skills_path):
            for item in sorted(os.listdir(skills_path)):
                item_path = os.path.join(skills_path, item)
                if os.path.isdir(item_path) and os.path.exists(os.path.join(item_path, "SKILL.md")):
                    available_skills.append(item)

        # Nếu không truyền tên skill hoặc truyền sai tên, hiển thị menu lựa chọn
        if not skill_name or skill_name not in available_skills:
            if not available_skills:
                print("❌ [Lỗi]: Không tìm thấy kỹ năng (skill) nào trong thư mục 'skills/'.")
                sys.exit(1)
            
            if skill_name:
                print(f"⚠️  Không tìm thấy kỹ năng: '{skill_name}'")
            
            print("\n📋 CÁC KỸ NĂNG (SKILLS) KHẢ DỤNG TRONG HỆ THỐNG:")
            for i, s in enumerate(available_skills, 1):
                print(f"  [{i}] {s}")
            
            try:
                choice = input(f"\n👉 Nhập số thứ tự (1-{len(available_skills)}) để chọn skill: ").strip()
                if choice.isdigit() and 1 <= int(choice) <= len(available_skills):
                    skill_name = available_skills[int(choice) - 1]
                else:
                    print("❌ Lựa chọn không hợp lệ. Đang dừng.")
                    sys.exit(1)
            except (KeyboardInterrupt, EOFError):
                print("\nĐã hủy bỏ.")
                sys.exit(0)

            print(f"🚀 Đã kích hoạt kỹ năng: {skill_name}")

        task = f"Hãy đóng vai trò chuyên gia phân tích hiệu năng và quét toàn bộ mã nguồn của dự án để phát hiện, tối ưu hóa các phần liên quan dựa theo hướng dẫn cụ thể trong skill: {skill_name}."
    elif len(args) > 0:
        task = args[0]
        
    harness = AIDeveloperHarness(auto_approve=auto_approve)
    if mock_mode_flag:
        harness.mock_mode = True
        harness.llm_router.mock_mode = True
    harness.execute_pipeline(task, skip_enricher=skip_enricher_flag)
