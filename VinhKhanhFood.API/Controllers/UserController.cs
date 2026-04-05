using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhKhanhFood.API.Data;
using VinhKhanhFood.API.Models;

namespace VinhKhanhFood.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;
        public UserController(AppDbContext context) { _context = context; }

        // 1. Lấy TOÀN BỘ danh sách User (Để hiện lên bảng ở trang Admin)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await _context.Users.ToListAsync();
        }

        // 2. Lấy thông tin CHI TIẾT của 1 User theo ID
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("Không tìm thấy người dùng này!");
            return Ok(user);
        }

        [HttpPost("login")]
        public async Task<ActionResult<User>> Login([FromBody] User loginInfo)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == loginInfo.Username && u.Password == loginInfo.Password);

            if (user == null) return Unauthorized("Sai tài khoản hoặc mật khẩu");
            return Ok(user);
        }

        [HttpPost("register")]
        public async Task<ActionResult<User>> Register(User user)
        {
            user.Id = 0;
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return Ok(user);
        }

        // 3. Cập nhật thông tin User
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, User user)
        {
            if (id != user.Id) return BadRequest("ID không khớp!");

            _context.Entry(user).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Users.Any(e => e.Id == id)) return NotFound();
                else throw;
            }

            return Ok("Cập nhật thông tin thành công!");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("Không tìm thấy người dùng này để xóa!");

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok($"Đã xóa thành công người dùng có ID: {id}");
        }
    }
}