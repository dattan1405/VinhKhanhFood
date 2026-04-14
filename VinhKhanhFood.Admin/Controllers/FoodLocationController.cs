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

        // Updated Create action: return JSON { success, message } consistent with Edit
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] FoodLocation model)
        {
            try
            {
                model.Name = model.Name?.Trim();
                model.Description = model.Description?.Trim();
                if (string.IsNullOrEmpty(model.Status))
                    model.Status = "pending";

                // Parse coords from raw form (allow comma or dot)
                string rawLat = Request.Form["Latitude"].ToString() ?? "";
                string rawLng = Request.Form["Longitude"].ToString() ?? "";
                rawLat = rawLat.Replace(',', '.').Trim();
                rawLng = rawLng.Replace(',', '.').Trim();

                // Build multipart for API
                using (var client = _httpClientFactory.CreateClient())
                using (var content = new MultipartFormDataContent())
                {
                    // add text fields
                    content.Add(new StringContent(model.Name ?? ""), "Name");
                    content.Add(new StringContent(model.Description ?? ""), "Description");
                    content.Add(new StringContent(model.Status ?? "pending"), "Status");

                    if (double.TryParse(rawLat, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double parsedLat))
                        content.Add(new StringContent(parsedLat.ToString(System.Globalization.CultureInfo.InvariantCulture)), "Latitude");

                    if (double.TryParse(rawLng, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double parsedLng))
                        content.Add(new StringContent(parsedLng.ToString(System.Globalization.CultureInfo.InvariantCulture)), "Longitude");

                    // add file if exists
                    if (model.ImageFile != null && model.ImageFile.Length > 0)
                    {
                        var streamContent = new StreamContent(model.ImageFile.OpenReadStream());
                        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(model.ImageFile.ContentType ?? "application/octet-stream");
                        content.Add(streamContent, "ImageFile", Path.GetFileName(model.ImageFile.FileName));
                    }

                    // send to API (assumes API accepts multipart POST to /api/Food)
                    var response = await client.PostAsync("http://localhost:5020/api/Food", content);
                    var responseBody = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        // Return JSON consistent with Edit
                        return Json(new { success = true, message = "Thêm mới thành công!" });
                    }
                    else
                    {
                        return Json(new { success = false, message = $"Lỗi từ API: {responseBody}" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Đè qua cụm hàm Translate cũ
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

                // Gửi sang API để lưu bằng MultipartFormDataContent
                using (var content = new MultipartFormDataContent())
                {
                    // Hàm phụ tiện lợi để add trường và check Null
                    void AddString(string value, string name) {
                        if (value != null) content.Add(new StringContent(value), name);
                    }

                    AddString(poi.Name, "Name");
                    AddString(poi.Description, "Description");
                    AddString(poi.Status, "Status");

                    content.Add(new StringContent(poi.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)), "Latitude");
                    content.Add(new StringContent(poi.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)), "Longitude");

                    // Bản dịch mới thêm vô
                    AddString(poi.Name_EN, "Name_EN");
                    AddString(poi.Description_EN, "Description_EN");
                    AddString(poi.Name_KO, "Name_KO");
                    AddString(poi.Description_KO, "Description_KO");
                    AddString(poi.Name_JA, "Name_JA");
                    AddString(poi.Description_JA, "Description_JA");
                    AddString(poi.Name_ZH, "Name_ZH");
                    AddString(poi.Description_ZH, "Description_ZH");

                    var request = new HttpRequestMessage(HttpMethod.Put, $"http://localhost:5020/api/Food/{id}") { Content = content };
                    var putRes = await client.SendAsync(request);
                    var respBody = await putRes.Content.ReadAsStringAsync();

                    if (putRes.IsSuccessStatusCode)
                        return Ok(new { message = "✨ Dịch và lưu thành công tất cả ngôn ngữ!" });

                    return BadRequest($"Lỗi API: {respBody}");
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"Lỗi: {ex.Message}");
            }
        }

        // ✅ DÙNG LIBRETRANSLATE
        private async Task<string?> TranslateText(string text, string targetLang, HttpClient client)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text))
                    return text;

                // ✅ Dùng LibreTranslate API Public
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

        // ✅ EDIT ACTION
        [HttpPost]
        public async Task<IActionResult> Edit(int id, [FromForm] FoodLocation model)
        {
            try
            {
                if (id <= 0)
                    return Json(new { success = false, message = "ID không hợp lệ" });

                // Prepare values (trim)
                string newName = model.Name?.Trim() ?? "";
                string newDesc = model.Description?.Trim() ?? "";

                string rawLat = Request.Form["Latitude"].ToString() ?? "";
                string rawLng = Request.Form["Longitude"].ToString() ?? "";
                rawLat = rawLat.Replace(',', '.').Trim();
                rawLng = rawLng.Replace(',', '.').Trim();

                using (var client = _httpClientFactory.CreateClient())
                using (var content = new MultipartFormDataContent())
                {
                    content.Add(new StringContent(newName), "Name");
                    content.Add(new StringContent(newDesc), "Description");
                    content.Add(new StringContent(model.Status ?? ""), "Status");

                    if (double.TryParse(rawLat, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double parsedLat))
                        content.Add(new StringContent(parsedLat.ToString(System.Globalization.CultureInfo.InvariantCulture)), "Latitude");

                    if (double.TryParse(rawLng, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double parsedLng))
                        content.Add(new StringContent(parsedLng.ToString(System.Globalization.CultureInfo.InvariantCulture)), "Longitude");

                    // translations if present (Sửa lỗi cho Name và Description)
                    if(model.Name_EN != null) content.Add(new StringContent(model.Name_EN), "Name_EN");
                    if(model.Description_EN != null) content.Add(new StringContent(model.Description_EN), "Description_EN");

                    if(model.Name_KO != null) content.Add(new StringContent(model.Name_KO), "Name_KO");
                    if(model.Description_KO != null) content.Add(new StringContent(model.Description_KO), "Description_KO");

                    if(model.Name_JA != null) content.Add(new StringContent(model.Name_JA), "Name_JA");
                    if(model.Description_JA != null) content.Add(new StringContent(model.Description_JA), "Description_JA");

                    if(model.Name_ZH != null) content.Add(new StringContent(model.Name_ZH), "Name_ZH");
                    if(model.Description_ZH != null) content.Add(new StringContent(model.Description_ZH), "Description_ZH");

                    if (model.ImageFile != null && model.ImageFile.Length > 0)
                    {
                        var streamContent = new StreamContent(model.ImageFile.OpenReadStream());
                        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(model.ImageFile.ContentType ?? "application/octet-stream");
                        content.Add(streamContent, "ImageFile", Path.GetFileName(model.ImageFile.FileName));
                    }

                    // Use PUT with HttpRequestMessage to send multipart (API must accept)
                    var request = new HttpRequestMessage(HttpMethod.Put, $"http://localhost:5020/api/Food/{id}") { Content = content };
                    var putRes = await client.SendAsync(request);
                    var respBody = await putRes.Content.ReadAsStringAsync();

                    if (putRes.IsSuccessStatusCode)
                        return Json(new { success = true, message = "✅ Cập nhật thành công!" });

                    return Json(new { success = false, message = $"Lỗi API: {respBody}" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ✅ DELETE ACTION
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var deleteRes = await client.DeleteAsync($"http://localhost:5020/api/Food/{id}");

                if (deleteRes.IsSuccessStatusCode)
                {
                    return Json(new { success = true });
                }

                return Json(new { success = false });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ✅ TOGGLE STATUS ACTION
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id, string currentStatus)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                // ✅ Bước 1: Patch lên thẳng API bằng JSON "online" / "pending"
                string newStatus = string.Equals(currentStatus, "online", StringComparison.OrdinalIgnoreCase) 
                                   ? "pending" : "online";
                
                // Content phải là kiểu JSON chuỗi bao quanh bởi dấu ngoặc kép để Deserialize thành phần tử [FromBody] string bên API
                var content = new StringContent($"\"{newStatus}\"", Encoding.UTF8, "application/json");

                // Gọi Endpoint PATCH /api/Food/{id}/status được expose trên server API
                var patchRes = await client.PatchAsync($"http://localhost:5020/api/Food/{id}/status", content);

                if (patchRes.IsSuccessStatusCode)
                {
                    return Json(new { success = true, message = "Cập nhật thành công!" });
                }

                var errMsg = await patchRes.Content.ReadAsStringAsync();
                return Json(new { success = false, message = $"Lỗi từ API: {errMsg}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}