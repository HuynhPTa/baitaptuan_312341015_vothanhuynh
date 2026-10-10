using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProductDetails.Models;
using ProductDetails.Services;

namespace ProductDetails.Pages;

public class DetailsModel : PageModel
{
    private readonly IProductService _service;
    public DetailsModel(IProductService service) => _service = service;

    public Product Product { get; private set; } = default!;

    public IActionResult OnGet(int id)
    {
        var product = _service.GetById(id);
        if (product is null) return NotFound();

        Product = product;
        return Page();
    }
}
