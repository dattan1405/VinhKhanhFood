using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký dịch vụ Database sử dụng Connection String từ file appsettings.json
builder.Services.AddDbContext<VinhKhanhFood.API.Data.AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

 // Tạo giao diện bấm nút Scalar
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.UseStaticFiles(); // cho phép truy cập các fiel tĩnh trong wwwroot

// REDIRECT: Khi mở web sẽ vào thẳng trang giao diện
app.MapGet("/", async (context) =>
{
    context.Response.Redirect("/scalar/v1");
});

app.Run();