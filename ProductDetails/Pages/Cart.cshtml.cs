using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProductDetails.Models;
using ProductDetails.Services;

namespace ProductDetails.Pages;

public class CartModel : PageModel
{
    private readonly IProductService _products;
    private readonly ICartService _cart;

    public CartModel(IProductService products, ICartService cart)
    {
        _products = products;
        _cart = cart;
    }

    public record CartLine(Product Product, int Quantity)
    {
        public decimal Subtotal => Product.Price * Quantity;
    }

    public List<CartLine> Lines { get; private set; } = new();
    public decimal Total => Lines.Sum(l => l.Subtotal);
    public int ItemCount => Lines.Sum(l => l.Quantity);

    public void OnGet() => Load();

    private bool IsAjax => Request.Headers["X-Requested-With"] == "fetch";

    // Thêm vào giỏ. Gọi bằng fetch (trả JSON) hoặc form thường (chuyển sang trang giỏ).
    public IActionResult OnPostAdd(int id, int qty = 1)
    {
        var product = _products.GetById(id);
        if (product is null) return NotFound();

        string message;
        if (product.Stock <= 0)
            message = "Sản phẩm đã hết hàng";
        else if (_cart.Add(id, qty, product.Stock))
            message = $"Đã thêm \"{product.Name}\" vào giỏ hàng";
        else
            message = "Bạn đã chọn tối đa số lượng còn trong kho";

        if (IsAjax) return new JsonResult(new { count = _cart.Count, message });

        TempData["Msg"] = message;
        return RedirectToPage();
    }

    public IActionResult OnPostUpdate(int id, int qty)
    {
        var product = _products.GetById(id);
        if (product is not null) _cart.Update(id, qty, product.Stock);
        return RedirectToPage();
    }

    public IActionResult OnPostRemove(int id)
    {
        _cart.Remove(id);
        return RedirectToPage();
    }

    public IActionResult OnPostClear()
    {
        _cart.Clear();
        return RedirectToPage();
    }

    // Demo: chưa có thanh toán thật, chỉ xóa giỏ và báo thành công.
    public IActionResult OnPostCheckout()
    {
        _cart.Clear();
        TempData["Msg"] = "Đặt hàng thành công (bản demo, chưa lưu vào database).";
        return RedirectToPage("/Index");
    }

    private void Load()
    {
        Lines = _cart.GetItems()
            .Select(i => new { Item = i, Product = _products.GetById(i.ProductId) })
            .Where(x => x.Product is not null)
            .Select(x => new CartLine(x.Product!, x.Item.Quantity))
            .ToList();
    }
}
