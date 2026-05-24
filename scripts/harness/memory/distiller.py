import os

def distill_and_persist_lessons(task_description: str, mock_mode: bool, log_file: str, evals_dir: str, lessons_path: str, call_llm_func):
    """Tự động phân tích lịch sử log chạy và báo cáo đánh giá để đúc kết bài học kinh nghiệm vào harness_lessons.md."""
    print("\n🧠 [Harness Distiller]: Đang phân tích log và tự động đúc kết bài học kinh nghiệm cho các lần chạy sau...")
    
    # 1. Đọc nội dung cũ nếu có
    existing_lessons = ""
    if os.path.exists(lessons_path):
        try:
            with open(lessons_path, "r", encoding="utf-8") as lf:
                existing_lessons = lf.read()
        except Exception:
            existing_lessons = ""
            
    # 2. Đọc log file và báo cáo evaluation mới nhất
    log_content = ""
    if os.path.exists(log_file):
        try:
            with open(log_file, "r", encoding="utf-8") as f:
                log_content = f.read()
        except Exception:
            log_content = ""
            
    report_path = os.path.join(evals_dir, "evaluation_report.md")
    report_content = ""
    if os.path.exists(report_path):
        try:
            with open(report_path, "r", encoding="utf-8") as r:
                report_content = r.read()
        except Exception:
            report_content = ""
            
    # Giới hạn dung lượng text gửi lên LLM để tránh quá tải token
    log_snippet = log_content[:15000] + ("\n...[TRUNCATED]..." if len(log_content) > 15000 else "")
    report_snippet = report_content[:8000] + ("\n...[TRUNCATED]..." if len(report_content) > 8000 else "")
    
    # 3. Prompt gửi cho LLM
    distiller_system = (
        "Bạn là một AI Developer Coach chuyên nghiệp cho dự án .NET 9 FloraCore.\n"
        "Nhiệm vụ của bạn là phân tích nhật ký thực thi (log) và báo cáo phản biện (evaluation report) "
        "để rút ra bài học kinh nghiệm sâu sắc giúp AI Developer Agent tránh lỗi và lập trình thông minh hơn ở các lần chạy sau.\n"
        "Bạn BẮT BUỘC phải viết bài học kinh nghiệm bằng tiếng Việt, có tiêu đề chính là `# BÀI HỌC KINH NGHIỆM ĐÃ TỰ ĐÚC KẾT`.\n"
        "Hãy tập trung vào:\n"
        "1. Lỗi biên dịch C# (mã lỗi CSxxxx) phát sinh và cách khắc phục.\n"
        "2. Lỗi thiết kế Clean Architecture, CQRS, MediatR không tuân thủ mẫu chuẩn.\n"
        "3. Lỗi kiểm thử (Tests) thất bại do sai constructor, namespace hoặc thiếu mock dữ liệu.\n"
        "4. Các lưu ý lập trình C# 12+ (Primary Constructors, ThrowIfNull, File-Scoped Namespaces) đã được tối ưu.\n\n"
        "Hãy gộp (merge) các kinh nghiệm mới này vào danh sách kinh nghiệm cũ (nếu có) một cách logic, "
        "phân loại theo nhóm rõ ràng, tránh trùng lặp thông tin. Hãy trả về toàn bộ nội dung file Markdown mới hoàn chỉnh."
    )
    
    distill_prompt = (
        f"Yêu cầu nhiệm vụ đã thực hiện: {task_description}\n\n"
        f"--- CÁC BÀI HỌC CŨ ĐÃ LƯU TRỮ (nếu có) ---\n"
        f"{existing_lessons if existing_lessons else '[Chưa có bài học cũ nào được lưu trữ]'}\n\n"
        f"--- BÁO CÁO KIỂM DUYỆT EVALUATOR ---\n"
        f"{report_snippet if report_snippet else '[Chưa có báo cáo kiểm duyệt]'}\n\n"
        f"--- NHẬT KÝ CHẠY LOG FILE (Tóm tắt) ---\n"
        f"```text\n{log_snippet}\n```\n\n"
        "Hãy phân tích và viết nội dung file Markdown bài học đúc kết tích lũy (bao gồm cả kinh nghiệm cũ đã gộp thông minh)."
    )
    
    # Gọi LLM hoặc chạy Mock
    new_lessons = ""
    if mock_mode:
        new_lessons = (
            "# BÀI HỌC KINH NGHIỆM ĐÃ TỰ ĐÚC KẾT\n\n"
            "## ⚙️ 1. Quản lý Lớp Domain & Application\n"
            "- **Bảo vệ tính thuần khiết của Domain:** Domain Layer tuyệt đối không phụ thuộc vào Infrastructure hoặc Application. "
            "Định nghĩa Entities phải kế thừa từ `BaseEntity`.\n\n"
            "## 🛠️ 2. Ép buộc Quy tắc C# 12+\n"
            "- **Primary Constructor Check:** Khi dùng Primary Constructor (C# 12+), nếu có tham số, phải kiểm tra null "
            "bằng `ArgumentNullException.ThrowIfNull` trước khi sử dụng.\n\n"
            "## 🧪 3. Viết Test Cases\n"
            "- **Constructor Signature Alignment:** Trước khi viết unit test, hãy đọc kỹ cấu trúc của lớp production thực tế để "
            "khai báo tham số mock khớp 100% với signature thực tế."
        )
    else:
        try:
            new_lessons = call_llm_func(distill_prompt, distiller_system, role="Distiller")
        except Exception as e:
            print(f"⚠️ [Harness Distiller Error]: Lỗi gọi LLM Distiller: {e}")
            return
            
    # Làm sạch Markdown block nếu LLM bọc trong ```markdown
    cleaned_lessons = new_lessons.strip()
    if cleaned_lessons.startswith("```markdown"):
        cleaned_lessons = cleaned_lessons[11:]
    elif cleaned_lessons.startswith("```"):
        cleaned_lessons = cleaned_lessons[3:]
    if cleaned_lessons.endswith("```"):
        cleaned_lessons = cleaned_lessons[:-3]
    cleaned_lessons = cleaned_lessons.strip()
    
    # 4. Ghi đè lên đĩa cứng
    try:
        with open(lessons_path, "w", encoding="utf-8") as lf:
            lf.write(cleaned_lessons)
        print(f"💾 [Harness Distiller Success]: Đã tự động đúc kết kinh nghiệm và cập nhật vào: {os.path.relpath(lessons_path)}")
    except Exception as e:
        print(f"⚠️ [Harness Distiller Error]: Không thể lưu file bài học kinh nghiệm: {e}")
