using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using VinhKhanhFood.Admin.Models;
using System.Text;
using System.Reflection;

namespace VinhKhanhFood.Admin.Controllers
{
    public class FoodLocationController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public FoodLocationController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index()
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync("http://localhost:5020/api/Food/all");

            List<FoodLocation> locations = new List<FoodLocation>();
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                locations = JsonConvert.DeserializeObject<List<FoodLocation>>(json) ?? new List<FoodLocation>();
            }
            return View(locations);
        }

        [HttpPost]
        public async Task<IActionResult> Create(FoodLocation model)
        {
            if (string.IsNullOrEmpty(model.Status))
            {
                model.Status = "pending";
            }

            if (model.ImageFile != null)
            {
                var imageDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
                if (!Directory.Exists(imageDirectory))
                {
                    Directory.CreateDirectory(imageDirectory);
                }

                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(model.ImageFile.FileName)}";
                var filePath = Path.Combine(imageDirectory, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ImageFile.CopyToAsync(stream);
                }

                model.ImageUrl = fileName;
            }

            using (var client = _httpClientFactory.CreateClient())
            {
                var response = await client.PostAsJsonAsync("http://localhost:5020/api/Food", model);

                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction("Index");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError("", $"Lỗi khi lưu dữ liệu: {errorContent}");
                }
            }

            return View("Index", new List<FoodLocation>());
        }

        // ✅ TRANSLATE ACTION - DÙNG LIBRETRANSLATE (MIỄN PHÍ 100%)
        [HttpPost]
        public async Task<IActionResult> Translate(int id)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var getRes = await client.GetAsync($"http://localhost:5020/api/Food/{id}");
                if (!getRes.IsSuccessStatusCode) return BadRequest("Không tìm thấy quán");

                var json = await getRes.Content.ReadAsStringAsync();
                var poi = JsonConvert.DeserializeObject<FoodLocation>(json);

                if (poi == null || string.IsNullOrWhiteSpace(poi.Name) || string.IsNullOrWhiteSpace(poi.Description))
                    return BadRequest("Dữ liệu không hợp lệ");

                // ✅ Dịch sang các ngôn ngữ
                poi.Name_EN = await TranslateText(poi.Name, "en", client);
                poi.Description_EN = await TranslateText(poi.Description, "en", client);
                
                poi.Name_KO = await TranslateText(poi.Name, "ko", client);
                poi.Description_KO = await TranslateText(poi.Description, "ko", client);
                
                poi.Name_JA = await TranslateText(poi.Name, "ja", client);
                poi.Description_JA = await TranslateText(poi.Description, "ja", client);
                
                poi.Name_ZH = await TranslateText(poi.Name, "zh", client);
                poi.Description_ZH = await TranslateText(poi.Description, "zh", client);

                System.Diagnostics.Debug.WriteLine($"✅ Dịch xong: EN={poi.Name_EN}, KO={poi.Name_KO}, JA={poi.Name_JA}, ZH={poi.Name_ZH}");

                // Gửi sang API để lưu
                var putRes = await client.PutAsJsonAsync($"http://localhost:5020/api/Food/{id}", poi);
                if (putRes.IsSuccessStatusCode)
                    return Ok(new { message = "✨ Dịch và lưu thành công tất cả ngôn ngữ!" });

                return BadRequest("Lỗi khi lưu dữ liệu");
            }
            catch (Exception ex)
            {
                return BadRequest($"Lỗi: {ex.Message}");
            }
        }

        // ✅ DÙNG LIBRETRANSLATE - MIỄN PHÍ 100% (Không cần API key)
        private async Task<string?> TranslateText(string text, string targetLang, HttpClient client)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text))
                    return text;

                // ✅ Dùng LibreTranslate API Public (không cần API key)
                string apiUrl = "https://api.mymemory.translated.net/get";
                string url = $"{apiUrl}?q={Uri.EscapeDataString(text)}&langpair=vi|{targetLang}";

                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<dynamic>(responseBody);
                    
                    string translatedText = result?.responseData?.translatedText?.ToString();
                    if (!string.IsNullOrEmpty(translatedText) && translatedText != text)
                    {
                        System.Diagnostics.Debug.WriteLine($"✨ Dịch {text} → {translatedText}");
                        return translatedText;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Translate Error: {ex.Message}");
            }

            return text; // Nếu lỗi, trả về text gốc
        }
    }
}