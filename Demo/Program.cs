global using Demo.Models;
global using Demo;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddSqlServer<DB>($@"
    Data Source=(LocalDB)\MSSQLLocalDB;
    AttachDbFilename={builder.Environment.ContentRootPath}\DB.mdf;
");
builder.Services.AddScoped<Helper>();

// TODO
builder.Services.AddAuthentication().AddCookie();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();

// Culture = en-MY, ms-MY, zh-CN, ja-JP, ko-KR, etc.
app.UseRequestLocalization("en-MY");

app.MapDefaultControllerRoute();
app.Run();
