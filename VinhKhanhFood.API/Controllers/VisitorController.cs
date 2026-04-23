using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhKhanhFood.API.Data;
using VinhKhanhFood.API.Models;

namespace VinhKhanhFood.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VisitorController : ControllerBase
    {
        private readonly AppDbContext _context;
        private const double EARTH_RADIUS_M = 6371000;  // Bán kính Trái Đất (mét)

        public VisitorController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("location")]
        public async Task<IActionResult> UpdateLocation([FromBody] UpdateVisitorLocationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.VisitorId))
                return BadRequest("VisitorId không hợp lệ");

            if (request.Latitude < -90 || request.Latitude > 90 || 
                request.Longitude < -180 || request.Longitude > 180)
                return BadRequest("Tọa độ không hợp lệ");

            try
            {
                // Tìm hoặc tạo visitor record
                Visitor? visitor = await _context.Visitors
                    .FirstOrDefaultAsync(x => x.VisitorId == request.VisitorId);

                if (visitor == null)
                {
                    visitor = new Visitor
                    {
                        VisitorId = request.VisitorId,
                        Latitude = request.Latitude,
                        Longitude = request.Longitude,
                        Timestamp = request.Timestamp ?? DateTime.UtcNow,
                        LastSeenUtc = DateTime.UtcNow
                    };
                    _context.Visitors.Add(visitor);
                }
                else
                {
                    visitor.Latitude = request.Latitude;
                    visitor.Longitude = request.Longitude;
                    visitor.Timestamp = request.Timestamp ?? DateTime.UtcNow;
                    visitor.LastSeenUtc = DateTime.UtcNow;
                }

                // Tìm POI gần nhất
                List<FoodLocation> locations = await _context.FoodLocations.ToListAsync();
                double minDistance = double.MaxValue;
                FoodLocation? nearestPoi = null;

                foreach (var loc in locations)
                {
                    double distance = CalculateDistance(request.Latitude, request.Longitude, loc.Latitude, loc.Longitude);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearestPoi = loc;
                    }
                }

                visitor.NearestPoiId = nearestPoi?.Id.ToString();
                visitor.DistanceToNearest = minDistance;

                await _context.SaveChangesAsync();

                return Ok(new 
                { 
                    success = true, 
                    nearestPoi = nearestPoi?.Name,
                    distance = minDistance
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActiveVisitors([FromQuery] int withinMinutes = 5)
        {
            DateTime cutoff = DateTime.UtcNow.AddMinutes(-withinMinutes);

            List<Visitor> visitors = await _context.Visitors
                .Where(x => x.LastSeenUtc >= cutoff)
                .ToListAsync();

            return Ok(new { count = visitors.Count, visitors });
        }

        [HttpGet("monitor-data")]
        public async Task<IActionResult> GetMonitorData()
        {
            try
            {
                // Lấy danh sách khách hoạt động trong 5 phút qua
                var activeThreshold = DateTime.UtcNow.AddMinutes(-5);
                var visitors = await _context.Visitors
                    .Where(v => v.LastSeenUtc >= activeThreshold)
                    .ToListAsync();

                // Kèm theo thông tin POI gần nhất
                var result = new List<object>();
                foreach (var visitor in visitors)
                {
                    var nearestPoi = await _context.FoodLocations
                        .FirstOrDefaultAsync(p => p.Id.ToString() == visitor.NearestPoiId);

                    result.Add(new
                    {
                        visitor.VisitorId,
                        visitor.Latitude,
                        visitor.Longitude,
                        visitor.DistanceToNearest,
                        NearestPoiName = nearestPoi?.Name ?? "Unknown",
                        NearestPoiLat = nearestPoi?.Latitude,
                        NearestPoiLng = nearestPoi?.Longitude,
                        visitor.LastSeenUtc
                    });
                }

                return Ok(new { count = visitors.Count, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("realtime-monitoring")]
        public async Task<IActionResult> GetRealtimeVisitors()
        {
            try
            {
                var threshold = DateTime.UtcNow.AddMinutes(-5);
                
                // Lấy visitor active + tên quán từ FoodLocations
                var data = await _context.Visitors
                    .Where(v => v.LastSeenUtc >= threshold)
                    .Select(v => new
                    {
                        id = v.VisitorId,
                        latitude = v.Latitude,
                        longitude = v.Longitude,
                        nearestPoiId = v.NearestPoiId,
                        nearestPoiName = _context.FoodLocations
                            .Where(f => f.Id.ToString() == v.NearestPoiId)
                            .Select(f => f.Name)
                            .FirstOrDefault() ?? "Đang di chuyển...",
                        distance = v.DistanceToNearest,
                        lastSeenUtc = v.LastSeenUtc
                    })
                    .ToListAsync();

                return Ok(new { count = data.Count, data });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            // Haversine formula
            double dLat = (lat2 - lat1) * Math.PI / 180.0;
            double dLon = (lon2 - lon1) * Math.PI / 180.0;
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Asin(Math.Sqrt(a));
            return EARTH_RADIUS_M * c;
        }

        public sealed class UpdateVisitorLocationRequest
        {
            public string VisitorId { get; set; } = string.Empty;
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public DateTime? Timestamp { get; set; }
        }
    }
}