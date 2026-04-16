using System.ComponentModel.DataAnnotations.Schema;

namespace VinhKhanhFood.API.Models
{
    [Table("QRManagement")]
    public class QrManagement
    {
        public int Id { get; set; }
        public string Token { get; set; } = string.Empty;
        public string Status { get; set; } = "Available";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UsedAt { get; set; }
        public string? DeviceId { get; set; }
    }
}