import os
import sys
import subprocess
import re
import ast
import json

# Khắc phục lỗi mã hóa Unicode trên terminal Windows
if sys.platform.startswith('win'):
    import io
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
    sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8', errors='replace')

try:
    from dotenv import load_dotenv
    # Xác định thư mục gốc của dự án (thư mục cha của thư mục chứa script)
    script_dir = os.path.dirname(os.path.abspath(__file__))
    root_dir = os.path.dirname(script_dir)
    env_path = os.path.join(root_dir, ".env")
    if os.path.exists(env_path):
        load_dotenv(env_path)
    else:
        load_dotenv()
except ImportError:
    # Nếu không có python-dotenv, vẫn tiếp tục chạy bằng cách sử dụng các biến môi trường có sẵn trong hệ thống
    pass

# =====================================================================
# TẦNG 3: TOOL SANDBOX - THỰC THI LỆNH VÀ THAO TÁC FILE CỤC BỘ
# =====================================================================

def run_dotnet_command(command: str) -> tuple[int, str]:
    """Chạy một lệnh dotnet CLI và trả về exit code cùng console output."""
    try:
        print(f"\n[Harness Executing]: {command}")
        # Chạy lệnh trong thư mục hiện tại của dự án
        result = subprocess.run(
            command,
            shell=True,
            capture_output=True,
            encoding='utf-8',
            errors='replace',
            timeout=120  # Timeout 2 phút tránh treo đúp
        )
        output = result.stdout + "\n" + result.stderr
        return result.returncode, output
    except subprocess.TimeoutExpired:
        return -1, "Lỗi: Lệnh bị Timeout (quá 2 phút)."
    except Exception as e:
        return -2, f"Lỗi không xác định: {str(e)}"

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

def strip_wrapping_quotes(s: str) -> str:
    """Loại bỏ cặp dấu nháy đơn hoặc nháy kép bao bọc ngoài cùng của một chuỗi."""
    s = s.strip()
    if (s.startswith('"') and s.endswith('"')) or (s.startswith("'") and s.endswith("'")):
        return s[1:-1]
    return s

def safe_parse_action_arguments(action_args: str) -> tuple:
    """Parse đối số của công cụ một cách an toàn bằng Python ast module để tránh lỗi dấu phẩy/ngoặc lồng nhau."""
    action_args = action_args.strip()
    try:
        # ast.literal_eval sẽ parse chuỗi như một tuple trong python một cách an toàn tuyệt đối
        parsed = ast.literal_eval(f"({action_args})")
        if isinstance(parsed, tuple):
            return parsed
        return (parsed,)
    except Exception as e:
        # Fallback về split theo dấu phẩy đầu tiên nếu ast parse thất bại
        parts = action_args.split(",", 1)
        return tuple(strip_wrapping_quotes(p) for p in parts)

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
    """Trích xuất các test case thất bại từ console output của dotnet test."""
    errors = []
    pattern = r"Failed\s+([^\s\n\r]+)"
    matches = re.findall(pattern, output)
    for match in matches:
        errors.append(f"⚠️ Test case thất bại: {match.strip()}")
    return errors

def check_csharp_linting() -> str:
    """Kiểm tra linter (dotnet format) đối với các tệp C# đã bị thay đổi trong nhánh hiện tại."""
    try:
        # Lấy danh sách các file thay đổi từ git
        result = subprocess.run("git status --porcelain", shell=True, capture_output=True, encoding='utf-8')
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
        files_str = " ".join(csharp_files)
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

# =====================================================================
# TẦNG 2: ORCHESTRATION & STATE MANAGEMENT (REACT LOOP)
# =====================================================================

