using System.Text.Json;
using ProductDetails.Models;

namespace ProductDetails.Services;

public interface ICartService
{
    List<CartItem> GetItems();
    int Count { get; }
    bool Add(int productId, int quantity, int maxStock);
    void Update(int productId, int quantity, int maxStock);
    void Remove(int productId);
    void Clear();
}

// Giỏ hàng lưu trong Session của từng người dùng (mất khi hết phiên / tắt app).
public class CartService : ICartService
{
    private const string Key = "cart";
    private readonly IHttpContextAccessor _accessor;

    public CartService(IHttpContextAccessor accessor) => _accessor = accessor;

    private ISession Session => _accessor.HttpContext!.Session;

    public List<CartItem> GetItems()
    {
        var json = Session.GetString(Key);
        return string.IsNullOrEmpty(json)
            ? new List<CartItem>()
            : JsonSerializer.Deserialize<List<CartItem>>(json) ?? new List<CartItem>();
    }

    private void Save(List<CartItem> items) => Session.SetString(Key, JsonSerializer.Serialize(items));

    public int Count => GetItems().Sum(i => i.Quantity);

    // Trả về false nếu không thể tăng thêm (đã đạt tồn kho)
    public bool Add(int productId, int quantity, int maxStock)
    {
        var items = GetItems();
        var item = items.FirstOrDefault(i => i.ProductId == productId);
        var current = item?.Quantity ?? 0;
        var target = Math.Min(current + Math.Max(1, quantity), maxStock);
        if (target <= current) return false;

        if (item is null) items.Add(new CartItem { ProductId = productId, Quantity = target });
        else item.Quantity = target;

        Save(items);
        return true;
    }

    public void Update(int productId, int quantity, int maxStock)
    {
        var items = GetItems();
        var item = items.FirstOrDefault(i => i.ProductId == productId);
        if (item is null) return;

        if (quantity <= 0) items.Remove(item);
        else item.Quantity = Math.Min(quantity, maxStock);
        Save(items);
    }

    public void Remove(int productId)
    {
        var items = GetItems();
        items.RemoveAll(i => i.ProductId == productId);
        Save(items);
    }

    public void Clear() => Session.Remove(Key);
}
