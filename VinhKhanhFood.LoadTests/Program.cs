using NBomber.CSharp;
using NBomber.Http.CSharp;
using System.Text;
using System.Text.Json;

// ============================================================
//  CẤU HÌNH CHUNG
// ============================================================
const string ApiBaseUrl = "http://localhost:5020";

using var httpClient = new HttpClient();
var rng = new Random();

// ============================================================
//  SCENARIO 1: AUDIO QUEUE
//  Mục đích: Kiểm tra khả năng tiếp nhận đồng thời nhiều người
//             tập trung ở cùng 1 quán → chứng minh Queue hoạt động,
//             server KHÔNG bị đứng máy.
//  Kịch bản: 100 "visitor ảo" liên tục bấm "Nghe Audio" tại 1 POI (PoiId = 1).
// ============================================================
var audioQueueScenario = Scenario.Create("audio_queue_stress", async context =>
{
    int poiId = 1; // Nhắm 100% vào PoiId = 1

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
    // Bơm 100 request/giây trong vòng 10 giây (tổng 1000 requests)
    Simulation.Inject(rate: 100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10))
);

// ============================================================
//  CHẠY SCENARIO + XUẤT BÁO CÁO
// ============================================================
NBomberRunner
    .RegisterScenarios(audioQueueScenario)
    .Run();