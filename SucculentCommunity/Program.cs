using Microsoft.EntityFrameworkCore;
using SucculentCommunity.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// 告訴系統我們要使用 Cookie 來記住使用者的登入狀態
builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Members/Login"; // 如果沒登入卻想看私密網頁，會被自動趕來登入頁
        options.AccessDeniedPath = "/Home/Index"; // 如果權限不足 (例如一般會員想看管理員畫面)，會被趕回首頁
    });

// 註冊 SucculentContext，並綁定剛剛寫在 json 裡的連線字串
builder.Services.AddDbContext<SucculentContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SucculentConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); // 這行負責檢查識別證，一定要加在 Authorization 前面！

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
