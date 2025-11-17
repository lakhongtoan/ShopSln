using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Api.Models;

namespace Shop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ShopDbContext _db;

    public CategoriesController(ShopDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Lấy tất cả danh mục
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Category>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Category>>> GetAll([FromQuery] bool? isActive = null)
    {
        var query = _db.Categories.AsQueryable();

        if (isActive.HasValue)
        {
            query = query.Where(c => c.IsActive == isActive.Value);
        }

        var categories = await query
            .OrderBy(c => c.Name)
            .ToListAsync();

        return Ok(categories);
    }

    /// <summary>
    /// Lấy danh mục theo ID
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Category), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Category>> GetById(int id, [FromQuery] bool includeProducts = false)
    {
        IQueryable<Category> query = _db.Categories;

        if (includeProducts)
        {
            query = query.Include(c => c.Products);
        }

        var category = await query.FirstOrDefaultAsync(c => c.CategoryId == id);

        if (category == null)
            return NotFound(new { message = "Không tìm thấy danh mục" });

        return Ok(category);
    }

    /// <summary>
    /// Tạo danh mục mới
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Category), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Category>> Create([FromBody] Category input)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        input.CreatedAt = DateTime.Now;
        input.UpdatedAt = DateTime.Now;
        input.IsActive = input.IsActive;

        _db.Categories.Add(input);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = input.CategoryId }, input);
    }

    /// <summary>
    /// Cập nhật danh mục
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] Category input)
    {
        if (id != input.CategoryId)
            return BadRequest(new { message = "ID không khớp" });

        var existingCategory = await _db.Categories.FindAsync(id);
        if (existingCategory == null)
            return NotFound(new { message = "Không tìm thấy danh mục" });

        existingCategory.Name = input.Name;
        existingCategory.IsActive = input.IsActive;
        existingCategory.UpdatedAt = DateTime.Now;
        existingCategory.UpdatedBy = input.UpdatedBy;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Xóa danh mục
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _db.Categories
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.CategoryId == id);

        if (category == null)
            return NotFound(new { message = "Không tìm thấy danh mục" });

        // Kiểm tra xem có sản phẩm nào đang sử dụng danh mục này không
        if (category.Products.Any())
        {
            return BadRequest(new { message = "Không thể xóa danh mục vì còn sản phẩm đang sử dụng" });
        }

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}

