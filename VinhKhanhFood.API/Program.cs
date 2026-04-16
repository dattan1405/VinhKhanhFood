using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Thêm chính sách CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader());
});

// Đăng ký dịch vụ Database sử dụng Connection String từ file appsettings.json
builder.Services.AddDbContext<VinhKhanhFood.API.Data.AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
builder.Services.AddRazorPages(); // ✅ THÊM DÒNG NÀY
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Kích hoạt CORS
app.UseCors("AllowAll");

app.UseStaticFiles(); // cho phép truy cập các fiel tĩnh trong wwwroot
//app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapRazorPages(); // ✅ THÊM DÒNG NÀY


// REDIRECT: Khi mở web sẽ vào thẳng trang giao diện
app.MapGet("/", async (context) =>
{
    context.Response.Redirect("/scalar/v1");
});

app.Run();