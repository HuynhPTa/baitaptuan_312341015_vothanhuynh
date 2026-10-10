using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProductDetails.Services;

namespace ProductDetails.Pages.Admin;

public class LoginModel : PageModel
{
    private readonly IAdminAuthService _auth;
    public LoginModel(IAdminAuthService auth) => _auth = auth;

    [BindProperty] public InputModel Input { get; set; } = new();
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        [StringLength(50)]
        public string Username { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [StringLength(100)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        public bool RememberMe { get; set; }
    }

    public IActionResult OnGet()
    {
        // Đã đăng nhập rồi thì vào thẳng trang admin
        if (User.Identity?.IsAuthenticated == true && User.IsInRole("Admin"))
            return RedirectToPage("/Admin/Index");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var result = _auth.Login(Input.Username, Input.Password);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Đăng nhập thất bại.");
            Input.Password = "";
            return Page();
        }

        var props = new AuthenticationProperties
        {
            IsPersistent = Input.RememberMe,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(Input.RememberMe ? 24 * 7 : 8)
        };
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, result.Principal!, props);

        // Chỉ chuyển hướng tới URL nội bộ để tránh open redirect
        if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
            return LocalRedirect(ReturnUrl);

        return RedirectToPage("/Admin/Index");
    }
}
