var builder = WebApplication.CreateBuilder(args);

// 1. Ðãng k? HttpClient ð? Admin có th? g?i sang API
builder.Services.AddHttpClient("MyAPI", client =>
{
    // (nh?n trên tr?nh duy?t lúc ch?y Scalar)
    client.BaseAddress = new Uri("http://localhost:5020/api/");
});

// 2. Thêm Session ð? lýu tr?ng thái ðãng nh?p (ð? bi?t ai là Admin, ai là Vendor)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Sau 30p không làm g? s? t? ðãng xu?t
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// 3. Kích ho?t Session (ð? TRÝ?C UseAuthorization)
app.UseSession();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}")
    .WithStaticAssets();


app.Run();
