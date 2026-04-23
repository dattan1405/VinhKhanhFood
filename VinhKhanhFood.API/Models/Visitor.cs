using System;

namespace VinhKhanhFood.API.Models
{
    public class Visitor
    {
        public int Id { get; set; }
        public string VisitorId { get; set; } = string.Empty;  // Device ID
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime Timestamp { get; set; }
        public DateTime LastSeenUtc { get; set; }
        public string? NearestPoiId { get; set; }  // ID quán gần nhất
        public double DistanceToNearest { get; set; }  // Khoảng cách (m)
    }
}