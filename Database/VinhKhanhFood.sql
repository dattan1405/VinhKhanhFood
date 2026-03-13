CREATE DATABASE VinhKhanhFoodDB;
GO

USE VinhKhanhFoodDB;
GO


-- Xóa dữ liệu 
TRUNCATE TABLE FoodLocations;
GO

INSERT INTO FoodLocations (Name, Latitude, Longitude, AudioUrl, Description)
VALUES 
(N'Ốc Oanh', 10.7601, 106.7042, NULL, 
  N'Ốc Oanh là quán ăn trứ danh tại phố Vĩnh Khánh, Quận 4. Nơi đây nổi tiếng nhất với món ốc hương trứng muối béo ngậy và càng ghẹ rang muối tuyết cay nồng. Không gian vỉa hè thoáng mát, đậm chất đường phố Sài Gòn, rất thích hợp để tụ tập bạn bè vào mỗi buổi chiều tối.'),

(N'Ốc Đào', 10.7605, 106.7045, NULL, 
  N'Ốc Đào là thương hiệu hải sản tươi sống lâu đời. Quán có không gian rộng rãi, lịch sự. Món đặc sản nên thử là ốc đỏ nướng mọi và răng mực xào bơ tỏi ăn kèm bánh mì giòn.'),

(N'Ốc Vũ', 10.759850, 106.704950, NULL, 
  N'Ốc Vũ nằm ngay đầu phố Vĩnh Khánh, nổi tiếng với các món xào bơ tỏi thơm nức mũi. Giá cả ở đây rất phải chăng và nhân viên phục vụ cực kỳ nhanh nhẹn.'),

(N'Ốc Thảo', 10.761200, 106.703500, NULL, 
  N'Ốc Thảo là điểm đến quen thuộc của giới sinh viên. Thực đơn ở đây cực kỳ đa dạng với hơn 30 loại ốc khác nhau, chế biến đủ kiểu từ hấp, luộc đến rang muối.'),

(N'Sủi Cảo Vĩnh Khánh', 10.761800, 106.703000, NULL, 
  N'Nếu đã ngán ốc, hãy ghé tiệm sủi cảo này. Viên sủi cảo nhân tôm thịt đầy đặn, nước dùng thanh ngọt, là món ăn lót dạ tuyệt vời trước khi bắt đầu cuộc vui.'),

(N'Phá Lấu Cô Lài', 10.759500, 106.705500, NULL, 
  N'Tiệm phá lấu có tuổi đời hơn 20 năm. Nước dùng phá lấu béo ngậy vị cốt dừa, lá xách và lòng bò được làm sạch sẽ, ăn cùng bánh mì là đúng bài.');
GO

-- Kiểm tra dữ liệu
SELECT * FROM FoodLocations;