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

        // 1. LẤY DANH SÁCH (GET)
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
                    loc.ImageUrl = $"{baseUrl}/images/{loc.ImageUrl}";
                }
            }
            return Ok(locations);
        }

        // 2. THÊM MỚI (POST)
        [HttpPost]
        public async Task<ActionResult<FoodLocation>> PostFoodLocation(FoodLocation foodLocation)
        {
            _context.FoodLocations.Add(foodLocation);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetLocations), new { id = foodLocation.Id }, foodLocation);
        }

        // 3. CẬP NHẬT (PUT)
        [HttpPut("{id}")]
        public async Task<IActionResult> PutFoodLocation(int id, FoodLocation foodLocation)
        {
            if (id != foodLocation.Id) return BadRequest();
            _context.Entry(foodLocation).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.FoodLocations.Any(e => e.Id == id)) return NotFound();
                else throw;
            }
            return NoContent();
        }

        // 4. XÓA (DELETE)
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