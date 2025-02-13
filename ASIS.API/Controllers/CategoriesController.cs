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

    public CategoriesController(ILogger<CategoriesController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public ActionResult<IEnumerable<Category>> GetAll()
    {
        return Ok(_categories);
    }

    [HttpGet("{id}")]
    public ActionResult<Category> GetById(int id)
    {
        var category = _categories.FirstOrDefault(c => c.Id == id);
        if (category == null)
        {
            return NotFound();
        }
        return Ok(category);
    }

    [HttpGet("active")]
    public ActionResult<IEnumerable<Category>> GetActive()
    {
        var activeCategories = _categories.Where(c => c.IsActive);
        return Ok(activeCategories);
    }

    [HttpGet("parent/{parentName}")]
    public ActionResult<IEnumerable<Category>> GetByParentCategory(string parentName)
    {
        var subcategories = _categories.Where(c => 
            c.ParentCategoryName != null && 
            c.ParentCategoryName.Equals(parentName, StringComparison.OrdinalIgnoreCase));
        return Ok(subcategories);
    }

    [HttpGet("root")]
    public ActionResult<IEnumerable<Category>> GetRootCategories()
    {
        var rootCategories = _categories.Where(c => c.ParentCategoryName == null);
        return Ok(rootCategories);
    }

    [HttpPost]
    public ActionResult<Category> Create(Category category)
    {
        if (_categories.Any(c => c.Name.Equals(category.Name, StringComparison.OrdinalIgnoreCase)))
        {
            return BadRequest("Category with this name already exists");
        }

        if (category.ParentCategoryName != null && 
            !_categories.Any(c => c.Name.Equals(category.ParentCategoryName, StringComparison.OrdinalIgnoreCase)))
        {
            return BadRequest("Parent category does not exist");
        }

        category.Id = _categories.Count > 0 ? _categories.Max(c => c.Id) + 1 : 1;
        category.CreatedAt = DateTime.UtcNow;
        _categories.Add(category);

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

        if (_categories.Any(c => c.Id != id && 
            c.Name.Equals(category.Name, StringComparison.OrdinalIgnoreCase)))
        {
            return BadRequest("Category with this name already exists");
        }

        if (category.ParentCategoryName != null && 
            !_categories.Any(c => c.Name.Equals(category.ParentCategoryName, StringComparison.OrdinalIgnoreCase)))
        {
            return BadRequest("Parent category does not exist");
        }

        existingCategory.Name = category.Name;
        existingCategory.Description = category.Description;
        existingCategory.IsActive = category.IsActive;
        existingCategory.ParentCategoryName = category.ParentCategoryName;
        existingCategory.ImageUrl = category.ImageUrl;
        existingCategory.LastModified = DateTime.UtcNow;

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
        if (_categories.Any(c => c.ParentCategoryName == category.Name))
        {
            return BadRequest("Cannot delete category that has subcategories");
        }

        _categories.Remove(category);
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

        category.IsActive = !category.IsActive;
        category.LastModified = DateTime.UtcNow;

        return NoContent();
    }
} 