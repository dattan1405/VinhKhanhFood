using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using VinhKhanhFood.API.Data;
using VinhKhanhFood.API.Models;

namespace VinhKhanhFood.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QrManagementController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<QrManagementController> _logger;

        public QrManagementController(AppDbContext context, ILogger<QrManagementController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public sealed class CreateQrTokenRequest
        {
            public string Token { get; set; } = string.Empty;
        }

        public sealed class VerifyQrTokenRequest
        {
            public string Token { get; set; } = string.Empty;
            public string DeviceId { get; set; } = string.Empty;
        }

        public sealed class DeviceHeartbeatRequest
        {
            public string DeviceId { get; set; } = string.Empty;
        }

        [HttpPost]
        public async Task<IActionResult> CreateToken([FromBody] CreateQrTokenRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Token))
            {
                return BadRequest("Token không hợp lệ");
            }

            bool exists = await _context.QRManagement.AnyAsync(x => x.Token == request.Token);
            if (exists)
            {
                return Conflict("Token đã tồn tại");
            }

            QrManagement entity = new QrManagement
            {
                Token = request.Token,
                Status = "Available",
                CreatedAt = DateTime.UtcNow
            };

            _context.QRManagement.Add(entity);
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }

        [HttpPost("verify")]
        public async Task<IActionResult> Verify([FromBody] VerifyQrTokenRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.DeviceId))
            {
                return BadRequest("Thiếu token hoặc deviceId");
            }

            _logger.LogInformation("Verify request: Token={Token} DeviceId={DeviceId}", request.Token, request.DeviceId);

            QrManagement? row = await _context.QRManagement.FirstOrDefaultAsync(x => x.Token == request.Token);
            if (row == null)
            {
                return NotFound(new { success = false, message = "Token không tồn tại" });
            }

            if (!string.Equals(row.Status, "Available", StringComparison.OrdinalIgnoreCase))
            {
                return Ok(new { success = false, message = "Token đã được sử dụng" });
            }

            row.Status = "Used";
            row.DeviceId = request.DeviceId;
            row.UsedAt = DateTime.UtcNow;
            row.LastSeenUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }

        [HttpPost("heartbeat")]
        public async Task<IActionResult> Heartbeat([FromBody] DeviceHeartbeatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.DeviceId))
            {
                return BadRequest("Thiếu deviceId");
            }

            _logger.LogInformation("Heartbeat request: DeviceId={DeviceId}", request.DeviceId);

            QrManagement? row = await _context.QRManagement
                .Where(x => x.Status == "Used" && x.DeviceId == request.DeviceId)
                .OrderByDescending(x => x.UsedAt ?? x.CreatedAt)
                .FirstOrDefaultAsync();

            if (row == null)
            {
                return NotFound(new { success = false, message = "Thiết bị chưa xác thực QR" });
            }

            row.LastSeenUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }

        [HttpGet("active-devices-count")]
        public async Task<IActionResult> GetActiveDevicesCount([FromQuery] int activeWithinSeconds = 30)
        {
            if (activeWithinSeconds <= 0 || activeWithinSeconds > 7200)
            {
                return BadRequest("activeWithinSeconds phải từ 1 đến 7200 (2 giờ)");
            }

            DateTime cutoff = DateTime.UtcNow.AddSeconds(-activeWithinSeconds);

            int count = await _context.QRManagement
                .Where(x =>
                    x.Status == "Used" &&
                    x.DeviceId != null &&
                    x.LastSeenUtc != null &&
                    x.LastSeenUtc >= cutoff)
                .Select(x => x.DeviceId!)
                .Distinct()
                .CountAsync();

            return Ok(new { count, activeWithinSeconds });
        }

        // Debug endpoint to list used QR entries and their device info (for troubleshooting)
        [HttpGet("debug-used")]
        public async Task<IActionResult> GetUsedDevicesDebug()
        {
            var rows = await _context.QRManagement
                .Where(x => x.Status == "Used")
                .OrderByDescending(x => x.UsedAt ?? x.CreatedAt)
                .Select(x => new { x.Token, x.DeviceId, x.UsedAt, x.LastSeenUtc })
                .ToListAsync();

            return Ok(new { count = rows.Count, items = rows });
        }
    }
}

