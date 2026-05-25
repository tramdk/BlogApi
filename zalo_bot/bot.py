#!/usr/bin/env python3
"""
Zalo Bot Webhook Server for AI Developer Harness.
Chạy local web server nhận webhook từ Zalo OA và gửi tin nhắn qua OpenAPI.

Cách dùng:
  1. Tạo Zalo OA và lấy Access Token (ZALO_OA_ACCESS_TOKEN) tại https://developers.zalo.me
  2. Thêm cấu hình vào file .env
  3. Chạy bot: python zalo_bot/bot.py
  4. Dùng ngrok để expose port: ngrok http 8000
  5. Cấu hình Webhook URL trên Zalo Developer: https://<domain-ngrok>.ngrok-free.app/webhook
"""

import os
import sys
import json
import queue
import urllib.request
import urllib.parse
import threading
import traceback
import logging
from pathlib import Path
from http.server import BaseHTTPRequestHandler, HTTPServer

logging.basicConfig(
    format="%(asctime)s [%(levelname)s] %(message)s",
    level=logging.INFO,
)
logger = logging.getLogger(__name__)

PROJECT_ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(PROJECT_ROOT))

from scripts.ai_developer_harness import AIDeveloperHarness

# Lưu trữ trạng thái và harness instance theo user_id
# Mỗi user_id sẽ có: {"running": bool, "pending_approval": bool, "msg_queue": queue.Queue, "harness_thread": Thread}
_user_sessions = {}
_harness_store = {}

# ---------------------------------------------------------------------------
# Output Capture — chặn print() để gửi về Zalo
# ---------------------------------------------------------------------------

class OutputCapture:
    """Chặn sys.stdout, đẩy output vào queue.Queue để stream về Zalo."""
    def __init__(self, msg_queue: queue.Queue):
        self.msg_queue = msg_queue
        self.buffer = ""

    def write(self, text):
        self.buffer += text
        # Gửi log khi buffer đủ dài hoặc gặp xuống dòng
        if len(self.buffer) >= 1000 or (text and text[-1] in "\n\r" and len(self.buffer) >= 150):
            self.msg_queue.put(("log", self.buffer))
            self.buffer = ""

    def flush(self):
        if self.buffer:
            self.msg_queue.put(("log", self.buffer))
            self.buffer = ""

# ---------------------------------------------------------------------------
# Zalo-aware Harness — hỗ trợ duyệt thao tác qua Zalo
# ---------------------------------------------------------------------------

class ZaloHarness(AIDeveloperHarness):
    """Subclass của AIDeveloperHarness hỗ trợ HITL qua Zalo Webhook."""

    def __init__(
        self,
        msg_queue: queue.Queue,
        user_id: str,
        auto_approve: bool = True,
        force_mock: bool | None = None,
    ):
        self._msg_queue = msg_queue
        self._user_id = user_id
        self._approval_evt = threading.Event()
        self._approval_val = False
        super().__init__(auto_approve=auto_approve)
        if force_mock:
            self.mock_mode = True
            self.llm_router.mock_mode = True

    def ask_approval(self, message: str, force_ask: bool = False) -> bool:
        if self.auto_approve:
            return True
        self._approval_evt.clear()
        self._approval_val = False
        self._msg_queue.put(("approval", (message, self._user_id)))
        self._approval_evt.wait()
        return self._approval_val

# ---------------------------------------------------------------------------
# Zalo OA OpenAPI Communication
# ---------------------------------------------------------------------------

