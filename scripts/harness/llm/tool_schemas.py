"""
Định nghĩa JSON Schema cho các công cụ (Tools) sử dụng trong Native Function Calling.
Sử dụng định dạng chuẩn của OpenAI/Deepseek, các provider khác như Gemini/Claude
sẽ được translate lại trong router.py.
"""

TOOLS_SCHEMA = [
    {
        "type": "function",
        "function": {
            "name": "execute_command",
            "description": "Thực thi lệnh shell (ví dụ: dotnet build, dotnet test, git status, git diff, git add). BẮT BUỘC dùng cho lệnh hệ thống. Tuyệt đối không dùng git commit.",
            "parameters": {
                "type": "object",
                "properties": {
                    "command": {
                        "type": "string",
                        "description": "Lệnh cần chạy (ví dụ: 'dotnet test')"
                    }
                },
                "required": ["command"]
            }
        }
    },
    {
        "type": "function",
        "function": {
            "name": "view_source",
            "description": "Đọc nội dung một tệp mã nguồn. Hỗ trợ phân trang để đọc các tệp tin dài.",
            "parameters": {
                "type": "object",
                "properties": {
                    "file_path": {
                        "type": "string",
                        "description": "Đường dẫn tuyệt đối hoặc tương đối tới file cần đọc"
                    },
                    "start_line": {
                        "type": "integer",
                        "description": "Dòng bắt đầu đọc (1-indexed). Bỏ trống nếu muốn đọc toàn bộ file (không khuyến khích với file lớn)."
                    },
                    "end_line": {
                        "type": "integer",
                        "description": "Dòng kết thúc đọc. Bỏ trống nếu muốn đọc toàn bộ file."
                    }
                },
                "required": ["file_path"]
            }
        }
    },
    {
        "type": "function",
        "function": {
            "name": "write_source",
            "description": "Ghi đè nội dung mới vào một tệp mã nguồn. Cần cung cấp toàn bộ nội dung file.",
            "parameters": {
                "type": "object",
                "properties": {
                    "file_path": {
                        "type": "string",
                        "description": "Đường dẫn tới file cần ghi"
                    },
                    "content": {
                        "type": "string",
                        "description": "Nội dung hoàn chỉnh sẽ ghi đè vào file"
                    }
                },
                "required": ["file_path", "content"]
            }
        }
    },
    {
        "type": "function",
        "function": {
            "name": "finish_task",
            "description": "Kết thúc lượt làm việc của Agent, gửi kết quả báo cáo cho Evaluator hoặc hoàn thành nhiệm vụ.",
            "parameters": {
                "type": "object",
                "properties": {
                    "summary": {
                        "type": "string",
                        "description": "Báo cáo chi tiết về những gì đã làm, kết quả đạt được."
                    }
                },
                "required": ["summary"]
            }
        }
    }
]
