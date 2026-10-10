using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProductDetails.Models;
using ProductDetails.Services;

namespace ProductDetails.Pages.Admin;

public class IndexModel : PageModel
{
    private readonly IProductService _products;
    public IndexModel(IProductService products) => _products = products;

    public int TotalProducts { get; private set; }
    public int TotalStock { get; private set; }
    public int OutOfStock { get; private set; }
    public decimal InventoryValue { get; private set; }
    public List<Product> Latest { get; private set; } = new();

    public void OnGet()
    {
        var all = _products.GetAll();
        TotalProducts = all.Count;
        TotalStock = all.Sum(p => p.Stock);
        OutOfStock = all.Count(p => p.Stock == 0);
        InventoryValue = all.Sum(p => p.Price * p.Stock);
        Latest = all.OrderByDescending(p => p.Id).Take(5).ToList();
    }

    public async Task<IActionResult> OnPostLogoutAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage("/Index");
    }
}
