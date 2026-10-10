using Microsoft.AspNetCore.Authentication.Cookies;
using ProductDetails.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages(options =>
{
    // Toàn bộ thư mục /Admin yêu cầu quyền Admin, trừ trang đăng nhập và trang từ chối truy cập
    options.Conventions.AuthorizeFolder("/Admin", "AdminOnly");
    options.Conventions.AllowAnonymousToPage("/Admin/Login");
    options.Conventions.AllowAnonymousToPage("/Admin/AccessDenied");

    // Trang thêm sản phẩm chỉ dành cho admin
    options.Conventions.AuthorizePage("/Create", "AdminOnly");
});

builder.Services.AddSingleton<IProductService, ProductService>();
builder.Services.AddSingleton<IAdminAuthService, AdminAuthService>();

// Đăng nhập bằng Cookie
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Admin/Login";
        options.AccessDeniedPath = "/Admin/AccessDenied";
        options.Cookie.Name = "TechStore.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        // Khi deploy HTTPS thật: options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));

// Giỏ hàng lưu trong Session (chưa dùng database)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o =>
{
    o.IdleTimeout = TimeSpan.FromHours(2);
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICartService, CartService>();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();   // phải đứng trước UseAuthorization
app.UseAuthorization();
app.MapRazorPages();
app.Run();
