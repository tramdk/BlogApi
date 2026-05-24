#!/usr/bin/env python3
"""
Telegram Bot wrapper for AI Developer Harness.
Chạy local bằng long-polling, không cần server public.

Cách dùng:
  1. Tạo bot qua @BotFather, lấy token
  2. Thêm TELEGRAM_BOT_TOKEN=xxx vào .env (hoặc biến môi trường)
  3. python telegram_bot/bot.py
"""

import asyncio
import logging
import os
import queue
import subprocess
import sys
import threading
import traceback
from pathlib import Path

logger = logging.getLogger(__name__)

from telegram import Update, InlineKeyboardButton, InlineKeyboardMarkup
from telegram.ext import Application, CommandHandler, CallbackQueryHandler, ContextTypes

logging.basicConfig(
    format="%(asctime)s [%(levelname)s] %(message)s",
    level=logging.INFO,
)

PROJECT_ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(PROJECT_ROOT))

from scripts.ai_developer_harness import AIDeveloperHarness

# Lưu harness instance theo chat_id để callback_handler có thể truy cập từ thread khác
_harness_store: dict[int, "TelegramHarness"] = {}

# ---------------------------------------------------------------------------
# Output Capture — chặn print() để gửi về Telegram
# ---------------------------------------------------------------------------

class OutputCapture:
    """Chặn sys.stdout, đẩy output vào queue.Queue để stream về Telegram."""
    def __init__(self, msg_queue: queue.Queue):
        self.msg_queue = msg_queue
        self.buffer = ""

    def write(self, text):
        self.buffer += text
        if len(self.buffer) >= 2000 or (text and text[-1] in "\n\r" and len(self.buffer) >= 200):
            self.msg_queue.put(("log", self.buffer))
            self.buffer = ""

    def flush(self):
        if self.buffer:
            self.msg_queue.put(("log", self.buffer))
            self.buffer = ""

# ---------------------------------------------------------------------------
# Telegram-aware Harness — thay ask_approval bằng inline keyboard
# ---------------------------------------------------------------------------

class TelegramHarness(AIDeveloperHarness):
    """Subclass của AIDeveloperHarness hỗ trợ HITL qua Telegram inline keyboard."""

    def __init__(
        self,
        msg_queue: queue.Queue,
        chat_id: int,
        auto_approve: bool = True,
        force_mock: bool | None = None,
    ):
        self._msg_queue = msg_queue
        self._chat_id = chat_id
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
        self._msg_queue.put(("approval", (message, self._chat_id)))
        self._approval_evt.wait()
        return self._approval_val

# ---------------------------------------------------------------------------
# Streaming — đọc queue và gửi message về Telegram
# ---------------------------------------------------------------------------

async def _safe_send(bot, chat_id: int, text: str, reply_markup=None):
    """Gửi message với retry khi bị flood ban."""
    import re as _re
    for attempt in range(3):
        try:
            await bot.send_message(
                chat_id=chat_id,
                text=text,
                disable_web_page_preview=True,
                reply_markup=reply_markup,
            )
            return
        except Exception as exc:
            err = str(exc).lower()
            if "retry after" in err:
                m = _re.search(r"retry after\s+(\d+)", err)
                wait = int(m.group(1)) + 1 if m else 5
                logger.warning("Flood wait %ds, sleeping...", wait)
                await asyncio.sleep(wait)
            elif attempt < 2:
                await asyncio.sleep(1)
            else:
                logger.error("Send failed after 3 retries: %s", exc)


async def stream_output(
    chat_id: int,
    msg_queue: queue.Queue,
    app: Application,
    chat_data: dict,
    harness_thread: threading.Thread,
):
    """Đọc queue.Queue → gửi Telegram message, có rate-limit guard."""
    MESSAGE_GAP = 0.35  # giây giữa các message để tránh flood ban

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
            for i in range(0, len(text), 4000):
                await _safe_send(app.bot, chat_id=chat_id, text=text[i : i + 4000])
                await asyncio.sleep(MESSAGE_GAP)
        elif typ == "approval":
            message, _chat_id = payload
            keyboard = InlineKeyboardMarkup([
                [
                    InlineKeyboardButton("✅ Đồng ý", callback_data="approve_yes"),
                    InlineKeyboardButton("❌ Từ chối", callback_data="approve_no"),
                ]
            ])
            chat_data["pending_approval"] = True
            await _safe_send(app.bot, chat_id=_chat_id, text=f"🛡️ {message}", reply_markup=keyboard)
            await asyncio.sleep(MESSAGE_GAP)

    chat_data["running"] = False
    chat_data["pending_approval"] = False
    await app.bot.send_message(
        chat_id=chat_id,
        text="✅ **Pipeline hoàn tất!**",
    )

