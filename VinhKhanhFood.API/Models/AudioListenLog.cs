using System.ComponentModel.DataAnnotations;

namespace VinhKhanhFood.API.Models
{
    public class AudioListenLog
    {
        [Key]
        public int Id { get; set; }

        public int PoiId { get; set; }

        // Lưu ClientId của điện thoại khách để biết ai đang nghe
        public string? VisitorId { get; set; }

        // Thời gian nghe (Dùng để vẽ biểu đồ theo ngày/tháng)
        public DateTime ListenedAtUtc { get; set; } = DateTime.UtcNow;
    }
}