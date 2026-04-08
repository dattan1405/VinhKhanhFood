namespace VinhKhanhFood.API.Models
{
    public class FoodLocation
    {
        public int Id { get; set; }

        // Tiếng Việt (Mặc định)
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? AudioUrl { get; set; }

        // Tiếng Anh (Mở rộng cho đa ngôn ngữ)
        public string? Name_EN { get; set; }
        public string? Description_EN { get; set; }

        // Tiếng Hàn
        public string? Name_KO { get; set; }
        public string? Description_KO { get; set; }

        // Tiếng Nhật
        public string? Name_JA { get; set; }
        public string? Description_JA { get; set; }

        // Tiếng Trung
        public string? Name_ZH { get; set; }
        public string? Description_ZH { get; set; }

        // Các thông số kỹ thuật dùng chung
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? ImageUrl { get; set; }

        // ✅ THÊM PROPERTY STATUS
        public string Status { get; set; } = "pending"; // "online" hoặc "pending"

        // Thông tin quản lý (Phân quyền Vendor)
        public int? OwnerId { get; set; }
    }
}