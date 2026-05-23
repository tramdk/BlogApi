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
import sys
import threading
import traceback
from pathlib import Path

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
        if len(self.buffer) >= 500 or (text and text[-1] in "\n\r"):
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

async def stream_output(
    chat_id: int,
    msg_queue: queue.Queue,
    app: Application,
    chat_data: dict,
    harness_thread: threading.Thread,
):
    """Vòng lặp: đọc queue.Queue → gửi Telegram message."""
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
            if text:
                for i in range(0, len(text), 4000):
                    await app.bot.send_message(
                        chat_id=chat_id,
                        text=text[i : i + 4000],
                        disable_web_page_preview=True,
                    )
        elif typ == "approval":
            message, _chat_id = payload
            keyboard = InlineKeyboardMarkup([
                [
                    InlineKeyboardButton("✅ Đồng ý", callback_data="approve_yes"),
                    InlineKeyboardButton("❌ Từ chối", callback_data="approve_no"),
                ]
            ])
            chat_data["pending_approval"] = True
            await app.bot.send_message(
                chat_id=_chat_id,
                text=f"🛡️ {message}",
                reply_markup=keyboard,
            )

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
        "`/run [flags] <nhiệm vụ>` — Chạy pipeline\n"
        "  `--mock`          chế độ giả lập\n"
        "  `--auto-approve`  tự động phê duyệt\n"
        "`/cancel` — Hủy pipeline đang chạy\n"
        "`/status` — Kiểm tra trạng thái\n"
        "`/help` — Hướng dẫn\n\n"
        "Ví dụ:\n"
        "`/run --mock Thêm entity PostCategory`\n"
        "`/run --auto-approve Fix bug build`"
    )


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
    app.add_handler(CommandHandler("help", cmd_help))
    app.add_handler(CallbackQueryHandler(callback_handler))

    print("🤖 Telegram Harness Bot đang chạy (long-polling)...")
    print(f"   Nhắn /help cho bot trên Telegram để bắt đầu.")
    app.run_polling(allowed_updates=Update.ALL_TYPES)


if __name__ == "__main__":
    main()
