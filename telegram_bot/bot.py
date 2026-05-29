#!/usr/bin/env python3
"""
Telegram Bot wrapper for AI Developer Harness.
Chạy local bằng long-polling, không cần server public.
"""

import logging
import os
import sys
from pathlib import Path

# Setup logging
logging.basicConfig(
    format="%(asctime)s [%(levelname)s] %(message)s",
    level=logging.INFO,
)
logger = logging.getLogger(__name__)

# Setup sys.path
PROJECT_ROOT = Path(__file__).resolve().parent.parent
if str(PROJECT_ROOT) not in sys.path:
    sys.path.insert(0, str(PROJECT_ROOT))

# Nạp cấu hình từ file .env
env_path = PROJECT_ROOT / ".env"
if env_path.exists():
    try:
        from dotenv import load_dotenv
        load_dotenv(env_path)
    except ImportError:
        pass

from telegram import Update
from telegram.ext import Application, CommandHandler, CallbackQueryHandler

# Import modular handlers
from telegram_bot.handlers import cmd_run, cmd_cancel, cmd_status, cmd_git, cmd_help
from telegram_bot.skill_handlers import cmd_skill, cmd_skills, cmd_config, cmd_log
from telegram_bot.callback import callback_handler

def main():
    token = os.getenv("TELEGRAM_BOT_TOKEN", "")
    if not token:
        print("❌ Chưa đặt TELEGRAM_BOT_TOKEN trong .env hoặc biến môi trường.")
        print("   Tạo bot qua @BotFather và thêm dòng:")
        print("   TELEGRAM_BOT_TOKEN=123456:ABC-DEF...")
        sys.exit(1)

    app = Application.builder().token(token).build()
    app.add_handler(CommandHandler("run", cmd_run))
    app.add_handler(CommandHandler("cancel", cmd_cancel))
    app.add_handler(CommandHandler("status", cmd_status))
    app.add_handler(CommandHandler("skill", cmd_skill))
    app.add_handler(CommandHandler("skills", cmd_skills))
    app.add_handler(CommandHandler("config", cmd_config))
    app.add_handler(CommandHandler("log", cmd_log))
    app.add_handler(CommandHandler("git", cmd_git))
    app.add_handler(CommandHandler("help", cmd_help))
    app.add_handler(CallbackQueryHandler(callback_handler))

    print("🤖 Telegram Harness Bot đang chạy (long-polling)...")
    print(f"   Nhắn /help cho bot trên Telegram để bắt đầu.")
    app.run_polling(allowed_updates=Update.ALL_TYPES)

if __name__ == "__main__":
    main()
