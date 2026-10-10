using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProductDetails.Models;
using ProductDetails.Services;

namespace ProductDetails.Pages;

public class CreateModel : PageModel
{
    private static readonly string[] AllowedExt = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
    private const long MaxImageBytes = 2 * 1024 * 1024; // 2MB

    private readonly IProductService _service;
    private readonly IWebHostEnvironment _env;

    public CreateModel(IProductService service, IWebHostEnvironment env)
    {
        _service = service;
        _env = env;
    }

    [BindProperty] public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm")]
        [StringLength(150)]
        public string Name { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập danh mục")]
        public string Category { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập thương hiệu")]
        public string Brand { get; set; } = "";

        [Range(0, 1_000_000_000, ErrorMessage = "Giá không hợp lệ")]
        public decimal Price { get; set; }

        [Range(0, 100000, ErrorMessage = "Tồn kho không hợp lệ")]
        public int Stock { get; set; }

        [StringLength(200)]
        public string? ShortDescription { get; set; }

        public string? Description { get; set; }

        // Mỗi dòng: "Tên: Giá trị"
        public string? Specs { get; set; }

        public IFormFile? Image { get; set; }
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        // Kiểm tra ảnh
        string? imageUrl = null;
        if (Input.Image is { Length: > 0 })
        {
            var ext = Path.GetExtension(Input.Image.FileName).ToLowerInvariant();
            if (!AllowedExt.Contains(ext))
                ModelState.AddModelError("Input.Image", "Chỉ chấp nhận ảnh .jpg, .png, .webp, .gif");
            else if (Input.Image.Length > MaxImageBytes)
                ModelState.AddModelError("Input.Image", "Ảnh tối đa 2MB");
        }

        if (!ModelState.IsValid) return Page();

        // Lưu ảnh vào wwwroot/uploads với tên ngẫu nhiên
        if (Input.Image is { Length: > 0 })
        {
            var ext = Path.GetExtension(Input.Image.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid():N}{ext}";
            var folder = Path.Combine(_env.WebRootPath, "uploads");
            Directory.CreateDirectory(folder);

            await using var stream = System.IO.File.Create(Path.Combine(folder, fileName));
            await Input.Image.CopyToAsync(stream);
            imageUrl = $"/uploads/{fileName}";
        }

        var product = new Product
        {
            Name = Input.Name.Trim(),
            Category = Input.Category.Trim(),
            Brand = Input.Brand.Trim(),
            Price = Input.Price,
            Stock = Input.Stock,
            ShortDescription = Input.ShortDescription?.Trim() ?? "",
            Description = Input.Description?.Trim() ?? "",
            ImageUrl = imageUrl,
            Specs = ParseSpecs(Input.Specs)
        };

        var saved = _service.Add(product);
        return RedirectToPage("/Details", new { id = saved.Id });
    }

    private static Dictionary<string, string> ParseSpecs(string? text)
    {
        var result = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(text)) return result;

        foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = line.IndexOf(':');
            if (idx <= 0) continue;
            var key = line[..idx].Trim();
            var value = line[(idx + 1)..].Trim();
            if (key.Length > 0) result[key] = value;
        }
        return result;
    }
}
