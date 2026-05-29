import asyncio
import queue
from telegram import Update
from telegram.ext import ContextTypes

from telegram_bot.harness_bridge import (
    _harness_store,
    _start_pipeline_thread,
    stream_output,
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

    elif data.startswith("skill_"):
        # Kích hoạt skill được chọn từ menu inline
        skill_name = data[len("skill_"):]
        if context.chat_data.get("running"):
            await query.edit_message_text("⚠️ Đang có pipeline chạy. Dùng /cancel để hủy trước.")
            return

        task = (
            f"Hãy đóng vai trò chuyên gia phân tích và quét toàn bộ mã nguồn của dự án "
            f"để phát hiện, tối ưu hóa các phần liên quan dựa theo hướng dẫn cụ thể trong skill: {skill_name}."
        )
        await query.edit_message_text(f"🚀 **Đang kích hoạt skill:** `{skill_name}`")

        chat_id = update.effective_chat.id
        msg_queue = queue.Queue()
        context.chat_data["running"] = True
        context.chat_data["msg_queue"] = msg_queue

        thread = _start_pipeline_thread(chat_id, msg_queue, context, task)
        asyncio.create_task(
            stream_output(chat_id, msg_queue, context.application, context.chat_data, thread)
        )