# ---------------------------------------------------------------------------
# Handlers
# ---------------------------------------------------------------------------

async def cmd_run(update: Update, context: ContextTypes.DEFAULT_TYPE):
    if context.chat_data.get("running"):
        await update.message.reply_text(
            "⚠️ Đang có pipeline chạy. Dùng /cancel để hủy trước."
        )
        return

    args = list(context.args)
    mock_mode = "--mock" in args
    auto_approve = "--auto-approve" in args
    args = [a for a in args if a not in ("--mock", "--auto-approve")]
    task = " ".join(args)
    if not task:
        await update.message.reply_text(
            "Cách dùng: `/run [--mock] [--auto-approve] <nhiệm vụ>`\n"
            "  `--mock`          chạy giả lập (không cần API key)\n"
            "  `--auto-approve`  tự động phê duyệt (bỏ qua HITL)\n"
            "Ví dụ: /run --mock Thêm entity PostCategory"
        )
        return

    chat_id = update.effective_chat.id
    msg_queue = queue.Queue()
    context.chat_data["running"] = True
    context.chat_data["msg_queue"] = msg_queue

    flag_labels = []
    if mock_mode:
        flag_labels.append("mock")
    if auto_approve:
        flag_labels.append("auto-approve")
    tag = f" [{', '.join(flag_labels)}]" if flag_labels else ""
    await update.message.reply_text(f"🚀 **Đang chạy{tag}:** `{task}`")

    def _run():
        old_stdout = sys.stdout
        sys.stdout = OutputCapture(msg_queue)
        harness = TelegramHarness(
            msg_queue=msg_queue,
            chat_id=chat_id,
            auto_approve=auto_approve,
            force_mock=mock_mode,
        )
        _harness_store[chat_id] = harness
        try:
            harness.execute_pipeline(task)
        except Exception as exc:
            msg_queue.put(("log", f"\n❌ **LỖI:** `{exc}`\n```\n{traceback.format_exc()}\n```"))
        finally:
            _harness_store.pop(chat_id, None)
            sys.stdout = old_stdout
            msg_queue.put(None)

    thread = threading.Thread(target=_run, daemon=True)
    context.chat_data["harness_thread"] = thread
    thread.start()

    asyncio.create_task(
        stream_output(chat_id, msg_queue, context.application, context.chat_data, thread)
    )


async def cmd_cancel(update: Update, context: ContextTypes.DEFAULT_TYPE):
    if not context.chat_data.get("running"):
        await update.message.reply_text("⚪ Không có pipeline nào đang chạy.")
        return

    harness = _harness_store.get(update.effective_chat.id)
    if harness:
        harness._approval_val = False
        harness._approval_evt.set()

    context.chat_data["running"] = False
    # Dừng streaming: queue.put(None) sẽ không đủ vì _run đang chạy.
    # Cách đơn giản: set flag và chờ thread kết thúc tự nhiên.
    msg_queue = context.chat_data.get("msg_queue")
    if msg_queue:
        msg_queue.put(("log", "🛑 **Đã yêu cầu hủy.** Pipeline sẽ dừng sau bước hiện tại."))

    await update.message.reply_text("🛑 Đã gửi yêu cầu hủy.")


async def cmd_status(update: Update, context: ContextTypes.DEFAULT_TYPE):
    running = context.chat_data.get("running", False)
    pending = context.chat_data.get("pending_approval", False)
    if running:
        status = "🟢 Đang chạy"
        if pending:
            status += " (⏳ chờ phê duyệt)"
    else:
        status = "⚪ Rảnh"
    await update.message.reply_text(f"📊 **Trạng thái:** {status}")


