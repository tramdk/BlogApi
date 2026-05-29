import asyncio
import json as _json
import queue
from telegram import Update, InlineKeyboardButton, InlineKeyboardMarkup
from telegram.ext import ContextTypes

from telegram_bot.harness_bridge import (
    PROJECT_ROOT,
    _start_pipeline_thread,
    stream_output,
    _safe_send_document,
)

async def cmd_skills(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """Liệt kê tất cả skills đã cài đặt trong thư mục skills/."""
    skills_path = PROJECT_ROOT / "skills"
    if not skills_path.exists():
        await update.message.reply_text("❌ Không tìm thấy thư mục `skills/` trong dự án.")
        return

    available = [
        d.name for d in sorted(skills_path.iterdir())
        if d.is_dir() and (d / "SKILL.md").exists()
    ]

    if not available:
        await update.message.reply_text("❌ Chưa có skill nào được cài đặt trong thư mục `skills/`.")
        return

    lines = ["📋 **Danh sách Skills đã cài đặt:**\n"]
    for i, s in enumerate(available, 1):
        lines.append(f"  `{i}.` {s}")
    lines.append("\n👉 Dùng `/skill <tên>` để kích hoạt, hoặc `/skill` để chọn qua menu.")
    await update.message.reply_text("\n".join(lines))

async def cmd_skill(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """Kích hoạt một skill cụ thể. Hiển thị menu chọn nếu không truyền tên."""
    if context.chat_data.get("running"):
        await update.message.reply_text("⚠️ Đang có pipeline chạy. Dùng /cancel để hủy trước.")
        return

    skills_path = PROJECT_ROOT / "skills"
    available = []
    if skills_path.exists():
        available = [
            d.name for d in sorted(skills_path.iterdir())
            if d.is_dir() and (d / "SKILL.md").exists()
        ]

    if not available:
        await update.message.reply_text("❌ Chưa có skill nào được cài đặt trong thư mục `skills/`.")
        return

    skill_name = " ".join(context.args).strip() if context.args else ""

    # Nếu tên hợp lệ thì chạy ngay
    if skill_name and skill_name in available:
        chat_id = update.effective_chat.id
        msg_queue = queue.Queue()
        context.chat_data["running"] = True
        context.chat_data["msg_queue"] = msg_queue
        task = (
            f"Hãy đóng vai trò chuyên gia phân tích và quét toàn bộ mã nguồn của dự án "
            f"để phát hiện, tối ưu hóa các phần liên quan dựa theo hướng dẫn cụ thể trong skill: {skill_name}."
        )
        await update.message.reply_text(f"🚀 **Đang kích hoạt skill:** `{skill_name}`")

        thread = _start_pipeline_thread(chat_id, msg_queue, context, task)
        asyncio.create_task(
            stream_output(chat_id, msg_queue, context.application, context.chat_data, thread)
        )
        return

    # Hiển thị menu inline keyboard để chọn skill
    if skill_name:
        await update.message.reply_text(f"⚠️ Không tìm thấy skill `{skill_name}`. Chọn từ danh sách:")

    buttons = [
        [InlineKeyboardButton(f"⚡ {s}", callback_data=f"skill_{s}")]
        for s in available
    ]
    keyboard = InlineKeyboardMarkup(buttons)
    await update.message.reply_text("📋 **Chọn skill để kích hoạt:**", reply_markup=keyboard)

async def cmd_config(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """Hiển thị nội dung cấu hình harness_config.json hiện tại."""
    config_path = PROJECT_ROOT / "harness_config.json"
    if not config_path.exists():
        await update.message.reply_text("❌ Không tìm thấy file `harness_config.json`.")
        return
    try:
        with open(config_path, "r", encoding="utf-8") as f:
            cfg = _json.load(f)

        lines = ["⚙️ **Cấu hình Harness hiện tại** (`harness_config.json`):\n"]

        lines.append("📜 **Policy Files:**")
        for p in cfg.get("policy_files", []):
            lines.append(f"  • `{p}`")

        lines.append("\n📚 **Skills & Guides:**")
        for s in cfg.get("skills_and_guides", []):
            lines.append(f"  • `{s}`")

        lines.append("\n💻 **Allowed Commands:**")
        for c in cfg.get("allowed_commands", []):
            lines.append(f"  • `{c}`")

        await update.message.reply_text("\n".join(lines))
    except Exception as e:
        await update.message.reply_text(f"❌ Lỗi đọc config: `{e}`")

async def cmd_log(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """Gửi file log của phiên Harness gần nhất."""
    log_path = PROJECT_ROOT / ".claude" / "evals" / "harness_run.log"
    if not log_path.exists():
        await update.message.reply_text("❌ Chưa có log nào. Chạy `/run` trước.")
        return
    try:
        content = log_path.read_text(encoding="utf-8", errors="replace")
        if not content.strip():
            await update.message.reply_text("📄 File log hiện đang trống.")
            return
        # Gửi dưới dạng document để tránh giới hạn 4096 ký tự
        await _safe_send_document(
            update.get_bot() if hasattr(update, "get_bot") else context.bot,
            chat_id=update.effective_chat.id,
            file_data=content.encode("utf-8"),
            filename="harness_run.log",
            caption=f"📋 Log phiên Harness gần nhất ({len(content.splitlines())} dòng)"
        )
    except Exception as e:
        await update.message.reply_text(f"❌ Lỗi đọc log: `{e}`")
