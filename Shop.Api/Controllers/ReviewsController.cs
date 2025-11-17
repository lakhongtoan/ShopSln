using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Api.Models;

namespace Shop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly ShopDbContext _db;

    public ReviewsController(ShopDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Lấy tất cả đánh giá với filter và pagination
    /// </summary>
    /// <param name="productId">ID sản phẩm (tùy chọn)</param>
    /// <param name="rating">Đánh giá từ 1-5 sao (tùy chọn)</param>
    /// <param name="isActive">Trạng thái hoạt động (tùy chọn)</param>
    /// <param name="page">Số trang (mặc định: 1)</param>
    /// <param name="pageSize">Số lượng mỗi trang (mặc định: 20)</param>
    /// <returns>Danh sách đánh giá với pagination</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProductReview>>> GetAll(
        [FromQuery] int? productId = null,
        [FromQuery] int? rating = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _db.ProductReviews
            .Include(r => r.Product)
            .AsQueryable();

        if (productId.HasValue)
        {
            query = query.Where(r => r.ProductId == productId.Value);
        }

        if (rating.HasValue && rating.Value >= 1 && rating.Value <= 5)
        {
            query = query.Where(r => r.Rating == rating.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(r => r.IsActive == isActive.Value);
        }

        var total = await query.CountAsync();
        var reviews = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var response = new
        {
            total,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling(total / (double)pageSize),
            data = reviews
        };

        return Ok(response);
    }

    /// <summary>
    /// Lấy đánh giá theo ID
    /// </summary>
    /// <param name="id">ID của đánh giá</param>
    /// <returns>Thông tin đánh giá</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProductReview), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductReview>> GetById(int id)
    {
        var review = await _db.ProductReviews
            .Include(r => r.Product)
            .FirstOrDefaultAsync(r => r.ReviewId == id);

        if (review == null)
            return NotFound(new { message = "Không tìm thấy đánh giá" });

        return Ok(review);
    }

    /// <summary>
    /// Lấy đánh giá của một sản phẩm kèm điểm trung bình
    /// </summary>
    /// <param name="productId">ID của sản phẩm</param>
    /// <param name="isActive">Chỉ lấy đánh giá đang hoạt động (tùy chọn)</param>
    /// <returns>Danh sách đánh giá và điểm trung bình</returns>
    [HttpGet("product/{productId:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProductReview>>> GetByProductId(
        long productId,
        [FromQuery] bool? isActive = null)
    {
        var query = _db.ProductReviews
            .Where(r => r.ProductId == productId)
            .OrderByDescending(r => r.CreatedAt)
            .AsQueryable();

        if (isActive.HasValue)
        {
            query = query.Where(r => r.IsActive == isActive.Value);
        }

        var reviews = await query.ToListAsync();

        // Tính điểm trung bình
        var averageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;
        var totalReviews = reviews.Count;

        var response = new
        {
            productId,
            reviews,
            averageRating = Math.Round(averageRating, 1),
            totalReviews
        };

        return Ok(response);
    }

    /// <summary>
    /// Tạo đánh giá mới cho sản phẩm
    /// </summary>
    /// <param name="input">Thông tin đánh giá</param>
    /// <returns>Đánh giá vừa tạo</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ProductReview), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductReview>> Create([FromBody] ProductReview input)
    {
        if (input.Rating < 1 || input.Rating > 5)
        {
            return BadRequest(new { message = "Đánh giá phải từ 1 đến 5 sao" });
        }

        // Kiểm tra sản phẩm tồn tại
        var product = await _db.Products.FindAsync(input.ProductId);
        if (product == null || !(product.IsActive ?? false))
            {
                return NotFound(new { message = "Sản phẩm không tồn tại hoặc đã bị vô hiệu hóa" });
            }


        input.CreatedAt = DateTime.Now;
        input.IsActive = input.IsActive; // Default true

        _db.ProductReviews.Add(input);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = input.ReviewId }, input);
    }

    /// <summary>
    /// Cập nhật đánh giá
    /// </summary>
    /// <param name="id">ID của đánh giá</param>
    /// <param name="input">Thông tin đánh giá cần cập nhật</param>
    /// <returns>NoContent nếu thành công</returns>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] ProductReview input)
    {
        if (id != input.ReviewId)
            return BadRequest(new { message = "ID không khớp" });

        if (input.Rating < 1 || input.Rating > 5)
        {
            return BadRequest(new { message = "Đánh giá phải từ 1 đến 5 sao" });
        }

        var existingReview = await _db.ProductReviews.FindAsync(id);
        if (existingReview == null)
            return NotFound(new { message = "Không tìm thấy đánh giá" });

        existingReview.Rating = input.Rating;
        existingReview.Comment = input.Comment;
        existingReview.IsActive = input.IsActive;

        await _db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Xóa đánh giá
    /// </summary>
    /// <param name="id">ID của đánh giá</param>
    /// <returns>NoContent nếu thành công</returns>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var review = await _db.ProductReviews.FindAsync(id);
        if (review == null)
            return NotFound(new { message = "Không tìm thấy đánh giá" });

        _db.ProductReviews.Remove(review);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Toggle trạng thái đánh giá (ẩn/hiện)
    /// </summary>
    /// <param name="id">ID của đánh giá</param>
    /// <returns>Đánh giá sau khi toggle</returns>
    [HttpPatch("{id:int}/toggle")]
    [ProducesResponseType(typeof(ProductReview), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductReview>> ToggleActive(int id)
    {
        var review = await _db.ProductReviews.FindAsync(id);
        if (review == null)
            return NotFound(new { message = "Không tìm thấy đánh giá" });

        review.IsActive = !review.IsActive;
        await _db.SaveChangesAsync();

        return Ok(review);
    }
}

