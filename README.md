# 🛵 Vinh Khanh Foodie - Audio Guide App

Ứng dụng hướng dẫn du lịch ẩm thực tại phố Vĩnh Khánh (Quận 4), tích hợp bản đồ tương tác và âm thanh tự động. Dự án được phát triển bằng hệ sinh thái .NET 9

---

## 🏗️ Kiến trúc dự án
* **Backend:** ASP.NET Core Web API 9.0 (Entity Framework Core).
* **Mobile App:** .NET MAUI 9.0 (Mô hình MVVM).
* **Database:** SQL Server (Đã bao gồm Script khởi tạo Schema & Data).

---

##  Cấu trúc mã nguồn
* `/VinhKhanhFood.API`: Xử lý dữ liệu, hình ảnh và cung cấp API.
* `/VinhKhanhFood.App`: Ứng dụng di động với giao diện XAML 
* `/Database`: Chứa file `VinhKhanhFood_Full.sql`

---

##  Hướng dẫn thiết lập

### 1. Khởi tạo Cơ sở dữ liệu (SQL Server)
1. Mở **SQL Server Management Studio (SSMS)**.
2. Tạo một Database mới tên là `VinhKhanhFood`.
3. Mở file `Database/VinhKhanhFood_Full.sql` trong project.
4. Nhấn **Execute (F5)** để tự động tạo bảng và đổ dữ liệu mẫu.

### 2. Chạy Backend (API)
1. Mở dự án `VinhKhanhFood.API`.
2. Sửa `DefaultConnection` trong `appsettings.json` cho khớp với Server SQL máy bạn.
3. Đảm bảo ảnh đã nằm trong thư mục `wwwroot/images/`.
4. Nhấn **F5** để chạy API và lấy địa chỉ IP (Ví dụ: `192.168.1.5`).

### 3. Chạy Mobile App (MAUI)
1. Mở dự án `VinhKhanhFood.App`.
2. **Cấu hình API:** Tìm file `Services/ApiService.cs`, sửa `BaseUrl` thành IP máy tính (Không dùng `localhost` nếu chạy Android Emulator).
3. **Google Maps:** Nếu bản đồ không hiện (ô lưới trắng), hãy thay API Key của bạn vào `AndroidManifest.xml` hoặc yêu cầu tác giả mở khóa Key.

---


##  Lưu ý khi lỗi Build
* Lỗi **"InitializeComponent"**: Thực hiện `Clean Solution` -> Xóa thư mục `bin/obj` -> `Rebuild`.
* Bản đồ trống: Kiểm tra API Key và quyền truy cập Internet.

---
