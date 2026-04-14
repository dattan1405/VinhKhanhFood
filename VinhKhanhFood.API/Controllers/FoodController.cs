using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhKhanhFood.API.Data;
using VinhKhanhFood.API.Models;
using System.Globalization;

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

        // ========================================================
        // ✅ 1. SỬA HÀM CREATE ĐỂ NHẬN ẢNH TỪ ADMIN 
        // ========================================================
        [HttpPost]
        public async Task<ActionResult<FoodLocation>> PostFoodLocation([FromForm] FoodLocation foodLocation, IFormFile? ImageFile)
        {
            // Trích xuất tọa độ nếu Admin gửi theo kiểu String trong Form Data (thay cho Formatter)
            string rawLat = Request.Form["Latitude"].ToString() ?? "";
            string rawLng = Request.Form["Longitude"].ToString() ?? "";
            if (double.TryParse(rawLat.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var lat))
                foodLocation.Latitude = lat;
            if (double.TryParse(rawLng.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var lng))
                foodLocation.Longitude = lng;

            // Xử lý upload và lưu ảnh trực tiếp trên thư mục API để App có thể gọi
            if (ImageFile != null && ImageFile.Length > 0)
            {
                var imageDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
                if (!Directory.Exists(imageDirectory))
                    Directory.CreateDirectory(imageDirectory);

                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(ImageFile.FileName)}";
                var filePath = Path.Combine(imageDirectory, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(stream);
                }

                foodLocation.ImageUrl = fileName;
            }

            _context.FoodLocations.Add(foodLocation);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetFoodLocation), new { id = foodLocation.Id }, foodLocation);
        }

        // ========================================================
        // ✅ 2. SỬA HÀM UPDATE ĐỂ SỬA ẢNH (Từ Edit modal Admin)
        // ========================================================
        [HttpPut("{id}")]
        public async Task<IActionResult> PutFoodLocation(int id, [FromForm] FoodLocation foodLocation, IFormFile? ImageFile)
        {
            var existingPoi = await _context.FoodLocations.FindAsync(id);
            if (existingPoi == null) return NotFound("Không tìm thấy địa điểm");

            existingPoi.Name = foodLocation.Name ?? existingPoi.Name;
            existingPoi.Description = foodLocation.Description ?? existingPoi.Description;
            existingPoi.Status = foodLocation.Status ?? existingPoi.Status;

            existingPoi.Name_EN = foodLocation.Name_EN ?? existingPoi.Name_EN;
            existingPoi.Name_KO = foodLocation.Name_KO ?? existingPoi.Name_KO;
            existingPoi.Name_JA = foodLocation.Name_JA ?? existingPoi.Name_JA;
            existingPoi.Name_ZH = foodLocation.Name_ZH ?? existingPoi.Name_ZH;

            // ✅ THÊM: update mô tả đa ngôn ngữ
            existingPoi.Description_EN = foodLocation.Description_EN ?? existingPoi.Description_EN;
            existingPoi.Description_KO = foodLocation.Description_KO ?? existingPoi.Description_KO;
            existingPoi.Description_JA = foodLocation.Description_JA ?? existingPoi.Description_JA;
            existingPoi.Description_ZH = foodLocation.Description_ZH ?? existingPoi.Description_ZH;

            string rawLat = Request.Form["Latitude"].ToString() ?? "";
            string rawLng = Request.Form["Longitude"].ToString() ?? "";
            if (double.TryParse(rawLat.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var lat))
                existingPoi.Latitude = lat;
            if (double.TryParse(rawLng.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var lng))
                existingPoi.Longitude = lng;

            // Xử lý ảnh (Khá giống Create)
            if (ImageFile != null && ImageFile.Length > 0)
            {
                var imageDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
                if (!Directory.Exists(imageDirectory)) Directory.CreateDirectory(imageDirectory);

                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(ImageFile.FileName)}";
                var filePath = Path.Combine(imageDirectory, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(stream);
                }

                existingPoi.ImageUrl = fileName; // Lưu Image Url Mới !
            }
            // else: Giữ nguyên ảnh cũ (existingPoi.ImageUrl)

            try
            {
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
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

        // 7. CẬP NHẬT TRẠNG THÁI (PATCH - Gọi: api/Food/5/status)
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] string newStatus)
        {
            var poi = await _context.FoodLocations.FindAsync(id);
            if (poi == null) return NotFound("Không tìm thấy địa điểm");

            poi.Status = newStatus;
            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Cập nhật trạng thái thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}