def send_zalo_message(user_id: str, text: str, buttons: list = None) -> bool:
    """Gửi tin nhắn (text hoặc kèm nút bấm) đến User qua Zalo OA OpenAPI."""
    token = os.getenv("ZALO_OA_ACCESS_TOKEN", "").strip()
    if not token:
        logger.error("ZALO_OA_ACCESS_TOKEN chưa được cấu hình.")
        return False

    url = "https://openapi.zalo.me/v3.0/oa/message/transaction"
    headers = {
        "Content-Type": "application/json",
        "access_token": token
    }

    # Zalo OA tin nhắn giao dịch có cấu trúc cơ bản
    # Nếu không có buttons, gửi tin nhắn text thông thường
    # Lưu ý: Tin nhắn dạng template/giao dịch yêu cầu thiết lập OA hợp lệ.
    # Để đơn giản và tương thích cao nhất, ta ưu tiên gửi dạng tin nhắn text.
    message_payload = {"text": text}

    # Nếu có nút bấm và OA đã được xác thực / hỗ trợ, có thể cấu hình template hoặc quick replies.
    # Để tránh lỗi định dạng template nghiêm ngặt của Zalo, ta thêm văn bản hướng dẫn trả lời nhanh
    # như là một phương án dự phòng cực kỳ tin cậy bên cạnh buttons.
    if buttons:
        # Nếu gửi kèm nút bấm (Zalo OA hỗ trợ các hành động qua Action hoặc qua Template)
        # Ở đây ta sẽ định nghĩa dạng danh sách action đơn giản nếu OA hỗ trợ.
        text += "\n\n👉 Bạn hãy chọn hoặc nhắn trả lời:\n"
        for btn in buttons:
            text += f"- Nhắn '{btn['title']}' (hoặc '{btn['payload']}')\n"
        message_payload = {"text": text}

    payload = {
        "recipient": {
            "user_id": user_id
        },
        "message": message_payload
    }

    try:
        req = urllib.request.Request(
            url,
            data=json.dumps(payload).encode("utf-8"),
            headers=headers,
            method="POST"
        )
        with urllib.request.urlopen(req, timeout=10) as response:
            res_body = response.read().decode("utf-8")
            res_data = json.loads(res_body)
            if res_data.get("error", 0) != 0:
                logger.error(f"Lỗi gửi tin nhắn Zalo: {res_data}")
                return False
            return True
    except Exception as e:
        logger.error(f"Lỗi kết nối Zalo OpenAPI: {e}")
        return False

# ---------------------------------------------------------------------------
# Streaming logs từ queue về Zalo
# ---------------------------------------------------------------------------

def stream_output_loop(
    user_id: str,
    msg_queue: queue.Queue,
    harness_thread: threading.Thread,
):
    """Chạy vòng lặp lấy logs từ Queue và gửi về Zalo."""
    import time
    session = _user_sessions.get(user_id, {})

    while harness_thread.is_alive() or not msg_queue.empty():
        try:
            item = msg_queue.get(timeout=0.5)
        except queue.Empty:
            continue

        if item is None:
            break

        typ, payload = item
        if typ == "log":
            text = payload.strip()
            if not text:
                continue
            # Chia nhỏ tin nhắn nếu dài hơn 1500 ký tự (Zalo giới hạn text message)
            for i in range(0, len(text), 1500):
                send_zalo_message(user_id, text[i : i + 1500])
                time.sleep(0.5)
        elif typ == "approval":
            message, _ = payload
            session["pending_approval"] = True
            # Gửi tin nhắn kèm hướng dẫn phản hồi bằng text
            send_zalo_message(
                user_id,
                f"🛡️ Yêu cầu phê duyệt hành động:\n\n{message}",
                buttons=[
                    {"title": "Đồng ý", "payload": "YES"},
                    {"title": "Từ chối", "payload": "NO"}
                ]
            )

    session["running"] = False
    session["pending_approval"] = False
    send_zalo_message(user_id, "✅ **Pipeline Harness đã hoàn tất!**")

# ---------------------------------------------------------------------------
# Xử lý các câu lệnh nhận được
# ---------------------------------------------------------------------------

