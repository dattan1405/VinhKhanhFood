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

        // 1. LẤY TẤT CẢ CHO ADMIN (Gọi: api/Food/all)
        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<FoodLocation>>> GetAllLocations()
        {
            return await _context.FoodLocations.ToListAsync();
        }

        // 2. LẤY DANH SÁCH CHO APP (Chỉ lấy Online - Gọi: api/Food)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<FoodLocation>>> GetLocations()
        {
            return await _context.FoodLocations
                                 .Where(x => x.Status == "online")
                                 .ToListAsync();
        }

        // 3. LẤY CHI TIẾT 1 QUÁN (Rất quan trọng để dịch AI - Gọi: api/Food/5)
        [HttpGet("{id}")]
        public async Task<ActionResult<FoodLocation>> GetFoodLocation(int id)
        {
            var foodLocation = await _context.FoodLocations.FindAsync(id);
            if (foodLocation == null) return NotFound();
            return foodLocation;
        }

        // 4. THÊM MỚI (POST)
        [HttpPost]
        public async Task<ActionResult<FoodLocation>> PostFoodLocation(FoodLocation foodLocation)
        {
            _context.FoodLocations.Add(foodLocation);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetFoodLocation), new { id = foodLocation.Id }, foodLocation);
        }

        // 5. CẬP NHẬT (PUT - Dùng để lưu bản dịch)
        [HttpPut("{id}")]
        public async Task<IActionResult> PutFoodLocation(int id, FoodLocation foodLocation)
        {
            if (id != foodLocation.Id) return BadRequest("ID không khớp");

            // 1. Tìm đối tượng thực sự đang nằm trong Database
            var existingPoi = await _context.FoodLocations.FindAsync(id);
            if (existingPoi == null) return NotFound("Không tìm thấy địa điểm");

            // 2. Ép Entity Framework cập nhật tất cả giá trị từ object gửi sang vào object trong DB
            // Cách này sẽ tự động nhận diện các cột mới Name_EN, Name_KO...
            _context.Entry(existingPoi).CurrentValues.SetValues(foodLocation);

            try
            {
                await _context.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine($"✅ API: Đã lưu thành công POI {id} vào Database");
                return NoContent();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ API Error: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        // 6. XÓA (DELETE)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFoodLocation(int id)
        {
            var foodLocation = await _context.FoodLocations.FindAsync(id);
            if (foodLocation == null) return NotFound();

            _context.FoodLocations.Remove(foodLocation);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}