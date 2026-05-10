using NBomber.CSharp;
using NBomber.Http.CSharp;
using System.Text;
using System.Text.Json;

// ============================================================
//  CẤU HÌNH CHUNG
// ============================================================
const string ApiBaseUrl = "http://localhost:5020";

// PoiId hợp lệ trong DB (chỉnh cho phù hợp với dữ liệu thực)
int[] poiIds = [1, 2, 3, 4, 5];

// Tọa độ vùng phố ẩm thực Vĩnh Khánh (mô phỏng thực tế)
// Visitor sẽ đứng ngẫu nhiên trong một khu vực nhỏ để kích hoạt
// cả 2 trạng thái: NEAREST_POI và BETWEEN_TWO_POIS
double baseLat = 10.7580;  // Chỉnh lại tọa độ thực của khu vực
double baseLng = 106.6850;

using var httpClient = new HttpClient();
var rng = new Random();

// ============================================================
//  SCENARIO 1: AUDIO QUEUE
//  Mục đích: Kiểm tra khả năng tiếp nhận đồng thời nhiều người
//             tập trung ở cùng 1 quán → chứng minh Queue hoạt động,
//             server KHÔNG bị đứng máy.
//  Kịch bản: 50 "visitor ảo" liên tục bấm "Nghe Audio" tại POI.
//            Một số nhắm vào cùng 1 PoiId (mô phỏng đám đông 1 quán),
//            một số phân tán sang quán khác (mô phỏng thực tế).
// ============================================================
var audioQueueScenario = Scenario.Create("audio_queue_stress", async context =>
{
    // 70% xác suất dồn vào 1 điểm nóng (PoiId = 1) → tạo Queue dài
    // 30% còn lại phân tán sang các POI khác
    int poiId = (rng.NextDouble() < 0.7)
        ? 1
        : poiIds[rng.Next(1, poiIds.Length)];

    // AudioText có độ dài ngắn (~1-2s phát) để hàng đợi tiêu thụ nhanh hơn,
    // tránh tích luỹ RAM quá lớn trong khi test.
    string[] audioSamples =
    [
        "Xin chào!",
        "Mời quý khách ghé thăm!",
        "Đây là quán ăn Vĩnh Khánh.",
    ];

    var body = new
    {
        ClientId = $"LoadTest_Device_{context.ScenarioInfo.ThreadNumber}_{rng.Next(1000, 9999)}",
        PoiId = poiId,
        AudioText = audioSamples[rng.Next(audioSamples.Length)]
    };

    var json = JsonSerializer.Serialize(body);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    var request = Http.CreateRequest("POST", $"{ApiBaseUrl}/api/AudioQueue/play-with-queue")
                      .WithHeader("Accept", "application/json")
                      .WithBody(content);

    return await Http.Send(httpClient, request);
})
.WithLoadSimulations(
    // Tăng dần từ 0 → 50 người trong 10 giây đầu (Ramp Up)
    Simulation.RampingInject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10)),
    // Giữ ổn định 50 người/giây trong 20 giây (Steady State)
    Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(20))
);

// ============================================================
//  SCENARIO 2: VISITOR LOCATION UPDATE (BETWEEN_TWO_POIS)
//  Mục đích: Mô phỏng nhiều visitor di chuyển, cập nhật vị trí
//             liên tục → server phải tính toán khoảng cách Haversine,
//             detect BETWEEN_TWO_POIS và trả về CorridorLabel.
//  Kịch bản: 20 "visitor ảo" cập nhật vị trí mỗi 2 giây (giống App thực).
// ============================================================
var locationUpdateScenario = Scenario.Create("visitor_location_update", async context =>
{
    // Mỗi "visitor ảo" có ID cố định để không tạo mới mãi trong DB
    string visitorId = $"LoadTest_Visitor_{context.ScenarioInfo.ThreadNumber:D3}";

    // Ngẫu nhiên nhỏ offset tọa độ (±0.0003 độ ≈ ±30 mét)
    // để mô phỏng visitor đang di chuyển trong phố
    double lat = baseLat + (rng.NextDouble() - 0.5) * 0.0006;
    double lng = baseLng + (rng.NextDouble() - 0.5) * 0.0006;

    var body = new
    {
        VisitorId = visitorId,
        Latitude = Math.Round(lat, 7),
        Longitude = Math.Round(lng, 7),
        Timestamp = DateTime.UtcNow
    };

    var json = JsonSerializer.Serialize(body);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    var request = Http.CreateRequest("POST", $"{ApiBaseUrl}/api/Visitor/location")
                      .WithHeader("Accept", "application/json")
                      .WithBody(content);

    return await Http.Send(httpClient, request);
})
.WithLoadSimulations(
    // 20 visitor ảo chạy liên tục 30 giây (giống app thực gửi location mỗi 2-3s)
    Simulation.KeepConstant(copies: 20, during: TimeSpan.FromSeconds(30))
);

// ============================================================
//  CHẠY CẢ 2 SCENARIO ĐỒNG THỜI + XUẤT BÁO CÁO
// ============================================================
NBomberRunner
    .RegisterScenarios(audioQueueScenario, locationUpdateScenario)
    .Run();