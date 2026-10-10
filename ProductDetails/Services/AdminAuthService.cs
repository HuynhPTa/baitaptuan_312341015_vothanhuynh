using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using ProductDetails.Models;

namespace ProductDetails.Services;

public record AdminLoginResult(bool Success, string? Error = null, ClaimsPrincipal? Principal = null);

public interface IAdminAuthService
{
    AdminLoginResult Login(string username, string password);
}

// Tài khoản admin lấy từ appsettings.json (mục "Admin"). Mật khẩu được băm bằng PasswordHasher
// nên không so sánh chuỗi thô. Khi có database, chỉ cần đổi nguồn lấy user + hash.
public class AdminAuthService : IAdminAuthService
{
    private const int MaxFailures = 5;
    private static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(5);

    private class Attempt
    {
        public int Failures;
        public DateTime? LockedUntil;
    }

    private readonly ConcurrentDictionary<string, Attempt> _attempts = new();
    private readonly PasswordHasher<AdminUser> _hasher = new();
    private readonly AdminUser _admin;
    private readonly string _passwordHash;

    public AdminAuthService(IConfiguration config)
    {
        _admin = new AdminUser
        {
            Username = config["Admin:Username"] ?? "admin",
            DisplayName = config["Admin:DisplayName"] ?? "Quản trị viên"
        };
        var password = config["Admin:Password"] ?? "Admin@123";
        _passwordHash = _hasher.HashPassword(_admin, password);
    }

    public AdminLoginResult Login(string username, string password)
    {
        username = (username ?? "").Trim();
        var attempt = _attempts.GetOrAdd(username.ToLowerInvariant(), _ => new Attempt());

        lock (attempt)
        {
            // Đang bị khóa tạm do nhập sai nhiều lần
            if (attempt.LockedUntil is { } until && until > DateTime.UtcNow)
            {
                var minutes = (int)Math.Ceiling((until - DateTime.UtcNow).TotalMinutes);
                return new AdminLoginResult(false, $"Đăng nhập sai quá nhiều lần. Thử lại sau {minutes} phút.");
            }

            // Luôn verify hash (kể cả khi sai username) để thời gian phản hồi giống nhau
            var verify = _hasher.VerifyHashedPassword(_admin, _passwordHash, password ?? "");
            var ok = string.Equals(username, _admin.Username, StringComparison.OrdinalIgnoreCase)
                     && verify != PasswordVerificationResult.Failed;

            if (!ok)
            {
                attempt.Failures++;
                if (attempt.Failures >= MaxFailures)
                {
                    attempt.LockedUntil = DateTime.UtcNow.Add(LockDuration);
                    attempt.Failures = 0;
                }
                return new AdminLoginResult(false, "Sai tên đăng nhập hoặc mật khẩu.");
            }

            attempt.Failures = 0;
            attempt.LockedUntil = null;
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, _admin.Username),
            new("DisplayName", _admin.DisplayName),
            new(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new AdminLoginResult(true, Principal: new ClaimsPrincipal(identity));
    }
}
