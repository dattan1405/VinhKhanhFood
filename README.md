import os

readme_content = """# Vinh Khanh Food - Ứng dụng Khám phá Ẩm thực Quận 4

Dự án phát triển ứng dụng di động bằng **.NET MAUI** kết hợp với **ASP.NET Core Web API** và **SQL Server**. Ứng dụng giúp người dùng trải nghiệm danh sách các quán ăn đặc sắc tại phố ẩm thực Vĩnh Khánh.

---

## Cấu hình Hệ thống 

### 1. Database (SQL Server)
- Mở file `VinhKhanhFood.API/appsettings.json`.
- Thay đổi `Server=...` thành tên máy tính của bạn để API kết nối được DB.

### 2. Chạy Web API
- Chọn project **VinhKhanhFood.API**.
- **Lưu ý:** Chuyển cấu hình chạy từ `https` sang `http` (Cổng 5020).
- Sau khi chạy, API sẽ có địa chỉ: `http://localhost:5020`.

### 3. Chạy Mobile App
- Chọn project **VinhKhanhFood.App**.
- App đã được cấu hình trỏ về địa chỉ `http://10.0.2.2:5020` để máy ảo Android có thể nhận diện được Server local.

---

##  Hướng dẫn chạy đồ án

Để App và API chạy cùng lúc
1. Chuột phải vào **Solution 'VinhKhanhFood'** -> chọn **Properties**.
2. Tại mục **Startup Project**, chọn **Multiple startup projects**.
3. Chỉnh cột **Action** của cả `VinhKhanhFood.API` và `VinhKhanhFood.App` thành **Start**.
4. Nhấn **F5** để khởi chạy cả hai cùng lúc.

---

