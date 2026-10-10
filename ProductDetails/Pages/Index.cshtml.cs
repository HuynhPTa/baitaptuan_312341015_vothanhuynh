using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProductDetails.Models;
using ProductDetails.Services;

namespace ProductDetails.Pages;

public class IndexModel : PageModel
{
    private readonly IProductService _service;
    public IndexModel(IProductService service) => _service = service;

    [BindProperty(SupportsGet = true)] public string? Keyword { get; set; }
    [BindProperty(SupportsGet = true)] public string? Category { get; set; }
    [BindProperty(SupportsGet = true)] public decimal? MinPrice { get; set; }
    [BindProperty(SupportsGet = true)] public decimal? MaxPrice { get; set; }

    public List<Product> Products { get; private set; } = new();
    public List<string> Categories { get; private set; } = new();

    public void OnGet() => Load();

    // Gọi bởi JavaScript mỗi khi người dùng gõ: GET /?handler=Search&Keyword=...
    // Chỉ trả về phần lưới sản phẩm (HTML), không tải lại cả trang.
    public IActionResult OnGetSearch()
    {
        Load();
        return Partial("_ProductGrid", Products);
    }

    private void Load()
    {
        Categories = _service.GetCategories();
        Products = _service.Search(Keyword, Category, MinPrice, MaxPrice);
    }
}