def handle_user_command(user_id: str, text: str):
    """Xử lý lệnh từ người dùng."""
    text = text.strip()
    session = _user_sessions.setdefault(user_id, {
        "running": False,
        "pending_approval": False,
        "msg_queue": None,
        "harness_thread": None
    })

    # 1. Nếu đang chờ duyệt (HITL approval)
    if session.get("pending_approval"):
        text_lower = text.lower().strip()
        harness = _harness_store.get(user_id)
        if harness:
            if text_lower in ("yes", "y", "co", "dong y", "approve", "ok", "đồng ý", "có"):
                harness._approval_val = True
                harness._approval_evt.set()
                session["pending_approval"] = False
                send_zalo_message(user_id, "✅ Đã xác nhận ĐỒNG Ý. Đang tiếp tục chạy...")
                return
            elif text_lower in ("no", "n", "khong", "tu choi", "reject", "từ chối", "không"):
                harness._approval_val = False
                harness._approval_evt.set()
                session["pending_approval"] = False
                send_zalo_message(user_id, "❌ Đã xác nhận TỪ CHỐI. Dừng pipeline.")
                return
            else:
                send_zalo_message(user_id, "⚠️ Phản hồi không hợp lệ. Vui lòng nhắn 'Đồng ý' (YES) hoặc 'Từ chối' (NO).")
                return

    # 2. Xử lý lệnh trợ giúp `/help`
    if text.startswith("/help"):
        help_msg = (
            "🤖 **Zalo Harness Bot**\n\n"
            "**Điều khiển Pipeline:**\n"
            "- `/run [flags] <nhiệm vụ>`: Khởi chạy pipeline công việc\n"
            "  `--mock`: Chạy giả lập không cần LLM API keys\n"
            "  `--auto-approve`: Tự động phê duyệt các hành động nguy hiểm\n"
            "- `/cancel`: Dừng khẩn cấp pipeline hiện tại\n"
            "- `/status`: Xem trạng thái bot hiện tại\n\n"
            "**Thao tác Git:**\n"
            "- `/git <lệnh>`: Thực thi lệnh git trong repo (ví dụ: `/git status` hoặc `/git diff`)\n\n"
            "**Ví dụ:**\n"
            "`/run --mock Thêm lớp PostCategory vào dự án`"
        )
        send_zalo_message(user_id, help_msg)
        return

    # 3. Xem trạng thái `/status`
    if text.startswith("/status"):
        running = session.get("running", False)
        pending = session.get("pending_approval", False)
        if running:
            status = "🟢 Đang chạy"
            if pending:
                status += " (⏳ chờ bạn phê duyệt)"
        else:
            status = "⚪ Rảnh"
        send_zalo_message(user_id, f"📊 **Trạng thái:** {status}")
        return

    # 4. Hủy chạy `/cancel`
    if text.startswith("/cancel"):
        if not session.get("running"):
            send_zalo_message(user_id, "⚪ Không có pipeline nào đang hoạt động.")
            return
        harness = _harness_store.get(user_id)
        if harness:
            harness._approval_val = False
            harness._approval_evt.set()
        session["running"] = False
        msg_queue = session.get("msg_queue")
        if msg_queue:
            msg_queue.put(("log", "🛑 **Đã gửi yêu cầu hủy.** Sẽ dừng sau bước hiện tại."))
        send_zalo_message(user_id, "🛑 Đã yêu cầu hủy pipeline.")
        return

    # 5. Lệnh Git `/git`
    if text.startswith("/git"):
        import subprocess
        cmd_parts = text[len("/git"):].strip()
        if not cmd_parts:
            send_zalo_message(user_id, "Cách dùng: `/git <lệnh>`\nVí dụ: `/git status`")
            return
        try:
            result = subprocess.run(
                f"git {cmd_parts}",
                shell=True,
                capture_output=True,
                encoding="utf-8",
                errors="replace",
                cwd=PROJECT_ROOT,
                timeout=60,
            )
            output = result.stdout or ""
            if result.stderr:
                output += "\n--- stderr ---\n" + result.stderr
            if not output.strip():
                output = "(không có output)"
            if len(output) > 1500:
                output = output[:1500] + "\n...(truncated)"
            send_zalo_message(user_id, f"```\n$ git {cmd_parts}\n{output}\n```")
        except Exception as e:
            send_zalo_message(user_id, f"❌ Lỗi git: `{e}`")
        return

    # 6. Khởi chạy nhiệm vụ `/run`
    if text.startswith("/run"):
        if session.get("running"):
            send_zalo_message(user_id, "⚠️ Đang có một pipeline chạy. Nhắn `/cancel` trước nếu muốn hủy.")
            return

        cmd_args = text[len("/run"):].strip().split()
        mock_mode = False
        auto_approve = False

        # Parse flags
        filtered_args = []
        for arg in cmd_args:
            if arg == "--mock":
                mock_mode = True
            elif arg == "--auto-approve":
                auto_approve = True
            else:
                filtered_args.append(arg)

        task = " ".join(filtered_args)
        if not task:
            send_zalo_message(user_id, "Cách dùng: `/run [--mock] [--auto-approve] <nhiệm vụ>`")
            return

        msg_queue = queue.Queue()
        session["running"] = True
        session["msg_queue"] = msg_queue

        flag_labels = []
        if mock_mode:
            flag_labels.append("mock")
        if auto_approve:
            flag_labels.append("auto-approve")
        tag = f" [{', '.join(flag_labels)}]" if flag_labels else ""
        send_zalo_message(user_id, f"🚀 **Bắt đầu chạy{tag}:** `{task}`")

        def _run_worker():
            old_stdout = sys.stdout
            sys.stdout = OutputCapture(msg_queue)
            harness = ZaloHarness(
                msg_queue=msg_queue,
                user_id=user_id,
                auto_approve=auto_approve,
                force_mock=mock_mode,
            )
            _harness_store[user_id] = harness
            try:
                harness.execute_pipeline(task)
            except Exception as exc:
                msg_queue.put(("log", f"\n❌ **LỖI:** `{exc}`\n```\n{traceback.format_exc()}\n```"))
            finally:
                _harness_store.pop(user_id, None)
                sys.stdout = old_stdout
                msg_queue.put(None)

        thread = threading.Thread(target=_run_worker, daemon=True)
        session["harness_thread"] = thread
        thread.start()

        # Bắt đầu stream logs
        stream_thread = threading.Thread(
            target=stream_output_loop,
            args=(user_id, msg_queue, thread),
            daemon=True
        )
        stream_thread.start()
        return

    # Nếu không trùng lệnh nào và không đang trong phiên approval
    send_zalo_message(user_id, "❓ Lệnh không hợp lệ. Vui lòng nhắn `/help` để xem danh sách các lệnh khả dụng.")

