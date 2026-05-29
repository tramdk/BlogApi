import os
import sys
import re
import ast
import json
import time
import subprocess
import copy

try:
    from dotenv import load_dotenv
    # Xác định thư mục gốc của dự án
    harness_dir = os.path.dirname(os.path.abspath(__file__))
    scripts_dir = os.path.dirname(harness_dir)
    root_dir = os.path.dirname(scripts_dir)
    env_path = os.path.join(root_dir, ".env")
    if os.path.exists(env_path):
        load_dotenv(env_path)
    else:
        load_dotenv()
except ImportError:
    pass

from harness.tools import (
    run_dotnet_command,
    read_source_file,
    write_source_file,
    extract_compiler_errors,
    extract_test_errors,
    check_csharp_linting
)
from harness.safety import (
    is_path_safe,
    safe_parse_action_arguments,
    format_observation,
    selective_rollback
)
from harness.llm import (
    LLMRouter,
    build_cache_contents,
    generate_directory_tree,
    TOOLS_SCHEMA
)
from harness.memory import distill_and_persist_lessons

class AIDeveloperHarness:
    def __init__(self, auto_approve: bool = False):
        harness_dir = os.path.dirname(os.path.abspath(__file__))
        scripts_dir = os.path.dirname(harness_dir)
        root_dir = os.path.dirname(scripts_dir)
        
        # Đọc và dọn dẹp các API Key để tránh lỗi dấu nháy kép từ file .env
        gemini_key = (os.getenv("GEMINI_API_KEY") or "").strip("'\" \t")
        openai_key = (os.getenv("OPENAI_API_KEY") or "").strip("'\" \t")
        claude_key = (os.getenv("CLAUDE_API_KEY") or os.getenv("ANTHROPIC_API_KEY") or "").strip("'\" \t")
        deepseek_key = (os.getenv("DEEPSEEK_API_KEY") or "").strip("'\" \t")
        
        # Xác định Provider dựa trên khóa thực tế hợp lệ
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
        self.current_role = "Planner"
        
        # Cấu hình lưu trữ tệp tin log trong thư mục chuyên dụng (.claude/evals/)
        self.evals_dir = os.path.join(root_dir, ".claude", "evals")
        os.makedirs(self.evals_dir, exist_ok=True)
        self.log_file = os.path.join(self.evals_dir, "harness_run.log")
        
        # Ghi log dòng chào mừng cho phiên làm việc mới (Ghi đè 'w' để tránh log phình to)
        with open(self.log_file, "w", encoding="utf-8") as f:
            f.write(f"=============================================================\n")
            f.write(f"PHIÊN LÀM VIỆC MỚI KHỞI CHẠY (MULTI-AGENT SPEC-DRIVEN FLOW)\n")
            f.write(f"=============================================================\n")
        
        # Cấu hình mặc định (fallback)
        self.policy_files = ["CODING_POLICY.md", "CLAUDE.md"]
        self.skills_and_guides = []
        self.allowed_cmds = ["dotnet build", "dotnet test", "dotnet restore", "dotnet clean"]

        # Đọc cấu hình từ harness_config.json nếu có
        config_path = os.path.join(root_dir, "harness_config.json")
        if os.path.exists(config_path):
            try:
                with open(config_path, "r", encoding="utf-8") as cf:
                    config_data = json.load(cf)
                    self.policy_files = config_data.get("policy_files", self.policy_files)
                    self.skills_and_guides = config_data.get("skills_and_guides", self.skills_and_guides)
                    self.allowed_cmds = config_data.get("allowed_commands", self.allowed_cmds)
                print(f"⚙️ [Harness Control]: Đã nạp cấu hình từ {config_path}")
            except Exception as e:
                print(f"⚠️ [Harness Control]: Lỗi khi đọc file cấu hình, sử dụng mặc định: {e}")

        # Tự động nạp chính sách lập trình cục bộ
        self.policy_content = ""
        try:
            for p_file in self.policy_files:
                p_path = os.path.join(root_dir, p_file)
                if os.path.exists(p_path):
                    with open(p_path, "r", encoding="utf-8") as pf:
                        self.policy_content = pf.read()
                    print(f"📖 [Harness Control]: Đang nạp chính sách lập trình từ '{p_file}'...")
                    break
        except Exception as e:
            print(f"⚠️ [Harness Control]: Lỗi khi quét/nạp chính sách lập trình cục bộ: {e}")

        # Tự động nạp các tài liệu kỹ năng (skills/guides)
        self.skills_contents = []
        for s_file in self.skills_and_guides:
            s_path = os.path.join(root_dir, s_file)
            if os.path.exists(s_path):
                try:
                    with open(s_path, "r", encoding="utf-8") as sf:
                        self.skills_contents.append((s_file, sf.read()))
                    print(f"📖 [Harness Control]: Đang nạp tài liệu kỹ năng từ '{s_file}'...")
                except Exception as e:
                    print(f"⚠️ [Harness Control]: Lỗi khi nạp tài liệu kỹ năng {s_file}: {e}")

        # Tự động nạp bài học tự động (harness_lessons.md) nếu có
        self.lessons_path = os.path.join(scripts_dir, "harness_lessons.md")
        self.lessons_content = ""
        try:
            if os.path.exists(self.lessons_path):
                with open(self.lessons_path, "r", encoding="utf-8") as lf:
                    self.lessons_content = lf.read()
                print(f"📖 [Harness Control]: Đang nạp bài học tự động từ '{os.path.relpath(self.lessons_path)}'...")
        except Exception as e:
            print(f"⚠️ [Harness Control]: Lỗi đọc lessons file: {e}")

        self.model_name = os.getenv("GEMINI_MODEL") or os.getenv("CLAUDE_MODEL") or os.getenv("OPENAI_MODEL") or os.getenv("DEEPSEEK_MODEL") or "gemini-2.5-flash"
        
        self.mock_mode = (self.provider == "mock" or not self.api_key)
        if self.mock_mode:
            print("[CẢNH BÁO]: Không tìm thấy khóa API của GEMINI, OPENAI, CLAUDE hoặc DEEPSEEK.")
            print("Harness sẽ chạy ở chế độ giả lập (Mock LLM) để minh họa quy trình.")
            
        self.llm_router = LLMRouter(
            provider=self.provider,
            api_key=self.api_key,
            model_name=self.model_name,
            log_file=self.log_file,
            mock_mode=self.mock_mode
        )

        self.modified_files = set()
        self.planner_production_files = []
        self.test_filter_keyword = "test"
        self.test_writer_files = []

    def _build_cache_helper(self):
        harness_dir = os.path.dirname(os.path.abspath(__file__))
        scripts_dir = os.path.dirname(harness_dir)
        root_dir = os.path.dirname(scripts_dir)
        return build_cache_contents(root_dir, self.policy_content, self.lessons_content, self.skills_contents)

    def extract_test_filter_keyword(self, plan: str, task_description: str) -> str:
        """Trích xuất keyword để filter test từ plan hoặc task description."""
        text = (plan or "") + " " + (task_description or "")
        text_lower = text.lower()
        features = ["statistics", "cart", "product", "order", "auth", "user", "post", "category",
                     "file", "website", "chat", "inbox", "idempotency", "search", "rating"]
        for feat in features:
            if feat in text_lower:
                return feat
        return "test"

    def ask_approval(self, message: str, force_ask: bool = False) -> bool:
        """Hỏi ý kiến người dùng trước khi thực thi hành động nhạy cảm hoặc phê duyệt chốt chặn."""
        if self.auto_approve:
            print(f"\n🛡️  [Harness HITL - AUTO APPROVED]: {message}")
            return True
        try:
            choice = input(f"\n🛡️  [Harness HITL]: {message}\n👉 Đồng ý thực thi? (y/n) [Mặc định: y]: ").strip().lower()
            return choice in ["", "y", "yes"]
        except Exception:
            return False

    def call_llm(self, prompt: str, system_instruction: str, stop_sequences: list[str] = None) -> str:
        """[LEGACY] Gọi LLM để lấy phân tích (text-only, không dùng function calling). Dùng cho Evaluator."""
        self.llm_router.current_role = self.current_role
        return self.llm_router.call_llm(
            prompt=prompt,
            system_instruction=system_instruction,
            build_cache_func=self._build_cache_helper,
            stop_sequences=stop_sequences
        )

    def call_llm_with_tools(self, messages: list[dict], system_instruction: str) -> dict:
        """Gọi LLM với Native Function Calling. Trả về dict chuẩn hóa."""
        self.llm_router.current_role = self.current_role
        return self.llm_router.call_llm_with_tools(
            messages=messages,
            system_instruction=system_instruction,
            tools_schema=TOOLS_SCHEMA,
            build_cache_func=self._build_cache_helper
        )

    def execute_mock_action(self, action_name: str, action_args: dict) -> str:
        """Thực thi action giả lập để trả về Observation trong Mock Mode."""
        if action_name == "execute_command":
            cmd = action_args.get("command", "")
            return format_observation(
                status="SUCCESS",
                summary=f"(MOCK) Chạy lệnh '{cmd}' thành công.",
                details="Mock output: Lệnh chạy thành công."
            )
        elif action_name == "view_source":
            filepath = action_args.get("file_path", "")
            return format_observation(
                status="SUCCESS",
                summary=f"(MOCK) Đọc file '{filepath}' thành công.",
                details="Nội dung file giả lập.",
                artifacts=[filepath]
            )
        elif action_name == "write_source":
            filepath = action_args.get("file_path", "")
            return format_observation(
                status="SUCCESS",
                summary=f"(MOCK) Ghi file '{filepath}' thành công.",
                details="Ghi file thành công lên hệ thống giả lập.",
                artifacts=[filepath]
            )
        return format_observation(
            status="ERROR",
            summary="Action không hợp lệ.",
            details="Mock mode không nhận biết action này."
        )

    def run_gan_evaluation(self, task: str) -> tuple[float, str]:
        """Thực thi vòng lặp Đánh giá Đối nghịch (GAN-Style Evaluator) đối với mã nguồn thay đổi."""
        print("\n🔍 [Harness Evaluator]: Đang chạy vòng lặp Đánh giá Đối nghịch (Adversarial Evaluation)...")
        
        # 1. Chạy test tự động để tính điểm Functionality
        print("🧪 [Harness Evaluator]: Chạy kiểm thử tự động phục vụ thang điểm Functionality...")
        filter_keyword = getattr(self, 'test_filter_keyword', 'test')
        test_code, test_out = run_dotnet_command(f"dotnet test --filter FullyQualifiedName~{filter_keyword}")
        
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
            
        lint_violations = check_csharp_linting()
        if lint_violations:
            error_summary += lint_violations
            
        if "Passed!" in test_out or "Passed" in test_out:
            match = re.search(r"Failed:\s*(\d+),\s*Passed:\s*(\d+)", test_out)
            if match:
                failed_tests = int(match.group(1))
                passed_tests = int(match.group(2))
                total_tests = passed_tests + failed_tests
            else:
                passed_tests = 36
                total_tests = 36
        elif test_code != 0 or compiler_errors:
            failed_tests = 1
            total_tests = 36
            
        func_score = 0.0
        if test_code == 0 and not compiler_errors:
            if total_tests > 0:
                func_score = (passed_tests / total_tests) * 10.0
            else:
                func_score = 10.0
        else:
            func_score = 0.0
            
        # 2. Lấy Git Diff (Loại bỏ shell=True)
        git_diff = ""
        try:
            result = subprocess.run(
                ["git", "diff", "HEAD"],
                shell=False,
                capture_output=True,
                encoding='utf-8',
                errors='replace'
            )
            git_diff = result.stdout or ""
        except Exception:
            git_diff = ""
            
        if not git_diff or not git_diff.strip():
            try:
                result = subprocess.run(
                    ["git", "diff", "HEAD~1"],
                    shell=False,
                    capture_output=True,
                    encoding='utf-8',
                    errors='replace'
                )
                git_diff = result.stdout or ""
            except Exception:
                git_diff = ""
                
        # 3. Tạo Prompt cho Evaluator
        evaluator_system = (
            "Bạn là một Adversarial Evaluator (Nhà đánh giá đối nghịch) cực kỳ nghiêm khắc và có chuyên môn cao về .NET 9.\n"
            "Nhiệm vụ của bạn là đánh giá những thay đổi code của Generator Agent dựa trên git diff và kết quả test.\n"
            "Hãy phê bình thẳng thắn, phát hiện mọi lỗi dù là nhỏ nhất. Đừng khen ngợi những đoạn code sơ sài.\n\n"
            "⚠️ BẮT BUỘC TUÂN THỦ NGUYÊN TẮC AN TOÀN BIÊN DỊCH & CHỐNG SỬA BỀ NỔI (ANTI-HOTFIX):\n"
            "- NẾU trong kết quả kiểm thử tự động có bất kỳ lỗi biên dịch (Compiler Errors) hoặc lỗi kiểm thử (Test Failures) nào, "
            "hoặc nếu Điểm Functionality tự động là 0.0, bạn BẮT BUỘC phải đánh giá điểm số 'functionality' là 0.0 và đặt 'weighted_score' DƯỚI 5.0.\n"
            "- Tuyệt đối KHÔNG ĐƯỢC CHẤP THUẬN (weighted_score phải < 7.5) đối với bất kỳ mã nguồn nào bị lỗi biên dịch hoặc lỗi test, bất kể thiết kế có đẹp thế nào.\n"
            "- BẮT BUỘC rà soát kỹ Git Diff để phát hiện các giải pháp đối phó, sửa bề nổi (hotfixes) như: hardcode giá trị từ file test (vd: `if (id == 99)`), bypass validation, bắt try-catch rỗng để ẩn exception. Nếu phát hiện hành vi này, bạn BẮT BUỘC phải đánh giá điểm 'craft' và 'weighted_score' DƯỚI 5.0.\n\n"
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
        if self.policy_content:
            evaluator_system += f"\n\n--- CHÍNH SÁCH LẬP TRÌNH BẮT BUỘC (CODING_POLICY.md) ---\n{self.policy_content}"
        
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
            response = (
                "### BÁO CÁO PHÂN TÍCH CỦA ADVERSARIAL EVALUATOR (GIẢ LẬP)\n\n"
                "1. **Design Quality (8.5/10)**: Mã nguồn tuân thủ Clean Architecture rất tốt. Các thực thể được tạo đúng Domain lớp.\n"
                "2. **Originality & Best Practices (8.0/10)**: Sử dụng Primary Constructor và File-Scoped namespace đẹp.\n"
                "3. **Craft & Polish (8.0/10)**: XML comments đầy đủ cho các public properties.\n"
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
                self.llm_router.current_role = "Evaluator"
                response = self.llm_router.call_llm(eval_prompt, evaluator_system, self._build_cache_helper)
            except Exception as e:
                print(f"[Harness Evaluator Error]: Không gọi được LLM Evaluator: {e}")
                # Sửa P0: Thay đổi điểm mặc định khi LLM Evaluator lỗi thành 0.0 thay vì 8.0
                response = f"{e}\n```json\n{{\"design_quality\": 0.0, \"best_practices\": 0.0, \"craft\": 0.0, \"functionality\": 0.0, \"weighted_score\": 0.0}}\n```"
                
        # Parse weighted_score từ JSON block
        weighted_score = 0.0
        try:
            json_match = re.search(r"```json\s*(.*?)\s*```", response, re.DOTALL)
            if json_match:
                score_data = json.loads(json_match.group(1))
                weighted_score = float(score_data.get("weighted_score", 0.0))
            else:
                matches = re.findall(r'"weighted_score":\s*([\d.]+)', response)
                if matches:
                    weighted_score = float(matches[-1])
        except Exception as e:
            print(f"[Harness Evaluator Warning]: Không trích xuất được điểm số JSON: {e}")
            # Sửa P0: Thay đổi điểm mặc định khi parse lỗi thành 0.0 thay vì 7.0
            weighted_score = 0.0
            
        return weighted_score, response

    def selective_rollback(self) -> str:
        """Khôi phục có chọn lọc: chỉ rollback production code, giữ nguyên test cases và docs."""
        return selective_rollback(self.modified_files)

    def generate_directory_tree(self) -> str:
        """Tự động quét cấu trúc thư mục dự án và tạo sơ đồ dạng cây để Agent hiểu kiến trúc và tránh sai đường dẫn."""
        harness_dir = os.path.dirname(os.path.abspath(__file__))
        scripts_dir = os.path.dirname(harness_dir)
        root_dir = os.path.dirname(scripts_dir)
        return generate_directory_tree(root_dir)

    def run_policy_validation(self) -> tuple[int, str]:
        """Chạy script kiểm tra coding policy tĩnh."""
        harness_dir = os.path.dirname(os.path.abspath(__file__))
        scripts_dir = os.path.dirname(harness_dir)
        root_dir = os.path.dirname(scripts_dir)
        try:
            # Sửa P0: Loại bỏ shell=True
            cmd = ['powershell.exe', '-ExecutionPolicy', 'Bypass', '-Command', './scripts/final-check.ps1 validate-policy']
            result = subprocess.run(
                cmd,
                shell=False,
                cwd=root_dir,
                capture_output=True,
                encoding='utf-8',
                errors='replace',
                timeout=60
            )
            output = (result.stdout or "") + "\n" + (result.stderr or "")
            return result.returncode, output
        except Exception as e:
            return -1, f"Lỗi thực thi validate-policy: {str(e)}"

    def condense_message_history(self, messages: list[dict]) -> list[dict]:
        """
        Nén lịch sử hội thoại dạng mảng messages để tiết kiệm token.
        Giữ nguyên tin nhắn user ban đầu và 2 lượt gần nhất. Thu gọn các lượt cũ hơn.
        """
        if len(messages) <= 6:
            return messages
        
        condensed = []
        # Luôn giữ nguyên tin nhắn user đầu tiên (chứa context + task description)
        condensed.append(messages[0])
        
        # Xác định các cặp (assistant + tool) messages ở giữa để nén
        # Giữ nguyên 4 messages cuối (~ 2 lượt tool call gần nhất)
        middle_messages = messages[1:-4]
        tail_messages = messages[-4:]
        
        for msg in middle_messages:
            if msg["role"] == "tool":
                content = msg.get("content", "")
                # Nén nội dung OBSERVATION quá dài từ các lượt cũ
                if len(content) > 300:
                    # Giữ lại STATUS và SUMMARY, cắt bỏ DETAILS dài
                    status_match = re.search(r"STATUS:\s*(\S+)", content)
                    summary_match = re.search(r"SUMMARY:\s*(.*?)(?=\n|$)", content)
                    status = status_match.group(1) if status_match else "UNKNOWN"
                    summary = summary_match.group(1) if summary_match else "Đã thực thi"
                    
                    condensed_msg = copy.deepcopy(msg)
                    condensed_msg["content"] = (
                        f"=== OBSERVATION (ĐÃ NÉN) ===\n"
                        f"STATUS: {status}\n"
                        f"SUMMARY: {summary}\n"
                        f"(Chi tiết đã lược bỏ để tiết kiệm token)\n"
                        f"===================\n"
                    )
                    condensed.append(condensed_msg)
                else:
                    condensed.append(msg)
            else:
                condensed.append(msg)
        
        # Giữ nguyên các messages gần nhất
        condensed.extend(tail_messages)
        return condensed

    def distill_and_persist_lessons(self, task_description: str):
        """Tự động phân tích lịch sử log chạy và báo cáo đánh giá để đúc kết bài học kinh nghiệm vào harness_lessons.md."""
        harness_dir = os.path.dirname(os.path.abspath(__file__))
        scripts_dir = os.path.dirname(harness_dir)
        root_dir = os.path.dirname(scripts_dir)
        
        def call_llm_helper(prompt, system_instruction, role):
            self.llm_router.current_role = role
            return self.llm_router.call_llm(prompt, system_instruction, self._build_cache_helper)
            
        distill_and_persist_lessons(
            task_description=task_description,
            mock_mode=self.mock_mode,
            log_file=self.log_file,
            evals_dir=self.evals_dir,
            lessons_path=self.lessons_path,
            call_llm_func=call_llm_helper
        )

    def print_tool_call(self, tool_call: dict, thought: str = ""):
        """Hiển thị tool call từ Native Function Calling một cách trực quan trên terminal."""
        func = tool_call["function"]
        name = func["name"]
        args = func["arguments"]
        
        if thought:
            print(f"\n🧠 [THOUGHT]: {thought}")
            
        print(f"🎬 [ACTION]: 🛠  {name}")
        
        if name == "write_source":
            print(f"   ├─ 📂 Đường dẫn: {args.get('file_path', '')}")
            print(f"   └─ 📝 Nội dung: [Mã nguồn được ghi - Xem chi tiết tại Preview]")
        elif name == "view_source":
            filepath = args.get('file_path', '')
            start = args.get('start_line', '')
            end = args.get('end_line', '')
            range_info = f" (dòng {start}-{end})" if start and end else ""
            print(f"   └─ 📂 Đường dẫn: {filepath}{range_info}")
        elif name == "execute_command":
            print(f"   └─ 💻 Lệnh chạy: '{args.get('command', '')}'")
        elif name == "finish_task":
            summary = args.get('summary', '')
            summary_clean = summary.replace("\\n", "\n").replace("\n", "\n      ")
            print(f"   └─ 🏁 Báo cáo kết quả:\n      {summary_clean}")
        else:
            print(f"   └─ ⚙️ Tham số: {args}")

    def run_agent_loop(self, role: str, system_instruction: str, initial_context: str, on_finish_callback) -> str:
        """Thực thi vòng lặp ReAct (Suy nghĩ -> Hành động -> Quan sát) cho một Agent cụ thể, sử dụng Native Function Calling."""
        print(f"\n=============================================================")
        print(f"🤖 KÍCH HOẠT AGENT: [{role.upper()}]")
        print(f"=============================================================")
        
        harness_dir = os.path.dirname(os.path.abspath(__file__))
        scripts_dir = os.path.dirname(harness_dir)
        root_dir = os.path.dirname(scripts_dir)
        
        self.current_role = role
        self.iteration_count = 0
        self.action_history = []
        self.write_history = {}
        self.test_failure_history = []
        
        # Khởi tạo messages array với tin nhắn user đầu tiên
        messages = [
            {"role": "user", "content": initial_context}
        ]
        
        while self.max_iterations < 0 or self.iteration_count < self.max_iterations:
            if self.iteration_count > 0:
                time.sleep(2)
            
            # Nén lịch sử nếu quá dài
            optimized_messages = self.condense_message_history(messages)
            
            # Gọi LLM với Native Function Calling
            response = self.call_llm_with_tools(optimized_messages, system_instruction)
            
            if response is None:
                print(f"\n🚨 [Harness Error]: LLM trả về None response. Dừng agent {role}.")
                return ""
            
            thought = response.get("thought", "")
            tool_calls = response.get("tool_calls", [])
            
            # Nếu LLM không trả về tool call nào, yêu cầu retry
            if not tool_calls:
                print(f"[Harness Warning]: LLM không gọi tool nào ở lượt {self.iteration_count}. Yêu cầu gọi lại...")
                # Append assistant message (chỉ có text) và user message yêu cầu gọi tool
                if thought:
                    messages.append({"role": "assistant", "content": thought})
                messages.append({
                    "role": "user",
                    "content": "Bạn BẮT BUỘC phải gọi một trong các tool: view_source, write_source, execute_command, hoặc finish_task. Hãy chọn một tool và gọi ngay."
                })
                self.iteration_count += 1
                continue
            
            # Lấy tool call đầu tiên (chỉ xử lý 1 tool mỗi lượt)
            tool_call = tool_calls[0]
            action_name = tool_call["function"]["name"]
            action_args = tool_call["function"]["arguments"]
            if isinstance(action_args, str):
                try:
                    action_args = json.loads(action_args)
                except json.JSONDecodeError:
                    action_args = {}
            
            # Hiển thị tool call
            self.print_tool_call(tool_call, thought)
            
            # Ghi log
            with open(self.log_file, "a", encoding="utf-8") as f:
                f.write(f"\n[{role} Lượt {self.iteration_count}]:\n")
                f.write(f"THOUGHT: {thought}\n")
                f.write(f"TOOL CALL: {action_name}({json.dumps(action_args, ensure_ascii=False)[:500]})\n")
            
            # === PHÁT HIỆN VÒNG LẶP (Loop Detection) ===
            current_action = (action_name, json.dumps(action_args, sort_keys=True, ensure_ascii=False)[:200])
            self.action_history.append(current_action)
            
            loop_warning = ""
            if len(self.action_history) >= 4 and self.action_history[-1] == self.action_history[-2] == self.action_history[-3] == self.action_history[-4]:
                print("\n🚨 [Harness Early Exit]: Phát hiện vòng lặp hành động trùng lặp liên tiếp 4 lần. Dừng Agent để tránh lãng phí token.")
                rollback_log = self.selective_rollback()
                print(f"⚠️ Hậu quả Rollback:\n{rollback_log}")
                return "FAIL: Đã kích hoạt ngắt khẩn cấp do vòng lặp hành động trùng lặp."
            elif len(self.action_history) >= 3 and self.action_history[-1] == self.action_history[-2] == self.action_history[-3]:
                loop_warning = (
                    "\n🚨 [CẢNH BÁO VÒNG LẶP HỆ THỐNG]: Bạn đang thực hiện chính xác cùng một thao tác liên tục và nhận cùng kết quả/lỗi.\n"
                    "Hãy đổi chiến thuật ngay! Bạn KHÔNG được lặp lại hành động này nữa. Hãy làm một trong các việc sau:\n"
                    "  1. Sử dụng 'view_source' để đọc lại file bị lỗi để xem cấu trúc và nội dung thực tế trước khi sửa.\n"
                    "  2. Đọc các file liên quan khác để tìm giải pháp hoặc namespace đúng.\n"
                    "  3. Thực hiện thay đổi khác (ví dụ: tạo stub class trước) thay vì lặp lại thao tác lỗi."
                )

            # Kiểm tra trùng nội dung ghi file
            if action_name == "write_source" and action_args.get("content"):
                filepath = action_args.get("file_path", "")
                content = action_args.get("content", "")
                content_hash = hash(content)
                if filepath not in self.write_history:
                    self.write_history[filepath] = []
                self.write_history[filepath].append(content_hash)
                
                if self.write_history[filepath].count(content_hash) >= 3:
                    print(f"\n🚨 [Harness Early Exit]: Phát hiện ghi trùng nội dung vào '{filepath}' 3 lần. Dừng Agent để tránh lãng phí token.")
                    rollback_log = self.selective_rollback()
                    print(f"⚠️ Hậu quả Rollback:\n{rollback_log}")
                    return "FAIL: Đã kích hoạt ngắt khẩn cấp do lặp lại nội dung ghi file."
                elif self.write_history[filepath].count(content_hash) >= 2:
                    loop_warning = (
                        f"\n🚨 [CẢNH BÁO VÒNG LẶP HỆ THỐNG]: Bạn đang ghi đè chính xác cùng một nội dung vào tệp '{filepath}' lần thứ {self.write_history[filepath].count(content_hash)}.\n"
                        "Mã nguồn này đã được áp dụng trước đó và không giúp vượt qua kiểm thử. Bạn KHÔNG được tiếp tục ghi đè nội dung này.\n"
                        "Hãy đổi chiến thuật: đọc kỹ stack trace lỗi, kiểm tra lại Mock setups, hoặc đọc các file kiểm thử tương ứng để hiểu logic."
                    )
            
            # === THỰC THI TOOL ===
            observation = ""
            
            if self.mock_mode:
                if action_name == "finish_task":
                    finish_msg = action_args.get("summary", "")
                    success, feedback = on_finish_callback(finish_msg)
                    if success:
                        print(f"✅ [Agent {role}] hoàn thành xuất sắc nhiệm vụ.")
                        return finish_msg
                    else:
                        observation = format_observation(
                            status="ERROR",
                            summary="Người đánh giá yêu cầu chỉnh sửa.",
                            details=feedback,
                            next_actions=["Điều chỉnh thiết kế hoặc sửa lỗi theo ý kiến góp ý."]
                        )
                else:
                    observation = self.execute_mock_action(action_name, action_args)
            else:
                if action_name == "execute_command":
                    cmd = action_args.get("command", "")
                    allowed_cmds = self.allowed_cmds
                    is_git = cmd.startswith("git ") and any(sub in cmd for sub in ["status", "add", "diff"])
                    
                    if "commit" in cmd.lower():
                        observation = format_observation(
                            status="ERROR",
                            summary="Lỗi bảo mật Harness.",
                            details="Harness nghiêm cấm chạy lệnh git commit. Mọi commit phải do kỹ sư con người thực hiện thủ công.",
                            next_actions=["Chỉ dùng git status, git diff hoặc git add. Không dùng git commit."]
                        )
                    elif not any(cmd.startswith(allowed) for allowed in allowed_cmds) and not is_git:
                        observation = format_observation(
                            status="ERROR",
                            summary="Lỗi bảo mật Harness.",
                            details=f"Harness nghiêm cấm chạy các lệnh tùy ý. Lệnh hợp lệ: {', '.join(allowed_cmds)} hoặc git status/diff/add.",
                            next_actions=[f"Chỉ dùng {', '.join(allowed_cmds)}, git status, git diff, git add."]
                        )
                    else:
                        is_git_change = "git " in cmd and "add" in cmd
                        if self.ask_approval(f"Agent muốn chạy lệnh hệ thống: '{cmd}'", force_ask=is_git_change):
                            code, out = run_dotnet_command(cmd)
                            status = "SUCCESS" if code == 0 else "ERROR"
                            details = out[:3000] if "build" in cmd else out[:1000]
                            test_fail_count = 0
                            if code != 0:
                                if "build" in cmd:
                                    comp_errs = extract_compiler_errors(out)
                                    if comp_errs:
                                        details += "\n\n--- CS COMPILER ERRORS ---\n" + "\n".join(comp_errs)
                                elif "test" in cmd:
                                    test_errs = extract_test_errors(out)
                                    if test_errs:
                                        test_fail_count = len(test_errs)
                                        details = "\n".join(test_errs)
                                        
                                        tags_only = []
                                        for err in test_errs:
                                            tags = [l for l in err.splitlines() if l.strip().startswith("🏷️")]
                                            tags_only.extend(tags)
                                        self.test_failure_history.append(tags_only)
                                        if len(self.test_failure_history) >= 4 and self.test_failure_history[-1] == self.test_failure_history[-2] == self.test_failure_history[-3] == self.test_failure_history[-4]:
                                            print("\n🚨 [Harness Early Exit]: Danh sách lỗi test không đổi liên tiếp 4 lần. Dừng Agent để tránh lãng phí token.")
                                            rollback_log = self.selective_rollback()
                                            print(f"⚠️ Hậu quả Rollback:\n{rollback_log}")
                                            return "FAIL: Đã kích hoạt ngắt khẩn cấp do lỗi test không thay đổi."
                                        elif len(self.test_failure_history) >= 3 and self.test_failure_history[-1] == self.test_failure_history[-2] == self.test_failure_history[-3]:
                                                loop_warning_test = (
                                                    "\n🚨 [CẢNH BÁO VÒNG LẶP HỆ THỐNG]: Danh sách test case thất bại hoàn toàn KHÔNG THAY ĐỔI sau 3 lần chạy test liên tiếp.\n"
                                                    "Điều này có nghĩa là các chỉnh sửa mã nguồn gần đây của bạn không có bất kỳ tác dụng nào đối với các lỗi kiểm thử hiện tại.\n"
                                                    "Hãy thay đổi chiến thuật: đọc kỹ stack trace lỗi, kiểm tra lại Mock setups, hoặc đọc các file kiểm thử tương ứng để hiểu logic."
                                                )
                                                details = loop_warning_test + "\n\n" + details

                            if ("build" in cmd or "test" in cmd) and code == 0:
                                val_code, val_out = self.run_policy_validation()
                                if val_code != 0:
                                    status = "ERROR"
                                    details += "\n\n🚨 CÁC LỖI VI PHẠM CODING POLICY (Tĩnh):\n" + val_out

                            observation = format_observation(
                                status=status,
                                summary=f"Chạy lệnh '{cmd}' " + ("thành công." if status == "SUCCESS" else f"thất bại ({test_fail_count} lỗi test)." if test_fail_count > 0 else "thất bại."),
                                details=details,
                                next_actions=["Đọc DETAILS bên trên. Nếu build thất bại: sửa code bằng write_source rồi build lại. HINT: lỗi namespace/convention? Dùng view_source('CODING_POLICY.md') để xem rules. Nếu thành công: gọi finish_task để kết thúc pha."]
                            )
                        else:
                            observation = format_observation(
                                status="ERROR",
                                summary="Từ chối chạy lệnh.",
                                details="Người dùng từ chối cấp quyền chạy lệnh này.",
                                next_actions=["Dùng view_source hoặc write_source thay vì execute_command, hoặc finish_task để kết thúc."]
                            )
                            
                elif action_name == "view_source":
                    filepath = action_args.get("file_path", "")
                    start_line = action_args.get("start_line")
                    end_line = action_args.get("end_line")
                    
                    normalized_name = os.path.basename(filepath).lower()
                    if normalized_name == ".env" or "appsettings" in normalized_name or ".env." in normalized_name:
                        observation = format_observation(
                            status="ERROR",
                            summary="Lỗi bảo mật Harness.",
                            details="Nghiêm cấm đọc tệp tin cấu hình hoặc bí mật (.env, appsettings).",
                            next_actions=["Đọc file source code (.cs) trong dự án thay vì file cấu hình."]
                        )
                    elif not is_path_safe(filepath, root_dir):
                        observation = format_observation(
                            status="ERROR",
                            summary="Lỗi bảo mật Harness.",
                            details="Nghiêm cấm đọc tệp tin nằm ngoài thư mục gốc dự án.",
                            next_actions=["Chỉ đọc file trong thư mục dự án: Domain/, Application/, Infrastructure/, Controllers/, FloraCore.Tests/."]
                        )
                    else:
                        content = read_source_file(filepath, start_line, end_line)
                        status = "SUCCESS" if not content.startswith("Lỗi đọc file") else "ERROR"
                        
                        # Phân trang thông minh: nếu file quá dài và không có range, gợi ý dùng phân trang
                        truncated = False
                        if start_line is None and end_line is None and len(content) > 12000:
                            total_lines = content.count('\n') + 1
                            content = content[:12000]
                            truncated = True
                            
                        details = content
                        next_acts = ["Dùng thông tin từ file này để viết/sửa code, hoặc đọc thêm file khác."]
                        if truncated:
                            next_acts = [
                                f"File này có {total_lines} dòng và đã bị cắt ngắn. Dùng view_source(file_path, start_line, end_line) để đọc phần tiếp theo.",
                                "Dùng thông tin đã đọc được để tiếp tục công việc."
                            ]
                        
                        observation = format_observation(
                            status=status,
                            summary=f"Đọc file '{filepath}'" + (f" (dòng {start_line}-{end_line})" if start_line else "") + (" thành công." if status == "SUCCESS" else " thất bại."),
                            details=details,
                            artifacts=[filepath],
                            next_actions=next_acts
                        )
                    
                elif action_name == "write_source":
                    filepath = action_args.get("file_path", "")
                    content = action_args.get("content", "")
                    
                    if not filepath or not content:
                        observation = format_observation(
                            status="ERROR",
                            summary="write_source missing parameters",
                            details="Hãy dùng write_source với đầy đủ hai parameter: file_path và content. Cả hai không được bỏ trống.",
                            next_actions=["Gọi lại write_source với đúng cú pháp (đầy đủ file_path và content)."]
                        )
                    elif not content.strip():
                        observation = format_observation(
                            status="ERROR",
                            summary="write_source empty content string",
                            details="Parameter content không được để trống (chỉ chứa khoảng trắng). Nếu bạn muốn tạo file trống, hãy viết một nội dung hợp lệ.",
                            next_actions=["Gọi lại write_source với content không rỗng."]
                        )
                    elif not is_path_safe(filepath, root_dir):
                        observation = format_observation(
                            status="ERROR",
                            summary="Lỗi bảo mật Harness.",
                            details="Nghiêm cấm ghi tệp tin nằm ngoài thư mục gốc dự án.",
                            next_actions=["Chỉ ghi file trong thư mục dự án: Domain/, Application/, Infrastructure/, Controllers/, FloraCore.Tests/."]
                        )
                    else:
                        try:
                            rel_path = os.path.relpath(os.path.abspath(filepath), root_dir)
                        except Exception:
                            rel_path = filepath
                        normalized_path = rel_path.lower().replace("\\", "/")
                        
                        if "ai_developer_harness.py" in normalized_path or "harness/" in normalized_path or ".env" in normalized_path or "appsettings" in normalized_path:
                            observation = format_observation(
                                status="ERROR",
                                summary="Lỗi bảo mật Harness.",
                                details="Nghiêm cấm sửa đổi file cấu hình hệ thống, file harness hoặc appsettings.",
                                next_actions=["Chỉ ghi file source code (.cs) trong dự án."],
                                artifacts=[filepath]
                            )
                        elif role == "Planner" and not (normalized_path.startswith("docs/") or "execution_plan.md" in normalized_path):
                            observation = format_observation(
                                status="ERROR",
                                summary="Ràng buộc vai trò Planner.",
                                details="Với vai trò Planner, bạn chỉ được phép ghi kế hoạch/đặc tả vào thư mục docs/ hoặc file plan. Không được sửa đổi mã nguồn.",
                                next_actions=["Chỉ ghi file trong docs/ hoặc file execution_plan.md."],
                                artifacts=[filepath]
                            )
                        elif role == "TestWriter" and not ("test" in normalized_path or "tests" in normalized_path):
                            observation = format_observation(
                                status="ERROR",
                                summary="Ràng buộc vai trò TestWriter.",
                                details="Với vai trò TestWriter, bạn chỉ được phép ghi hoặc sửa đổi file test (nằm trong thư mục tests hoặc tên file chứa 'test'). Không sửa đổi code production.",
                                next_actions=["Chỉ ghi file trong thư mục FloraCore.Tests/."],
                                artifacts=[filepath]
                            )
                        else:
                            print(f"\n==========================================")
                            print(f"📄 PREVIEW FILE CẦN GHI: '{filepath}' ({role})")
                            print(f"==========================================")
                            print(content[:500] + ("\n...[còn tiếp]..." if len(content) > 500 else ""))
                            print(f"==========================================\n")
                            
                            if self.ask_approval(f"Đồng ý cho Agent ghi file: '{filepath}'?"):
                                res = write_source_file(filepath, content)
                                status = "SUCCESS" if res == "Ghi file thành công." else "ERROR"
                                if status == "SUCCESS":
                                    self.modified_files.add(os.path.abspath(filepath))
                                observation = format_observation(
                                    status=status,
                                    summary=res,
                                    details=res,
                                    artifacts=[filepath],
                                    next_actions=["Tiếp tục: viết thêm file production code khác, hoặc gọi execute_command('dotnet build FloraCore.csproj') để kiểm tra build."]
                                )
                            else:
                                observation = format_observation(
                                    status="ERROR",
                                    summary="Từ chối ghi file.",
                                    details="Người dùng từ chối ghi tệp tin lên đĩa cứng.",
                                    next_actions=["Kiểm tra lại filepath hoặc nội dung, dùng write_source với thông tin khác."],
                                    artifacts=[filepath]
                                )
                            
                elif action_name == "finish_task":
                    finish_msg = action_args.get("summary", "")
                    success, feedback = on_finish_callback(finish_msg)
                    if success:
                        return finish_msg
                    else:
                        observation = format_observation(
                            status="ERROR",
                            summary="Yêu cầu sửa đổi từ Kỹ sư/Evaluator.",
                            details=feedback,
                            next_actions=["Dựa trên feedback chi tiết trong DETAILS, hãy chỉnh sửa lại."],
                            artifacts=["feedback_from_evaluator"]
                        )
                else:
                    observation = format_observation(
                        status="ERROR",
                        summary="Công cụ không hợp lệ.",
                        details=f"Không hỗ trợ tool '{action_name}'.",
                        next_actions=["Chỉ dùng: view_source, write_source, execute_command, finish_task."]
                    )
            
            # Chèn loop_warning vào observation nếu có
            if loop_warning:
                observation = observation.replace("=== OBSERVATION ===", f"=== OBSERVATION ===\n{loop_warning}")
             
            # Ghi log observation
            with open(self.log_file, "a", encoding="utf-8") as f:
                f.write(f"\nOBSERVATION:\n{observation}\n")
                
            # === CẬP NHẬT MESSAGES ARRAY ===
            # 1. Append assistant message (thought + tool_call)
            assistant_msg = {"role": "assistant"}
            if thought:
                assistant_msg["content"] = thought
            assistant_msg["tool_calls"] = [{
                "id": tool_call["id"],
                "function": {
                    "name": action_name,
                    "arguments": action_args
                }
            }]
            messages.append(assistant_msg)
            
            # 2. Append tool result message
            # Nén observation nếu là lệnh thành công dài
            condensed_observation = observation
            if action_name == "execute_command" and "SUCCESS" in observation and len(observation) > 1500:
                cmd = action_args.get("command", "")
                condensed_observation = format_observation(
                    status="SUCCESS",
                    summary=f"Thực thi lệnh '{cmd}' thành công.",
                    details="(Chi tiết log thành công đã lược bỏ.)",
                    next_actions=["Đọc kết quả bên trên. Nếu cần chạy dotnet test hoặc finish_task."]
                )
                    
            messages.append({
                "role": "tool",
                "tool_call_id": tool_call["id"],
                "name": action_name,
                "content": condensed_observation
            })
            
            # Nâng thêm max_iterations nếu finish_task bị reject và đang ở chế độ giới hạn lượt
            if action_name == "finish_task" and self.max_iterations >= 0:
                # finish_task đã được xử lý ở trên (return nếu success), nếu tới đây nghĩa là bị reject
                self.max_iterations = max(self.max_iterations, self.iteration_count + 3)
                
            self.iteration_count += 1
                
        print(f"⚠️ [Harness Alert]: Agent {role} đạt giới hạn lặp tối đa.")
        return ""

    def execute_pipeline(self, task_description: str):
        """Thực thi toàn bộ quy trình: Lập Plan -> Viết Test -> Lập trình -> Đánh giá đối nghịch."""
        print(f"\n=============================================================")
        print(f"🚀 KHỞI ĐỘNG SPEC-DRIVEN PIPELINE CHO TÁC VỤ:")
        print(f"   👉 '{task_description}'")
        print(f"=============================================================")
        
        harness_dir = os.path.dirname(os.path.abspath(__file__))
        scripts_dir = os.path.dirname(harness_dir)
        root_dir = os.path.dirname(scripts_dir)
        
        # 1. Đọc chính sách lập trình cục bộ
        for p_file in self.policy_files:
            p_path = os.path.join(root_dir, p_file)
            if os.path.exists(p_path):
                try:
                    with open(p_path, "r", encoding="utf-8") as f:
                        self.policy_content = f.read()
                    print(f"📖 [Harness Control]: Đang nạp chính sách lập trình mới nhất từ '{p_file}'...")
                    break
                except Exception as e:
                    print(f"⚠️ [Harness Control]: Lỗi đọc file {p_file}: {e}")

        # 2. Xây dựng System Instructions cho từng vai trò
        # LƯU Ý: Đã loại bỏ react_instruction cũ (THOUGHT: ... ACTION: ...)
        # vì Native Function Calling tự xử lý format qua tool definitions.
        tool_usage_note = (
            "\n\n--- HƯỚNG DẪN SỬ DỤNG CÔNG CỤ ---\n"
            "Bạn có quyền sử dụng các công cụ sau thông qua Native Function Calling:\n"
            "1. view_source(file_path, start_line?, end_line?) — Đọc file. Hỗ trợ phân trang (start_line, end_line) cho file dài.\n"
            "2. write_source(file_path, content) — Ghi file mới hoặc ghi đè file có sẵn.\n"
            "3. execute_command(command) — Chạy lệnh. Chỉ: dotnet build/test/restore/clean, git status/diff/add.\n"
            "4. finish_task(summary) — Kết thúc pha với báo cáo.\n"
            "🚨 MỖI LƯỢT CHỈ GỌI ĐÚNG 1 TOOL. Luôn suy nghĩ trước khi hành động.\n"
            "---\n"
        )

        architecture_guidelines = (
            "\n\n--- KIẾN TRÚC DỰ ÁN ---\n"
            "Clean Architecture: Domain -> Application -> Infrastructure -> Controllers.\n"
            "Xem directory tree và các file .cs hiện có trong codebase để biết namespace convention.\n"
            f"Tham khảo [docs/guides/DDD_GUIDE.md](file:///{root_dir.replace(chr(92), '/')}/docs/guides/DDD_GUIDE.md) cho design guidelines chi tiết.\n"
            "---\n"
        )

        planner_system = (
            "Bạn là một AI Software Architect cực kỳ thông minh, làm việc trên dự án .NET 9 FloraCore (Clean Architecture).\n"
            "Nhiệm vụ của bạn là lập kế hoạch thực thi (AI Execution Plan) chi tiết và phân rã các task nhỏ để phân phối cho các Agent khác.\n"
            "Bạn cần khảo sát cấu trúc dự án (bằng cách đọc các file liên quan nếu cần) và xây dựng một kế hoạch bao gồm:\n"
            "- Phân tích kiến trúc: Cần tạo mới/chỉnh sửa các thực thể (Domain), DTO/Queries/Commands/Handlers (Application), AppDbContext/Repository (Infrastructure), Controllers (Web/API).\n"
            "- Đặc tả chi tiết các thay đổi.\n"
            "- Danh sách các test cases cần viết trước (TDD).\n"
            "Sau khi hoàn thành bản kế hoạch, bạn BẮT BUỘC phải gọi `finish_task` với nội dung kế hoạch chi tiết bằng Markdown để Kỹ sư con người phê duyệt.\n"
            "Chính sách lập trình bắt buộc: dùng view_source để đọc CODING_POLICY.md nếu cần.\n"
            "Bài học kinh nghiệm: dùng view_source để đọc scripts/harness_lessons.md nếu cần."
        )
        planner_system += architecture_guidelines
        planner_system += tool_usage_note

        testwriter_system = (
            "Bạn là một QA/Developer chuyên viết unit test và integration test sử dụng xUnit và FluentAssertions cho dự án .NET 9 FloraCore.\n"
            "Nhiệm vụ của bạn là hiện thực hóa các kịch bản test theo Bản kế hoạch (Execution Plan) đã được phê duyệt.\n"
            "Bạn chỉ được phép ghi hoặc sửa đổi các tệp kiểm thử trong thư mục kiểm thử (ví dụ: FloraCore.Tests hoặc các file có hậu tố Tests.cs).\n"
            "Tuyệt đối KHÔNG ĐƯỢC sửa đổi bất kỳ tệp code production nào (trong Domain, Application, Infrastructure, Controllers).\n"
            "Sau khi viết xong các test cases, hãy chạy `dotnet build` để đảm bảo chúng biên dịch thành công. LƯU Ý: các test cases có thể thất bại khi chạy vì code production chưa được viết.\n"
            "🚨 Trước khi viết test cho bất kỳ class nào: PHẢI dùng `view_source` để đọc mã nguồn production nhằm kiểm tra chính xác namespace, constructor signature và methods.\n"
            f"Mẫu cấu trúc test: xem [FloraCore.Tests/Application/WebsiteInfo/](file:///{root_dir.replace(chr(92), '/')}/FloraCore.Tests/Application/WebsiteInfo/).\n"
            "\n--- 🧪 TEST PATTERNS (đọc để tránh lỗi thường gặp) ---\n"
            "1. Mock IQueryable + async: Handler gọi ToListAsync(). Dùng `source.AsAsyncQueryable()` thay vì `source.AsQueryable()`.\n"
            "   System.Linq.AsyncQueryable không support với List.AsQueryable().\n"
            "2. Date timing: KHÔNG dùng DateTime.Now trực tiếp trong seed và filter. Dùng `var today = DateTime.Today;` 1 biến chung.\n"
            "3. Integration Auth: Route prefix PHẢI là /api/v1/ (VD: /api/v1/auth/login). User PHẢI register trước login.\n"
            "4. ApiResponse unwrap: Controller trả về ApiResponse<T>. Dùng .Data để lấy body thật. Cần using System.Net.Http.Json.\n"
            "5. Seed data FK: Order.UserId PHẢI là Guid của user đã seed. ShippingAddress NOT NULL.\n"
            "6. Custom factory: Dùng CustomWebApplicationFactory (IClassFixture), không WebApplicationFactory<Program>.\n"
            "---\n"
            "Đọc scripts/harness_lessons.md để biết thêm chi tiết từng pattern.\n"
            "Chính sách lập trình: đọc CODING_POLICY.md nếu cần.\n"
            "Bài học kinh nghiệm: đọc scripts/harness_lessons.md nếu cần.\n"
            "Gọi `finish_task` với báo cáo các file test đã viết khi hoàn thành."
        )
        testwriter_system += architecture_guidelines
        testwriter_system += tool_usage_note

        developer_system = (
            "Bạn là một Kỹ sư .NET 9 Senior, làm việc trên dự án FloraCore (Clean Architecture + CQRS + MediatR).\n"
            "Nhiệm vụ của bạn là hiện thực hóa logic nghiệp vụ trong các lớp Domain, Application, Infrastructure, Controllers dựa trên Bản kế hoạch (Execution Plan) và vượt qua toàn bộ các test cases đã được viết.\n"
            "Hãy tuân thủ nghiêm ngặt chính sách lập trình. Nếu build báo lỗi CS0246 (namespace) hoặc CS1729 (constructor signature), hãy dùng view_source('CODING_POLICY.md') để xem rules cụ thể.\n"
            "\n🚨 NGUYÊN TẮC CHẨN ĐOÁN LỖI (ROOT CAUSE ANALYSIS - RCA):\n"
            "Trước khi thực hiện bất kỳ chỉnh sửa mã nguồn nào để fix build hoặc test, bạn BẮT BUỘC phải phân tích và mô tả rõ trong phần suy nghĩ (thought):\n"
            "  1. SYMPTOMS: Lỗi thực tế và thông báo lỗi nhận được là gì?\n"
            "  2. TRACE: Lỗi xảy ra ở dòng nào trong code production/test, đường đi của call stack?\n"
            "  3. ROOT CAUSE: Tại sao lỗi xảy ra (lệch kiểu dữ liệu, thiếu đăng ký Dependency Injection, logic nghiệp vụ bị sai lệch)?\n"
            "  4. RESOLUTION: Giải pháp sửa đổi triệt để từ gốc rễ thay vì sửa đổi bề nổi.\n"
            "⚠️ TUÂN THỦ: Tuyệt đối không hardcode dữ liệu giả của test case vào code production (ví dụ: `if (id == 99)`). Mọi sửa đổi phải giải quyết triệt để logic nghiệp vụ từ gốc.\n"
            "\n🚨 CHIẾN LƯỢC PHÁT TRIỂN:\n"
            "BƯỚC 1 - VIẾT HẾT CODE PRODUCTION:\n"
            "   - Đọc kế hoạch (Execution Plan) để biết danh sách các file production code cần tạo (DTO/Query/Handler/Repository/Controller).\n"
            "   - Dùng `write_source` để viết lần lượt từng file production code vào đúng đường dẫn.\n"
            "   - KHÔNG được gọi `execute_command` trong Bước 1.\n"
            "\nBƯỚC 2 - BUILD CHỈ ĐỊNH (production-only):\n"
            "   - Gọi `execute_command('dotnet build FloraCore.csproj')` để chỉ build project production.\n"
            "   - Nếu build FAIL: đọc compiler errors (CSxxxx) từ OBSERVATION, dùng view_source + write_source để sửa, build lại.\n"
            "\nBƯỚC 3 - CHẠY TEST (chỉ sau khi Build Success):\n"
            "   - Gọi `execute_command('dotnet test --filter FullyQualifiedName~<FEATURE_KEYWORD>')` để chạy test.\n"
            "   - Thay <FEATURE_KEYWORD> bằng keyword rút từ Execution Plan hoặc tên feature (VD: statistics, cart, product, auth, order, user, post, category, search, website, chat, file, inbox).\n"
            "\n🚨 DEBUG INTEGRATION TEST (batch — 1 lần đọc hết):\n"
            "   OBSERVATION sẽ hiển thị TẤT CẢ lỗi test + stack trace + classification tag (🏷️).\n"
            "   Hãy làm như sau trong MỘT lần:\n"
            "   1. Đọc HẾT các lỗi + tag để tìm pattern chung (vd: toàn bộ là 404 → sai route prefix; toàn bộ là FK → thiếu seed data).\n"
            "   2. Nếu pattern là \"cùng một root cause\" → sửa 1 lần cho tất cả (vd: sửa route từ /api/auth → /api/v1/auth).\n"
            "   3. Nếu lỗi độc lập → ưu tiên sửa lỗi DB/HTTP trước (thường là root cause), rồi ASSERT cuối.\n"
            "   4. KHÔNG chạy lại test sau mỗi lỗi — chỉ chạy lại SAU KHI sửa xong tất cả.\n"
            "   ⚠️  KHÔNG ghi đè file test. Chỉ sửa production code hoặc test infrastructure (factory, seed).\n"
            "   🏷️  Tag classification giúp bạn biết ngay hướng fix mà không cần đọc stack trace:\n"
            "       HTTP 404 → sai URL (thiếu version prefix)\n"
            "       HTTP 401 → sai credential (user chưa register)\n"
            "       DB FOREIGN_KEY → seed data thiếu reference\n"
            "       JSON KeyNotFound → chưa unwrap ApiResponse.Data\n"
            "       SYSTEM: InvalidOperation → IQueryable không support async (dùng AsAsyncQueryable thay AsQueryable)\n"
            "\n--- 🧪 TEST PATTERNS (đọc để fix nhanh) ---\n"
            "1. Mock IQueryable: Handler gọi ToListAsync(). Nếu test dùng .AsQueryable() → crash IAsyncEnumerable. Dùng .AsAsyncQueryable().\n"
            "2. Date timing: Dùng DateTime.Today thay DateTime.Now để tránh micro-giây lệch.\n"
            "3. Route prefix: PHẢI là /api/v1/ (không /api/).\n"
            "4. ApiResponse wrap: Controller trả về ApiResponse<T>. Mở bằng .Data.\n"
            "5. Seed FK: UserId phải khớp user đã seed. ShippingAddress NOT NULL.\n"
            "6. Factory: Dùng CustomWebApplicationFactory.\n"
            "Đọc scripts/harness_lessons.md để biết thêm chi tiết từng pattern.\n"
            "\nBƯỚC 4 - BÁO CÁO:\n"
            "   - Chỉ khi Bước 2 và 3 đều thành công, gọi `finish_task`.\n"
            "\n🚨 QUY TẮC PHÁT TRIỂN QUAN TRỌNG:\n"
            "- BẮT BUỘC C# 12+ Primary Constructors cho DI classes.\n"
            "- BẮT BUỘC `ArgumentNullException.ThrowIfNull(dependency)` đầu constructor.\n"
            "- Tham khảo file mẫu:\n"
            f"  * [GenericRepository.cs](file:///{root_dir.replace(chr(92), '/')}/Infrastructure/Repositories/GenericRepository.cs)\n"
            f"  * [ProductRepository.cs](file:///{root_dir.replace(chr(92), '/')}/Infrastructure/Repositories/ProductRepository.cs)\n"
            f"  * [UnitOfWork.cs](file:///{root_dir.replace(chr(92), '/')}/Infrastructure/Data/UnitOfWork.cs)\n"
            f"  * Handlers in [Application/Features/WebsiteInfo/Commands](file:///{root_dir.replace(chr(92), '/')}/Application/Features/WebsiteInfo/Commands/)\n"
            f"  * Controller mẫu: [OrdersController.cs](file:///{root_dir.replace(chr(92), '/')}/Controllers/OrdersController.cs) (Primary Constructor)\n"
            "Bài học kinh nghiệm: đọc scripts/harness_lessons.md nếu cần.\n"
        )
        developer_system += architecture_guidelines
        developer_system += tool_usage_note

        # --- CALLBACK PHA 1: PLANNING ---
        def on_planner_finish(plan_content: str):
            print("\n=============================================================")
            print("📋 BẢN KẾ HOẠCH THỰC THI (PROPOSED EXECUTION PLAN):")
            print("=============================================================")
            print(plan_content)
            print("=============================================================\n")
            
            plan_dir = os.path.join(root_dir, "docs", "plans")
            os.makedirs(plan_dir, exist_ok=True)
            plan_path = os.path.join(plan_dir, "execution_plan.md")
            try:
                with open(plan_path, "w", encoding="utf-8") as f:
                    f.write(plan_content)
                print(f"💾 Đã lưu kế hoạch vào: docs/plans/execution_plan.md")
            except Exception as e:
                print(f"⚠️ Không thể lưu file kế hoạch: {e}")
                
            production_files = []
            path_pattern = r"(?:Application/Features/\S+\.cs|Infrastructure/\S+\.cs|Controllers/\S+\.cs|Domain/\S+\.cs)"
            matches = re.findall(path_pattern, plan_content)
            for m in matches:
                cleaned = m.strip('`').strip()
                if cleaned not in production_files:
                    production_files.append(cleaned)
            self.planner_production_files = production_files
            self.test_filter_keyword = self.extract_test_filter_keyword(plan_content, task_description)
            print(f"🔑 [Harness]: Test filter keyword: '{self.test_filter_keyword}'")
            if production_files:
                print(f"📋 [Harness]: Phát hiện {len(production_files)} file production code cần tạo trong kế hoạch: {', '.join(production_files)}")
                
            approved = self.ask_approval("Bạn có đồng ý phê duyệt Bản kế hoạch thực thi này không?", force_ask=True)
            if approved:
                # Đọc lại bản kế hoạch từ ổ đĩa đề phòng trường hợp lập trình viên chỉnh sửa thủ công
                try:
                    if os.path.exists(plan_path):
                        with open(plan_path, "r", encoding="utf-8") as f:
                            disk_plan_content = f.read()
                        if disk_plan_content.strip() != plan_content.strip():
                            print("\n⚙️ [Harness Control]: Phát hiện bản kế hoạch đã được chỉnh sửa thủ công trên đĩa. Đang nạp lại...")
                            plan_content = disk_plan_content
                            
                            # Phân tích lại danh sách files và test filter keyword
                            production_files = []
                            matches = re.findall(path_pattern, plan_content)
                            for m in matches:
                                cleaned = m.strip('`').strip()
                                if cleaned not in production_files:
                                    production_files.append(cleaned)
                            self.planner_production_files = production_files
                            self.test_filter_keyword = self.extract_test_filter_keyword(plan_content, task_description)
                            print(f"🔄 [Harness Reloaded]: Cập nhật Test filter keyword: '{self.test_filter_keyword}'")
                            if production_files:
                                print(f"📋 [Harness Reloaded]: Cập nhật danh sách {len(production_files)} file production: {', '.join(production_files)}")
                except Exception as e:
                    print(f"⚠️ Lỗi khi nạp lại kế hoạch từ ổ đĩa: {e}")
                
                self.current_plan = plan_content
                return True, ""
            else:
                feedback = input("\n📝 Nhập ý kiến góp ý/yêu cầu sửa đổi của bạn cho bản kế hoạch:\n👉 ")
                return False, f"Kỹ sư con người từ chối phê duyệt bản kế hoạch này với ý kiến đóng góp sau:\n{feedback}"

        # --- CALLBACK PHA 2: TEST WRITING ---
        def on_testwriter_finish(test_report: str):
            print("\n=============================================================")
            print("🧪 THÔNG BÁO TỪ TESTWRITER AGENT:")
            print("=============================================================")
            print(test_report)
            print("=============================================================\n")
            
            test_file_pattern = r"FloraCore\.Tests/\S+\.cs|FloraCore\.Tests\\\S+\.cs"
            found_test_files = re.findall(test_file_pattern, test_report)
            self.test_writer_files = list(set(found_test_files))
            if self.test_writer_files:
                print(f"📋 [Harness]: Phát hiện {len(self.test_writer_files)} file test: {', '.join(self.test_writer_files)}")
            
            if self.mock_mode:
                approved = self.ask_approval("Bạn có phê duyệt bộ Test Cases này không?", force_ask=True)
                if approved:
                    return True, ""
                feedback = input("\n📝 Nhập ý kiến góp ý cho bộ test cases:\n👉 ")
                return False, f"Kỹ sư từ chối phê duyệt với phản hồi:\n{feedback}"
                
            print("⚙️ Đang biên dịch bộ test mới...")
            code, out = run_dotnet_command("dotnet build")
            if code != 0:
                print("❌ Lỗi biên dịch bộ tests!")
                errs = extract_compiler_errors(out)
                
                critical_errs = []
                for err in errs:
                    missing_symbol_codes = [
                        "CS0246", "CS0234", "CS0103", "CS1061", "CS1729", 
                        "CS0117", "CS1503", "CS0122", "CS0029", "CS1501", 
                        "CS0426", "CS0120", "CS0266"
                    ]
                    is_missing_symbol = any(code_id in err for code_id in missing_symbol_codes)
                    if not is_missing_symbol:
                        critical_errs.append(err)
                        
                if critical_errs:
                    return False, f"Lỗi biên dịch bộ test mới (Lỗi cú pháp/Cấu trúc thực tế):\n" + "\n".join(critical_errs)
                else:
                    print("ℹ️ Phát hiện lỗi biên dịch do chưa có code production tương ứng (chấp nhận ở pha TestWriter). Tiếp tục...")
                
            print("\n🔍 Git Diff của thư mục test:")
            diff_res = subprocess.run(
                ["git", "diff", "HEAD", "--", "*Tests*"],
                shell=False,
                capture_output=True,
                encoding='utf-8',
                errors='replace'
            )
            print(diff_res.stdout or "Không phát hiện thay đổi trong thư mục test.")
            
            approved = self.ask_approval("Bạn có phê duyệt bộ Test Cases trên không?", force_ask=True)
            if approved:
                return True, ""
            else:
                feedback = input("\n📝 Nhập ý kiến góp ý/yêu cầu sửa đổi cho bộ test cases:\n👉 ")
                return False, f"Kỹ sư con người không phê duyệt bộ test cases này với lý do:\n{feedback}"

        # --- CALLBACK PHA 3 & 4: IMPLEMENTATION & EVALUATION ---
        def on_developer_finish(dev_report: str):
            print("\n=============================================================")
            print("💻 THÔNG BÁO TỪ DEVELOPER AGENT:")
            print("=============================================================")
            print(dev_report)
            print("=============================================================\n")
            
            if self.mock_mode:
                score, report = self.run_gan_evaluation(task_description)
                print(f"\n🏆 THANG ĐIỂM ĐẠT ĐƯỢC: {score:.2f}/10.0 (Yêu cầu tối thiểu: {self.pass_threshold})")
                print(report)
                if score >= self.pass_threshold:
                    return True, ""
                else:
                    return False, f"Evaluator từ chối với điểm {score:.2f} < {self.pass_threshold}.\nBáo cáo:\n{report}"
                    
            print("⚙️ Chạy build hệ thống (production code)...")
            code, out = run_dotnet_command("dotnet build FloraCore.csproj")
            if code != 0:
                errs = extract_compiler_errors(out)
                missing_files = []
                if hasattr(self, 'planner_production_files') and self.planner_production_files:
                    for file in self.planner_production_files:
                        file_path = os.path.join(root_dir, file) if not os.path.isabs(file) else file
                        if not os.path.exists(file_path):
                            missing_files.append(file)
                
                if missing_files:
                    feedback_msg = "PRODUCTION CODE THIẾU FILE:\n"
                    feedback_msg += "\n".join([f"- {file}" for file in missing_files])
                    feedback_msg += "\n\nHãy dừng `finish_task` lại và sử dụng `write_source` để viết đầy đủ các file production code này trước khi build. VIẾT HẾT -> BUILD."
                    return False, feedback_msg
                
                return False, "PRODUCTION CODE BUILD FAILED. Hãy viết THÊM production code hoặc sửa lỗi compiler trước khi build lại. HINT: Lỗi namespace/convention? Dùng `view_source('CODING_POLICY.md')` để xem rules. Xem lỗi compiler bên dưới để định hướng file nào cần tạo/sửa:\n" + "\n".join(errs)
                
            print("🧹 Kiểm tra Coding Policy tĩnh...")
            val_code, val_out = self.run_policy_validation()
            if val_code != 0:
                return False, "Phát hiện lỗi vi phạm Coding Policy (Tĩnh). Dùng `view_source('CODING_POLICY.md')` để xem rules cụ thể, tìm section liên quan đến lỗi bên dưới, rồi sửa code bằng `write_source`. Chi tiết:\n" + val_out
                
            print("⚙️ Chạy tests hệ thống (chỉ chạy test liên quan đến feature mới)...")
            filter_keyword = getattr(self, 'test_filter_keyword', 'test')
            t_code, t_out = run_dotnet_command(f"dotnet test --filter FullyQualifiedName~{filter_keyword}")
            if t_code != 0:
                errs = extract_test_errors(t_out)
                tags = set()
                for e in errs:
                    for line in e.splitlines():
                        if line.strip().startswith("🏷️"):
                            tags.add(line.strip())
                tag_summary = "\n".join(sorted(tags)) if tags else ""
                return False, "TESTS FAIL: Sửa tất cả lỗi dưới đây TRONG MỘT LẦN (không chạy lại test sau mỗi lỗi).\n\n" + (tag_summary + "\n\n" if tag_summary else "") + "\n".join(errs)
                
            score, report = self.run_gan_evaluation(task_description)
            print(f"\n🏆 [EVALUATION SCORECARD]: THANG ĐIỂM ĐẠT ĐƯỢC: {score:.2f}/10.0 (Yêu cầu tối thiểu: {self.pass_threshold})")
            print(report)
            
            report_path = os.path.join(self.evals_dir, "evaluation_report.md")
            try:
                with open(report_path, "w", encoding="utf-8") as f:
                    f.write(report)
            except Exception as e:
                print(f"⚠️ Lỗi ghi report: {e}")
                
            if score >= self.pass_threshold:
                print("\n✅ Vượt qua vòng thẩm định chất lượng!")
                return True, ""
            else:
                rollback_log = self.selective_rollback()
                print(f"\n⚠️ Rollback log:\n{rollback_log}")
                return False, (
                    f"Hệ thống Evaluator từ chối phê duyệt code triển khai vì chưa đạt chuẩn chất lượng (Điểm: {score:.2f} < {self.pass_threshold}).\n"
                    f"--- BÁO CÁO PHÊ BÌNH CỦA EVALUATOR ---\n{report}\n\n"
                    f"--- HẬU QUẢ ROLLBACK ---\n{rollback_log}\n\n"
                    f"Vui lòng thiết kế hướng đi khác để hoàn thành task đạt chất lượng hơn."
                )

        # 4. CHẠY PIPELINE
        directory_tree = self.generate_directory_tree()
        context_tree_header = (
            f"SƠ ĐỒ CẤU TRÚC THƯ MỤC DỰ ÁN THỰC TẾ (Dựa vào đây để định hướng đúng đường dẫn file và namespace, tránh ghi đè nhầm hoặc tạo file rác):\n"
            f"```text\n{directory_tree}\n```\n\n"
        )
        
        # --- CHẠY PHA 1: PLANNING ---
        self.current_plan = ""
        plan = self.run_agent_loop(
            role="Planner",
            system_instruction=planner_system,
            initial_context=context_tree_header + f"Yêu cầu nhiệm vụ: {task_description}\n\nHãy khảo sát mã nguồn và lập Bản kế hoạch thực thi (AI Execution Plan) chi tiết. Sử dụng finish_task để gửi kế hoạch.",
            on_finish_callback=on_planner_finish
        )
        if not plan:
            print("❌ Pha lập kế hoạch thất bại hoặc bị ngắt sớm. Dừng pipeline.")
            return

        if not self.current_plan:
            self.current_plan = plan

        # --- CHẠY PHA 2: TEST WRITING ---
        test_report = self.run_agent_loop(
            role="TestWriter",
            system_instruction=testwriter_system,
            initial_context=context_tree_header + f"Bản kế hoạch đã duyệt:\n{self.current_plan}\n\nHãy viết các file unit/integration test phản ánh đúng đặc tả này. Chỉ sửa thư mục test. Gọi finish_task khi hoàn thành.",
            on_finish_callback=on_testwriter_finish
        )
        if not test_report:
            print("❌ Pha viết test thất bại hoặc bị ngắt sớm. Dừng pipeline.")
            return

        # --- CHẠY PHA 3 & 4: LẬP TRÌNH VÀ THẨM ĐỊNH ĐỐI NGHỊCH ---
        dev_report = self.run_agent_loop(
            role="Developer",
            system_instruction=developer_system,
            initial_context=context_tree_header + f"Bản kế hoạch:\n{self.current_plan}\n\nBáo cáo test cases:\n{test_report}\n\nHãy bắt đầu sửa code production để pass tất cả tests. Gọi finish_task khi hoàn tất.",
            on_finish_callback=on_developer_finish
        )
        if not dev_report:
            print("❌ Pha lập trình/thẩm định thất bại hoặc bị ngắt sớm. Dừng pipeline.")
            return
            
        print("\n🎉 [PIPELINE SUCCESS]: Đã hoàn thành tác vụ xuất sắc thông qua Spec-Driven Development!")
        self.distill_and_persist_lessons(task_description)
