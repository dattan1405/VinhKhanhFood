using Microsoft.AspNetCore.Mvc;

namespace VinhKhanhFood.API.Controllers
{
    public class QrLaunchController : Controller
    {
        [HttpGet("/qr/launch")]
        public IActionResult Launch([FromQuery] string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return Content("Lỗi: Thiếu mã Token.", "text/plain; charset=utf-8");

            // Trả về một trang HTML nhỏ, dùng JS ép trình duyệt mở app MAUI qua custom scheme
            var html = $@"
            <!DOCTYPE html>
            <html lang='vi'>
            <head>
                <meta charset='utf-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>Đang mở ứng dụng...</title>
                <style>
                    body {{ font-family: sans-serif; text-align: center; padding: 40px 20px; background: #f8fafc; }}
                    .btn {{ display: inline-block; padding: 12px 24px; background: #064E3B; color: white; text-decoration: none; border-radius: 8px; font-weight: bold; margin-top: 20px; }}
                    .spinner {{ border: 4px solid #f3f3f3; border-top: 4px solid #064E3B; border-radius: 50%; width: 40px; height: 40px; animation: spin 1s linear infinite; margin: 0 auto 20px; }}
                    @keyframes spin {{ 0% {{ transform: rotate(0deg); }} 100% {{ transform: rotate(360deg); }} }}
                </style>
                <script>
                    window.onload = function() {{
                        // Bắn lệnh mở app
                        window.location.href = 'vinhkhanhfood://launch?token={token}';
                    }};
                </script>
            </head>
            <body>
                <div class='spinner'></div>
                <h2>Đang chuyển hướng đến Ứng dụng...</h2>
                <p>Nếu ứng dụng không tự mở, hãy bấm nút bên dưới:</p>
                <a href='vinhkhanhfood://launch?token={token}' class='btn'>Mở App VinhKhanhFood</a>
            </body>
            </html>";

            return Content(html, "text/html; charset=utf-8");
        }
    }
}