# ---------------------------------------------------------------------------
# Webhook Server Handler
# ---------------------------------------------------------------------------

class ZaloWebhookHandler(BaseHTTPRequestHandler):
    """Bộ xử lý HTTP request cho webhook Zalo OA."""

    def log_message(self, format, *args):
        # Tắt log mặc định của server vào stderr để tránh làm rối màn hình
        pass

    def do_GET(self):
        """Dùng cho xác minh Webhook của Zalo (nếu có yêu cầu GET challenge)"""
        self.send_response(200)
        self.send_header("Content-Type", "text/plain")
        self.end_headers()
        self.wfile.write(b"Zalo Webhook Server is running.")

    def do_POST(self):
        """Xử lý các sự kiện webhook gửi từ Zalo OA"""
        content_length = int(self.headers.get("Content-Length", 0))
        post_data = self.rfile.read(content_length)

        try:
            payload = json.loads(post_data.decode("utf-8"))
            event_name = payload.get("event_name")
            sender_id = payload.get("sender", {}).get("id")

            # Chỉ xử lý sự kiện tin nhắn text từ người dùng
            if event_name == "user_send_text" and sender_id:
                text_msg = payload.get("message", {}).get("text", "")
                
                # Kiểm tra quyền Admin nếu cấu hình ZALO_ADMIN_USER_ID
                admin_id = os.getenv("ZALO_ADMIN_USER_ID", "").strip()
                if admin_id and sender_id != admin_id:
                    logger.warning(f"Người dùng không có quyền truy cập: {sender_id}")
                    send_zalo_message(sender_id, "⛔ Bạn không có quyền điều khiển bot này.")
                else:
                    # Chạy xử lý lệnh trên thread riêng để phản hồi webhook ngay lập tức (tránh timeout)
                    threading.Thread(
                        target=handle_user_command,
                        args=(sender_id, text_msg),
                        daemon=True
                    ).start()

            self.send_response(200)
            self.send_header("Content-Type", "application/json")
            self.end_headers()
            self.wfile.write(json.dumps({"status": "ok"}).encode("utf-8"))

        except Exception as e:
            logger.error(f"Lỗi phân tích webhook: {e}")
            self.send_response(400)
            self.end_headers()

# ---------------------------------------------------------------------------
# Entry point
# ---------------------------------------------------------------------------

def main():
    # Load biến môi trường từ .env
    env_path = PROJECT_ROOT / ".env"
    if env_path.exists():
        try:
            from dotenv import load_dotenv
            load_dotenv(env_path)
        except ImportError:
            pass

    token = os.getenv("ZALO_OA_ACCESS_TOKEN", "").strip()
    if not token:
        logger.error("❌ Chưa đặt ZALO_OA_ACCESS_TOKEN trong file .env")
        logger.info("Vui lòng thiết lập biến môi trường này để bot có thể hoạt động.")
        sys.exit(1)

    port = int(os.getenv("ZALO_PORT", 8000))
    server = HTTPServer(("0.0.0.0", port), ZaloWebhookHandler)
    logger.info(f"🤖 Zalo Harness Bot Webhook Server đang chạy tại port {port}...")
    logger.info("Hãy dùng ngrok để expose port này ra internet và cấu hình webhook Zalo OA.")

    try:
        server.serve_forever()
    except KeyboardInterrupt:
        logger.info("Đang dừng server...")
        server.server_close()

if __name__ == "__main__":
    main()