async def cmd_help(update: Update, context: ContextTypes.DEFAULT_TYPE):
    await update.message.reply_text(
        "🤖 **Harness Bot**\n\n"
        "**Pipeline:**\n"
        "`/run [flags] <nhiệm vụ>` — Chạy pipeline\n"
        "  `--mock`          chế độ giả lập\n"
        "  `--auto-approve`  tự động phê duyệt\n"
        "`/cancel` — Hủy pipeline\n"
        "`/status` — Trạng thái hiện tại\n\n"
        "**Git:**\n"
        "`/git <lệnh>` — Chạy lệnh git\n"
        "  Lệnh read-only (status/diff/log) chạy ngay.\n"
        "  Lệnh ghi (commit/push/add) cần xác nhận.\n\n"
        "Ví dụ:\n"
        "`/run --mock Thêm entity PostCategory`\n"
        "`/git status`\n"
        "`/git log --oneline -5`"
    )


async def cmd_git(update: Update, context: ContextTypes.DEFAULT_TYPE):
    if not context.args:
        await update.message.reply_text(
            "Cách dùng: `/git <lệnh>`\n"
            "Ví dụ:\n"
            "`/git status`\n"
            "`/git diff`\n"
            "`/git add .`\n"
            "`/git commit -m \"fix: sửa lỗi\"`\n"
            "`/git push`\n"
            "`/git log --oneline -5`\n"
            "`/git pull`"
        )
        return

    cmd_parts = " ".join(context.args)
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
        if len(output) > 3900:
            output = output[:3900] + "\n...(truncated)"
        await update.message.reply_text(
            f"```\n$ git {cmd_parts}\n{output}\n```",
        )
    except subprocess.TimeoutExpired:
        await update.message.reply_text("⏰ Lệnh git bị timeout (60s).")
    except Exception as e:
        await update.message.reply_text(f"❌ Lỗi: `{e}`")
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

        # Truncate nếu quá dài
        if len(output) > 3900:
            output = output[:3900] + "\n...(truncated)"

        await update.message.reply_text(
            f"```\n$ git {cmd_parts}\n{output[:3900]}\n```",
        )
    except subprocess.TimeoutExpired:
        await update.message.reply_text("⏰ Lệnh git bị timeout (60s).")
    except Exception as e:
        await update.message.reply_text(f"❌ Lỗi: `{e}`")


async def callback_handler(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    data = query.data
    if data.startswith("approve_"):
        result = data == "approve_yes"
        harness = _harness_store.get(update.effective_chat.id)
        if harness:
            harness._approval_val = result
            harness._approval_evt.set()
        context.chat_data["pending_approval"] = False
        await query.edit_message_text(
            f"{'✅' if result else '❌'} {query.message.text}"
        )

# ---------------------------------------------------------------------------
# Entry point
# ---------------------------------------------------------------------------

def main():
    token = os.getenv("TELEGRAM_BOT_TOKEN", "")
    if not token:
        env_path = PROJECT_ROOT / ".env"
        if env_path.exists():
            try:
                from dotenv import load_dotenv
                load_dotenv(env_path)
                token = os.getenv("TELEGRAM_BOT_TOKEN", "")
            except ImportError:
                pass
    if not token:
        print("❌ Chưa đặt TELEGRAM_BOT_TOKEN trong .env hoặc biến môi trường.")
        print("   Tạo bot qua @BotFather và thêm dòng:")
        print("   TELEGRAM_BOT_TOKEN=123456:ABC-DEF...")
        sys.exit(1)

    app = Application.builder().token(token).build()
    app.add_handler(CommandHandler("run", cmd_run))
    app.add_handler(CommandHandler("cancel", cmd_cancel))
    app.add_handler(CommandHandler("status", cmd_status))
    app.add_handler(CommandHandler("git", cmd_git))
    app.add_handler(CommandHandler("help", cmd_help))
    app.add_handler(CallbackQueryHandler(callback_handler))

    print("🤖 Telegram Harness Bot đang chạy (long-polling)...")
    print(f"   Nhắn /help cho bot trên Telegram để bắt đầu.")
    app.run_polling(allowed_updates=Update.ALL_TYPES)


if __name__ == "__main__":
    main()
