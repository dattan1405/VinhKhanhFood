using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhKhanhFood.API.Data;
using VinhKhanhFood.API.Services;

namespace VinhKhanhFood.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AudioQueueController : ControllerBase
    {
        private readonly AudioQueueService _audioQueueService;
        private readonly AppDbContext _context;

        public AudioQueueController(AudioQueueService audioQueueService, AppDbContext context)
        {
            _audioQueueService = audioQueueService;
            _context = context;
        }

        [HttpPost("play-with-queue")]
        public async Task<IActionResult> PlayAudioWithQueue([FromBody] PlayAudioRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ClientId) || request.PoiId <= 0 || string.IsNullOrWhiteSpace(request.AudioText))
            {
                return BadRequest("Invalid request: ClientId, PoiId, and AudioText are required");
            }

            var result = await _audioQueueService.EnqueueAudioAsync(request.PoiId, request.ClientId, request.AudioText);

            return Ok(result);
        }

        [HttpPost("cancel-audio")]
        public async Task<IActionResult> CancelAudio([FromBody] CancelAudioRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.RequestId) || request.PoiId <= 0)
            {
                return BadRequest("Invalid request: RequestId and PoiId are required");
            }

            await _audioQueueService.CancelAudioAsync(request.RequestId, request.PoiId);

            return Ok(new { success = true, message = "Audio cancelled" });
        }

        [HttpGet("queue-status/{poiId}")]
        public IActionResult GetQueueStatus(int poiId)
        {
            var status = _audioQueueService.GetQueueStatus(poiId);
            return Ok(status);
        }

        public class PlayAudioRequest
        {
            public string ClientId { get; set; } = string.Empty;
            public int PoiId { get; set; }
            public string AudioText { get; set; } = string.Empty;
        }

        public class CancelAudioRequest
        {
            public string RequestId { get; set; } = string.Empty;
            public int PoiId { get; set; }
        }

        // Thêm API lấy thống kê
        [HttpGet("stats/top-listens")]
        public async Task<IActionResult> GetTopListens([FromQuery] int days = 0)
        {
            var query = _context.AudioListenLogs.AsQueryable();
            if (days > 0)
            {
                var startDate = DateTime.UtcNow.Date.AddDays(-days);
                query = query.Where(l => l.ListenedAtUtc >= startDate);
            }

            // Lấy top 5 POI được nghe nhiều nhất, chỉ tính POI còn tồn tại trong CSDL
            var stats = await query
                .GroupBy(l => l.PoiId)
                .Select(g => new {
                    PoiId = g.Key,
                    
                    ListenCount = g.Count() // x2
                })
                .Join(
                    _context.FoodLocations,
                    log => log.PoiId,
                    food => food.Id,
                    (log, food) => new {
                        log.PoiId,
                        log.ListenCount,
                        PoiName = food.Name
                    }
                )
                .OrderByDescending(x => x.ListenCount)
                .Take(5)
                .ToListAsync();

            return Ok(stats);
        }

        [HttpGet("stats/daily-trend")]
        public async Task<IActionResult> GetDailyTrend([FromQuery] int days = 7)
        {
            var logsQuery = _context.AudioListenLogs.AsQueryable();
            
            if (days > 0)
            {
                var startDate = DateTime.UtcNow.Date.AddDays(-days);
                logsQuery = logsQuery.Where(l => l.ListenedAtUtc >= startDate);
            }

            var logs = await logsQuery
                .Select(l => l.ListenedAtUtc)
                .ToListAsync();

            var trend = logs
                .GroupBy(d => d.Date)
                .Select(g => new {
                    Date = g.Key.ToString("dd/MM"),
                    Count = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToList();

            return Ok(trend);
        }
    }
}
