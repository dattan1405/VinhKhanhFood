using NBomber.CSharp;
using NBomber.Http.CSharp;

// Tạo HttpClient dùng chung để tối ưu hiệu năng
using var httpClient = new HttpClient();

// Định nghĩa kịch bản test
var scenario = Scenario.Create("test_vinh_khanh_api", async context =>
{
    // Tạo dữ liệu giả giống như App gửi lên
    var body = new
    {
        ClientId = $"Test_Device_{Guid.NewGuid()}",
        PoiId = 5,
        AudioText = "Phố ẩm thực Vĩnh Khánh xin chào!"
    };

    var json = System.Text.Json.JsonSerializer.Serialize(body);

    // Xây dựng request HTTP POST
    var request = Http.CreateRequest("POST", "http://localhost:5020/api/AudioQueue/play-with-queue")
                      .WithHeader("Content-Type", "application/json")
                      .WithBody(json);

    // Gửi đi và trả về kết quả cho NBomber thống kê
    return await Http.Send(httpClient, request);
})
.WithLoadSimulations(
    // Giả lập 100 User chạy liên tục trong 30 giây
    Simulation.KeepConstant(100, TimeSpan.FromSeconds(30))
);

// Chạy và xuất báo cáo
NBomberRunner
    .RegisterScenarios(scenario)
    .Run();