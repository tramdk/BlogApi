# ☸️ Kubernetes Deployment & Troubleshooting Guide

Tài liệu này hướng dẫn chi tiết cách triển khai dự án **BlogApi** lên Kubernetes (K8s) sử dụng **Kind** (Kubernetes in Docker) và cách xử lý các vấn đề thường gặp.

---

## 🛠️ 1. Chuẩn bị môi trường (Local)

### Công cụ cần thiết:
1.  **Docker Desktop**: Đã cài đặt và đang chạy.
2.  **Kind (Kubernetes in Docker)**: Dùng để tạo cluster K8s siêu nhanh trên máy cá nhân.
3.  **Kubectl**: Công cụ dòng lệnh để điều khiển K8s.

### Cách khởi tạo Cluster:
Nếu bạn chưa có cluster, hãy chạy lệnh sau (sử dụng file `kind.exe` trong thư mục gốc):
```bash
./kind.exe create cluster --name blog-cluster
```

---

## 🏗️ 2. Quy trình triển khai (Deployment Workflow)

Mỗi khi bạn có thay đổi về code, hãy thực hiện các bước sau:

### Bước 1: Build Docker Image
```bash
docker build -t blogapi:local .
```

### Bước 2: Nạp Image vào Kind
Kind không tự kéo image từ máy host, bạn phải nạp nó vào:
```bash
./kind.exe load docker-image blogapi:local --name blog-cluster
```

### Bước 3: Triển khai các Manifests
```bash
kubectl apply -f k8s/
```

### Bước 4: Truy cập ứng dụng (Port-forward)
```bash
kubectl port-forward service/blogapi-service 8080:80
```
Truy cập: [http://localhost:8080/scalar/v1](http://localhost:8080/scalar/v1)

---

## 🔍 3. Giám sát hệ thống (Monitoring)

| Lệnh | Mục đích |
| :--- | :--- |
| `kubectl get pods` | Xem danh sách và trạng thái các Pod. |
| `kubectl get svc` | Xem danh sách các Service (App, Database). |
| `kubectl logs -f <pod-name>` | Xem log trực tiếp của ứng dụng (Real-time). |
| `kubectl describe pod <pod-name>` | Xem chi tiết sự kiện và lỗi của một Pod cụ thể. |

---

## 🆘 4. Xử lý sự cố (Troubleshooting)

### Lỗi `ImagePullBackOff`
*   **Triệu chứng:** Trạng thái Pod đứng yên ở `ImagePullBackOff`.
*   **Nguyên nhân:** K8s tìm không thấy image `blogapi:local`.
*   **Khắc phục:** Đảm bảo bạn đã chạy lệnh `kind load docker-image`. Kiểm tra lại tên image trong `k8s/deployment.yaml`.

### Lỗi `CrashLoopBackOff`
*   **Triệu chứng:** Pod khởi động rồi tắt liên tục.
*   **Nguyên nhân:** Thường do lỗi kết nối Database hoặc thiếu Biến môi trường.
*   **Khắc phục:** 
    1. Kiểm tra log: `kubectl logs <pod-name>`.
    2. Đảm bảo `blog-db-service` đang chạy: `kubectl get pods`.
    3. Kiểm tra chuỗi kết nối trong `k8s/secret.yaml`.

### Lỗi không truy cập được API qua Browser
*   **Triệu chứng:** `localhost:8080` báo Connection Refused.
*   **Khắc phục:** Đảm bảo lệnh `kubectl port-forward` vẫn đang chạy và không bị tắt.

---

## 🧹 5. Dọn dẹp (Cleanup)

Để xóa toàn bộ những gì đã cài đặt:
```bash
# Xóa các tài nguyên nhưng giữ cluster
kubectl delete -f k8s/

# Xóa sạch toàn bộ Cluster
./kind.exe delete cluster --name blog-cluster
```
