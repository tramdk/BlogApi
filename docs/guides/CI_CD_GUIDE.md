# Hướng Dẫn Tích Hợp & Triển Khai Liên Tục (CI/CD Guide)

Tài liệu này quy định chi tiết về quy trình Continuous Integration (CI) và Continuous Deployment (CD) cho toàn bộ dự án (bao gồm Backend `BlogApi` và Frontend `cái-tiệm-hoa-của-chin`).

## 1. Tổng Quan Kiến Trúc CI/CD
*   **Version Control:** GitHub.
*   **CI/CD Pipeline:** GitHub Actions.
*   **Backend Hosting:** Render / Koyeb (Chạy qua Docker container).
*   **Frontend Hosting:** Vercel.
*   **Database:** PostgreSQL (Cloud DB).

## 2. Luồng Làm Việc Git (Git Flow) & Trigger Rules
Quy trình được tự động hóa dựa trên các sự kiện (events) của Git:
*   **Pull Request (PR) vào `main` / `master` (CI):** 
    *   Tự động chạy script: `Linter` -> `Build` -> `Unit Tests`.
    *   KHÔNG triển khai (Deploy) ở bước này.
    *   Nếu bất kỳ tool nào báo lỗi, PR sẽ bị chặn (Blocked) không cho phép Merge.
*   **Merge/Push lên `main` / `master` (CD):**
    *   Kích hoạt quy trình CI (Build & Test một lần nữa để chắc chắn).
    *   Khởi chạy tiến trình Deploy.
    *   Backend: Build Docker Image và Push lên server Render/Koyeb khởi động lại dịch vụ.
    *   Frontend: Vercel sẽ tự động hook vào nhánh `main` để build và deploy.

## 3. Cấu Hình Khối (Pipeline Configurations)

### 3.1. Frontend Pipeline (Vercel)
Vì Vercel cung cấp sẵn tính năng tự động phân tích và deploy React/TypeScript, chúng ta sẽ để Vercel tự quản lý bằng cách liên kết trực tiếp Github Repository.
*   **Môi trường Preview:** Khi tạo một PR mới, Vercel sẽ tự động tạo một đường dẫn tạm (Preview URL) để team có thể kiểm thử tính năng ngay lập tức.
*   **Lưu ý quan trọng:** Cần đảm bảo file `vercel.json` định nghĩa đúng build command (`npm run build`) và output directory (thường là `dist` cho Vite). Đảm bảo không quên cấu hình Biến Môi Trường (Environment Variables) trên dashboard của Vercel (như `VITE_API_BASE_URL`).

### 3.2. Backend Pipeline (GitHub Actions -> Render/Koyeb)
Backend yêu cầu tự cấu hình CI/CD thông qua file `.github/workflows/dotnet.yml`.
**A. Tiến trình CI (Continuous Integration):**
1.  **Checkout Code:** Lấy code mới nhất.
2.  **Setup .NET:** Cài đặt phiên bản SDK phù hợp (Ví dụ: .NET 8).
3.  **Restore:** Chạy `dotnet restore` để tải các dependencies.
4.  **Build:** Chạy `dotnet build --no-restore --configuration Release` để đảm bảo code biên dịch chuẩn.
5.  **Test:** Chạy `dotnet test --no-build --verbosity normal` để đảm bảo không bị lỗi logic.

**B. Tiến trình CD (Continuous Deployment):**
*   **Cách 1 (Dockerize):** GitHub Actions thực hiện login vào Docker Container Registry, build file `Dockerfile` của `BlogApi` và push Image lên. Server Render/Koyeb sẽ pull Image mới về để khởi chạy.
*   **Cách 2 (Webhook):** Nếu Render/Koyeb đã kết nối Github, chúng ta chỉ cần chạy CI thành công, và Render/Koyeb tự động lắng nghe và build trên server của họ.

## 4. Quản Lý Biến Môi Trường (Environments & Secrets)
TUYỆT ĐỐI không commit các file như `.env`, `appsettings.Development.json` hay connection strings.
*   **Github Secrets:** Dùng để lưu các biến dùng khi build (ví dụ docker password, sonar token). Truy cập vào `Settings > Secrets and variables > Actions`.
*   **Deployment Variables:** Mật khẩu DB, chuỗi kết nối JWT, API Key dùng ở Production CẦN ĐƯỢC CAI ĐẶT riêng trên Dashboard của nền tảng (Render/Koyeb/Vercel) tương ứng.

## 5. Rollback (Khôi Phục Bản Rút Gọn)
*   **Backend:** Nếu có lỗi ở Production, thực hiện tính năng "Rollback to previous commit" trên dashboard của Render/Koyeb (hoặc revert commit trên nhánh `main` để kích hoạt lại CD).
*   **Frontend:** Trên Vercel Dashboard, chọn menu `Deployments`, click vào 3 chấm ở bản build trước đó và chọn `Promote to Production` hoặc `Rollback` để lùi ngay lập tức (instant rollback) mà không cần build lại.

---
**Ghi Chú:** Bất cứ thay đổi nào đến luồng CI/CD phải được kiểm thử kỹ trên các Repository Nháp trước khi áp dụng lên file chính thức. Đoạn mã cụ thể dùng cho GitHub Actions được giữ tại `.github/workflows/dotnet.yml`.
