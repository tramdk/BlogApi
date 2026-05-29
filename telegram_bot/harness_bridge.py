import asyncio
import io
import logging
import os
import queue
import sys
import threading
import traceback
from pathlib import Path

logger = logging.getLogger(__name__)

from telegram import Update, InlineKeyboardButton, InlineKeyboardMarkup
from telegram.ext import Application, ContextTypes

PROJECT_ROOT = Path(__file__).resolve().parent.parent
if str(PROJECT_ROOT) not in sys.path:
    sys.path.insert(0, str(PROJECT_ROOT))

from scripts.ai_developer_harness import AIDeveloperHarness

# Lưu harness instance theo chat_id để callback_handler có thể truy cập từ thread khác
_harness_store: dict[int, "TelegramHarness"] = {}

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

async def _safe_send_document(bot, chat_id: int, file_data: bytes, filename: str, caption: str = None):
    """Gửi document với retry khi bị flood ban."""
    import re as _re
    for attempt in range(3):
        try:
            await bot.send_document(
                chat_id=chat_id,
                document=io.BytesIO(file_data),
                filename=filename,
                caption=caption,
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
                logger.error("Send document failed after 3 retries: %s", exc)

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
            if len(text) > 1500:
                title = text.split("\n")[0][:100] if text.split("\n") else "Log Output"
                caption = f"📄 {title}..."
                await _safe_send_document(
                    app.bot,
                    chat_id=chat_id,
                    file_data=text.encode("utf-8"),
                    filename="harness_log.txt",
                    caption=caption
                )
                await asyncio.sleep(MESSAGE_GAP)
            else:
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

def _start_pipeline_thread(chat_id, msg_queue, context, task, auto_approve=True, force_mock=None, skip_enricher=False):
    """Tạo TelegramHarness + thread chạy pipeline, trả về thread object."""
    def _run():
        old_stdout = sys.stdout
        sys.stdout = OutputCapture(msg_queue)
        harness = TelegramHarness(
            msg_queue=msg_queue,
            chat_id=chat_id,
            auto_approve=auto_approve,
            force_mock=force_mock,
        )
        _harness_store[chat_id] = harness
        try:
            harness.execute_pipeline(task, skip_enricher=skip_enricher)
        except Exception as exc:
            msg_queue.put(("log", f"\n❌ **LỖI:** `{exc}`\n```\n{traceback.format_exc()}\n```"))
        finally:
            _harness_store.pop(chat_id, None)
            sys.stdout = old_stdout
            msg_queue.put(None)

    thread = threading.Thread(target=_run, daemon=True)
    context.chat_data["harness_thread"] = thread
    thread.start()
    return thread
