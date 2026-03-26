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

        /// <summary>
        /// Lấy danh sách quán ăn kèm theo URL hình ảnh
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<FoodLocation>>> GetLocations()
        {
            var locations = await _context.FoodLocations.ToListAsync();

            // THAY SỐ THÀNH SỐ CỔNG HTTP (trong file launchSettings.json)
            var baseUrl = "http://10.0.2.2:5020";

            foreach (var loc in locations)
            {
                if (!string.IsNullOrEmpty(loc.ImageUrl))
                {
                    // Nó sẽ ghép thành: http://10.0.2.2:5123/images/oc_dao.jpg
                    loc.ImageUrl = $"{baseUrl}/images/{loc.ImageUrl}";
                }
            }

            return Ok(locations);
        }
    }
}