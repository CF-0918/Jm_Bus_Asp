global using Demo.Models;
global using Demo;
using Demo.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddSqlServer<DB>($@"
    Data Source=(LocalDB)\MSSQLLocalDB;
    AttachDbFilename={builder.Environment.ContentRootPath}\DB.mdf;
");
builder.Services.AddScoped<Helper>();
//cf add new session service for use newsletter
builder.Services.AddSession(); // Add session services

builder.Services.AddAuthentication().AddCookie();
builder.Services.AddHttpContextAccessor();

// Register services in DI container
builder.Services.AddScoped<RegisterService>();
builder.Services.AddHostedService<BackgroundUpdaterService>();


var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseSession();

// Register the BlockedUserMiddleware
app.UseMiddleware<BlockedUserMiddleware>();

// Culture = en-MY, ms-MY, zh-CN, ja-JP, ko-KR, etc.
app.UseRequestLocalization("en-MY");

app.MapDefaultControllerRoute();
app.Run();
