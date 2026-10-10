using ProductDetails.Models;

namespace ProductDetails.Services;

public interface IProductService
{
    List<Product> GetAll();
    Product? GetById(int id);
    List<string> GetCategories();
    Product Add(Product product);
    Product? Delete(int id);
    List<Product> Search(string? keyword, string? category, decimal? minPrice, decimal? maxPrice);
    bool Update(Product product);
    int? AdjustStock(int id, int delta);
}

// Dữ liệu mẫu trong bộ nhớ. Thay bằng EF Core / DB thật khi cần.
public class ProductService : IProductService
{
    private readonly List<Product> _products = new()
    {
        new Product {
            Id = 1, Name = "Laptop Dell XPS 13", Category = "Laptop", Brand = "Dell",
            Price = 32990000, Stock = 12, Emoji = "💻",
            ShortDescription = "Mỏng nhẹ, màn hình InfinityEdge 13.4 inch.",
            Description = "Dell XPS 13 phù hợp cho dân văn phòng và lập trình viên cần một chiếc máy gọn nhẹ, hiệu năng ổn định và pin bền.",
            Specs = new() { ["CPU"] = "Intel Core i7-1360P", ["RAM"] = "16GB LPDDR5", ["Ổ cứng"] = "512GB SSD", ["Màn hình"] = "13.4\" FHD+", ["Trọng lượng"] = "1.19 kg" }
        },
        new Product {
            Id = 2, Name = "iPhone 15 Pro", Category = "Điện thoại", Brand = "Apple",
            Price = 28990000, Stock = 0, Emoji = "📱",
            ShortDescription = "Khung titan, chip A17 Pro.",
            Description = "iPhone 15 Pro có khung titan nhẹ, cổng USB-C và camera chính 48MP cho ảnh chi tiết.",
            Specs = new() { ["Chip"] = "A17 Pro", ["Bộ nhớ"] = "256GB", ["Màn hình"] = "6.1\" OLED", ["Camera"] = "48MP + 12MP + 12MP" }
        },
        new Product {
            Id = 3, Name = "Tai nghe Sony WH-1000XM5", Category = "Phụ kiện", Brand = "Sony",
            Price = 7490000, Stock = 30, Emoji = "🎧",
            ShortDescription = "Chống ồn chủ động hàng đầu.",
            Description = "Tai nghe không dây chống ồn với âm thanh chi tiết, pin lên đến 30 giờ.",
            Specs = new() { ["Kết nối"] = "Bluetooth 5.2", ["Pin"] = "30 giờ", ["Trọng lượng"] = "250g" }
        }
    };

    public List<Product> GetAll() => _products;
    public Product? GetById(int id) => _products.FirstOrDefault(p => p.Id == id);

    private readonly object _lock = new();

    // Thêm vào danh sách trong bộ nhớ. Dữ liệu mất khi tắt app (chưa dùng database).
    public Product Add(Product product)
    {
        lock (_lock)
        {
            product.Id = _products.Count == 0 ? 1 : _products.Max(p => p.Id) + 1;
            _products.Add(product);
            return product;
        }
    }

    public Product? Delete(int id)
    {
        lock (_lock)
        {
            var product = _products.FirstOrDefault(p => p.Id == id);
            if (product is not null) _products.Remove(product);
            return product;
        }
    }

    public bool Update(Product updated)
    {
        lock (_lock)
        {
            var p = _products.FirstOrDefault(x => x.Id == updated.Id);
            if (p is null) return false;

            p.Name = updated.Name;
            p.Category = updated.Category;
            p.Brand = updated.Brand;
            p.Price = updated.Price;
            p.Stock = updated.Stock;
            p.ShortDescription = updated.ShortDescription;
            p.Description = updated.Description;
            p.Specs = updated.Specs;
            p.ImageUrl = updated.ImageUrl;
            return true;
        }
    }

    // delta dương = nhập thêm, âm = giảm. Tồn kho không xuống dưới 0.
    public int? AdjustStock(int id, int delta)
    {
        lock (_lock)
        {
            var p = _products.FirstOrDefault(x => x.Id == id);
            if (p is null) return null;
            p.Stock = Math.Max(0, p.Stock + delta);
            return p.Stock;
        }
    }
    public List<string> GetCategories() =>
        _products.Select(p => p.Category).Distinct().OrderBy(c => c).ToList();

    // Tìm trong bộ nhớ bằng LINQ (chưa dùng database).
    // Tìm theo tên, hãng, danh mục, mô tả, thông số; không phân biệt hoa thường và dấu.
    public List<Product> Search(string? keyword, string? category, decimal? minPrice, decimal? maxPrice)
    {
        IEnumerable<Product> query = _products;

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(p => p.Category == category);

        if (minPrice.HasValue) query = query.Where(p => p.Price >= minPrice.Value);
        if (maxPrice.HasValue) query = query.Where(p => p.Price <= maxPrice.Value);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var terms = Normalize(keyword).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            query = query.Where(p =>
            {
                var haystack = Normalize($"{p.Name} {p.Brand} {p.Category} {p.ShortDescription} {p.Description} " +
                                         string.Join(' ', p.Specs.Select(kv => kv.Key + " " + kv.Value)));
                return terms.All(haystack.Contains); // mọi từ khóa đều phải khớp
            });
        }

        return query.ToList();
    }

    private static string Normalize(string text)
    {
        var formD = text.ToLowerInvariant().Replace('đ', 'd').Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder();
        foreach (var c in formD)
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        return sb.ToString();
    }
}