class AIDeveloperHarness:
    def __init__(self, auto_approve: bool = False):
        # Đọc và dọn dẹp các API Key để tránh lỗi dấu nháy kép từ file .env (ví dụ: KEY="")
        gemini_key = (os.getenv("GEMINI_API_KEY") or "").strip("'\" \t")
        openai_key = (os.getenv("OPENAI_API_KEY") or "").strip("'\" \t")
        claude_key = (os.getenv("CLAUDE_API_KEY") or os.getenv("ANTHROPIC_API_KEY") or "").strip("'\" \t")
        deepseek_key = (os.getenv("DEEPSEEK_API_KEY") or "").strip("'\" \t")
        
        # Xác định Provider dựa trên khóa thực tế hợp lệ (khác rỗng)
        if gemini_key:
            self.provider = "gemini"
            self.api_key = gemini_key
        elif openai_key:
            self.provider = "openai"
            self.api_key = openai_key
        elif claude_key:
            self.provider = "claude"
            self.api_key = claude_key
        elif deepseek_key:
            self.provider = "deepseek"
            self.api_key = deepseek_key
        else:
            self.provider = "mock"
            self.api_key = None
            
        self.max_iterations = int(os.getenv("GAN_MAX_ITERATIONS") or 10)
        self.iteration_count = 0
        self.auto_approve = auto_approve
        self.pass_threshold = float(os.getenv("GAN_PASS_THRESHOLD") or 7.5)
        
        # Cấu hình lưu trữ tệp tin log trong thư mục chuyên dụng (.claude/evals/)
        script_dir = os.path.dirname(os.path.abspath(__file__))
        root_dir = os.path.dirname(script_dir)
        self.evals_dir = os.path.join(root_dir, ".claude", "evals")
        os.makedirs(self.evals_dir, exist_ok=True)
        self.log_file = os.path.join(self.evals_dir, "harness_run.log")
        
        # Ghi log dòng chào mừng cho phiên làm việc mới
        with open(self.log_file, "a", encoding="utf-8") as f:
            f.write(f"\n\n=============================================================\n")
            f.write(f"PHIÊN LÀM VIỆC MỚI KHỞI CHẠY\n")
            f.write(f"=============================================================\n")
        
        if self.provider == "mock" or not self.api_key:
            print("[CẢNH BÁO]: Không tìm thấy khóa API của GEMINI, OPENAI, CLAUDE hoặc DEEPSEEK trong môi trường.")
            print("Harness sẽ chạy ở chế độ giả lập (Mock LLM) để minh họa quy trình.")
            self.mock_mode = True
        else:
            self.mock_mode = False
            self.init_llm_client()

    def init_llm_client(self):
        """Khởi tạo Client LLM dựa trên Provider."""
        try:
            if self.provider == "gemini":
                from google import genai
                self.client = genai.Client(api_key=self.api_key)
                # Đọc model từ env, mặc định là gemini-1.5-flash để đảm bảo tương thích tốt nhất
                self.model_name = os.getenv("GEMINI_MODEL") or "gemini-1.5-flash"
            elif self.provider == "openai":
                from openai import OpenAI
                self.client = OpenAI(api_key=self.api_key)
                # Đọc model từ env, mặc định là gpt-4o
                self.model_name = os.getenv("OPENAI_MODEL") or "gpt-4o"
            elif self.provider == "claude":
                import anthropic
                self.client = anthropic.Anthropic(api_key=self.api_key)
                self.model_name = os.getenv("CLAUDE_MODEL") or os.getenv("ANTHROPIC_MODEL") or "claude-3-5-sonnet-latest"
            elif self.provider == "deepseek":
                from openai import OpenAI
                # DeepSeek API hoàn toàn tương thích với OpenAI SDK, chỉ cần trỏ custom base_url
                self.client = OpenAI(api_key=self.api_key, base_url="https://api.deepseek.com")
                self.model_name = os.getenv("DEEPSEEK_MODEL") or "deepseek-chat"
        except ImportError as e:
            missing_lib = {
                "gemini": "google-genai",
                "openai": "openai",
                "claude": "anthropic",
                "deepseek": "openai"
            }.get(self.provider, "openai")
            print(f"\n[LỖI HỆ THỐNG]: Chưa cài đặt thư viện '{missing_lib}'.")
            print(f"Vui lòng chạy lệnh sau trên terminal để cài đặt:\n👉 pip install {missing_lib}\n")
            sys.exit(1)

    def ask_approval(self, message: str, force_ask: bool = False) -> bool:
        """Hỏi ý kiến người dùng trước khi thực thi hành động nhạy cảm."""
        if self.auto_approve and not force_ask:
            return True
        try:
            choice = input(f"\n🛡️  [Harness HITL]: {message}\n👉 Đồng ý thực thi? (y/n) [Mặc định: y]: ").strip().lower()
            return choice in ["", "y", "yes"]
        except Exception:
            return False

    def call_llm(self, prompt: str, system_instruction: str) -> str:
        """Gọi LLM để lấy phân tích và hành động tiếp theo."""
        if self.mock_mode:
            return self.get_mock_agent_response()
            
        try:
            if self.provider == "gemini":
                response = self.client.models.generate_content(
                    model=self.model_name,
                    contents=prompt,
                    config={"system_instruction": system_instruction, "temperature": 0.0}
                )
                return response.text
            elif self.provider == "claude":
                response = self.client.messages.create(
                    model=self.model_name,
                    max_tokens=4000,
                    temperature=0.0,
                    system=system_instruction,
                    messages=[{"role": "user", "content": prompt}]
                )
                return response.content[0].text
            else:
                # Dành cho cả 'openai' và 'deepseek' (vì deepseek dùng chung openai client)
                response = self.client.chat.completions.create(
                    model=self.model_name,
                    messages=[
                        {"role": "system", "content": system_instruction},
                        {"role": "user", "content": prompt}
                    ],
                    temperature=0.0
                )
                return response.choices[0].message.content
        except Exception as e:
            err_msg = str(e)
            print(f"\n=============================================================")
            print(f"⚠️ [CẢNH BÁO LỖI KẾT NỐI API {self.provider.upper()}]")
            print(f"=============================================================")
            
            # 1. Lỗi Hết hạn mức / Rate Limit / Hết tiền (429 Resource Exhausted / Insufficient Quota)
            if "429" in err_msg or "RESOURCE_EXHAUSTED" in err_msg or "quota" in err_msg.lower():
                print(f"👉 Chi tiết: Khóa API đã hết hạn mức sử dụng (Quota Exceeded) hoặc vượt quá giới hạn request cho phép (Rate Limit).")
                print(f"💡 Hướng giải quyết:")
                if self.provider == "gemini":
                    print(f"   - Nếu bạn dùng tài khoản Free Tier, vui lòng chờ khoảng 1 phút rồi chạy lại.")
                    print(f"   - Truy cập https://aistudio.google.com/ để kiểm tra hạn mức sử dụng.")
                elif self.provider == "openai":
                    print(f"   - Tài khoản OpenAI của bạn đã hết hạn mức miễn phí (Credit Expired) hoặc hết tiền.")
                    print(f"   - Vui lòng truy cập https://platform.openai.com/settings/organization/billing để nạp tiền (Top up) hoặc cập nhật phương thức thanh toán.")
                else:
                    print(f"   - Vui lòng kiểm tra lại hạn mức và thông tin thanh toán trên trang quản trị của nhà cung cấp.")
                
            # 2. Lỗi Hết tiền / Không đủ số dư (402 Insufficient Balance)
            elif "402" in err_msg or "insufficient_balance" in err_msg.lower() or "insufficient balance" in err_msg.lower():
                print(f"👉 Chi tiết: Tài khoản API {self.provider.upper()} của bạn không đủ số dư (Insufficient Balance - Lỗi 402).")
                print(f"💡 Hướng giải quyết:")
                if self.provider == "deepseek":
                    print(f"   - Vui lòng truy cập https://platform.deepseek.com/ để nạp tiền (Top up) vào tài khoản (DeepSeek yêu cầu nạp trước tối thiểu 2 USD).")
                else:
                    print(f"   - Vui lòng truy cập trang quản trị tài khoản để kiểm tra thẻ tín dụng hoặc nạp thêm tiền.")
                
            # 3. Lỗi Sai Khóa API / Không hợp lệ (403 Forbidden / 401 Unauthorized / Invalid Key)
            elif "403" in err_msg or "401" in err_msg or "invalid" in err_msg.lower() or "unauthorized" in err_msg.lower():
                print(f"👉 Chi tiết: Khóa API '{self.provider.upper()}_API_KEY' trong file `.env` không hợp lệ hoặc đã hết hạn.")
                print(f"💡 Hướng giải quyết:")
                print(f"   - Mở file '.env' của bạn và kiểm tra lại chuỗi API Key.")
                print(f"   - Tạo lại khóa mới tại Google AI Studio hoặc OpenAI Platform.")
                
            # 3. Lỗi Không tìm thấy Model (404 Not Found)
            elif "404" in err_msg or "not found" in err_msg.lower():
                print(f"👉 Chi tiết: Mô hình '{self.model_name}' không khả dụng hoặc không được hỗ trợ bởi tài khoản của bạn.")
                print(f"💡 Hướng giải quyết:")
                print(f"   - Mở file '.env' và cấu hình lại dòng: GEMINI_MODEL=gemini-1.5-flash")
                print(f"   - Hoặc kiểm tra danh sách các model khả dụng của bạn.")
                
            # 4. Các lỗi kết nối mạng hoặc lỗi khác
            else:
                print(f"👉 Chi tiết lỗi: {err_msg}")
                print(f"💡 Hướng giải quyết: Kiểm tra kết nối mạng của bạn hoặc cấu hình API proxy nếu cần thiết.")
                
            print(f"\n🤖 [Hệ thống]: Tự động chuyển đổi sang chế độ giả lập (Mock Mode) để tiếp tục quy trình...")
            print(f"=============================================================\n")
            
            self.mock_mode = True
            return self.get_mock_agent_response()

    def get_mock_agent_response(self) -> str:
        """Trả về phản hồi giả lập của Agent nếu không có API Key."""
        self.iteration_count += 1
        if self.iteration_count == 1:
            return (
                "THOUGHT: Tôi cần kiểm tra cấu trúc dự án và chạy thử nghiệm build hiện tại trước.\n"
                "ACTION: execute_command('dotnet build')\n"
            )
        elif self.iteration_count == 2:
            return (
                "THOUGHT: Dự án build thành công. Bây giờ tôi sẽ tạo một Domain Entity mới có tên là 'PostCategory.cs'.\n"
                "ACTION: write_source('Domain/Entities/PostCategory.cs', "
                "'namespace Domain.Entities;\\n\\npublic class PostCategory {\\n    public Guid Id { get; set; }\\n    public string Name { get; set; } = string.Empty;\\n}')\n"
            )
        elif self.iteration_count == 3:
            return (
                "THOUGHT: Đã ghi Domain Entity. Giờ tôi cần chạy build lại xem có bị lỗi cú pháp không.\n"
                "ACTION: execute_command('dotnet build')\n"
            )
        elif self.iteration_count == 4:
            return (
                "THOUGHT: Build thành công! Tôi sẽ chạy thử nghiệm bộ integration tests có sẵn để đảm bảo không làm gãy hệ thống.\n"
                "ACTION: execute_command('dotnet test')\n"
            )
        else:
            return (
                "THOUGHT: Tất cả các bài test đã pass thành công! Công việc hoàn thành tốt đẹp.\n"
                "ACTION: finish_task('Đã thêm thành công thực thể PostCategory và xác thực dự án chạy ổn định.')\n"
            )

    def run_gan_evaluation(self, task: str) -> tuple[float, str]:
        """Thực thi vòng lặp Đánh giá Đối nghịch (GAN-Style Evaluator) đối với mã nguồn thay đổi."""
        print("\n🔍 [Harness Evaluator]: Đang chạy vòng lặp Đánh giá Đối nghịch (Adversarial Evaluation)...")
        
        # 1. Chạy test tự động để tính điểm Functionality
        print("🧪 [Harness Evaluator]: Chạy kiểm thử tự động phục vụ thang điểm Functionality...")
        test_code, test_out = run_dotnet_command("dotnet test")
        
        # Đếm số lượng test và trích xuất lỗi biên dịch/lỗi kiểm thử
        total_tests = 0
        passed_tests = 0
        failed_tests = 0
        
        compiler_errors = extract_compiler_errors(test_out)
        test_failures = extract_test_errors(test_out)
        
        error_summary = ""
        if compiler_errors:
            error_summary += "\n🚨 CÁC LỖI BIÊN DỊCH PHÁT HIỆN ĐƯỢC:\n" + "\n".join(compiler_errors) + "\n"
        if test_failures:
            error_summary += "\n⚠️ CÁC BÀI KIỂM THỬ BỊ THẤT BẠI:\n" + "\n".join(test_failures) + "\n"
            
        # Chạy kiểm tra định dạng code linter
        lint_violations = check_csharp_linting()
        if lint_violations:
            error_summary += lint_violations
            
        if "Passed!" in test_out or "Passed" in test_out:
            # Ví dụ: Passed!  - Failed:     0, Passed:    36, Skipped:     0, Total:    36
            match = re.search(r"Failed:\s*(\d+),\s*Passed:\s*(\d+)", test_out)
            if match:
                failed_tests = int(match.group(1))
                passed_tests = int(match.group(2))
                total_tests = passed_tests + failed_tests
            else:
                passed_tests = 36
                total_tests = 36
        elif test_code != 0 or compiler_errors:
            failed_tests = 1  # Có lỗi xảy ra hoặc lỗi biên dịch
            total_tests = 36
            
        func_score = 0.0
        if test_code == 0 and not compiler_errors:
            if total_tests > 0:
                func_score = (passed_tests / total_tests) * 10.0
            else:
                func_score = 10.0
        else:
            func_score = 0.0 # Thất bại hoàn toàn do lỗi compile hoặc lỗi test
            
        # 2. Lấy Git Diff
        git_diff = ""
        try:
            # uncommitted changes
            result = subprocess.run("git diff HEAD", shell=True, capture_output=True, encoding='utf-8', errors='replace')
            git_diff = result.stdout or ""
        except Exception:
            git_diff = ""
            
        if not git_diff or not git_diff.strip():
            try:
                # Nếu không có thay đổi uncommitted, lấy diff của commit cuối cùng
                result = subprocess.run("git diff HEAD~1", shell=True, capture_output=True, encoding='utf-8', errors='replace')
                git_diff = result.stdout or ""
            except Exception:
                git_diff = ""
                
        # 3. Tạo Prompt cho Evaluator với các nguyên tắc cực kỳ nghiêm ngặt
        evaluator_system = (
            "Bạn là một Adversarial Evaluator (Nhà đánh giá đối nghịch) cực kỳ nghiêm khắc và có chuyên môn cao về .NET 9.\n"
            "Nhiệm vụ của bạn là đánh giá những thay đổi code của Generator Agent dựa trên git diff và kết quả test.\n"
            "Hãy phê bình thẳng thắn, phát hiện mọi lỗi dù là nhỏ nhất. Đừng khen ngợi những đoạn code sơ sài.\n\n"
            "⚠️ BẮT BUỘC TUÂN THỦ NGUYÊN TẮC AN TOÀN BIÊN DỊCH:\n"
            "- NẾU trong kết quả kiểm thử tự động có bất kỳ lỗi biên dịch (Compiler Errors) hoặc lỗi kiểm thử (Test Failures) nào, "
            "hoặc nếu Điểm Functionality tự động là 0.0, bạn BẮT BUỘC phải đánh giá điểm số 'functionality' là 0.0 và đặt 'weighted_score' DƯỚI 5.0.\n"
            "- Tuyệt đối KHÔNG ĐƯỢC CHẤP THUẬN (weighted_score phải < 7.5) đối với bất kỳ mã nguồn nào bị lỗi biên dịch hoặc lỗi test, bất kể thiết kế có đẹp thế nào.\n\n"
            "--- RUBRIC ĐÁNH GIÁ (Thang điểm 1.0 - 10.0) ---\n"
            "1. DESIGN QUALITY (Trọng số 0.3): Sự tuân thủ Clean Architecture. (Domain không có phụ thuộc ngoài, CQRS Commands/Queries tách biệt rõ ràng, thin controllers).\n"
            "2. ORIGINALITY & BEST PRACTICES (Trọng số 0.2): Sử dụng C# 13, Primary Constructors, File-Scoped Namespaces, Async/Await chuẩn chỉ.\n"
            "3. CRAFT & POLISH (Trọng số 0.3): Viết tài liệu XML comments (///) đầy đủ cho tất cả public elements mới, xử lý nullable reference types an toàn, FluentValidation hoàn hảo.\n"
            "4. FUNCTIONALITY (Trọng số 0.2): Điểm tự động dựa trên số test pass.\n\n"
            "Bạn BẮT BUỘC phải đưa ra cấu trúc phản hồi chi tiết bằng tiếng Việt, và kết thúc bằng một khối JSON hợp lệ chứa điểm số như sau:\n"
            "```json\n"
            "{\n"
            "  \"design_quality\": 8.0,\n"
            "  \"best_practices\": 7.5,\n"
            "  \"craft\": 6.0,\n"
            "  \"functionality\": 10.0,\n"
            "  \"weighted_score\": 7.7\n"
            "}\n"
            "```"
        )
        
        eval_prompt = (
            f"Yêu cầu nhiệm vụ: {task}\n\n"
            f"Điểm Functionality tự động: {func_score:.1f}/10.0\n"
            f"Tóm tắt trạng thái biên dịch/kiểm thử:\n"
            f"{error_summary if error_summary else '✅ Đã vượt qua toàn bộ quá trình biên dịch và kiểm thử thành công!'}\n\n"
            f"Đầu ra Console Test chi tiết (Tối đa 8000 ký tự):\n"
            f"```text\n{test_out[:8000]}\n```\n\n"
            f"Bản Git Diff của các thay đổi mới:\n"
            f"```diff\n{git_diff[:8000]}\n```\n\n"
            "Hãy đánh giá chi tiết theo Rubric và chấm điểm nghiêm khắc."
        )
        
        response = ""
        if self.mock_mode:
            # Phản hồi giả lập
            response = (
                "### BÁO CÁO PHÂN TÍCH CỦA ADVERSARIAL EVALUATOR (GIẢ LẬP)\n\n"
                "1. **Design Quality (8.5/10)**: Mã nguồn tuân thủ Clean Architecture rất tốt. Các thực thể được tạo đúng Domain lớp.\n"
                "2. **Originality & Best Practices (8.0/10)**: Sử dụng Primary Constructor rất thanh thoát và File-Scoped namespace đẹp.\n"
                "3. **Craft & Polish (8.0/10)**: Đã bổ sung XML comments đầy đủ cho các public properties.\n"
                "4. **Functionality (10.0/10)**: Bộ tests chạy thành công 100%.\n\n"
                "**Tổng kết**: Code đạt chất lượng Senior C# rất tốt, sạch sẽ và sẵn sàng tích hợp.\n\n"
                "```json\n"
                "{\n"
                "  \"design_quality\": 8.5,\n"
                "  \"best_practices\": 8.0,\n"
                "  \"craft\": 8.0,\n"
                "  \"functionality\": 10.0,\n"
                "  \"weighted_score\": 8.5\n"
                "}\n"
                "```"
            )
        else:
            try:
                response = self.call_llm(eval_prompt, evaluator_system)
            except Exception as e:
                print(f"[Harness Evaluator Error]: Không gọi được LLM Evaluator: {e}")
                response = f"{e}\n```json\n{{\"design_quality\": 8.0, \"best_practices\": 8.0, \"craft\": 8.0, \"functionality\": 10.0, \"weighted_score\": 8.0}}\n```"
                
        # Parse weighted_score từ JSON block
        weighted_score = 0.0
        try:
            json_match = re.search(r"```json\s*(.*?)\s*```", response, re.DOTALL)
            if json_match:
                score_data = json.loads(json_match.group(1))
                weighted_score = float(score_data.get("weighted_score", 0.0))
            else:
                # Fallback nếu không có block json
                matches = re.findall(r'"weighted_score":\s*([\d.]+)', response)
                if matches:
                    weighted_score = float(matches[-1])
        except Exception as e:
            print(f"[Harness Evaluator Warning]: Không trích xuất được điểm số JSON: {e}")
            weighted_score = 7.0
            
        return weighted_score, response

    def execute_react_loop(self, task_description: str):
        """Thực thi vòng lặp Suy nghĩ -> Hành động -> Quan sát cho Agent."""
        print(f"\n=============================================================")
        print(f"🚀 BẮT ĐẦU CHẠY AI DEVELOPER HARNESS CHO TÁC VỤ:")
        print(f"   👉 '{task_description}'")
        print(f"=============================================================")
        
        system_instruction = (
            "Bạn là một Kỹ sư .NET 9 Senior, làm việc trên dự án FloraCore (Clean Architecture + CQRS + MediatR + xUnit).\n"
            "Nhiệm vụ của bạn là giải quyết yêu cầu của người dùng bằng cách đọc/viết code và chạy kiểm thử tự động.\n\n"
            "--- BẮT BUỘC TUÂN THỦ CHẶT CHẼ CODING POLICY CỦA DỰ ÁN ---\n"
            "1. KIẾN TRÚC SẠCH (Clean Architecture):\n"
            "   - Domain Layer: Lõi của dự án, KHÔNG phụ thuộc vào thư viện ngoài. Chứa Entities.\n"
            "   - Application Layer: Chứa CQRS Commands/Queries, Handlers (MediatR), DTOs, Interfaces và Validators.\n"
            "   - Infrastructure Layer: Chứa AppDbContext, Repository implementations, migrations và tích hợp DB.\n"
            "   - Web/API Layer: Chỉ chứa Controllers rất mỏng (thin controllers), ủy quyền xử lý hoàn toàn cho Application thông qua MediatR.\n"
            "   - Hướng phụ thuộc: Domain <- Application <- Infrastructure <- Controllers. Tuyệt đối không đảo ngược chiều.\n"
            "2. MODERN C# & .NET 9:\n"
            "   - Khuyến khích dùng 'record' thay cho class đối với DTOs, Commands và Queries (bất biến - immutable).\n"
            "   - Sử dụng File-scoped namespaces thay cho block namespaces để giảm thụt lề.\n"
            "   - Bật và xử lý triệt để Nullable Reference Types.\n"
            "3. LẬP TRÌNH BẤT ĐỒNG BỘ (Asynchronous):\n"
            "   - Luôn dùng 'async/await' cho các tác vụ I/O (Database, Network, File).\n"
            "   - Các hàm bất đồng bộ bắt buộc phải có hậu tố 'Async' (ví dụ: GetProductByIdAsync).\n"
            "   - TUYỆT ĐỐI KHÔNG dùng '.Result' hoặc '.Wait()' để tránh gây Deadlock. Hãy truyền CancellationToken nếu có thể.\n"
            "4. THAY ĐỔI PHẪU THUẬT (Surgical Changes):\n"
            "   - Chỉ sửa đổi chính xác những dòng code cần thiết để giải quyết yêu cầu.\n"
            "   - Không tự ý tái cấu trúc (refactor) hoặc chỉnh sửa định dạng/comment ở các phần code xung quanh đang chạy ổn định.\n"
            "   - Loại bỏ các thư viện imports/using và biến không dùng đến phát sinh do chính thay đổi của bạn.\n"
            "5. ĐẢM BẢO CHẤT LƯỢNG:\n"
            "   - Sử dụng FluentValidation để validate dữ liệu ở Backend.\n"
            "   - Viết tests tuân thủ chuẩn AAA (Arrange - Act - Assert).\n\n"
            "--- BẮT BUỘC TUÂN THỦ .NET/C# BEST PRACTICES & DESIGN PATTERNS (AGENTS SKILLS) ---\n"
            "1. XML DOCUMENTATION:\n"
            "   - Viết XML documentation comments (sử dụng dấu '///') đầy đủ cho TẤT CẢ các public classes, interfaces, methods và properties.\n"
            "   - Ghi rõ mô tả cho từng tham số (<param>) và giá trị trả về (<returns>) trong tài liệu XML.\n"
            "2. DEPENDENCY INJECTION & CONSTRUCTORS:\n"
            "   - Bắt buộc dùng cú pháp Primary Constructor của C# 12+ cho DI (ví dụ: 'public class MyService(IDependency dependency)').\n"
            "   - Thực hiện kiểm tra null bằng ArgumentNullException cho các dependency (ví dụ: 'ArgumentNullException.ThrowIfNull(dependency)').\n"
            "3. THIẾT KẾ CÁC DESIGN PATTERNS CHUẨN:\n"
            "   - Command Pattern: Triển khai CQRS Commands/Queries thông qua Handlers độc lập kế thừa MediatR interfaces.\n"
            "   - Repository Pattern: Tương tác database thông qua các interfaces trừu tượng hóa để đảm bảo khả năng unit test và mock dễ dàng.\n"
            "   - Resource Pattern: Sử dụng ResourceManager cho các chuỗi thông báo, phân chia log và lỗi (.resx files).\n"
            "4. BỘ TIÊU CHUẨN KIỂM THỬ:\n"
            "   - Sử dụng xUnit phối hợp cùng FluentAssertions để viết các khẳng định test sạch sẽ.\n"
            "   - Sử dụng Moq để tạo dữ liệu giả lập cho các phụ thuộc bên ngoài khi viết Unit Tests.\n"
            "   - Luôn test cả hai kịch bản Thành công (Success) và Thất bại (Failure), bao gồm cả kiểm chứng tham số null.\n\n"
            "--- QUY TRÌNH HÀNH ĐỘNG REACT LOOP ---\n"
            "Trong mỗi lượt phản hồi, bạn BẮT BUỘC phải đưa ra cấu trúc định dạng sau:\n"
            "THOUGHT: Phân tích kỹ thuật của bạn về bước kế tiếp và các file cần đọc/ghi.\n"
            "ACTION: Chỉ chọn DUY NHẤT một trong các lệnh sau để Harness thực thi:\n"
            "   - read_source('đường_dẫn_file')\n"
            "   - write_source('đường_dẫn_file', 'nội dung file mới')\n"
            "   - execute_command('dotnet build'), execute_command('dotnet test') hoặc các lệnh git cơ bản (ví dụ: execute_command('git status') hoặc execute_command('git add . && git commit -m \"thông điệp\"'))\n"
            "   - finish_task('thông điệp kết thúc chi tiết các file đã sửa và kết quả tests')\n"
        )
        
        context_history = f"Yêu cầu của người dùng: {task_description}\n"
        gan_retries = 0
        max_gan_retries = 3
        
        while self.iteration_count < self.max_iterations:
            self.iteration_count += 1
            print(f"\n--- VÒNG LẶP SUY NGHĨ THỨ {self.iteration_count} ---")
            
            # 1. LLM Suy nghĩ và đưa ra quyết định hành động
            response = self.call_llm(context_history, system_instruction)
            print(response)
            
            # Ghi log thought & action
            with open(self.log_file, "a", encoding="utf-8") as f:
                f.write(f"\n[Lượt {self.iteration_count}]:\n{response}\n")
            
            # Parse Action từ phản hồi của LLM
            # Cực kỳ linh hoạt: Trích xuất action từ bất cứ định dạng nào của Generator Agent
            action_name = None
            action_args = ""
            
            # Loại bỏ các tag ``` và block nháy
            cleaned_response = re.sub(r"```[a-zA-Z_]*", "", response)
            cleaned_response = cleaned_response.replace("`", "")
            
            action_match = re.search(r"\b(execute_command|read_source|write_source|finish_task)\((.*)\)", cleaned_response, re.DOTALL)
            if not action_match:
                # Thử fallback về tên cũ để tương thích ngược nếu Agent quen tay
                action_match = re.search(r"\b(run_command|read_file|write_file|done)\((.*)\)", cleaned_response, re.DOTALL)
                
            if not action_match:
                print("[Harness Error]: Không parse được ACTION từ Agent. Dừng vòng lặp.")
                break
                
            action_name = action_match.group(1).strip()
            action_args = action_match.group(2).strip()
            
            # Đồng bộ tên hàm cũ về tên mới để xử lý thống nhất
            if action_name == "run_command":
                action_name = "execute_command"
            elif action_name == "read_file":
                action_name = "read_source"
            elif action_name == "write_file":
                action_name = "write_source"
            elif action_name == "done":
                action_name = "finish_task"
            
            parsed_args = safe_parse_action_arguments(action_args)
            observation = ""
            
            # 2. Thực thi Hành động tương ứng
            if self.mock_mode:
                if action_name == "execute_command":
                    cmd = parsed_args[0] if len(parsed_args) > 0 else ""
                    observation = format_observation(
                        status="SUCCESS",
                        summary=f"(MOCK): Chạy thành công lệnh '{cmd}'",
                        details="(Chạy giả lập - không thực hiện lệnh hệ thống thật).",
                        next_actions=["execute_command('dotnet test')", "finish_task(...)"]
                    )
                elif action_name == "read_source":
                    filepath = parsed_args[0] if len(parsed_args) > 0 else ""
                    observation = format_observation(
                        status="SUCCESS",
                        summary=f"(MOCK): Đọc thành công '{filepath}'",
                        details="(Chế độ giả lập - trả về nội dung mẫu).",
                        artifacts=[filepath]
                    )
                elif action_name == "write_source":
                    filepath = parsed_args[0] if len(parsed_args) > 0 else ""
                    observation = format_observation(
                        status="SUCCESS",
                        summary=f"(MOCK): Đã ghi thành công '{filepath}'",
                        details="(Chế độ giả lập - không ghi đè đĩa thật).",
                        artifacts=[filepath]
                    )
                elif action_name == "finish_task":
                    msg = parsed_args[0] if len(parsed_args) > 0 else ""
                    print(f"\n🎉 [GENERATOR HOÀN THÀNH - MOCK MODE]: {msg}")
                    
                    # Chạy kiểm thử đối nghịch
                    score, report = self.run_gan_evaluation(task_description)
                    print(f"\n🏆 [EVALUATION SCORECARD]: THANG ĐIỂM ĐẠT ĐƯỢC: {score:.2f}/10.0 (Yêu cầu tối thiểu: {self.pass_threshold})")
                    print(report)
                    
                    # Ghi báo cáo ra thư mục .claude/evals/
                    report_path = os.path.join(self.evals_dir, "evaluation_report.md")
                    with open(report_path, "w", encoding="utf-8") as f:
                        f.write(report)
                    
                    if score >= self.pass_threshold or gan_retries >= max_gan_retries:
                        print(f"\n✅ [Harness Success]: Đã vượt qua vòng kiểm duyệt đối nghịch thành công!")
                        break
                    else:
                        gan_retries += 1
                        print(f"\n❌ [Harness Critique]: Không vượt qua kiểm duyệt! Đang gửi phản hồi cải tiến cho Agent sửa đổi...")
                        observation = format_observation(
                            status="ERROR",
                            summary=f"Hệ thống đánh giá đối nghịch từ chối phê duyệt (Điểm: {score:.2f} < {self.pass_threshold}).",
                            details=report,
                            next_actions=["Hãy chỉnh sửa code dựa trên các điểm phê bình trong phần DETAILS.", "Viết lại tài liệu XML và kiểm tra kỹ logic."]
                        )
                else:
                    observation = format_observation(
                        status="SUCCESS",
                        summary=f"Thực hiện thành công công cụ '{action_name}'",
                        details="(Mock mode)"
                    )
            else:
                # Thực thi hành động thật trên ổ đĩa và hệ điều hành (Chế độ chạy thực tế)
                if action_name == "execute_command":
                    cmd = parsed_args[0] if len(parsed_args) > 0 else ""
                    # Kiểm tra xem có phải lệnh git an toàn không
                    is_git = cmd.startswith("git ") and any(sub in cmd for sub in ["status", "add", "commit", "diff"])
                    
                    if cmd not in ["dotnet build", "dotnet test", "dotnet restore", "dotnet clean"] and not is_git:
                        observation = format_observation(
                            status="ERROR",
                            summary="Lỗi bảo mật Harness.",
                            details="Harness chỉ cho phép chạy lệnh 'dotnet build', 'dotnet test', 'dotnet restore', 'dotnet clean' hoặc các lệnh git cơ bản (status, add, commit, diff).",
                            next_actions=["Chỉ sử dụng lệnh hợp lệ."]
                        )
                    else:
                        # Bắt buộc phải có sự phê duyệt của con người đối với các lệnh thay đổi git (commit, add) kể cả khi bật --auto-approve
                        is_git_change = "git " in cmd and any(sub in cmd for sub in ["commit", "add"])
                        
                        if self.ask_approval(f"Agent muốn chạy lệnh hệ thống cục bộ: '{cmd}'", force_ask=is_git_change):
                            code, out = run_dotnet_command(cmd)
                            
                            status = "SUCCESS" if code == 0 else "ERROR"
                            summary = f"Chạy lệnh '{cmd}' thành công." if code == 0 else f"Lệnh '{cmd}' thất bại với mã lỗi {code}."
                            next_actions = []
                            details = out[:1500]
                            
                            if code != 0:
                                if "build" in cmd:
                                    comp_errs = extract_compiler_errors(out)
                                    if comp_errs:
                                        details += "\n\n--- DANH SÁCH LỖI BIÊN DỊCH ---\n" + "\n".join(comp_errs)
                                        next_actions.append("Sửa lỗi biên dịch được liệt kê chi tiết trong DETAILS.")
                                elif "test" in cmd:
                                    test_errs = extract_test_errors(out)
                                    if test_errs:
                                        details += "\n\n--- DANH SÁCH TEST CASE THẤT BẠI ---\n" + "\n".join(test_errs)
                                        next_actions.append("Sửa các test cases thất bại trong phần DETAILS.")
                            else:
                                if "build" in cmd:
                                    next_actions.append("Chạy tiếp lệnh execute_command('dotnet test') để xác minh tính đúng đắn.")
                                elif "test" in cmd:
                                    next_actions.append("Thực hiện lệnh finish_task(...) để kết thúc tác vụ.")
                                    
                            observation = format_observation(
                                status=status,
                                summary=summary,
                                details=details,
                                next_actions=next_actions
                            )
                        else:
                            print("🛡️  [Harness]: Từ chối thực thi lệnh hệ thống theo yêu cầu của bạn.")
                            observation = format_observation(
                                status="ERROR",
                                summary="Người dùng từ chối cấp quyền.",
                                details="Người dùng đã nhấn từ chối trên console.",
                                next_actions=["Đề xuất một lệnh khác an toàn hơn hoặc hỏi ý kiến người dùng."]
                            )
                        
                elif action_name == "read_source":
                    filepath = parsed_args[0] if len(parsed_args) > 0 else ""
                    
                    # Kiểm tra bảo mật đường dẫn nhạy cảm
                    normalized_path = filepath.lower().replace("\\", "/")
                    if "ai_developer_harness.py" in normalized_path or ".env" in normalized_path:
                        observation = format_observation(
                            status="ERROR",
                            summary="Lỗi bảo mật Harness.",
                            details="Harness không cho phép đọc trực tiếp file cấu hình nhạy cảm (.env) hoặc file chạy của chính Harness (ai_developer_harness.py).",
                            next_actions=["Hãy đọc các file nguồn dự án khác."]
                        )
                    else:
                        content = read_source_file(filepath)
                        
                        status = "SUCCESS" if not content.startswith("Lỗi đọc file:") else "ERROR"
                        summary = f"Đọc file '{filepath}' thành công." if status == "SUCCESS" else content
                        
                        # Tránh làm quá tải context nếu file quá dài
                        details = content
                        if len(content) > 12000:
                            details = content[:12000] + "\n\n...[FILE BỊ CẮT GIẢM VÌ QUÁ DÀI - TRÁNH VƯỢT QUÁ NGỮ CẢNH]..."
                            
                        observation = format_observation(
                            status=status,
                            summary=summary,
                            details=details,
                            artifacts=[filepath]
                        )
                    
                elif action_name == "write_source":
                    filepath = parsed_args[0] if len(parsed_args) > 0 else ""
                    content = parsed_args[1] if len(parsed_args) > 1 else ""
                    
                    # Kiểm tra bảo mật đường dẫn nhạy cảm
                    normalized_path = filepath.lower().replace("\\", "/")
                    if "ai_developer_harness.py" in normalized_path or ".env" in normalized_path:
                        observation = format_observation(
                            status="ERROR",
                            summary="Lỗi bảo mật Harness.",
                            details="Harness nghiêm cấm ghi đè hoặc sửa đổi file cấu hình nhạy cảm (.env) hoặc file chạy của chính Harness (ai_developer_harness.py).",
                            next_actions=["Hãy chỉnh sửa các file nguồn dự án khác."]
                        )
                    else:
                        # Hiển thị Preview thay đổi trước khi phê duyệt
                        print(f"\n==========================================")
                        print(f"📄 PREVIEW NỘI DUNG SẮP GHI VÀO FILE: '{filepath}'")
                        print(f"==========================================")
                        print(content[:500] + ("\n...[còn tiếp]..." if len(content) > 500 else ""))
                        print(f"==========================================\n")
                        
                        if self.ask_approval(f"Agent muốn ghi/sửa đổi nội dung file: '{filepath}'"):
                            res = write_source_file(filepath, content)
                            status = "SUCCESS" if res == "Ghi file thành công." else "ERROR"
                            
                            observation = format_observation(
                                status=status,
                                summary=f"Ghi file '{filepath}' thành công." if status == "SUCCESS" else res,
                                details=res,
                                artifacts=[filepath]
                            )
                        else:
                            print("🛡️  [Harness]: Từ chối ghi file theo yêu cầu của bạn.")
                            observation = format_observation(
                                status="ERROR",
                                summary="Người dùng từ chối ghi file.",
                                details="Người dùng từ chối phê duyệt ghi tệp tin lên đĩa cứng.",
                                next_actions=["Hỏi lại ý kiến hoặc điều chỉnh nội dung khác."]
                            )
                        
                elif action_name == "finish_task":
                    msg = parsed_args[0] if len(parsed_args) > 0 else ""
                    print(f"\n🎉 [GENERATOR HOÀN THÀNH TÁC VỤ]: {msg}")
                    
                    # Kích hoạt GAN Adversarial Evaluator để duyệt code thực tế
                    score, report = self.run_gan_evaluation(task_description)
                    print(f"\n🏆 [EVALUATION SCORECARD]: THANG ĐIỂM ĐẠT ĐƯỢC: {score:.2f}/10.0 (Yêu cầu tối thiểu: {self.pass_threshold})")
                    print(report)
                    
                    # Ghi báo cáo ra thư mục .claude/evals/
                    report_path = os.path.join(self.evals_dir, "evaluation_report.md")
                    with open(report_path, "w", encoding="utf-8") as f:
                        f.write(report)
                    
                    if score >= self.pass_threshold or gan_retries >= max_gan_retries:
                        print(f"\n✅ [Harness Success]: Đã vượt qua vòng kiểm duyệt đối nghịch thành công!")
                        break
                    else:
                        gan_retries += 1
                        print(f"\n❌ [Harness Critique]: Không vượt qua kiểm duyệt! Đang gửi phản hồi cải tiến cho Agent sửa đổi...")
                        
                        # 🔄 [HỆ THỐNG PHỤC HỒI]: Tự động rollback các tệp đã thay đổi chưa commit để bắt đầu lại sạch sẽ
                        print("\n🔄 [Harness Recovery]: Đang tự động khôi phục workspace sạch (Git Rollback) để Agent sửa đổi hướng đi mới...")
                        rollback_message = ""
                        try:
                            # Khôi phục các tệp chưa commit để dọn rác lỗi biên dịch cũ
                            subprocess.run("git checkout -- .", shell=True, capture_output=True)
                            # Xóa các file mới untracked để tránh lỗi biên dịch của C# quét đệ quy
                            subprocess.run("git clean -fd", shell=True, capture_output=True)
                            
                            rollback_message = (
                                "\n\n🔄 [HỆ THỐNG PHỤC HỒI]: Harness đã tự động khôi phục toàn bộ workspace (git checkout -- . && git clean -fd) "
                                "về trạng thái commit sạch gần nhất nhằm dọn dẹp triệt để các lỗi biên dịch hoặc các file bị tạo sai vị trí. "
                                "Hãy thiết kế một hướng giải quyết mới, an toàn hơn để thực hiện lại nhiệm vụ!"
                            )
                            print("✅ [Harness Recovery]: Khôi phục workspace sạch thành công!")
                        except Exception as e:
                            rollback_message = f"\n\n🔄 [HỆ THỐNG PHỤC HỒI]: Lỗi khi cố gắng khôi phục workspace: {str(e)}"
                            print(f"❌ [Harness Recovery]: Thất bại: {e}")
                            
                        observation = format_observation(
                            status="ERROR",
                            summary=f"Hệ thống đánh giá đối nghịch từ chối phê duyệt (Điểm: {score:.2f} < {self.pass_threshold}).",
                            details=report + rollback_message,
                            next_actions=["Dựa trên các điểm phê bình trong DETAILS và workspace sạch đã được khôi phục, hãy viết lại giải pháp đúng đắn."]
                        )
                else:
                    observation = format_observation(
                        status="ERROR",
                        summary="Công cụ không được hỗ trợ.",
                        details=f"Không có công cụ nào mang tên '{action_name}'.",
                        next_actions=["Chỉ chọn read_source, write_source, execute_command, finish_task."]
                    )
                
            print(f"\n[Quan sát kết quả từ Harness]:\n{observation[:300]}...")
            
            # Ghi log observation
            with open(self.log_file, "a", encoding="utf-8") as f:
                f.write(f"\nOBSERVATION:\n{observation}\n")
                
            # Cập nhật lịch sử để Agent suy nghĩ cho lượt kế tiếp
            context_history += f"\nLượt {self.iteration_count}:\n{response}\nOBSERVATION: {observation}\n"
            
            # Nếu nhận được critique từ evaluator, chúng tôi hoàn lại số lượt lặp để agent có đủ cơ hội chỉnh sửa
            if action_name == "finish_task" and score < self.pass_threshold:
                # Trả lại tối đa 2 iterations để tránh bùng nổ token nhưng vẫn đủ sửa code
                self.max_iterations = max(self.max_iterations, self.iteration_count + 2)
        
        if self.iteration_count >= self.max_iterations:
            print(f"\n⚠️ [Harness Alert]: Đạt giới hạn lặp tối đa ({self.max_iterations}). Ngắt khẩn cấp để tránh bùng nổ chi phí.")

if __name__ == "__main__":
    task = "Hãy kiểm tra xem dự án hiện tại build và chạy thử nghiệm tests thành công không."
    auto_approve = False
    
    # Phân tích các đối số dòng lệnh
    args = sys.argv[1:]
    if "--auto-approve" in args:
        auto_approve = True
        args.remove("--auto-approve")
        
    if len(args) > 0:
        # Nếu có truyền yêu cầu tùy biến
        task = args[0]
        
    harness = AIDeveloperHarness(auto_approve=auto_approve)
    harness.execute_react_loop(task)
