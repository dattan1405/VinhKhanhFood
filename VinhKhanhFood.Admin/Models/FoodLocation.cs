namespace VinhKhanhFood.Admin.Models
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

        // Các thông số kỹ thuật dùng chung
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? ImageUrl { get; set; }

        // Thông tin quản lý (Phân quyền Vendor)
        public int? OwnerId { get; set; }
    }
}