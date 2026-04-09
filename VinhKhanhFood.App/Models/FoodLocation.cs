using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VinhKhanhFood.App.Models
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

        // Thuộc tính thông minh: Tự động bốc tên quán theo ngôn ngữ đã chọn
        public string DisplayName => App.CurrentLanguage switch
        {
            "en" => !string.IsNullOrEmpty(Name_EN) ? Name_EN : Name,
            "ko" => !string.IsNullOrEmpty(Name_KO) ? Name_KO : Name,
            "ja" => !string.IsNullOrEmpty(Name_JA) ? Name_JA : Name,
            "zh" => !string.IsNullOrEmpty(Name_ZH) ? Name_ZH : Name,
            _ => Name // Mặc định là tiếng Việt ("vi")
        };

        // Thuộc tính thông minh: Tự động bốc mô tả theo ngôn ngữ đã chọn
        public string DisplayDescription => App.CurrentLanguage switch
        {
            "en" => !string.IsNullOrEmpty(Description_EN) ? Description_EN : Description,
            "ko" => !string.IsNullOrEmpty(Description_KO) ? Description_KO : Description,
            "ja" => !string.IsNullOrEmpty(Description_JA) ? Description_JA : Description,
            "zh" => !string.IsNullOrEmpty(Description_ZH) ? Description_ZH : Description,
            _ => Description // Mặc định là tiếng Việt ("vi")
        };

        // Trạng thái hoạt động
        public string? Status { get; set; }

        // Thông tin quản lý (Phân quyền Vendor)
        public int? OwnerId { get; set; }
    }
}
