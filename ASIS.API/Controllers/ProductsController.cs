using Microsoft.AspNetCore.Mvc;
using ASIS.API.Models;

namespace ASIS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private static List<Product> _products = new List<Product>
    {
        new Product 
        { 
            Id = 1, 
            Name = "Sample Product",
            Description = "This is a sample product",
            Price = 29.99m,
            StockQuantity = 100,
            Category = "Electronics"
        }
    };

    private readonly ILogger<ProductsController> _logger;

    public ProductsController(ILogger<ProductsController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public ActionResult<IEnumerable<Product>> GetAll()
    {
        return Ok(_products);
    }

    [HttpGet("{id}")]
    public ActionResult<Product> GetById(int id)
    {
        var product = _products.FirstOrDefault(p => p.Id == id);
        if (product == null)
        {
            return NotFound();
        }
        return Ok(product);
    }

    [HttpPost]
    public ActionResult<Product> Create(Product product)
    {
        product.Id = _products.Count > 0 ? _products.Max(p => p.Id) + 1 : 1;
        product.CreatedAt = DateTime.UtcNow;
        _products.Add(product);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    [HttpPut("{id}")]
    public IActionResult Update(int id, Product product)
    {
        var existingProduct = _products.FirstOrDefault(p => p.Id == id);
        if (existingProduct == null)
        {
            return NotFound();
        }

        existingProduct.Name = product.Name;
        existingProduct.Description = product.Description;
        existingProduct.Price = product.Price;
        existingProduct.StockQuantity = product.StockQuantity;
        existingProduct.Category = product.Category;
        existingProduct.IsAvailable = product.IsAvailable;
        existingProduct.LastModified = DateTime.UtcNow;

        return NoContent();
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        var product = _products.FirstOrDefault(p => p.Id == id);
        if (product == null)
        {
            return NotFound();
        }

        _products.Remove(product);
        return NoContent();
    }

    [HttpGet("category/{category}")]
    public ActionResult<IEnumerable<Product>> GetByCategory(string category)
    {
        var products = _products.Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
        return Ok(products);
    }

    [HttpGet("available")]
    public ActionResult<IEnumerable<Product>> GetAvailable()
    {
        var products = _products.Where(p => p.IsAvailable && p.StockQuantity > 0);
        return Ok(products);
    }
} 