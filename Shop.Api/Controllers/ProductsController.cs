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
    /// <returns>Danh sách tất cả sản phẩm</returns>
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
    /// Lấy sản phẩm theo ID
    /// </summary>
    /// <param name="id">ID của sản phẩm</param>
    /// <param name="includeReviews">Có bao gồm đánh giá không (mặc định: false)</param>
    /// <returns>Thông tin sản phẩm</returns>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(Product), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Product>> GetById(long id, [FromQuery] bool includeReviews = false)
    {
        var query = _db.Products
            .Include(p => p.Category)
            .Include(p => p.Brand);

        if (includeReviews)
        {
            query = query.Include(p => p.Reviews);
        }

        var product = await query.FirstOrDefaultAsync(p => p.ProductID == id);
        
        if (product == null) 
            return NotFound(new { message = "Không tìm thấy sản phẩm" });

        if (includeReviews)
        {
            // Lọc chỉ lấy reviews đang active
            var activeReviews = product.Reviews?.Where(r => r.IsActive).ToList() ?? new List<ProductReview>();
            var averageRating = activeReviews.Any() ? activeReviews.Average(r => r.Rating) : 0;
            
            var response = new
            {
                product = product,
                reviews = activeReviews,
                averageRating = Math.Round(averageRating, 1),
                totalReviews = activeReviews.Count
            };

            return Ok(response);
        }

        return Ok(product);
    }

    /// <summary>
    /// Tạo sản phẩm mới
    /// </summary>
    /// <param name="input">Thông tin sản phẩm</param>
    /// <returns>Sản phẩm vừa tạo</returns>
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
    /// <param name="id">ID của sản phẩm</param>
    /// <param name="input">Thông tin sản phẩm cần cập nhật</param>
    /// <returns>NoContent nếu thành công</returns>
    [HttpPut("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(long id, [FromBody] Product input)
    {
        if (id != input.ProductID)
            return BadRequest(new { message = "ID không khớp" });

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var existingProduct = await _db.Products.FindAsync(id);
        if (existingProduct == null)
            return NotFound(new { message = "Không tìm thấy sản phẩm" });

        // Cập nhật các trường
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
    /// <param name="id">ID của sản phẩm</param>
    /// <returns>NoContent nếu thành công</returns>
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

