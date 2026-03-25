using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhKhanhFood.API.Data;
using VinhKhanhFood.API.Models;

namespace VinhKhanhFood.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FoodController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FoodController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>S
        /// Lấy danh sách quán ăn kèm theo URL hình ảnh
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<FoodLocation>>> GetLocations()
        {
            // 1. Lấy dữ liệu gốc từ SQL Server
            var locations = await _context.FoodLocations.ToListAsync();

            // 2. Thuật toán xử lý Dynamic URL (Chuẩn Decoupling)
            // Lấy địa chỉ Server hiện tại (ví dụ: https://localhost:7161 hoặc IP máy tính)
            var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";

            foreach (var loc in locations)
            {
                if (!string.IsNullOrEmpty(loc.ImageUrl))
                {
                    // Chuyển "oc_oanh.jpg" thành địa chỉ như localhost
                    loc.ImageUrl = $"{baseUrl}/images/{loc.ImageUrl}";
                }
            }

            return Ok(locations);
        }
    }
}