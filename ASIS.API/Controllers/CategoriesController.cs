using Microsoft.AspNetCore.Mvc;
using ASIS.API.Models;

namespace ASIS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private static List<Category> _categories = new List<Category>
    {
        new Category 
        { 
            Id = 1, 
            Name = "Electronics",
            Description = "Electronic devices and accessories",
            IsActive = true,
            ParentCategoryName = null
        },
        new Category 
        { 
            Id = 2, 
            Name = "Smartphones",
            Description = "Mobile phones and accessories",
            IsActive = true,
            ParentCategoryName = "Electronics"
        }
    };

    private readonly ILogger<CategoriesController> _logger;
    private readonly ProductsController _productsController;

    public CategoriesController(ILogger<CategoriesController> logger, ProductsController productsController)
    {
        _logger = logger;
        _productsController = productsController;
    }

    [HttpGet]
    public ActionResult<IEnumerable<CategoryWithProductCount>> GetAll()
    {
        var categoriesWithCount = _categories.Select(c => new CategoryWithProductCount
        {
            Category = c,
            ProductCount = GetProductCountForCategory(c.Name)
        });
        return Ok(categoriesWithCount);
    }

    [HttpGet("{id}")]
    public ActionResult<CategoryWithProductCount> GetById(int id)
    {
        var category = _categories.FirstOrDefault(c => c.Id == id);
        if (category == null)
        {
            return NotFound();
        }

        var categoryWithCount = new CategoryWithProductCount
        {
            Category = category,
            ProductCount = GetProductCountForCategory(category.Name)
        };
        return Ok(categoryWithCount);
    }

    [HttpGet("active")]
    public ActionResult<IEnumerable<CategoryWithProductCount>> GetActive()
    {
        var activeCategories = _categories
            .Where(c => c.IsActive)
            .Select(c => new CategoryWithProductCount
            {
                Category = c,
                ProductCount = GetProductCountForCategory(c.Name)
            });
        return Ok(activeCategories);
    }

    [HttpGet("parent/{parentName}")]
    public ActionResult<IEnumerable<CategoryWithProductCount>> GetByParentCategory(string parentName)
    {
        var subcategories = _categories
            .Where(c => c.ParentCategoryName != null && 
                   c.ParentCategoryName.Equals(parentName, StringComparison.OrdinalIgnoreCase))
            .Select(c => new CategoryWithProductCount
            {
                Category = c,
                ProductCount = GetProductCountForCategory(c.Name)
            });
        return Ok(subcategories);
    }

    [HttpGet("root")]
    public ActionResult<IEnumerable<CategoryWithProductCount>> GetRootCategories()
    {
        var rootCategories = _categories
            .Where(c => c.ParentCategoryName == null)
            .Select(c => new CategoryWithProductCount
            {
                Category = c,
                ProductCount = GetProductCountForCategory(c.Name)
            });
        return Ok(rootCategories);
    }

    [HttpGet("search")]
    public ActionResult<IEnumerable<CategoryWithProductCount>> Search([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest("Search query cannot be empty");
        }

        var matchingCategories = _categories
            .Where(c => c.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                       c.Description.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Select(c => new CategoryWithProductCount
            {
                Category = c,
                ProductCount = GetProductCountForCategory(c.Name)
            });

        return Ok(matchingCategories);
    }

    [HttpPost]
    public ActionResult<Category> Create(Category category)
    {
        if (string.IsNullOrWhiteSpace(category.Name))
        {
            return BadRequest("Category name cannot be empty");
        }

        if (_categories.Any(c => c.Name.Equals(category.Name, StringComparison.OrdinalIgnoreCase)))
        {
            return BadRequest("Category with this name already exists");
        }

        if (category.ParentCategoryName != null)
        {
            var parentCategory = _categories.FirstOrDefault(c => 
                c.Name.Equals(category.ParentCategoryName, StringComparison.OrdinalIgnoreCase));
            
            if (parentCategory == null)
            {
                return BadRequest("Parent category does not exist");
            }

            if (!parentCategory.IsActive)
            {
                return BadRequest("Cannot create category under inactive parent category");
            }
        }

        category.Id = _categories.Count > 0 ? _categories.Max(c => c.Id) + 1 : 1;
        category.CreatedAt = DateTime.UtcNow;
        category.Name = category.Name.Trim();
        _categories.Add(category);

        _logger.LogInformation("Created new category: {CategoryName}", category.Name);
        return CreatedAtAction(nameof(GetById), new { id = category.Id }, category);
    }

    [HttpPut("{id}")]
    public IActionResult Update(int id, Category category)
    {
        var existingCategory = _categories.FirstOrDefault(c => c.Id == id);
        if (existingCategory == null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(category.Name))
        {
            return BadRequest("Category name cannot be empty");
        }

        if (_categories.Any(c => c.Id != id && 
            c.Name.Equals(category.Name, StringComparison.OrdinalIgnoreCase)))
        {
            return BadRequest("Category with this name already exists");
        }

        if (category.ParentCategoryName != null)
        {
            // Prevent circular reference
            if (category.Name.Equals(category.ParentCategoryName, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Category cannot be its own parent");
            }

            var parentCategory = _categories.FirstOrDefault(c => 
                c.Name.Equals(category.ParentCategoryName, StringComparison.OrdinalIgnoreCase));
            
            if (parentCategory == null)
            {
                return BadRequest("Parent category does not exist");
            }

            // Check if this would create a circular reference
            var currentParent = parentCategory;
            while (currentParent != null)
            {
                if (currentParent.ParentCategoryName?.Equals(category.Name, StringComparison.OrdinalIgnoreCase) == true)
                {
                    return BadRequest("This would create a circular reference in category hierarchy");
                }
                currentParent = _categories.FirstOrDefault(c => 
                    c.Name.Equals(currentParent.ParentCategoryName, StringComparison.OrdinalIgnoreCase));
            }
        }

        existingCategory.Name = category.Name.Trim();
        existingCategory.Description = category.Description?.Trim();
        existingCategory.IsActive = category.IsActive;
        existingCategory.ParentCategoryName = category.ParentCategoryName?.Trim();
        existingCategory.ImageUrl = category.ImageUrl?.Trim();
        existingCategory.LastModified = DateTime.UtcNow;

        _logger.LogInformation("Updated category: {CategoryName}", category.Name);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        var category = _categories.FirstOrDefault(c => c.Id == id);
        if (category == null)
        {
            return NotFound();
        }

        // Check if there are any subcategories
        if (_categories.Any(c => c.ParentCategoryName?.Equals(category.Name, StringComparison.OrdinalIgnoreCase) == true))
        {
            return BadRequest("Cannot delete category that has subcategories");
        }

        // Check if there are any products in this category
        var productCount = GetProductCountForCategory(category.Name);
        if (productCount > 0)
        {
            return BadRequest($"Cannot delete category that has {productCount} products");
        }

        _categories.Remove(category);
        _logger.LogInformation("Deleted category: {CategoryName}", category.Name);
        return NoContent();
    }

    [HttpPatch("{id}/toggle-status")]
    public IActionResult ToggleStatus(int id)
    {
        var category = _categories.FirstOrDefault(c => c.Id == id);
        if (category == null)
        {
            return NotFound();
        }

        // If trying to deactivate, check if there are active subcategories
        if (category.IsActive && 
            _categories.Any(c => c.ParentCategoryName?.Equals(category.Name, StringComparison.OrdinalIgnoreCase) == true 
                             && c.IsActive))
        {
            return BadRequest("Cannot deactivate category that has active subcategories");
        }

        category.IsActive = !category.IsActive;
        category.LastModified = DateTime.UtcNow;

        _logger.LogInformation("Toggled status for category: {CategoryName} to {Status}", 
            category.Name, category.IsActive ? "active" : "inactive");
        return NoContent();
    }

    private int GetProductCountForCategory(string categoryName)
    {
        var result = _productsController.GetByCategory(categoryName);
        if (result.Result is OkObjectResult okResult && 
            okResult.Value is IEnumerable<Product> products)
        {
            return products.Count();
        }
        return 0;
    }
}

public class CategoryWithProductCount
{
    public Category Category { get; set; } = null!;
    public int ProductCount { get; set; }
} 