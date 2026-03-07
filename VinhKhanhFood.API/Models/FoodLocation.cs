namespace VinhKhanhFood.API.Models
{
    public class FoodLocation
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // Tên quán ốc
        public double Latitude { get; set; } // Vĩ độ GPS
        public double Longitude { get; set; } // Kinh độ GPS
        public string AudioUrl { get; set; } = string.Empty; // Link file âm thanh thuyết minh
        public string Description { get; set; } = string.Empty; // Mô tả ngắn về quán
    }
}