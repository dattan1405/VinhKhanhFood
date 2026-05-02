using System.Text.Json.Serialization;
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
        private const double EarthRadiusMeters = 6371000;
        private const double BetweenPoiMaxDistanceMeters = 80;
        private const double BetweenPoiGapToleranceMeters = 20;
        private const double MidpointToleranceMeters = 25;

        private readonly AppDbContext _context;

        public VisitorController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("location")]
        public async Task<IActionResult> UpdateLocation([FromBody] UpdateVisitorLocationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.VisitorId))
            {
                return BadRequest("VisitorId không hợp lệ");
            }

            if (request.Latitude < -90 || request.Latitude > 90 ||
                request.Longitude < -180 || request.Longitude > 180)
            {
                return BadRequest("Tọa độ không hợp lệ");
            }

            try
            {
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

                List<FoodLocation> locations = await _context.FoodLocations
                    .AsNoTracking()
                    .ToListAsync();

                PoiMatchResult poiMatch = BuildPoiMatch(locations, request.Latitude, request.Longitude);

                visitor.NearestPoiId = poiMatch.PrimaryPoi?.Id.ToString();
                visitor.DistanceToNearest = poiMatch.PrimaryDistanceMeters;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    mode = poiMatch.IsBetweenPois ? "BETWEEN_TWO_POIS" : "NEAREST_POI",
                    nearestPoiId = poiMatch.PrimaryPoi?.Id,
                    nearestPoi = poiMatch.PrimaryPoi?.Name,
                    nearestDistance = Math.Round(poiMatch.PrimaryDistanceMeters, 2),
                    secondaryPoiId = poiMatch.SecondaryPoi?.Id,
                    secondaryPoi = poiMatch.SecondaryPoi?.Name,
                    secondaryDistance = poiMatch.SecondaryDistanceMeters is null
                        ? (double?)null
                        : Math.Round(poiMatch.SecondaryDistanceMeters.Value, 2),
                    isBetweenPois = poiMatch.IsBetweenPois,
                    corridorLabel = poiMatch.CorridorLabel,
                    midpointDistance = poiMatch.MidpointDistanceMeters is null
                        ? (double?)null
                        : Math.Round(poiMatch.MidpointDistanceMeters.Value, 2)
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

            List<FoodLocation> locations = await _context.FoodLocations
                .AsNoTracking()
                .ToListAsync();

            List<VisitorMonitorItem> data = visitors
                .Select(visitor => BuildMonitorItem(visitor, locations))
                .ToList();

            return Ok(new { count = data.Count, visitors = data });
        }

        [HttpGet("monitor-data")]
        public async Task<IActionResult> GetMonitorData()
        {
            return await BuildMonitorResponseAsync(5);
        }

        [HttpGet("realtime-monitoring")]
        public async Task<IActionResult> GetRealtimeVisitors()
        {
            return await BuildMonitorResponseAsync(5);
        }

        private async Task<IActionResult> BuildMonitorResponseAsync(int withinMinutes)
        {
            try
            {
                DateTime cutoff = DateTime.UtcNow.AddMinutes(-withinMinutes);

                List<Visitor> visitors = await _context.Visitors
                    .Where(v => v.LastSeenUtc >= cutoff)
                    .ToListAsync();

                List<FoodLocation> locations = await _context.FoodLocations
                    .AsNoTracking()
                    .ToListAsync();

                List<VisitorMonitorItem> result = visitors
                    .Select(visitor => BuildMonitorItem(visitor, locations))
                    .ToList();

                return Ok(new { count = result.Count, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        private VisitorMonitorItem BuildMonitorItem(Visitor visitor, IReadOnlyList<FoodLocation> locations)
        {
            PoiMatchResult match = BuildPoiMatch(locations, visitor.Latitude, visitor.Longitude);

            return new VisitorMonitorItem
            {
                Id = visitor.VisitorId,
                Latitude = visitor.Latitude,
                Longitude = visitor.Longitude,
                NearestPoiId = match.PrimaryPoi?.Id.ToString(),
                NearestPoiName = match.PrimaryPoi?.Name ?? "Đang di chuyển...",
                SecondaryPoiId = match.SecondaryPoi?.Id.ToString(),
                SecondaryPoiName = match.SecondaryPoi?.Name,
                IsBetweenPois = match.IsBetweenPois,
                CorridorLabel = match.CorridorLabel,
                Distance = match.PrimaryDistanceMeters,
                SecondaryDistance = match.SecondaryDistanceMeters,
                MidpointDistance = match.MidpointDistanceMeters,
                LastSeenUtc = visitor.LastSeenUtc
            };
        }

        private PoiMatchResult BuildPoiMatch(IReadOnlyList<FoodLocation> locations, double visitorLat, double visitorLng)
        {
            List<PoiDistanceItem> ranked = locations
                .Where(x => x.Latitude != 0 || x.Longitude != 0)
                .Select(x => new PoiDistanceItem
                {
                    Id = x.Id,
                    Name = x.Name,
                    Latitude = x.Latitude,
                    Longitude = x.Longitude,
                    DistanceMeters = CalculateDistance(visitorLat, visitorLng, x.Latitude, x.Longitude)
                })
                .OrderBy(x => x.DistanceMeters)
                .ToList();

            if (ranked.Count == 0)
            {
                return PoiMatchResult.Empty();
            }

            PoiDistanceItem primary = ranked[0];
            PoiDistanceItem? secondary = ranked.Count > 1 ? ranked[1] : null;

            bool isBetweenPois = false;
            double? midpointDistance = null;
            string? corridorLabel = null;

            if (secondary != null)
            {
                double midpointLat = (primary.Latitude + secondary.Latitude) / 2.0;
                double midpointLng = (primary.Longitude + secondary.Longitude) / 2.0;

                midpointDistance = CalculateDistance(visitorLat, visitorLng, midpointLat, midpointLng);

                isBetweenPois =
                    primary.DistanceMeters <= BetweenPoiMaxDistanceMeters &&
                    secondary.DistanceMeters <= BetweenPoiMaxDistanceMeters &&
                    Math.Abs(primary.DistanceMeters - secondary.DistanceMeters) <= BetweenPoiGapToleranceMeters &&
                    midpointDistance <= MidpointToleranceMeters;

                if (isBetweenPois)
                {
                    corridorLabel = $"Giữa {primary.Name} và {secondary.Name}";
                }
            }

            return new PoiMatchResult
            {
                PrimaryPoi = primary,
                SecondaryPoi = secondary,
                PrimaryDistanceMeters = primary.DistanceMeters,
                SecondaryDistanceMeters = secondary?.DistanceMeters,
                MidpointDistanceMeters = midpointDistance,
                IsBetweenPois = isBetweenPois,
                CorridorLabel = corridorLabel
            };
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            double dLat = (lat2 - lat1) * Math.PI / 180.0;
            double dLon = (lon2 - lon1) * Math.PI / 180.0;

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Asin(Math.Sqrt(a));
            return EarthRadiusMeters * c;
        }

        public sealed class UpdateVisitorLocationRequest
        {
            public string VisitorId { get; set; } = string.Empty;
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public DateTime? Timestamp { get; set; }
        }

        private sealed class PoiDistanceItem
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public double DistanceMeters { get; set; }
        }

        private sealed class PoiMatchResult
        {
            public PoiDistanceItem? PrimaryPoi { get; set; }
            public PoiDistanceItem? SecondaryPoi { get; set; }
            public double PrimaryDistanceMeters { get; set; }
            public double? SecondaryDistanceMeters { get; set; }
            public double? MidpointDistanceMeters { get; set; }
            public bool IsBetweenPois { get; set; }
            public string? CorridorLabel { get; set; }

            public static PoiMatchResult Empty()
            {
                return new PoiMatchResult();
            }
        }

        private sealed class VisitorMonitorItem
        {
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonPropertyName("latitude")]
            public double Latitude { get; set; }

            [JsonPropertyName("longitude")]
            public double Longitude { get; set; }

            [JsonPropertyName("nearestPoiId")]
            public string? NearestPoiId { get; set; }

            [JsonPropertyName("nearestPoiName")]
            public string NearestPoiName { get; set; } = "Đang di chuyển...";

            [JsonPropertyName("secondaryPoiId")]
            public string? SecondaryPoiId { get; set; }

            [JsonPropertyName("secondaryPoiName")]
            public string? SecondaryPoiName { get; set; }

            [JsonPropertyName("isBetweenPois")]
            public bool IsBetweenPois { get; set; }

            [JsonPropertyName("corridorLabel")]
            public string? CorridorLabel { get; set; }

            [JsonPropertyName("distance")]
            public double Distance { get; set; }

            [JsonPropertyName("secondaryDistance")]
            public double? SecondaryDistance { get; set; }

            [JsonPropertyName("midpointDistance")]
            public double? MidpointDistance { get; set; }

            [JsonPropertyName("lastSeenUtc")]
            public DateTime LastSeenUtc { get; set; }
        }
    }
}