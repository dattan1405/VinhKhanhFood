using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhKhanhFood.API.Data;
using VinhKhanhFood.API.Models;
using System.Globalization;
using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace VinhKhanhFood.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FoodController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        public FoodController(AppDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
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
        //  1. SỬA HÀM CREATE ĐỂ NHẬN ẢNH TỪ ADMIN 
        // ========================================================
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] FoodLocation model)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();

                using MultipartFormDataContent content = new MultipartFormDataContent();
                content.Add(new StringContent(model.Name ?? string.Empty), "Name");
                content.Add(new StringContent(model.Description ?? string.Empty), "Description");
                content.Add(new StringContent(model.Status ?? "pending"), "Status");
                content.Add(new StringContent(model.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)), "Latitude");
                content.Add(new StringContent(model.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)), "Longitude");

                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    StreamContent streamContent = new StreamContent(model.ImageFile.OpenReadStream());
                    streamContent.Headers.ContentType = new MediaTypeHeaderValue(model.ImageFile.ContentType ?? "application/octet-stream");
                    content.Add(streamContent, "ImageFile", Path.GetFileName(model.ImageFile.FileName));
                }

                HttpResponseMessage response = await client.PostAsync("http://192.168.31.26:5020/api/Food", content);

                if (!response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    return Ok(new { success = false, message = $"Lỗi khi tạo dữ liệu trên API: {responseBody}" });
                }

                string createdJson = await response.Content.ReadAsStringAsync();
                FoodLocation? createdPoi = JsonConvert.DeserializeObject<FoodLocation>(createdJson);

                if (createdPoi == null || createdPoi.Id <= 0)
                {
                    return Ok(new { success = false, message = "API không trả về ID hợp lệ." });
                }


                using MultipartFormDataContent updateContent = new MultipartFormDataContent();
                updateContent.Add(new StringContent(createdPoi.Name ?? string.Empty), "Name");
                updateContent.Add(new StringContent(createdPoi.Description ?? string.Empty), "Description");
                updateContent.Add(new StringContent(createdPoi.Status ?? "pending"), "Status");
                updateContent.Add(new StringContent(createdPoi.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)), "Latitude");
                updateContent.Add(new StringContent(createdPoi.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)), "Longitude");

                HttpResponseMessage updateRes = await client.PutAsync(
                    $"http://192.168.31.26:5020/api/Food/{createdPoi.Id}",
                    updateContent);

                if (!updateRes.IsSuccessStatusCode)
                {
                    string updateErr = await updateRes.Content.ReadAsStringAsync();
                    return Ok(new { success = false, message = $"Lỗi update QR: {updateErr}" });
                }

                return Ok(new { success = true, message = "Thêm POI thành công!" });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = "Lỗi kết nối: " + ex.Message });
            }
        }

        // ========================================================
        //  2. SỬA HÀM UPDATE ĐỂ SỬA ẢNH (Từ Edit modal Admin)
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

            // update mô tả đa ngôn ngữ
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

            // Xử lý ảnh
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