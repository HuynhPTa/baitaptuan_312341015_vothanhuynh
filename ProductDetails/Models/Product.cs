namespace ProductDetails.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Brand { get; set; } = "";
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string Emoji { get; set; } = "📦";
    public string? ImageUrl { get; set; }
    public string ShortDescription { get; set; } = "";
    public string Description { get; set; } = "";
    public Dictionary<string, string> Specs { get; set; } = new();
}
