using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Api.Models;

namespace Shop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ShopDbContext _db;

    public ProductsController(ShopDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Lấy tất cả sản phẩm
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Product>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Product>>> GetAll()
    {
        var products = await _db.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Where(p => p.IsActive == true)
            .ToListAsync();

        return Ok(products);
    }

    /// <summary>
    /// Lấy sản phẩm theo ID (có thể kèm review)
    /// </summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(Product), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<object>> GetById(
        long id,
        [FromQuery] bool includeReviews = false)
    {
        // Base query
        IQueryable<Product> query = _db.Products
            .Include(p => p.Category)
            .Include(p => p.Brand);

        // Nếu có yêu cầu include reviews
        if (includeReviews)
        {
            // FIX: KHÔNG dùng Include(p => p.Reviews)
            // Reviews phải đúng tên navigation trong model DB-First: Reviews
            query = query.Include(p => p.Reviews);
        }

        var product = await query.FirstOrDefaultAsync(p => p.ProductID == id);

        if (product == null)
            return NotFound(new { message = "Không tìm thấy sản phẩm" });

        if (!includeReviews)
            return Ok(product);

        // Lọc review đang active
        var activeReviews = product.Reviews?
            .Where(r => r.IsActive == true)
            .ToList() ?? new List<ProductReview>();

        var averageRating = activeReviews.Any()
            ? Math.Round(activeReviews.Average(r => r.Rating), 1)
            : 0;

        return Ok(new
        {
            product,
            reviews = activeReviews,
            averageRating,
            totalReviews = activeReviews.Count
        });
    }

    /// <summary>
    /// Tạo sản phẩm mới
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Product), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Product>> Create([FromBody] Product input)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        input.CreatedAt = DateTime.Now;
        input.UpdatedAt = DateTime.Now;

        _db.Products.Add(input);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = input.ProductID }, input);
    }

    /// <summary>
    /// Cập nhật sản phẩm
    /// </summary>
    [HttpPut("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(long id, [FromBody] Product input)
    {
        if (id != input.ProductID)
            return BadRequest(new { message = "ID không khớp" });

        var existingProduct = await _db.Products.FindAsync(id);
        if (existingProduct == null)
            return NotFound(new { message = "Không tìm thấy sản phẩm" });

        // Cập nhật fields
        existingProduct.Name = input.Name;
        existingProduct.Description = input.Description;
        existingProduct.Price = input.Price;
        existingProduct.SalePrice = input.SalePrice;
        existingProduct.Image = input.Image;
        existingProduct.ImageGallery = input.ImageGallery;
        existingProduct.CategoryId = input.CategoryId;
        existingProduct.BrandId = input.BrandId;
        existingProduct.StockQuantity = input.StockQuantity;
        existingProduct.IsActive = input.IsActive ?? true;
        existingProduct.IsFeatured = input.IsFeatured ?? false;
        existingProduct.UpdatedAt = DateTime.Now;
        existingProduct.UpdatedBy = input.UpdatedBy;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Xóa sản phẩm
    /// </summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id)
    {
        var entity = await _db.Products.FirstOrDefaultAsync(p => p.ProductID == id);

        if (entity == null)
            return NotFound(new { message = "Không tìm thấy sản phẩm" });

        _db.Products.Remove(entity);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
