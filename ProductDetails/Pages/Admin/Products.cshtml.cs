using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProductDetails.Models;
using ProductDetails.Services;

namespace ProductDetails.Pages.Admin;

public class ProductsModel : PageModel
{
    private readonly IProductService _products;
    private readonly IWebHostEnvironment _env;

    public ProductsModel(IProductService products, IWebHostEnvironment env)
    {
        _products = products;
        _env = env;
    }

    public List<Product> Products { get; private set; } = new();

    public void OnGet() => Products = _products.GetAll().OrderByDescending(p => p.Id).ToList();

    public IActionResult OnPostDelete(int id)
    {
        var removed = _products.Delete(id);
        if (removed is null)
        {
            TempData["Msg"] = "Không tìm thấy sản phẩm.";
            return RedirectToPage();
        }

        DeleteImageFile(removed.ImageUrl);
        TempData["Msg"] = $"Đã xóa sản phẩm \"{removed.Name}\".";
        return RedirectToPage();
    }

    // Xóa file ảnh trong wwwroot/uploads (chỉ lấy tên file để tránh path traversal)
    private void DeleteImageFile(string? imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl) || !imageUrl.StartsWith("/uploads/")) return;

        var fileName = Path.GetFileName(imageUrl);
        var path = Path.Combine(_env.WebRootPath, "uploads", fileName);
        if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
    }
}
