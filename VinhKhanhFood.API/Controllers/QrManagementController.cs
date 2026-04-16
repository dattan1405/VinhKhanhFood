using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhKhanhFood.API.Data;
using VinhKhanhFood.API.Models;
using System.ComponentModel.DataAnnotations.Schema; 

namespace VinhKhanhFood.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QrManagementController : ControllerBase
    {
        private readonly AppDbContext _context;

        public QrManagementController(AppDbContext context)
        {
            _context = context;
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

        [HttpPost]
        public async Task<IActionResult> CreateToken([FromBody] CreateQrTokenRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Token))
                return BadRequest("Token không hợp lệ");

            bool exists = await _context.QRManagement.AnyAsync(x => x.Token == request.Token);
            if (exists) return Conflict("Token đã tồn tại");

            var entity = new QrManagement
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
                return BadRequest("Thiếu token hoặc deviceId");

            var row = await _context.QRManagement.FirstOrDefaultAsync(x => x.Token == request.Token);
            if (row == null) return NotFound(new { success = false, message = "Token không tồn tại" });

            if (!string.Equals(row.Status, "Available", StringComparison.OrdinalIgnoreCase))
                return Ok(new { success = false, message = "Token đã được sử dụng" });

            row.Status = "Used";
            row.DeviceId = request.DeviceId;
            row.UsedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }
    }
}

