using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProductDetails.Models;
using ProductDetails.Services;

namespace ProductDetails.Pages.Admin;

public class EditModel : PageModel
{
    private static readonly string[] AllowedExt = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
    private const long MaxImageBytes = 2 * 1024 * 1024;

    private readonly IProductService _products;
    private readonly IWebHostEnvironment _env;

    public EditModel(IProductService products, IWebHostEnvironment env)
    {
        _products = products;
        _env = env;
    }

    public Product Product { get; private set; } = default!;
    [BindProperty] public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm"), StringLength(150)]
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
        public string? Specs { get; set; }       // mỗi dòng: "Tên: Giá trị"

        public IFormFile? Image { get; set; }    // ảnh mới (nếu chọn)
        public bool RemoveImage { get; set; }    // tick để xóa ảnh hiện tại
    }

    public IActionResult OnGet(int id)
    {
        var product = _products.GetById(id);
        if (product is null) return NotFound();

        Product = product;
        Input = new InputModel
        {
            Name = product.Name,
            Category = product.Category,
            Brand = product.Brand,
            Price = product.Price,
            Stock = product.Stock,
            ShortDescription = product.ShortDescription,
            Description = product.Description,
            Specs = string.Join("\n", product.Specs.Select(kv => $"{kv.Key}: {kv.Value}"))
        };
        return Page();
    }

    // Lưu thay đổi thông tin
    public async Task<IActionResult> OnPostSaveAsync(int id)
    {
        var product = _products.GetById(id);
        if (product is null) return NotFound();
        Product = product;

        if (Input.Image is { Length: > 0 })
        {
            var ext = Path.GetExtension(Input.Image.FileName).ToLowerInvariant();
            if (!AllowedExt.Contains(ext))
                ModelState.AddModelError("Input.Image", "Chỉ chấp nhận ảnh .jpg, .png, .webp, .gif");
            else if (Input.Image.Length > MaxImageBytes)
                ModelState.AddModelError("Input.Image", "Ảnh tối đa 2MB");
        }
        if (!ModelState.IsValid) return Page();

        var oldImage = product.ImageUrl;
        var imageUrl = oldImage;
        if (Input.Image is { Length: > 0 }) imageUrl = await SaveImageAsync(Input.Image);
        else if (Input.RemoveImage) imageUrl = null;
        if (imageUrl != oldImage) DeleteImageFile(oldImage);

        _products.Update(new Product
        {
            Id = id,
            Name = Input.Name.Trim(),
            Category = Input.Category.Trim(),
            Brand = Input.Brand.Trim(),
            Price = Input.Price,
            Stock = Input.Stock,
            ShortDescription = Input.ShortDescription?.Trim() ?? "",
            Description = Input.Description?.Trim() ?? "",
            Specs = ParseSpecs(Input.Specs),
            ImageUrl = imageUrl
        });

        TempData["Msg"] = "Đã lưu thay đổi.";
        return RedirectToPage(new { id });
    }

    // Nhập thêm (số dương) hoặc giảm (số âm) tồn kho
    public IActionResult OnPostAddStock(int id, int amount)
    {
        var newStock = _products.AdjustStock(id, amount);
        if (newStock is null) return NotFound();

        TempData["Msg"] = $"Đã cập nhật tồn kho: {newStock} sản phẩm.";
        return RedirectToPage(new { id });
    }

    public IActionResult OnPostDelete(int id)
    {
        var removed = _products.Delete(id);
        if (removed is null) return NotFound();

        DeleteImageFile(removed.ImageUrl);
        TempData["Msg"] = $"Đã xóa sản phẩm \"{removed.Name}\".";
        return RedirectToPage("/Admin/Products");
    }

    private async Task<string> SaveImageAsync(IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid():N}{ext}";
        var folder = Path.Combine(_env.WebRootPath, "uploads");
        Directory.CreateDirectory(folder);

        await using var stream = System.IO.File.Create(Path.Combine(folder, fileName));
        await file.CopyToAsync(stream);
        return $"/uploads/{fileName}";
    }

    private void DeleteImageFile(string? imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl) || !imageUrl.StartsWith("/uploads/")) return;
        var path = Path.Combine(_env.WebRootPath, "uploads", Path.GetFileName(imageUrl));
        if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
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
            if (key.Length > 0) result[key] = line[(idx + 1)..].Trim();
        }
        return result;
    }
}